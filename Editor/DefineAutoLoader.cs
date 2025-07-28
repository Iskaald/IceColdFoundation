namespace IceCold.Editor
{
    using UnityEditor;
    
    [InitializeOnLoad]
    public static class DefineAutoLoader
    {
        private const string DefineSymbol = "ICE_COLD";
        
        static DefineAutoLoader()
        {
            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            if (defines.Contains(DefineSymbol)) return;
            
            if (!string.IsNullOrEmpty(defines) && !defines.EndsWith(";"))
                defines += ";";
            defines += DefineSymbol;
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
        }
    }
}