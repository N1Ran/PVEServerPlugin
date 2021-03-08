﻿using System;

namespace PVEServerPlugin.ProcessHandlers
{
    public abstract class Base
    {

        protected DateTime m_lastUpdate;

        /// <summary>
        ///     Initializer
        /// </summary>
        public Base()
        {
            m_lastUpdate = DateTime.Now;

            //Log.Info(string.Format("Added process handler: Raised every {0}ms", GetUpdateResolution()));
        }

        public DateTime LastUpdate
        {
            get => m_lastUpdate;
            set => m_lastUpdate = value;
        }

        /// <summary>
        ///     Returns whether this handler is ready to be run
        /// </summary>
        /// <returns></returns>
        public bool CanProcess()
        {
            return DateTime.Now - m_lastUpdate > TimeSpan.FromMilliseconds(GetUpdateResolution());
        }

        /// <summary>
        ///     Gets the processing resolution of this handler in milliseconds.
        /// </summary>
        /// <returns>resolution in ms</returns>
        public virtual int GetUpdateResolution()
        {
            return 1000;
        }

        /// <summary>
        ///     Called when CanProcess() returns true.  This gets overriden and is the main handling function
        /// </summary>
        public virtual void Handle()
        {
            m_lastUpdate = DateTime.Now;
        }

    }
}