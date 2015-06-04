﻿using SmartHub.Core.Plugins.Utils;
using SmartHub.Plugins.AquaController.Core;
using System;

namespace SmartHub.Plugins.AquaController.Data
{
    public class Controller
    {
        public virtual Guid Id { get; set; }
        public virtual string Name { get; set; }
        public virtual ControllerType Type { get; set; }
        public virtual bool IsVisible { get; set; }
        public virtual string Configuration { get; set; }

        public virtual dynamic DeserializeConfiguration(Type type)
        {
            var json = string.IsNullOrWhiteSpace(Configuration) ? "{}" : Configuration;
            return Extensions.FromJson(type, json);
        }
        public virtual dynamic DeserializeConfiguration()
        {
            var json = string.IsNullOrWhiteSpace(Configuration) ? "{}" : Configuration;
            return Extensions.FromJson(json);
        }
        public virtual void SerializeConfiguration(object value)
        {
            Configuration = value.ToJson();
        }
    }
}