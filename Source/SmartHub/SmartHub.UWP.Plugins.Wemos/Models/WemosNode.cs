﻿using SmartHub.UWP.Plugins.Wemos.Core;
using SQLite.Net.Attributes;
using System;

namespace SmartHub.UWP.Plugins.Wemos.Models
{
    public class WemosNode
    {
        [PrimaryKey, NotNull]
        public int NodeID { get; set; }
        public string Name { get; set; }
        [NotNull]
        public WemosLineType Type { get; set; }
        public float ProtocolVersion { get; set; }
        public string IPAddress { get; set; }

        public string FirmwareName { get; set; }
        public float FirmwareVersion { get; set; }

        public DateTime LastTimeStamp { get; set; }
        public int LastBatteryValue { get; set; }
    }
}
