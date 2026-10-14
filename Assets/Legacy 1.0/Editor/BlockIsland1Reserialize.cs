using System.Linq;
using UnityEditor;
using UnityEngine;

// The 1.0 assets came from a recovered Unity 2018 project; ~1,000 meshes/materials are stored in a
// serialization version Unity 6 only reads with a warning ("below the supported minimum").
// Re-saving them once upgrades them to the current format.
// Menu: Block Island > 1.0 Assets aktualisieren, or batchmode -executeMethod BlockIsland1Reserialize.Run
public static class BlockIsland1Reserialize {
    [MenuItem("Block Island/1.0 Assets aktualisieren")]
    public static void Run() {
        string[] paths = AssetDatabase.FindAssets("", new[] { "Assets/Legacy 1.0" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".asset") || p.EndsWith(".mat") || p.EndsWith(".prefab") || p.EndsWith(".anim") || p.EndsWith(".controller"))
            .Distinct().ToArray();
        AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssets);
        AssetDatabase.SaveAssets();
        Debug.Log("BLOCK ISLAND 1.0: " + paths.Length + " assets re-saved in the current Unity format.");
    }
}
