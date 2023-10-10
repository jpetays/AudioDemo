using Prg;
using UnityEditor;

namespace Editor.Prg.Dependencies
{
    internal static class AssetHistoryUpdaterMenu
    {
        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Asset History/";

        [MenuItem(MenuItem + "Force Update", false, 10)]
        private static void ForceUpdateAssetHistory()
        {
            Debug.Log("*");
            AssetHistoryUpdater.ForceUpdateAssetHistory();
        }
    }
}
