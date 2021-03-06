﻿using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using SmartHub.Core.Plugins;
using SmartHub.Core.Plugins.Utils;
using SmartHub.Plugins.Controllers;
using SmartHub.Plugins.HttpListener.Api;
using SmartHub.Plugins.HttpListener.Attributes;
using SmartHub.Plugins.Monitors;
using SmartHub.Plugins.Scripts;
using SmartHub.Plugins.SignalR;
using SmartHub.Plugins.WebUI.Attributes;
using SmartHub.Plugins.Zones.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartHub.Plugins.Zones
{
    [AppSection("Зоны", SectionType.Common, "/webapp/zones/dashboard.js", "SmartHub.Plugins.Zones.Resources.js.dashboard.js", TileTypeFullName = "SmartHub.Plugins.Zones.ZonesTile")]
    [JavaScriptResource("/webapp/zones/dashboard-view.js", "SmartHub.Plugins.Zones.Resources.js.dashboard-view.js")]
    [JavaScriptResource("/webapp/zones/dashboard-model.js", "SmartHub.Plugins.Zones.Resources.js.dashboard-model.js")]
    [HttpResource("/webapp/zones/dashboard.html", "SmartHub.Plugins.Zones.Resources.js.dashboard.html")]

    [AppSection("Зоны", SectionType.System, "/webapp/zones/settings.js", "SmartHub.Plugins.Zones.Resources.js.settings.js")]
    [JavaScriptResource("/webapp/zones/settings-view.js", "SmartHub.Plugins.Zones.Resources.js.settings-view.js")]
    [JavaScriptResource("/webapp/zones/settings-model.js", "SmartHub.Plugins.Zones.Resources.js.settings-model.js")]
    [HttpResource("/webapp/zones/settings.html", "SmartHub.Plugins.Zones.Resources.js.settings.html")]

    [CssResource("/webapp/zones/css/style.css", "SmartHub.Plugins.Zones.Resources.css.style.css", AutoLoad = true)]

    // zone editor
    [JavaScriptResource("/webapp/zones/zone-editor.js", "SmartHub.Plugins.Zones.Resources.js.zone-editor.js")]
    [JavaScriptResource("/webapp/zones/zone-editor-view.js", "SmartHub.Plugins.Zones.Resources.js.zone-editor-view.js")]
    [JavaScriptResource("/webapp/zones/zone-editor-model.js", "SmartHub.Plugins.Zones.Resources.js.zone-editor-model.js")]
    [HttpResource("/webapp/zones/zone-editor.html", "SmartHub.Plugins.Zones.Resources.js.zone-editor.html")]

    // zone viewer
    [JavaScriptResource("/webapp/zones/zone.js", "SmartHub.Plugins.Zones.Resources.js.zone.js")]
    [JavaScriptResource("/webapp/zones/zone-view.js", "SmartHub.Plugins.Zones.Resources.js.zone-view.js")]
    [JavaScriptResource("/webapp/zones/zone-model.js", "SmartHub.Plugins.Zones.Resources.js.zone-model.js")]
    [HttpResource("/webapp/zones/zone.html", "SmartHub.Plugins.Zones.Resources.js.zone.html")]

    [Plugin]
    public class ZonesPlugin : PluginBase
    {
        #region SignalR events
        private void NotifyForSignalR(object msg)
        {
            Context.GetPlugin<SignalRPlugin>().Broadcast(msg);
        }
        #endregion

        #region Plugin overrides
        public override void InitDbModel(ModelMapper mapper)
        {
            mapper.Class<Zone>(cfg => cfg.Table("Zones_Zones"));
        }
        #endregion

        #region API
        public List<Zone> Get()
        {
            using (var session = Context.OpenSession())
                return session.Query<Zone>()
                    .OrderBy(zone => zone.Name)
                    .ToList();
        }
        public Zone Get(Guid id)
        {
            using (var session = Context.OpenSession())
                return session.Get<Zone>(id);
        }
        public void Delete(Guid id)
        {
            using (var session = Context.OpenSession())
            {
                var zone = session.Load<Zone>(id);
                session.Delete(zone);
                session.Flush();
            }
        }
        #endregion

        #region Web API
        public string BuildTileContent()
        {
            //SensorValue lastSVHeaterTemperature = mySensors.GetLastSensorValue(heaterController.SensorTemperature);
            //SensorValue lastSVHeaterSwitch = mySensors.GetLastSensorValue(heaterController.SensorSwitch);
            //SensorValue lastSVTemperatureOuter = mySensors.GetLastSensorValue(SensorTemperatureOuter);
            //SensorValue lastSVHumidityOuter = mySensors.GetLastSensorValue(SensorHumidityOuter);
            //SensorValue lastSVAtmospherePressure = mySensors.GetLastSensorValue(SensorAtmospherePressure);
            //SensorValue lastSVForecast = mySensors.GetLastSensorValue(SensorForecast);

            StringBuilder sb = new StringBuilder();
            //sb.Append("<div>Температура воды: " + (lastSVHeaterTemperature != null ? lastSVHeaterTemperature.Value + " °C" : "&lt;нет данных&gt;") + "</div>");
            //sb.Append("<div>Обогреватель: " + (lastSVHeaterSwitch != null ? (lastSVHeaterSwitch.Value == 1 ? "Вкл." : "Выкл.") : "&lt;нет данных&gt;") + "</div>");
            //sb.Append("<div>Температура снаружи: " + (lastSVTemperatureOuter != null ? lastSVTemperatureOuter.Value + " °C" : "&lt;нет данных&gt;") + "</div>");
            //sb.Append("<div>Влажность снаружи: " + (lastSVHumidityOuter != null ? lastSVHumidityOuter.Value + " %" : "&lt;нет данных&gt;") + "</div>");
            //sb.Append("<div>Давление: " + (lastSVAtmospherePressure != null ? (int)(lastSVAtmospherePressure.Value / 133.3f) + " mmHg" : "&lt;нет данных&gt;") + "</div>");

            //string[] weather = { "Ясно", "Солнечно", "Облачно", "К дождю", "Дождь", "-" };
            //sb.Append("<div>Прогноз: " + (lastSVForecast != null ? weather[(int)lastSVForecast.Value] : "&lt;нет данных&gt;") + "</div>");
            return sb.ToString();
        }
        public string BuildSignalRReceiveHandler()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("if (data.MsgId == 'ZonesTileContent') { ");
            sb.Append("model.tileModel.set({ 'content': data.Data }); ");
            sb.Append("}");
            return sb.ToString();
        }

        public object BuildZoneWebModel(Zone zone)
        {
            if (zone == null)
                return null;

            return new
            {
                Id = zone.Id,
                Name = zone.Name,
                MonitorsIds = zone.MonitorsList,
                ControllersIds = zone.ControllersList,
                ScriptsIds = zone.ScriptsList
            };
        }
        public object BuildZoneRichWebModel(Zone zone)
        {
            if (zone == null)
                return null;

            var pluginMonitors = Context.GetPlugin<MonitorsPlugin>();
            var ids = Extensions.FromJson(typeof(List<Guid>), zone.MonitorsList) as List<Guid>;
            var monitors = ids.Select(id => pluginMonitors.BuildMonitorRichWebModel(pluginMonitors.Get(id))).ToArray();

            var pluginControllers = Context.GetPlugin<ControllersPlugin>();
            ids = Extensions.FromJson(typeof(List<Guid>), zone.ControllersList) as List<Guid>;
            var controllers = ids.Select(id => pluginControllers.BuildControllerWebModel(pluginControllers.Get(id))).ToArray();

            var pluginScripts = Context.GetPlugin<ScriptsPlugin>();
            ids = Extensions.FromJson(typeof(List<Guid>), zone.ScriptsList) as List<Guid>;
            var scripts = ids.Select(id => pluginScripts.BuildScriptRichWebModel(pluginScripts.GetScript(id))).ToArray();

            return new
            {
                Id = zone.Id,
                Name = zone.Name,
                MonitorsList = monitors,
                ControllersList = controllers,
                ScriptsList = scripts
            };
        }

        [HttpCommand("/api/zones/list")]
        private object apiGetZones(HttpRequestParams request)
        {
            return Get()
                .Select(BuildZoneWebModel)
                .Where(x => x != null)
                .ToArray();
        }
        [HttpCommand("/api/zones/get")]
        private object apiGetZone(HttpRequestParams request)
        {
            var id = request.GetRequiredGuid("id");
            return BuildZoneWebModel(Get(id));
        }
        [HttpCommand("/api/zones/get/rich")]
        private object apiGetZoneRich(HttpRequestParams request)
        {
            var id = request.GetRequiredGuid("id");
            return BuildZoneRichWebModel(Get(id));
        }

        [HttpCommand("/api/zones/add")]
        private object apiAddZone(HttpRequestParams request)
        {
            var name = request.GetRequiredString("name");

            using (var session = Context.OpenSession())
            {
                Zone zone = new Zone()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    MonitorsList = "[]",
                    ControllersList = "[]",
                    ScriptsList = "[]",

                    GraphsList = "[]"
                };

                session.Save(zone);
                session.Flush();
            }

            //NotifyForSignalR(new { MsgId = "SensorNameChanged", Data = new { Id = id, Name = name } });

            return null;
        }
        [HttpCommand("/api/zones/setname")]
        private object apiSetZoneName(HttpRequestParams request)
        {
            var id = request.GetRequiredGuid("id");
            var name = request.GetRequiredString("name");

            using (var session = Context.OpenSession())
            {
                var zone = session.Load<Zone>(id);
                zone.Name = name;
                session.Flush();
            }

            //NotifyForSignalR(new
            //{
            //    MsgId = "SensorNameChanged",
            //    Data = new
            //    {
            //        Id = id,
            //        Name = name
            //    }
            //});

            return null;
        }
        [HttpCommand("/api/zones/setconfiguration")]
        private object apiSetZoneConfiguration(HttpRequestParams request)
        {
            var id = request.GetRequiredGuid("id");
            var monitorsIds = request.GetRequiredString("monitorsIds");
            var controllersIds = request.GetRequiredString("controllersIds");
            var scriptsIds = request.GetRequiredString("scriptsIds");
            //var graphsIds = request.GetRequiredString("graphsIds");

            using (var session = Context.OpenSession())
            {
                var zone = session.Load<Zone>(id);
                zone.MonitorsList = monitorsIds;
                zone.ControllersList = controllersIds;
                zone.ScriptsList = scriptsIds;
                //zone.GraphsList = graphsIds;

                session.Flush();
            }

            //NotifyForSignalR(new { MsgId = "ControllerIsVisibleChanged", Data = BuildControllerWebModel(ctrl) });

            return null;
        }
        [HttpCommand("/api/zones/delete")]
        private object apiDeleteZone(HttpRequestParams request)
        {
            var id = request.GetRequiredGuid("Id");
            Delete(id);
            //NotifyForSignalR(new { MsgId = "SensorDeleted", Data = new { Id = id } });
            return null;
        }
        #endregion
    }
}
