using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Block Island 1.0 migration check. Menu: Block Island > 1.0 Migration prüfen, or batchmode:
// Unity -batchmode -quit -projectPath ... -executeMethod BlockIsland1MigrationCheck.Run
public static class BlockIsland1MigrationCheck {

    [MenuItem("Block Island/1.0 Migration prüfen")]
    public static void Run() {
        StringBuilder r = new StringBuilder();
        int problems = 0;

        GameObject prefab = Resources.Load<GameObject>("BlockSet");
        GameObject go = Object.Instantiate(prefab);
        BlockSet set = go.GetComponent<BlockSet>();
        // BlockSet.Awake builds the runtime list; call it explicitly in case the editor skipped it.
        if(BlockSet.instance != set) set.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
        Block[] slots = BlockSet.blockIsland1Slots;
        r.AppendLine("BlockSet blocks total: " + set.Count + ", Block Island 1.0 slots: " + slots.Length);

        int empty = 0;
        for(int i = 0; i < slots.Length; i++) {
            Block b = slots[i];
            if(b == null) { empty++; problems++; r.AppendLine("  slot " + i + ": NOT LOADED"); continue; }
            string issue = null;
            GameObjectBlock gob = b as GameObjectBlock;
            if(gob != null) {
                if(gob.gameObject == null) issue = "figure/object prefab missing";
                else {
                    int missing = 0;
                    foreach(Transform t in gob.gameObject.GetComponentsInChildren<Transform>(true))
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if(missing > 0) issue = missing + " missing script(s) in prefab " + gob.gameObject.name;
                }
            } else {
                try { b.Build(); } catch(System.Exception e) { issue = "Build() failed: " + e.Message; }
                Face f = b.GetPreviewFace();
                if(issue == null && f != null && f.material == null) issue = "face without material";
                if(issue == null && f != null && f.material != null && f.material.shader != null && ShaderUtil.ShaderHasError(f.material.shader)) issue = "shader error: " + f.material.shader.name;
            }
            if(issue != null) { problems++; r.AppendLine("  slot " + i + " " + b.name + " (" + b.GetType().Name + "): " + issue); }
        }

        // shaders rebuilt by the migration
        foreach(string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Legacy 1.0" })) {
            Shader s = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            if(s != null && ShaderUtil.ShaderHasError(s) && !s.name.StartsWith("Hidden/")) { problems++; r.AppendLine("  shader error: " + s.name); }
        }

        // whole native world: every voxel must resolve to its own 1.0 slot block
        BlockIslandNativeWorld.Begin();
        var counts = new Dictionary<string, long>();
        long voxels = 0, wrong = 0;
        for(int x = 0; x < 1024; x++) for(int z = 0; z < 1024; z++) for(int y = 0; y < 256; y++) {
            ushort id; byte meta;
            if(!BlockIslandNativeWorld.Raw(x, y, z, out id, out meta) || id == 0) continue;
            voxels++;
            DataBlock d = BlockIslandNativeWorld.Sample(x, y, z);
            Block expected = id - 1 < slots.Length ? slots[id - 1] : null;
            if(d.block != expected || d.block == null) { wrong++; string k = "id " + id; counts[k] = counts.ContainsKey(k) ? counts[k] + 1 : 1; }
        }
        BlockIslandNativeWorld.End();
        r.AppendLine("World voxels: " + voxels + ", not resolved to their 1.0 block: " + wrong);
        foreach(var kv in counts) r.AppendLine("  " + kv.Key + ": " + kv.Value);
        if(wrong > 0) problems++;

        Object.DestroyImmediate(go);
        r.Insert(0, (problems == 0 ? "OK" : "PROBLEME: " + problems) + "\n");
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Migration 1.0 nach 2.0/pruefbericht.txt"));
        File.WriteAllText(path, r.ToString());
        Debug.Log("BLOCK ISLAND 1.0 MIGRATION CHECK\n" + r);
    }
}
