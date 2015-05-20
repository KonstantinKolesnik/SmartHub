﻿using SmartHub.Plugins.Weather.Api;
using SmartHub.Plugins.WebUI.Attributes;
using SmartHub.Plugins.WebUI.Tiles;
using System;
using System.Linq;

namespace SmartHub.Plugins.Weather
{
    [Tile]
    public class WeatherTile : TileBase
    {
        public override void PopulateModel(TileWeb webTile, dynamic options)
        {
            webTile.title = "Weather";
            webTile.url = "webapp/weather/forecast";

            string strCityId = options.cityId;
            if (string.IsNullOrWhiteSpace(strCityId))
            {
                webTile.content = "Missing cityId parameter";
                return;
            }
            Guid cityId;
            if (!Guid.TryParse(strCityId, out cityId))
            {
                webTile.content = "CityId parameter must contain GUID value";
                return;
            }

            var data = Context.GetPlugin<WeatherPlugin>().GetWeatherData(DateTime.Now);
            WeatherLocatioinModel location = data.FirstOrDefault(l => l.LocationId == cityId);

            if (location == null)
            {
                webTile.content = string.Format("Location with id = {0} is not found", cityId);
                return;
            }

            webTile.title = location.LocationName;

            // текущая погода
            if (location.Now == null)
            {
                webTile.content = "Current weather is undefined";
                return;
            }

            string formattedNow = WeatherUtils.FormatTemperature(location.Now.Temperature);
            webTile.content = string.Format("сейчас: {0}°C", formattedNow);
            webTile.className = "btn-info th-tile-icon th-tile-icon-wa " + WeatherUtils.GetIconClass(location.Now.Code);

            // погода на завтра
            var tomorrow = location.Forecast.FirstOrDefault();
            if (tomorrow != null)
            {
                string formattedTomorrow = WeatherUtils.FormatTemperatureRange(tomorrow.MinTemperature, tomorrow.MaxTemperature);
                webTile.content += string.Format("\nзавтра: {0}°C", formattedTomorrow);
            }
        }
    }
}
