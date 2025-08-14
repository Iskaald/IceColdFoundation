using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IceCold.Interface;
using UnityEngine;

namespace IceCold.Logger
{
    [ServicePriority(0)]
    public class IceColdLoggerService : ILoggerService
    {
        private LoggerConfig config;
        
        private List<GroupLogFilterSettings> sortedGroupSettings;
        public bool IsInitialized { get; private set; }
        
        public void Initialize()
        {
            IceColdLogger.Init(this);
            config = IceColdConfig.GetConfig<LoggerConfig>(nameof(LoggerConfig));
            
            if (config != null && config.groupSettings != null)
            {
                // We now sort by the length of the *absolute* path
                sortedGroupSettings = config.groupSettings
                    .OrderByDescending(g => g.AbsolutePath.Length)
                    .ToList();
            }
            else
            {
                sortedGroupSettings = new List<GroupLogFilterSettings>();
                Debug.LogError("IceColdLoggerService: LoggerConfig is missing or invalid!");
            }
            
            IsInitialized = true;
        }

        public void Deinitialize()
        {
            config = null;
            sortedGroupSettings?.Clear();
            IsInitialized = false;
        }
        
        public void OnWillQuit() { }
        
        public void Log(string message, [CallerFilePath] string callerPath = "")
        {
            var group = FindGroupForPath(callerPath);
            if (ShouldLog(group, s => s.log))
                Debug.Log(FormatMessage(group?.groupName, message));
        }

        public void LogWarning(string message, string callerPath = "")
        {
            var group = FindGroupForPath(callerPath);
            if (ShouldLog(group, s => s.warning))
                Debug.LogWarning(FormatMessage(group?.groupName, message));
        }

        public void LogError(string message, string callerPath)
        {
            var group = FindGroupForPath(callerPath);
            if (ShouldLog(group, s => s.error))
                Debug.LogError(FormatMessage(group?.groupName, message));
        }

        public void LogException(Exception exception, string callerPath)
        {
            var group = FindGroupForPath(callerPath);
            if (ShouldLog(group, s => s.error))
                Debug.LogException(exception);
        }

        private GroupLogFilterSettings FindGroupForPath(string callerPath)
        {
            if (string.IsNullOrEmpty(callerPath) || sortedGroupSettings == null)
                return null;

            var normalizedCallerPath = callerPath.Replace('\\', '/');

            foreach (var groupSetting in sortedGroupSettings)
            {
                if (normalizedCallerPath.StartsWith(groupSetting.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                {
                    return groupSetting;
                }
            }
            return null;
        }
        
        private bool ShouldLog(GroupLogFilterSettings group, Func<LogFilterSettings, bool> levelCheck)
        {
            if (!IsInitialized) return false;

            LogFilterSettings settings;
            if (group != null)
            {
                if (Application.isEditor) settings = group.editorSettings;
                else if (Debug.isDebugBuild) settings = group.developmentSettings;
                else settings = group.releaseSettings;
            }
            else
            {
                if (Application.isEditor) settings = config.editorFilterSettings;
                else if (Debug.isDebugBuild) settings = config.debugFilterSettings;
                else settings = config.releaseFilterSettings;
            }

            return levelCheck(settings);
        }
        
        private string FormatMessage(string groupName, string message)
        {
            return $"[{groupName ?? "Default"}] {message}";
        }
    }
}