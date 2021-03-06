﻿using System;

namespace SmartHub.UWP.Plugins.Wemos.Core.Messages
{
    public class WemosMessageEventArgs : EventArgs
    {
        public WemosMessage Message
        {
            get; set;
        }

        public WemosMessageEventArgs()
        {
        }
        public WemosMessageEventArgs(WemosMessage message)
        {
            Message = message;
        }
    }
}
