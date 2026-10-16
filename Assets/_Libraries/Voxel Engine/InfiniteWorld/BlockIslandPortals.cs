using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// Portals between the worlds: "Portal → Tanviir" and "Portal → Block Island".
// A portal is one block that shows a 4x4x4 cube standing on the ground (the placed block is at the front-left of its
// centre). Its sides show a 360° picture of the other world (taken at the other world's portal, or where the player
// last was there) and Hovl Studio's "Portal space" effect. Walking into the cube starts PortalTransition.
// Both blocks have fixed IDs (4000/4001), so saves stay valid whatever else changes in the BlockSet.
public class PortalBlock : GameObjectBlock {
    public InfiniteWorldSave.WorldType target;
    public override void OnBlockCreate(Vector3i pos, BlockDirection dir) { base.OnBlockCreate(pos, dir); PortalRegistry.Set(InfiniteWorldSave.CurrentWorldName, target, pos); }
    public override void OnBlockDestroy(Vector3i pos) { base.OnBlockDestroy(pos); PortalRegistry.Remove(InfiniteWorldSave.CurrentWorldName, target, pos); }
}

public static class PortalBlocks {
    public const int ToTanviirId = 4000, ToBlockIslandId = 4001;
    public const string Prefix = "Portal → ";
    static GameObject templateRoot;
    static readonly GameObject[] templates = new GameObject[2];

    public static string SaveNameOf(InfiniteWorldSave.WorldType t) { return t == InfiniteWorldSave.WorldType.Tanviir ? InfiniteWorldSave.TanviirSaveName : InfiniteWorldSave.BlockIslandSaveName; }
    public static Color ColorOf(InfiniteWorldSave.WorldType t) { return t == InfiniteWorldSave.WorldType.Tanviir ? new Color(.35f, .85f, 1f) : new Color(1f, .72f, .3f); }

    // Called by BlockSet.Awake after all other blocks and before the prefab blocks (4096+).
    public static void Ensure(BlockSet set) {
        List<Block> blocks = set.GetBlocks();
        if(blocks.Count > ToTanviirId) { Debug.LogError("PORTALS: BlockSet already has " + blocks.Count + " blocks; portal IDs 4000/4001 are taken."); return; }
        while(blocks.Count < ToTanviirId) {
            GameObjectBlock p = ScriptableObject.CreateInstance<GameObjectBlock>();
            p.name = "Reserved " + blocks.Count; p.hideFlags = HideFlags.DontSave; p.retired = true; blocks.Add(p);
        }
        blocks.Add(Make(InfiniteWorldSave.WorldType.Tanviir, 0));
        blocks.Add(Make(InfiniteWorldSave.WorldType.BlockIsland, 1));
    }

    static PortalBlock Make(InfiniteWorldSave.WorldType target, int i) {
        PortalBlock b = ScriptableObject.CreateInstance<PortalBlock>();
        b.name = Prefix + SaveNameOf(target); b.hideFlags = HideFlags.DontSave; b.target = target;
        b.gameObject = Template(i, target, b.name);
        b.icon = Icon(target);
        return b;
    }

    // Inactive template under a hidden inactive root: instances made from it by the chunk renderer are active,
    // the template itself never runs.
    static GameObject Template(int i, InfiniteWorldSave.WorldType target, string name) {
        if(templateRoot == null) {
            templateRoot = new GameObject("Portal Templates"); templateRoot.SetActive(false);
            if(Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(templateRoot);
        }
        if(templates[i] == null) {
            var go = new GameObject(name); go.transform.SetParent(templateRoot.transform, false);
            go.AddComponent<BlockIslandPortal>().target = target;
            templates[i] = go;
        }
        return templates[i];
    }

    // Inventory icon: a glowing ring in the portal colour around a starry centre.
    static Texture2D Icon(InfiniteWorldSave.WorldType target) {
        const int n = 64; Color c = ColorOf(target);
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, name = "Portal Icon" };
        var rnd = new System.Random(target == InfiniteWorldSave.WorldType.Tanviir ? 7 : 11);
        for(int y = 0; y < n; y++) for(int x = 0; x < n; x++) {
            float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1;
            float box = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            float ring = Mathf.Exp(-Mathf.Pow((box - .82f) / .09f, 2));
            Color inside = Color.Lerp(new Color(.04f, .03f, .12f), new Color(.18f, .1f, .35f), 1 - box) * (box < .8f ? 1 : 0);
            Color px = inside + c * ring * 1.3f; px.a = box < .97f ? 1 : 0;
            if(box < .75f && rnd.NextDouble() < .025) px = Color.Lerp(px, Color.white, .9f);
            t.SetPixel(x, y, px);
        }
        t.Apply(); return t;
    }
}

// One placed portal in the streamed world (instantiated by InfiniteChunkMeshRenderer as a child of its chunk).
public sealed class BlockIslandPortal : MonoBehaviour {
    public InfiniteWorldSave.WorldType target;
    public static readonly List<BlockIslandPortal> All = new List<BlockIslandPortal>();
    public const float Size = 4f;

    public Vector3i Cell { get; private set; }
    public Vector3 Center { get; private set; }           // centre of the 4x4x4 cube
    public string TargetSaveName { get { return PortalBlocks.SaveNameOf(target); } }

    GameObject visual; Renderer view; Material viewMat; int shownVersion = -1; bool armed, live;

    void Start() {
        if(transform.parent == null || transform.parent.name != "InfiniteSpecialBlocks") return;   // template or preview copy
        live = true;
        Vector3 p = transform.position;
        Cell = new Vector3i(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
        // the cube covers the placed cell and reaches one block left/front and two blocks right/back of it
        Center = new Vector3(Cell.x + 1f, Cell.y + Size * .5f, Cell.z + 1f);
        BuildVisual();
        All.Add(this);
        PortalRegistry.Set(InfiniteWorldSave.CurrentWorldName, target, Cell);
        armed = !Contains(PlayerPosition());               // somebody arriving inside must first step out
        PortalWorldWatcher.Ensure();
    }
    void OnDestroy() { All.Remove(this); if(viewMat != null) Destroy(viewMat); }

    public bool Contains(Vector3 p) {
        const float inset = .15f;
        return p.x > Center.x - Size * .5f + inset && p.x < Center.x + Size * .5f - inset
            && p.z > Center.z - Size * .5f + inset && p.z < Center.z + Size * .5f - inset
            && p.y > Cell.y - .5f && p.y < Cell.y + Size + 1f;
    }
    static Vector3 PlayerPosition() {
        GameObject pl = GameObject.FindWithTag("Player");
        return pl != null ? pl.transform.position : new Vector3(0, -10000, 0);
    }

    void Update() {
        if(!live) return;
        if(shownVersion != PortalSnapshots.Version) ApplySnapshot();
        if(PortalTransition.Busy || GameState.IsPause) return;
        bool inside = Contains(PlayerPosition());
        if(!inside) { armed = true; return; }
        if(armed) { armed = false; PortalTransition.Begin(this); }
    }

    void BuildVisual() {
        visual = new GameObject("Portal Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.position = Center; visual.transform.rotation = Quaternion.identity;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(cube.GetComponent<Collider>());
        cube.name = "Portal Window";
        cube.transform.SetParent(visual.transform, false);
        cube.transform.localScale = Vector3.one * (Size - .02f);
        view = cube.GetComponent<Renderer>();
        view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; view.receiveShadows = false;
        Shader sh = Resources.Load<Shader>("Portal/PortalView");
        if(sh != null) { viewMat = new Material(sh) { name = "Portal View" }; viewMat.SetColor("_EdgeColor", PortalBlocks.ColorOf(target)); }
        ApplySnapshot();

        // Hovl "Portal space": a vertical ring about 4 blocks across, one on each side of the cube.
        GameObject fx = Resources.Load<GameObject>("Portal/Portal space");
        if(fx != null) {
            for(int side = 0; side < 4; side++) {
                Quaternion rot = Quaternion.Euler(0, side * 90f, 0);
                GameObject e = Instantiate(fx, visual.transform);
                e.name = "Portal Effect " + side;
                e.transform.localRotation = rot;
                e.transform.localPosition = rot * new Vector3(0, 0, Size * .5f + .03f) + new Vector3(0, -1.5f, 0);   // prefab pivot is 1.5 below its ring centre
                e.transform.localScale = Vector3.one * .95f;
                foreach(Light l in e.GetComponentsInChildren<Light>(true)) l.enabled = false;
                foreach(Renderer r in e.GetComponentsInChildren<Renderer>(true)) {
                    // the stone arch and the darkening cloud of the original effect are not wanted on a glowing cube
                    if(r.name == "Portal space" || r.name == "Darkness") r.enabled = false;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                }
                foreach(ParticleSystem ps in e.GetComponentsInChildren<ParticleSystem>(true)) if(ps.name == "Darkness") { var em = ps.emission; em.enabled = false; }
            }
        }
    }

    void ApplySnapshot() {
        shownVersion = PortalSnapshots.Version;
        if(view == null) return;
        Cubemap c = PortalSnapshots.Get(TargetSaveName);
        if(c != null && viewMat != null) { viewMat.SetTexture("_Cube", c); view.sharedMaterial = viewMat; return; }
        // nothing seen of the other world yet: a nebula (the galaxy's star cubemap) instead
        if(viewMat != null) { viewMat.SetTexture("_Cube", GalaxyCube()); view.sharedMaterial = viewMat; }
    }

    static Texture galaxyCube;
    static Texture GalaxyCube() {
        if(galaxyCube != null) return galaxyCube;
        Material g = Resources.Load<Material>("Portal/Galaxy/GalaxyMaterialSkybox_21");
        if(g != null && g.HasProperty("_StarsTexture")) galaxyCube = g.GetTexture("_StarsTexture");
        return galaxyCube;
    }

    public void SetVisible(bool on) { if(visual != null) visual.SetActive(on); }
}

// Where the portals are, per world: Documents/Block Island/Portale/portale.txt ("world <tab> target <tab> x y z").
public static class PortalRegistry {
    struct Entry { public string world; public int target; public Vector3i cell; }
    static List<Entry> entries;
    static string FilePath { get { return Path.Combine(BlockIslandPaths.Folder("Portale"), "portale.txt"); } }

    static void Load() {
        if(entries != null) return;
        entries = new List<Entry>();
        try {
            if(!File.Exists(FilePath)) return;
            foreach(string line in File.ReadAllLines(FilePath)) {
                string[] f = line.Split('\t'); if(f.Length < 3) continue;
                string[] c = f[2].Split(' '); int t, x, y, z;
                if(c.Length == 3 && int.TryParse(f[1], out t) && int.TryParse(c[0], out x) && int.TryParse(c[1], out y) && int.TryParse(c[2], out z))
                    entries.Add(new Entry { world = f[0], target = t, cell = new Vector3i(x, y, z) });
            }
        } catch(Exception e) { Debug.LogWarning("PORTALS: registry not readable: " + e.Message); }
    }
    static void Save() {
        try {
            var lines = new List<string>();
            foreach(Entry e in entries) lines.Add(e.world + "\t" + e.target + "\t" + e.cell.x + " " + e.cell.y + " " + e.cell.z);
            File.WriteAllLines(FilePath, lines.ToArray());
        } catch(Exception e) { Debug.LogWarning("PORTALS: registry not writable: " + e.Message); }
    }
    // One portal per world and target: the most recently placed or seen one.
    public static void Set(string world, InfiniteWorldSave.WorldType target, Vector3i cell) {
        if(string.IsNullOrEmpty(world)) return; Load();
        for(int i = 0; i < entries.Count; i++) if(entries[i].world == world && entries[i].target == (int)target) {
            if(entries[i].cell.Equals(cell)) return;
            entries.RemoveAt(i); break;
        }
        entries.Add(new Entry { world = world, target = (int)target, cell = cell }); Save();
    }
    public static void Remove(string world, InfiniteWorldSave.WorldType target, Vector3i cell) {
        Load();
        int n = entries.RemoveAll(e => e.world == world && e.target == (int)target && e.cell.Equals(cell));
        if(n > 0) Save();
    }
    public static bool TryGet(string world, InfiniteWorldSave.WorldType target, out Vector3i cell) {
        Load(); cell = new Vector3i(0, 0, 0);
        foreach(Entry e in entries) if(e.world == world && e.target == (int)target) { cell = e.cell; return true; }
        return false;
    }
    public static bool TryGetAny(string world, out Vector3i cell) {
        Load(); cell = new Vector3i(0, 0, 0);
        foreach(Entry e in entries) if(e.world == world) { cell = e.cell; return true; }
        return false;
    }
    public static bool HasAny(string world) { Vector3i c; return TryGetAny(world, out c); }
}

// 360° pictures of the worlds: Documents/Block Island/Portale/<world>.jpg (the six cube faces side by side).
public static class PortalSnapshots {
    public const int Res = 512;
    public static int Version { get; private set; }
    static readonly Dictionary<string, Cubemap> cache = new Dictionary<string, Cubemap>();
    static readonly Dictionary<string, DateTime> cacheTime = new Dictionary<string, DateTime>();

    public static string PathOf(string world) { return Path.Combine(BlockIslandPaths.Folder("Portale"), InfiniteWorldSave.SanitizeName(world) + ".jpg"); }

    public static Cubemap Get(string world) {
        string p = PathOf(world);
        if(!File.Exists(p)) return null;
        DateTime t = File.GetLastWriteTimeUtc(p); Cubemap c;
        if(cache.TryGetValue(world, out c) && c != null && cacheTime[world] == t) return c;
        try {
            var strip = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if(!strip.LoadImage(File.ReadAllBytes(p)) || strip.width != Res * 6 || strip.height != Res) { UnityEngine.Object.Destroy(strip); return null; }
            if(c == null) c = new Cubemap(Res, TextureFormat.RGB24, true) { name = "Portal view " + world };
            for(int f = 0; f < 6; f++) c.SetPixels(strip.GetPixels(f * Res, 0, Res, Res), (CubemapFace)f);
            c.Apply(true, false);
            UnityEngine.Object.Destroy(strip);
            cache[world] = c; cacheTime[world] = t;
            return c;
        } catch(Exception e) { Debug.LogWarning("PORTALS: picture of " + world + " not readable: " + e.Message); return null; }
    }

    // Renders the world around 'eye' into a cubemap and saves it. Portal cubes and the player are hidden meanwhile.
    public static bool Capture(string world, Vector3 eye) {
        Camera main = Camera.main;
        if(main == null || string.IsNullOrEmpty(world)) return false;
        var go = new GameObject("Portal Snapshot Camera");
        Cubemap cube = null; var hidden = new List<Renderer>();
        try {
            Camera cam = go.AddComponent<Camera>();
            cam.CopyFrom(main); cam.enabled = false; cam.targetTexture = null;
            go.transform.position = eye; go.transform.rotation = Quaternion.identity;
            foreach(BlockIslandPortal p in BlockIslandPortal.All) foreach(Renderer r in p.GetComponentsInChildren<Renderer>()) if(r.enabled) { r.enabled = false; hidden.Add(r); }
            GameObject pl = GameObject.FindWithTag("Player");
            if(pl != null) foreach(Renderer r in pl.GetComponentsInChildren<Renderer>()) if(r.enabled) { r.enabled = false; hidden.Add(r); }
            cube = new Cubemap(Res, TextureFormat.RGB24, false);
            if(!cam.RenderToCubemap(cube)) return false;
        } finally {
            foreach(Renderer r in hidden) if(r != null) r.enabled = true;
            UnityEngine.Object.Destroy(go);
        }
        var strip = new Texture2D(Res * 6, Res, TextureFormat.RGB24, false);
        for(int f = 0; f < 6; f++) strip.SetPixels(f * Res, 0, Res, Res, cube.GetPixels((CubemapFace)f));
        strip.Apply(false);
        try { File.WriteAllBytes(PathOf(world), strip.EncodeToJPG(88)); }
        catch(Exception e) { Debug.LogWarning("PORTALS: picture not saved: " + e.Message); return false; }
        finally { UnityEngine.Object.Destroy(strip); UnityEngine.Object.Destroy(cube); }
        cache.Remove(world);
        Version++;
        Debug.Log("PORTALS: 360° picture of " + world + " taken at " + eye);
        return true;
    }
}

// Keeps the 360° picture of the current world up to date: at the nearest portal when the player is close to one,
// otherwise (worlds without a portal) where the player is - the place a portal journey would arrive.
public sealed class PortalWorldWatcher : MonoBehaviour {
    const float FirstAfter = 15f, Every = 120f, NearPortal = 48f;
    float next;
    public static void Ensure() {
        if(FindFirstObjectByType<PortalWorldWatcher>() != null) return;
        new GameObject("Portal World Watcher").AddComponent<PortalWorldWatcher>();
    }
    void Start() { next = Time.unscaledTime + FirstAfter; }
    void Update() {
        if(Time.unscaledTime < next || PortalTransition.Busy || GameState.IsPause) return;
        next = Time.unscaledTime + Every;
        Camera cam = Camera.main; if(cam == null) return;
        string world = InfiniteWorldSave.CurrentWorldName;
        BlockIslandPortal nearest = null; float best = NearPortal;
        foreach(BlockIslandPortal p in BlockIslandPortal.All) { float d = Vector3.Distance(p.Center, cam.transform.position); if(d < best) { best = d; nearest = p; } }
        if(nearest != null) PortalSnapshots.Capture(world, nearest.Center);
        else if(!PortalRegistry.HasAny(world)) PortalSnapshots.Capture(world, cam.transform.position);
    }
}
