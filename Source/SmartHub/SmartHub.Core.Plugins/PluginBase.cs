﻿using NHibernate.Mapping.ByCode;
using NLog;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace SmartHub.Core.Plugins
{
    public abstract class PluginBase
    {
        #region Fields
        [Import(typeof(IServiceContext))]
        private IServiceContext context;
        private readonly Logger logger;
        #endregion

        #region Properties
        protected IServiceContext Context
        {
            get { return context; }
        }
        protected Logger Logger
        {
            get { return logger; }
        }
        #endregion

        #region Constructor
        protected PluginBase()
        {
            logger = LogManager.GetLogger(GetType().FullName);
        }
        #endregion

        #region Plugin virtuals
        public virtual void InitDbModel(ModelMapper mapper)
        {
        }
        public virtual void InitPlugin()
        {
        }
        public virtual void StartPlugin()
        {
        }
        public virtual void StopPlugin()
        {
        }
        #endregion

        #region Public methods
        public void Run<T>(T[] actions, Action<T> task)
        {
            if (actions != null && actions.Any())
                foreach (var action in actions)
                {
                    try
                    {
                        task(action);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message, ex);
                    }
                }
        }
        #endregion
    }
}