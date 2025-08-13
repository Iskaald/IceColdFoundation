using System;
using IceCold.Interface;
using UnityEngine;

namespace IceCold.Logger
{
    [ServicePriority(0)]
    public class IceColdLoggerService : ILoggerService
    {
        private LoggerConfig config;
        public bool IsInitialized { get; private set; }
        
        public void Initialize()
        {
            IceColdLogger.Init(this);
            config = IceColdConfig.GetConfig<LoggerConfig>(nameof(LoggerConfig));
            
            IsInitialized = true;
        }

        public void Deinitialize()
        {
            config = null;
            IsInitialized = false;
        }
        
        public void OnWillQuit() { }

        public void Log(string message)
        {
            if (Application.isEditor)
            {
                if (config.editorFilterSettings.log)
                    Debug.Log(message);
            }
            else if (Debug.isDebugBuild)
            {
                if (config.debugFilterSettings.log)
                    Debug.Log(message);
            }
            else
            {
                if (config.releaseFilterSettings.log)
                    Debug.Log(message);
            }
        }

        public void LogWarning(string message)
        {
            if (Application.isEditor)
            {
                if (config.editorFilterSettings.warning)
                    Debug.LogWarning(message);
            }
            else if (Debug.isDebugBuild)
            {
                if (config.debugFilterSettings.warning)
                    Debug.LogWarning(message);
            }
            else
            {
                if (config.releaseFilterSettings.warning)
                    Debug.LogWarning(message);
            }
        }

        public void LogError(string message)
        {
            if (Application.isEditor)
            {
                if (config.editorFilterSettings.error)
                    Debug.LogError(message);
            }
            else if (Debug.isDebugBuild)
            {
                if (config.debugFilterSettings.error)
                    Debug.LogError(message);
            }
            else
            {
                if (config.releaseFilterSettings.error)
                    Debug.LogError(message);
            }
        }

        public void LogException(Exception exception)
        {
            if (Application.isEditor)
            {
                if (config.editorFilterSettings.error)
                    Debug.LogException(exception);
            }
            else if (Debug.isDebugBuild)
            {
                if (config.debugFilterSettings.error)
                    Debug.LogException(exception);
            }
            else
            {
                if (config.releaseFilterSettings.error)
                    Debug.LogException(exception);
            }
        }
    }
}