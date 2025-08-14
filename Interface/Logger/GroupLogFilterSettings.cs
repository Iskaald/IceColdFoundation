using System;
using UnityEngine;

namespace IceCold.Interface
{
    [Serializable]
    public class GroupLogFilterSettings
    {
        [Tooltip("The name of the log group (derived from the folder name).")]
        public string groupName;

        public string ProjectPath { get; set; }

        public string AbsolutePath { get; set; }
        
        public LogFilterSettings editorSettings;
        public LogFilterSettings developmentSettings;
        public LogFilterSettings releaseSettings;
        
        public GroupLogFilterSettings(string groupName, string projectPath, string absolutePath)
        {
            this.groupName = groupName;
            ProjectPath = projectPath;
            AbsolutePath = absolutePath.Replace('\\', '/');
            
            editorSettings = new LogFilterSettings(true, true, true);
            developmentSettings = new LogFilterSettings(true, true, true);
            releaseSettings = new LogFilterSettings(false, false, true);
        }
    }
}