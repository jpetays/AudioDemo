using Prg.Util;
using UnityEditor;
using Debug = Prg.Debug;

namespace Editor.Prg.Dependencies
{
    internal static class CheckDependenciesMenu
    {
        private static bool _hasLogger;

        private static void SetLogger()
        {
            if (_hasLogger)
            {
                return;
            }
            _hasLogger = true;
            LogConfig.Create();
            LogConfig.ForceLogging(typeof(CheckDependencies));
        }

        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Dependencies/";

        [MenuItem(MenuItem + "Check Asset Usages", false, 10)]
        private static void CheckAssetUsage()
        {
            SetLogger();
            Debug.Log("*");
            CheckDependencies.CheckAssetUsage(Selection.assetGUIDs);
        }

        [MenuItem(MenuItem + "List 'Text Asset' Usage", false, 11)]
        private static void CheckTextAssetUsage()
        {
            SetLogger();
            Debug.Log("*");
            CheckReferences.CheckTextMeshProUsage(Selection.assetGUIDs);
        }

        [MenuItem(MenuItem + "Check Missing References in Prefabs", false, 20)]
        private static void CheckReferencesInPrefabs()
        {
            SetLogger();
            Debug.Log("*");
            CheckReferences.CheckReferencesInPrefabs();
        }

        [MenuItem(MenuItem + "Check Missing References in Scenes", false, 21)]
        private static void CheckReferencesInScenes()
        {
            SetLogger();
            Debug.Log("*");
            CheckReferences.CheckReferencesInScenes();
        }
    }
}
