using Prg;
using UnityEditor;

namespace Editor.Prg.Dependencies
{
    internal static class CheckDependenciesMenu
    {
        private const string MenuRoot = "Prg/";
        private const string MenuItem = MenuRoot + "Dependencies/";

        [MenuItem(MenuItem + "Check Usages", false, 10)]
        private static void CheckUsages()
        {
            Debug.Log("*");
            CheckDependencies.CheckUsages();
        }
    }
}
