﻿using System.ComponentModel;

namespace SmartHub.Plugins.Controllers.Core
{
    public enum ControllerType
    {
        [Description("Обогреватель")]
        Heater,
        [Description("Выключатель по расписанию")]
        ScheduledSwitch,
        [Description("Уровень воды")]
        WaterLevel,



        [Description("Освещение")]
        Light,
        [Description("PH")]
        PH,
        [Description("CO2")]
        CO2,
        [Description("ORP")]
        ORP,
        [Description("Кормление")]
        Feeder,
        [Description("Другой")]
        Custom
    }
}
