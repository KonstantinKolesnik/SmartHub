﻿using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NLog;
using SmartHub.Core.Plugins;
using SmartHub.Core.Plugins.Utils;
using SmartHub.Plugins.HttpListener.Api;
using SmartHub.Plugins.HttpListener.Attributes;
using SmartHub.Plugins.Timer.Attributes;
using SmartHub.Plugins.Weather.Api;
using SmartHub.Plugins.Weather.Data;
using SmartHub.Plugins.WebUI.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SmartHub.Plugins.Weather
{
    // forecast
    [AppSection("Погода", SectionType.Common, "/webapp/weather/forecast.js", "SmartHub.Plugins.Weather.Resources.js.forecast.forecast.js")]
    [JavaScriptResource("/webapp/weather/forecast-model.js", "SmartHub.Plugins.Weather.Resources.js.forecast.forecast-model.js")]
    [JavaScriptResource("/webapp/weather/forecast-view.js", "SmartHub.Plugins.Weather.Resources.js.forecast.forecast-view.js")]

    [HttpResource("/webapp/weather/forecast.tpl", "SmartHub.Plugins.Weather.Resources.js.forecast.forecast.tpl")]
    [HttpResource("/webapp/weather/forecast-item.tpl", "SmartHub.Plugins.Weather.Resources.js.forecast.forecast-item.tpl")]
    [HttpResource("/webapp/weather/forecast-item-value.tpl", "SmartHub.Plugins.Weather.Resources.js.forecast.forecast-item-value.tpl")]
    [HttpResource("/webapp/weather/forecast-item-value-now.tpl", "SmartHub.Plugins.Weather.Resources.js.forecast.forecast-item-value-now.tpl")]

    // settings
    [AppSection("Погодные локации", SectionType.System, "/webapp/weather/locations.js", "SmartHub.Plugins.Weather.Resources.js.settings.locations.js")]
    [JavaScriptResource("/webapp/weather/locations-model.js", "SmartHub.Plugins.Weather.Resources.js.settings.locations-model.js")]
    [JavaScriptResource("/webapp/weather/locations-view.js", "SmartHub.Plugins.Weather.Resources.js.settings.locations-view.js")]

    [HttpResource("/webapp/weather/locations-layout.tpl", "SmartHub.Plugins.Weather.Resources.js.settings.locations-layout.tpl")]
    [HttpResource("/webapp/weather/locations-list.tpl", "SmartHub.Plugins.Weather.Resources.js.settings.locations-list.tpl")]
    [HttpResource("/webapp/weather/locations-list-item.tpl", "SmartHub.Plugins.Weather.Resources.js.settings.locations-list-item.tpl")]
    [HttpResource("/webapp/weather/locations-form.tpl", "SmartHub.Plugins.Weather.Resources.js.settings.locations-form.tpl")]

    // css
    [CssResource("/webapp/weather/css/weather-icons.min.css", "SmartHub.Plugins.Weather.Resources.css.weather-icons.min.css", AutoLoad = true)]
    [CssResource("/webapp/weather/css/weather-forecast.css", "SmartHub.Plugins.Weather.Resources.css.weather-forecast.css", AutoLoad = true)]

    // fonts
    [HttpResource("/webapp/weather/fonts/weathericons-regular-webfont.eot", "SmartHub.Plugins.Weather.Resources.fonts.weathericons-regular-webfont.eot", "application/vnd.ms-fontobject")]
    [HttpResource("/webapp/weather/fonts/weathericons-regular-webfont.svg", "SmartHub.Plugins.Weather.Resources.fonts.weathericons-regular-webfont.svg", "image/svg+xml")]
    [HttpResource("/webapp/weather/fonts/weathericons-regular-webfont.ttf", "SmartHub.Plugins.Weather.Resources.fonts.weathericons-regular-webfont.ttf", "application/x-font-truetype")]
    [HttpResource("/webapp/weather/fonts/weathericons-regular-webfont.woff", "SmartHub.Plugins.Weather.Resources.fonts.weathericons-regular-webfont.woff", "application/font-woff")]

    [Plugin]
    public class WeatherPlugin : PluginBase
    {
        #region Fields
        private const int UPDATE_PERIOD = 20;
        private readonly object lockObject = new object();

        private const string SERVICE_URL_FORMAT = "http://api.openweathermap.org/data/2.5/forecast?q={0}&units=metric&APPID=9948774b7ea6673661f1bd773a48d23c";
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        #endregion

        #region Plugin overrides
        public override void InitDbModel(ModelMapper mapper)
        {
            mapper.Class<Location>(cfg => cfg.Table("Weather_Location"));
            mapper.Class<WeatherData>(cfg => cfg.Table("Weather_Data"));
        }
        #endregion

        #region Public methods
        public static DateTime DateTimeFromUnixTimestampSeconds(long seconds)
        {
            return unixEpoch.AddSeconds(seconds);
        }
        public WeatherLocatioinModel[] GetWeatherData(DateTime now)
        {
            var list = new List<WeatherLocatioinModel>();

            using (var session = Context.OpenSession())
            {
                var locations = session.Query<Location>().ToArray();
                var data = session.Query<WeatherData>().Where(d => d.Date >= now.Date).ToArray();

                foreach (var location in locations)
                {
                    var locationData = data.Where(d => d.Location.Id == location.Id).ToArray();
                    var model = ModelBuilder.LoadLocationWeatherData(now, location, locationData);
                    list.Add(model);
                }
            }

            return list.ToArray();
        }

        public void ReloadWeatherData()
        {
            lock (lockObject)
            {
                using (var session = Context.OpenSession())
                {
                    var locations = session.Query<Location>().ToList();
                    foreach (var location in locations)
                        UpdateOneLocation(location, session);
                }
            }
        }
        public void ReloadWeatherData(Guid locationId)
        {
            lock (lockObject)
            {
                using (var session = Context.OpenSession())
                {
                    var location = session.Get<Location>(locationId);
                    if (location != null)
                        UpdateOneLocation(location, session);
                }
            }
        }
        #endregion

        #region Event handlers
        [RunPeriodically(UPDATE_PERIOD)]
        public void AutomaticUpdate(DateTime now)
        {
            Logger.Info("Automatic update all locations");

            ReloadWeatherData();

            Logger.Info("Update completed");
        }
        #endregion

        #region Private methods
        private void UpdateOneLocation(Location location, ISession session)
        {
            try
            {
                var forecast = LoadForecast(location.Query, Logger);
                UpdateWeatherData(session, location, forecast, Logger);
            }
            catch (Exception ex)
            {
                string msg = string.Format("loading error (location {0})", location);
                Logger.ErrorException(msg, ex);
            }
        }

        private static dynamic LoadForecast(string query, Logger logger)
        {
            string encodedCityName = WebUtility.UrlEncode(query);
            string url = string.Format(SERVICE_URL_FORMAT, encodedCityName);

            logger.Info("send request: {0}", url);

            using (var client = new WebClient())
            {
                var json = client.DownloadString(url);
                logger.Info("complete");

                return Extensions.FromJson(json);
            }
        }
        private static void UpdateWeatherData(ISession session, Location location, dynamic forecast, Logger logger)
        {
            var list = forecast.list as IEnumerable;

            int count = 0;

            if (list != null)
            {
                foreach (dynamic item in list)
                {
                    long seconds = item.dt;

                    var dataItem = GetWeatherDataItem(seconds, session, location);
                    UpdateWeatherDataItem(dataItem, item);
                    session.Flush();

                    count++;
                }
            }

            logger.Info("updated {0} items", count);
        }
        private static WeatherData GetWeatherDataItem(long seconds, ISession session, Location location)
        {
            var date = DateTimeFromUnixTimestampSeconds(seconds);

            var dataItem = session
                .Query<WeatherData>()
                .FirstOrDefault(obj => obj.Date == date && obj.Location.Id == location.Id);

            if (dataItem == null)
            {
                dataItem = new WeatherData { Id = Guid.NewGuid(), Date = date, Location = location };
                session.Save(dataItem);
            }

            return dataItem;
        }
        private static void UpdateWeatherDataItem(WeatherData dataItem, dynamic item)
        {
            dataItem.Temperature = item.main.temp;
            dataItem.Cloudiness = item.clouds.all;
            dataItem.Humidity = item.main.humidity;
            dataItem.Pressure = item.main.pressure;
            dataItem.WindDirection = item.wind.deg;
            dataItem.WindSpeed = item.wind.speed;

            if (item.weather != null && item.weather.First != null)
            {
                dynamic w = item.weather.First;

                dataItem.WeatherDescription = w.description;
                dataItem.WeatherCode = w.icon;
            }
            else
                dataItem.WeatherCode = dataItem.WeatherDescription = null;
        }
        #endregion

        #region Web API
        [HttpCommand("/api/weather/locations/add")]
        public object AddLocation(HttpRequestParams request)
        {
            var displayName = request.GetRequiredString("displayName");
            var query = request.GetRequiredString("query");

            using (var session = Context.OpenSession())
            {
                var location = new Location
                {
                    Id = Guid.NewGuid(),
                    DisplayName = displayName,
                    Query = query
                };

                session.Save(location);
                session.Flush();
            }

            return null;
        }

        [HttpCommand("/api/weather/locations/delete")]
        public object DeleteLocation(HttpRequestParams request)
        {
            var locationId = request.GetRequiredGuid("locationId");
            using (var session = Context.OpenSession())
            {
                var location = session.Get<Location>(locationId);

                if (location != null)
                {
                    session.Delete(location);
                    session.Flush();
                }
            }

            return null;
        }

        [HttpCommand("/api/weather/update")]
        public object UpdateAllWeather(HttpRequestParams request)
        {
            ReloadWeatherData();

            return null;
        }

        [HttpCommand("/api/weather/locations/update")]
        public object UpdateLocationWeather(HttpRequestParams request)
        {
            var locationId = request.GetRequiredGuid("locationId");
            ReloadWeatherData(locationId);

            return null;
        }

        [HttpCommand("/api/weather/all")]
        public object GetWeather(HttpRequestParams request)
        {
            WeatherLocatioinModel[] weatherData = GetWeatherData(DateTime.Now);

            return weatherData.Select(BuildLocationModel).ToArray();
        }

        [HttpCommand("/api/weather/locations/list")]
        public object GetLocations(HttpRequestParams request)
        {
            using (var session = Context.OpenSession())
            {
                var locations = session.Query<Location>()
                    .OrderBy(l => l.DisplayName)
                    .ToList();

                var model = locations
                    .Select(l => new
                    {
                        id = l.Id,
                        displayName = l.DisplayName,
                        query = l.Query
                    })
                    .ToList();

                return model;
            }
        }

        #region Helpers
        private object BuildModel(WeatherDataModel data)
        {
            return data == null
                ? null
                : new
                {
                    when = data.DateTime.ToShortTimeString(),
                    t = WeatherUtils.FormatTemperature(data.Temperature),
                    p = data.Pressure,
                    h = data.Humidity,
                    icon = WeatherUtils.GetIconClass(data.Code),
                    description = data.Description
                };
        }
        private object BuildModel(DailyWeatherDataModel data)
        {
            return data == null
                ? null
                : new
                {
                    when = data.DateTime.ToString("M"),
                    t = WeatherUtils.FormatTemperatureRange(data.MinTemperature, data.MaxTemperature),
                    p = string.Format("{0} .. {1}", data.MinPressure, data.MaxPressure),
                    h = string.Format("{0} .. {1}", data.MinHumidity, data.MaxHumidity),
                    icon = WeatherUtils.GetIconClass(data.Code),
                    description = data.Description
                };
        }
        private object BuildLocationModel(WeatherLocatioinModel data)
        {
            return new
            {
                id = data.LocationId,
                name = data.LocationName,
                now = BuildModel(data.Now),
                day = data.Today.Select(BuildModel).ToArray(),
                forecast = data.Forecast.Select(BuildModel).ToArray()
            };
        }
        #endregion
        #endregion
    }
}
