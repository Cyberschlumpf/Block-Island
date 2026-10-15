using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

// Prefab blocks: prefabs exported from the Unity editor (Assets/Block Island Prefabs -> ~/Block Island/Prefabs/*.biprefab,
// see PrefabBundleExporter) that the player turns into buildable blocks in game (key P, PrefabImporterGUI).
//
// Block IDs: prefab blocks live at fixed BlockSet indices FirstIndex + slot. Everything below FirstIndex is
// the built-in blockset (padded with hidden placeholders), so later blockset extensions never shift them and
// saved worlds keep pointing at the right prefab block. A slot is never reused; removing a prefab block
// only hides it.
//
// Registry ~/Block Island/Prefabs/blockset.txt, one tab-separated line per slot:
//   slot  name  bundle file  scale  animation  speed  loop  height  removed
//   animation: "" = the prefab's own Animator controller, "-" = no animation, otherwise a clip name
public static class PrefabBlockLibrary {
    public const int FirstIndex = 4096;
    public const string Extension = ".biprefab";
    public const string StandardAnimation = "", NoAnimation = "-";

    public sealed class Entry {
        public int slot;
        public string name = "", bundle = "", animation = StandardAnimation;
        public float scale = 1f, speed = 1f, height = 0f;
        public bool loop = true, removed;
    }

    static readonly List<Entry> entries = new List<Entry>();
    static bool loaded;
    static readonly Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    // Same base folder as the save games (~/Block Island).
    public static string Folder {
        get {
            // Automated tests use their own folder (environment variable), never the player's prefabs.
            string over = Environment.GetEnvironmentVariable("BI_PREFAB_DIR");
            if(!string.IsNullOrEmpty(over)) { Directory.CreateDirectory(over); return over; }
            return BlockIslandPaths.Folder(BlockIslandPaths.Prefabs);
        }
    }
    static string RegistryPath { get { return Path.Combine(Folder, "blockset.txt"); } }
    public static string IconPath(int slot) { return Path.Combine(Folder, "icons", "slot" + slot + ".png"); }

    public static List<Entry> Entries { get { Load(); return entries; } }

    // ------------------------------------------------------------------ registry
    static void Load() {
        if(loaded) return;
        loaded = true; entries.Clear();
        try {
            if(!File.Exists(RegistryPath)) return;
            foreach(string line in File.ReadAllLines(RegistryPath)) {
                if(line.Length == 0 || line[0] == '#') continue;
                string[] p = line.Split('\t');
                if(p.Length < 9) continue;
                var e = new Entry {
                    slot = int.Parse(p[0], CultureInfo.InvariantCulture), name = p[1], bundle = p[2],
                    scale = F(p[3], 1f), animation = p[4], speed = F(p[5], 1f), loop = p[6] == "1", height = F(p[7], 0f), removed = p[8] == "1"
                };
                entries.Add(e);
            }
            entries.Sort((a, b) => a.slot.CompareTo(b.slot));
        } catch(Exception ex) { Debug.LogError("PREFAB BLOCKS: registry unreadable: " + ex.Message); }
    }
    static float F(string s, float d) { float v; return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : d; }
    static string S(float v) { return v.ToString("0.####", CultureInfo.InvariantCulture); }

    public static void Save() {
        Load();
        var lines = new List<string> { "# Block Island prefab blocks: slot	name	bundle	scale	animation	speed	loop	height	removed" };
        foreach(Entry e in entries)
            lines.Add(e.slot + "\t" + Clean(e.name) + "\t" + Clean(e.bundle) + "\t" + S(e.scale) + "\t" + Clean(e.animation) + "\t" + S(e.speed) + "\t" + (e.loop ? "1" : "0") + "\t" + S(e.height) + "\t" + (e.removed ? "1" : "0"));
        string tmp = RegistryPath + ".tmp";
        File.WriteAllLines(tmp, lines.ToArray());
        if(File.Exists(RegistryPath)) File.Delete(RegistryPath);
        File.Move(tmp, RegistryPath);
    }
    static string Clean(string s) { return (s ?? "").Replace("\t", " ").Replace("\n", " ").Replace("\r", " "); }

    public static Entry Find(Block b) {
        if(b == null || b.blockID < FirstIndex) return null;
        Load(); int slot = b.blockID - FirstIndex;
        foreach(Entry e in entries) if(e.slot == slot) return e;
        return null;
    }
    public static bool IsPrefabBlock(Block b) { return Find(b) != null; }
    public static bool NameTaken(string name, Entry except) {
        Load();
        foreach(Entry e in entries) if(e != except && !e.removed && string.Equals(e.name, name, StringComparison.OrdinalIgnoreCase)) return true;
        Block b = BlockSet.instance != null ? BlockSet.instance.FindBlock(name) : null;
        return b != null && (except == null || Find(b) != except);
    }

    // ------------------------------------------------------------------ bundles
    public static string[] ListBundles() {
        string[] f = Directory.GetFiles(Folder, "*" + Extension);
        for(int i = 0; i < f.Length; i++) f[i] = Path.GetFileName(f[i]);
        Array.Sort(f, StringComparer.OrdinalIgnoreCase);
        return f;
    }

    // The prefab inside a .biprefab file. Bundles stay loaded: placed copies keep using their assets.
    public static GameObject LoadPrefab(string bundleFile) {
        if(string.IsNullOrEmpty(bundleFile)) return null;
        GameObject go;
        if(prefabs.TryGetValue(bundleFile, out go) && go != null) return go;
        string path = Path.Combine(Folder, bundleFile);
        if(!File.Exists(path)) return null;
        AssetBundle ab;
        if(!bundles.TryGetValue(bundleFile, out ab) || ab == null) {
            ab = AssetBundle.LoadFromFile(path);
            if(ab == null) { Debug.LogError("PREFAB BLOCKS: cannot open " + path + " (built with another Unity version?)"); return null; }
            bundles[bundleFile] = ab;
        }
        GameObject[] all = ab.LoadAllAssets<GameObject>();
        string wanted = Path.GetFileNameWithoutExtension(bundleFile);
        go = null;
        foreach(GameObject g in all) if(string.Equals(g.name, wanted, StringComparison.OrdinalIgnoreCase)) go = g;
        if(go == null && all.Length > 0) go = all[0];
        prefabs[bundleFile] = go;
        return go;
    }

    // Re-read a bundle that was exported again from the editor (placed copies are rebuilt by the caller).
    public static void Reload(string bundleFile) {
        AssetBundle ab;
        if(bundles.TryGetValue(bundleFile, out ab) && ab != null) ab.Unload(false);
        bundles.Remove(bundleFile); prefabs.Remove(bundleFile);
    }

    // Animation clips available on a prefab (Animator controller clips and legacy Animation clips), one per name:
    // a clip is played by name (PrefabBlockInstance).
    public static List<AnimationClip> Clips(GameObject prefab) {
        var list = new List<AnimationClip>(); var names = new HashSet<string>();
        if(prefab == null) return list;
        foreach(Animator a in prefab.GetComponentsInChildren<Animator>(true))
            if(a.runtimeAnimatorController != null) foreach(AnimationClip c in a.runtimeAnimatorController.animationClips) if(c != null && names.Add(c.name)) list.Add(c);
        foreach(Animation an in prefab.GetComponentsInChildren<Animation>(true))
            foreach(AnimationState s in an) if(s.clip != null && names.Add(s.clip.name)) list.Add(s.clip);
        return list;
    }

    // ------------------------------------------------------------------ blockset
    // Called at the end of BlockSet.Awake: pad to FirstIndex, then one GameObjectBlock per slot.
    public static void EnsureBlocks(BlockSet set) {
        Load();
        List<Block> blocks = set.GetBlocks();
        if(blocks.Count > FirstIndex) { Debug.LogError("PREFAB BLOCKS: built-in blockset reached " + blocks.Count + " blocks; prefab slots must start above it."); return; }
        while(blocks.Count < FirstIndex) blocks.Add(Placeholder("Reserved " + blocks.Count));
        foreach(Entry e in entries) {
            while(blocks.Count < FirstIndex + e.slot) blocks.Add(Placeholder("Prefab Slot " + (blocks.Count - FirstIndex)));
            blocks.Add(CreateBlock(e));
        }
    }

    static GameObjectBlock Placeholder(string n) {
        GameObjectBlock b = ScriptableObject.CreateInstance<GameObjectBlock>();
        b.name = n; b.hideFlags = HideFlags.DontSave; b.retired = true;
        return b;
    }

    static GameObjectBlock CreateBlock(Entry e) {
        GameObjectBlock b = Placeholder(e.name);
        if(e.removed) return b;
        b.gameObject = LoadPrefab(e.bundle);
        b.retired = b.gameObject == null;
        if(b.retired) Debug.LogWarning("PREFAB BLOCKS: '" + e.name + "' – file " + e.bundle + " missing; block hidden until it is back.");
        b.icon = LoadIcon(e.slot);
        return b;
    }

    static Texture2D LoadIcon(int slot) {
        string p = IconPath(slot);
        if(!File.Exists(p)) return null;
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
        return t.LoadImage(File.ReadAllBytes(p)) ? t : null;
    }

    // New block from the importer: next free slot, registered in the running BlockSet immediately.
    public static Block Register(Entry e, Texture2D icon) {
        Load();
        int next = 0; foreach(Entry x in entries) next = Mathf.Max(next, x.slot + 1);
        e.slot = next; e.removed = false;
        entries.Add(e);
        SaveIcon(e.slot, icon);
        Save();
        BlockSet set = BlockSet.instance;
        if(set == null) return null;
        GameObjectBlock b = CreateBlock(e);
        set.AddRuntimeBlock(b, FirstIndex + e.slot);
        return b;
    }

    public static void Update(Entry e, Texture2D icon) {
        if(icon != null) SaveIcon(e.slot, icon);
        Save();
        Block b = BlockSet.instance != null ? BlockSet.instance[FirstIndex + e.slot] : null;
        if(b != null) { b.name = e.name; if(icon != null) b.icon = LoadIcon(e.slot); }
        PrefabBlockInstance.ApplyAll(e);
    }

    public static void Remove(Entry e) {
        e.removed = true; Save();
        Block b = BlockSet.instance != null ? BlockSet.instance[FirstIndex + e.slot] : null;
        if(b != null) b.retired = true;
    }

    static void SaveIcon(int slot, Texture2D icon) {
        if(icon == null) return;
        string p = IconPath(slot);
        Directory.CreateDirectory(Path.GetDirectoryName(p));
        File.WriteAllBytes(p, icon.EncodeToPNG());
    }

    // ------------------------------------------------------------------ placed copies
    // Called by InfiniteChunkMeshRenderer for every instantiated prefab block.
    public static void SetupInstance(GameObject instance, Block block, float floorY) {
        Entry e = Find(block);
        if(e == null) return;
        PrefabBlockInstance p = instance.AddComponent<PrefabBlockInstance>();
        p.Init(e, floorY, false);
    }
}
