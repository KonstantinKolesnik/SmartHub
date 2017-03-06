﻿using SmartHub.UWP.Plugins.Wemos.Controllers.Models;
using SmartHub.UWP.Plugins.Wemos.Core;
using SmartHub.UWP.Plugins.Wemos.Core.Models;
using System;
using System.Collections.Generic;

namespace SmartHub.UWP.Plugins.Wemos.Controllers
{
    public struct Period
    {
        public DateTime From;
        public DateTime To;
        public bool IsEnabled;
    }

    public class WemosScheduledSwitchController : WemosControllerBase
    {
        public class ControllerConfiguration
        {
            public int LineSwitchID
            {
                get; set;
            } = -1;
            public List<Period> ActivePeriods
            {
                get;
            } = new List<Period>();

            public static ControllerConfiguration Default
            {
                get { return new ControllerConfiguration(); }
            }
        }

        #region Fields
        private ControllerConfiguration configuration = null;
        #endregion

        #region Properties
        public WemosLine LineSwitch
        {
            get { return host.GetLine(configuration.LineSwitchID); }
        }
        #endregion

        #region Constructor
        public WemosScheduledSwitchController(WemosController model)
            : base (model)
        {
            if (string.IsNullOrEmpty(model.Configuration))
            {
                configuration = ControllerConfiguration.Default;
                model.SerializeConfiguration(configuration);
            }
            else
                configuration = model.DeserializeConfiguration<ControllerConfiguration>();
        }
        #endregion

        #region Abstract Overrides
        public override bool IsMyMessage(WemosMessage message)
        {
            return WemosPlugin.IsMessageFromLine(message, LineSwitch);
        }
        public async override void RequestLinesValues()
        {
            await host.RequestLineValue(LineSwitch);
        }
        protected async override void Process()
        {
            DateTime now = DateTime.Now;
            bool isActiveNew = false;
            foreach (var range in configuration.ActivePeriods)
                isActiveNew |= (range.IsEnabled && IsInRange(now, range));

            await host.SetLineValue(LineSwitch, isActiveNew ? 1 : 0);
        }
        #endregion

        #region Private methods
        private static bool IsInRange(DateTime dt, Period range)
        {
            TimeSpan start = range.From.ToLocalTime().TimeOfDay;
            TimeSpan end = range.To.ToLocalTime().TimeOfDay;
            TimeSpan now = dt.TimeOfDay;

            if (start < end)
                return start <= now && now <= end;
            else
                return !(end < now && now < start);
        }
        #endregion
    }
}