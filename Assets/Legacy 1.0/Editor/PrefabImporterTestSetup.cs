using UnityEditor;
using UnityEngine;

// Test only: copies a 1.0 animated figure into Assets/Block Island Prefabs and exports it.
// batchmode: -executeMethod PrefabImporterTestSetup.Setup   (with BI_PREFAB_DIR set to a test folder)
//            -executeMethod PrefabImporterTestSetup.Cleanup
public static class PrefabImporterTestSetup {
    const string Copy = PrefabBundleExporter.SourceFolder + "/Testfigur Bogenschuetze.prefab", Copy2 = PrefabBundleExporter.SourceFolder + "/Testfigur Kuh.prefab";
    public static void Setup() {
        if(!AssetDatabase.IsValidFolder(PrefabBundleExporter.SourceFolder)) AssetDatabase.CreateFolder("Assets", "Block Island Prefabs");
        AssetDatabase.CopyAsset("Assets/Legacy 1.0/GameObject/Archer_02.prefab", Copy);
        AssetDatabase.CopyAsset("Assets/Legacy 1.0/GameObject/29cow.prefab", Copy2);
        AssetDatabase.Refresh();
        PrefabBundleExporter.ExportAll();
    }
    public static void Cleanup() { AssetDatabase.DeleteAsset(Copy); AssetDatabase.DeleteAsset(Copy2); }
}
