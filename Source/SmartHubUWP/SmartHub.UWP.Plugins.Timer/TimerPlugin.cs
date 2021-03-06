﻿using SmartHub.UWP.Core.Plugins;
using SmartHub.UWP.Plugins.Timer.Attributes;
using System;
using System.Collections.Generic;
using System.Composition;
using Windows.System.Threading;

namespace SmartHub.UWP.Plugins.Timer
{
    [Plugin]
    public class TimerPlugin : PluginBase
    {
        #region Fields
        private const int TIMER_INTERVAL = 10; // seconds
        //private System.Threading.Timer timer;
        private ThreadPoolTimer timer = null;
        private bool isTimerActive = false;
        private readonly List<PeriodicalAction> periodicalActions = new List<PeriodicalAction>();
        #endregion

        #region Imports
        [ImportMany]
        public IEnumerable<Lazy<Action<DateTime>, RunPeriodicallyAttribute>> PeriodicalHandlers
        {
            get; set;
        }
        #endregion

        #region Plugin overrides
        public override void InitPlugin()
        {
            //timer = new System.Threading.Timer(timerCallback, null, 0, (int)TimeSpan.FromSeconds(TIMER_INTERVAL).TotalMilliseconds);
            timer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(timerCallback), TimeSpan.FromSeconds(TIMER_INTERVAL));

            // register periodical actions:
            var now = DateTime.Now;
            //Logger.Info("Register periodical actions at {0:yyyy.MM.dd, HH:mm:ss}", now);
            foreach (var handler in PeriodicalHandlers)
                periodicalActions.Add(new PeriodicalAction(handler.Value, handler.Metadata.Interval, now/*, Logger*/));
        }
        public override void StartPlugin()
        {
            isTimerActive = true;
        }
        public override void StopPlugin()
        {
            isTimerActive = false;
        }
        #endregion

        #region Event handlers
        private /*async*/ void timerCallback(object state)
        {
            if (isTimerActive)
            {
                //isTimerActive = false;

                var now = DateTime.Now;

                // periodical actions
                foreach (var handler in periodicalActions)
                    handler.TryToExecute(now);

                // do some work not connected with UI:
                //await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { });

                //isTimerActive = true;
            }
        }
        #endregion
    }
}

/*
        [Export(typeof(Action<DateTime>)), RunPeriodically(Interval = 10)]
        public Action<DateTime> aaa => ((dt) =>
        {
            int a = 0;
        });
*/
