using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

// Prefabs saved in Assets/Block Island Prefabs/ are exported automatically to ~/Block Island/Prefabs/<name>.biprefab
// (one AssetBundle per prefab with its meshes, materials, textures and animations). In the game, key P opens the
// prefab importer, which turns them into buildable blocks (PrefabBlockLibrary).
// Menu: Block Island/Prefabs exportieren. Batch: -executeMethod PrefabBundleExporter.ExportAll
public class PrefabBundleExporter : AssetPostprocessor {
    public const string SourceFolder = "Assets/Block Island Prefabs";
    static readonly HashSet<string> pending = new HashSet<string>();

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
        bool any = false;
        foreach(string p in imported) if(IsSource(p)) { pending.Add(p); any = true; }
        foreach(string p in moved) if(IsSource(p)) { pending.Add(p); any = true; }
        // Bundles cannot be built during an import: export right after it.
        if(any) EditorApplication.delayCall += ExportPending;
    }

    static bool IsSource(string p) { return p.StartsWith(SourceFolder + "/") && p.EndsWith(".prefab"); }

    static void ExportPending() {
        if(pending.Count == 0) return;
        var list = new List<string>(pending); pending.Clear();
        Export(list);
    }

    [MenuItem("Block Island/Prefabs exportieren")]
    public static void ExportAll() {
        if(!AssetDatabase.IsValidFolder(SourceFolder)) { AssetDatabase.CreateFolder("Assets", "Block Island Prefabs"); Debug.Log("PREFAB EXPORT: created " + SourceFolder + " – save prefabs there."); return; }
        var list = new List<string>();
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab", new[] { SourceFolder })) list.Add(AssetDatabase.GUIDToAssetPath(guid));
        Export(list);
    }

    [MenuItem("Block Island/Prefab-Ordner öffnen")]
    static void OpenFolder() { EditorUtility.RevealInFinder(PrefabBlockLibrary.Folder); }

    static void Export(List<string> prefabPaths) {
        if(prefabPaths.Count == 0) return;
        string temp = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "BlockIslandPrefabBundles");
        Directory.CreateDirectory(temp);
        var builds = new List<AssetBundleBuild>();
        foreach(string p in prefabPaths) {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(p) == null) continue;
            builds.Add(new AssetBundleBuild { assetBundleName = BundleName(p), assetNames = new[] { p } });
        }
        if(builds.Count == 0) return;
        // The game is a macOS build; bundles must match its platform.
        var manifest = BuildPipeline.BuildAssetBundles(temp, builds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneOSX);
        if(manifest == null) { Debug.LogError("PREFAB EXPORT failed – see the console above."); return; }
        string dst = PrefabBlockLibrary.Folder;
        foreach(var b in builds) {
            string src = Path.Combine(temp, b.assetBundleName);
            if(!File.Exists(src)) { Debug.LogError("PREFAB EXPORT: no bundle for " + b.assetNames[0]); continue; }
            string target = Path.Combine(dst, Path.GetFileNameWithoutExtension(b.assetNames[0]) + PrefabBlockLibrary.Extension);
            File.Copy(src, target, true);
            Debug.Log("PREFAB EXPORT: " + b.assetNames[0] + " -> " + target);
        }
    }

    // AssetBundle names are lower case and must be unique.
    static string BundleName(string path) {
        string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        foreach(char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
        return n.Replace(' ', '_') + ".bundle";
    }
}
