using UnityEngine;
using UnityEditor;

// Block Island 1.0 history column (block "26", fireSpot prefab): its glass tube material lost its texture in the
// migration and rendered as an opaque beige cylinder, hiding the rotating poster drum ("Die Geschichte der Inselwelt
// Block Island") inside. Make it clear glass again. Menu Block Island/Geschichtssäule reparieren, or batch -executeMethod HistoryColumnFix.Run.
public static class HistoryColumnFix {
    const string GlassPath = "Assets/Legacy 1.0/Resources/BlockIsland1/blockset/prefabs/lanttern3ds/materials/glass.mat";
    [MenuItem("Block Island/Geschichtssäule reparieren")]
    public static void Run() {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(GlassPath);
        if(m == null) { Debug.LogError("HISTORY COLUMN: " + GlassPath + " missing"); return; }
        Shader s = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        m.shader = s;
        m.SetColor("_Color", new Color(.86f, .93f, 1f, .16f));
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m); AssetDatabase.SaveAssets();
        Debug.Log("HISTORY COLUMN: glass is clear again (" + s.name + ")");
    }
}
