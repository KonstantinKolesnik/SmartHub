﻿using SQLite.Net.Attributes;
using System;

namespace SmartHub.UWP.Plugins.Things.Models
{
    public class Line
    {
        [PrimaryKey, NotNull]
        public string ID
        {
            get; set;
        }
        public string DeviceID
        {
            get; set;
        }
        public string LineID
        {
            get; set;
        }
        [NotNull]
        public LineType Type
        {
            get; set;
        }

        public string Route
        {
            get; set;
        }
        public string Name
        {
            get; set;
        }

        // value = value * factor + offset
        [NotNull, Default(true, 1.0)]
        public float Factor
        {
            get; set;
        }
        [NotNull, Default()]
        public float Offset
        {
            get; set;
        }

        public float RequirableValue
        {
            get; set;
        }
        public DateTime TimeStamp
        {
            get; set;
        }
        public float Value
        {
            get; set;
        }

        [NotNull, Default()]
        public bool IsWritable
        {
            get; set;
        }

        public string MonitorConfiguration
        {
            get; set;
        }
        //[NotNull]
        //public float Factor
        //{
        //    get; set;
        //} = 1;
        //[NotNull, Default()]
        //public float Offset
        //{
        //    get; set;
        //} = 0;
        //public string Units
        //{
        //    get; set;
        //}
        //[NotNull, Default]
        //public float Min
        //{
        //    get; set;
        //}
        //[NotNull, Default]
        //public float Max
        //{
        //    get; set;
        //}
        //[NotNull, Default(true, 10)]
        //public int ValuesCount
        //{
        //    get; set;
        //}

        //[NotNull, Default]
        //public int Precision
        //{
        //    get; set;
        //}



        public string ControlConfiguration
        {
            get; set;
        }

        #region Public methods
        public string GetUnits()
        {
            switch (Type)
            {
                case LineType.Switch: return "";
                case LineType.Temperature: return "°C";
                case LineType.Humidity: return "%";
                case LineType.Pressure: return "Pa"; // mmHg
                case LineType.Weight: return "kg";
                case LineType.Voltage: return "V";
                case LineType.Current: return "A";
                case LineType.Power: return "Wt";
                case LineType.Rain: return "";
                case LineType.UV: return "";
                case LineType.Distance: return "m";
                case LineType.LightLevel: return "lux";
                case LineType.IR: return "";
                case LineType.AirQuality: return "";
                case LineType.Vibration: return "";
                case LineType.Ph: return "";
                case LineType.ORP: return "";

                default: return "";
            }
        }
        public bool IsSynced()
        {
            return Value == RequirableValue;
        }
        #endregion
    }
}
