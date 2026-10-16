using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

// Function blocks (category FUNKTION): one real, solid block each, with a visual built at runtime.
// IDs are fixed from 4004 on in the order of Defs below: new blocks are only ever appended, never reordered.
// Enter while aiming at a function block "uses" it (ring the bell, edit a sign's text, rename a teleporter).
public class FunctionBlock : GameObjectBlock {
    public FunctionBlocks.Def def;
    static bool busy;
    public override bool IsSolid() { return true; }
    public override void OnBlockCreate(Vector3i pos, BlockDirection dir) {
        base.OnBlockCreate(pos, dir);
        if(def == null || def.part || busy) return;
        // taller blocks (door, info column) fill the cells above with hidden part blocks
        busy = true;
        try {
            for(int i = 1; i < def.height; i++) {
                Vector3i p = pos + new Vector3i(0, i, 0);
                if(Map.instance == null || !Map.instance.GetBlock(p).IsEmpty()) break;
                var part = new DataBlock(BlockSet.instance[FunctionBlocks.PartId], dir);
                Map.instance.SetBlockAndRebuild(p, part); InfiniteWorldSave.RecordPlayerEdit(p, part);
            }
        } finally { busy = false; }
        if(def.askText != null) FunctionBlockUI.AskText(def.askTitle, def.askText, "", t => FunctionBlockData.Set(pos, string.IsNullOrEmpty(t) ? def.askText : t));
        if(def.placed != null) def.placed(pos, dir);
    }
    public override void OnBlockDestroy(Vector3i pos) {
        base.OnBlockDestroy(pos);
        FunctionBlockData.Remove(pos);
        if(def == null || busy || Map.instance == null) return;
        busy = true;
        try {
            // find the bottom of a multi-cell block and remove all of it
            Vector3i bottom = pos;
            if(def.part) { while((Map.instance.GetBlock(bottom - new Vector3i(0, 1, 0)).block as FunctionBlock) != null && ((FunctionBlock)Map.instance.GetBlock(bottom - new Vector3i(0, 1, 0)).block).def.part) bottom -= new Vector3i(0, 1, 0); bottom -= new Vector3i(0, 1, 0); }
            var air = new DataBlock();
            var baseBlock = Map.instance.GetBlock(bottom).block as FunctionBlock;
            if(baseBlock == null || baseBlock.def.part) return;
            if(!bottom.Equals(pos)) { Map.instance.SetBlockAndRebuild(bottom, air); InfiniteWorldSave.RecordPlayerEdit(bottom, air); FunctionBlockData.Remove(bottom); }
            for(int i = 1; i < baseBlock.def.height; i++) {
                Vector3i p = bottom + new Vector3i(0, i, 0);
                if(p.Equals(pos)) continue;
                var fb = Map.instance.GetBlock(p).block as FunctionBlock;
                if(fb == null || !fb.def.part) break;
                Map.instance.SetBlockAndRebuild(p, air); InfiniteWorldSave.RecordPlayerEdit(p, air);
            }
        } finally { busy = false; }
    }
}

public static partial class FunctionBlocks {
    public const int FirstId = 4004;
    public sealed class Def {
        public string name; public Type component; public Func<Texture2D> icon;
        public string askTitle, askText;          // blocks that ask for a text when placed (sign, teleporter)
        public int height = 1;                    // cells it fills upwards (door 2, info column 3)
        public bool part;                         // hidden upper part of a taller block
        public Action<Vector3i> use;              // Enter while aiming at it
        public Action<Vector3i, BlockDirection> placed;
    }
    public static int PartId { get { return FirstId + 5; } }
    // PERMANENT ORDER: index i has block ID FirstId + i.
    public static readonly Def[] Defs = {
        new Def { name = "Glocke", component = typeof(FBBell), icon = () => FBIcons.Bell() },
        new Def { name = "Laterne (Abendlicht)", component = typeof(FBLamp), icon = () => FBIcons.Lamp(), use = FunctionBlocks.CycleLamp },
        new Def { name = "Leuchtfeuer", component = typeof(FBBeacon), icon = () => FBIcons.Beacon() },
        new Def { name = "Schild mit Text", component = typeof(FBTextSign), icon = () => FBIcons.Sign(), askTitle = "SCHILD BESCHRIFTEN", askText = "Willkommen" },
        new Def { name = "Teleporter", component = typeof(FBTeleporter), icon = () => FBIcons.Teleporter(), askTitle = "TELEPORTER BENENNEN  •  gleicher Name = verbunden", askText = "Teleporter" },
        new Def { name = "Funktionsblock (Teil)", component = typeof(FBNothing), icon = () => null, part = true },
        new Def { name = "Wetterstation", component = typeof(FBWeatherStation), icon = () => FBIcons2.WeatherStation() },
        new Def { name = "Weltzeituhren", component = typeof(FBWorldClocks), icon = () => FBIcons2.Panel(new Color(.15f, .2f, .35f)) },
        new Def { name = "Mondphase", component = typeof(FBMoon), icon = () => FBIcons2.Moon() },
        new Def { name = "Gezeitentafel Block Island", component = typeof(FBTides), icon = () => FBIcons2.Panel(new Color(.1f, .3f, .5f)) },
        new Def { name = "Feuerwerk", component = typeof(FBFireworks), icon = () => FBIcons2.Fireworks(), use = FBFireworks.LaunchAt },
        new Def { name = "Aufzug", component = typeof(FBElevator), icon = () => FBIcons2.Stripes(new Color(.9f, .75f, .1f)) },
        new Def { name = "Förderband", component = typeof(FBConveyor), icon = () => FBIcons2.Stripes(new Color(.35f, .35f, .38f)) },
        new Def { name = "Sprungfeder", component = typeof(FBSpring), icon = () => FBIcons2.Spring() },
        new Def { name = "Tür (automatisch)", component = typeof(FBDoor), icon = () => FBIcons2.Door(), height = 2 },
        new Def { name = "Minecart-Bahnhof", component = typeof(FBStation), icon = () => FBIcons2.Station() },
        new Def { name = "Weiche (Minecart)", component = typeof(FBSwitch), icon = () => FBIcons2.Switch(), use = FBSwitch.ToggleAt },
        new Def { name = "Musikbox", component = typeof(FBJukebox), icon = () => FBIcons2.Jukebox(), use = FBJukebox.NextAt },
        new Def { name = "Naturklang", component = typeof(FBNatureSound), icon = () => FBIcons2.Nature(), use = FBNatureSound.CycleAt },
        new Def { name = "Wegpunkt", component = typeof(FBWaypoint), icon = () => FBIcons2.Flag(), askTitle = "WEGPUNKT BENENNEN  •  Taste N zeigt alle Wegpunkte", askText = "Wegpunkt" },
        new Def { name = "Bauwerk-Spawner", component = typeof(FBSpawner), icon = () => FBIcons2.Spawner(), placed = FBSpawner.Placed, use = (p) => FBSpawner.Placed(p, BlockDirection.FORWARD) },
        new Def { name = "Frost-Brunnen", component = typeof(FBFountain), icon = () => FBIcons2.Fountain() },
        new Def { name = "Info-Säule (eigene Bilder)", component = typeof(FBPictureColumn), icon = () => FBIcons2.Column(), height = 3 },
    };

    static GameObject templateRoot;
    static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();

    // Called by BlockSet.Awake after the info sign (4002/4003).
    public static void Ensure(BlockSet set) {
        List<Block> blocks = set.GetBlocks();
        if(blocks.Count != FirstId) { Debug.LogError("FUNCTION BLOCKS: BlockSet has " + blocks.Count + " blocks, expected " + FirstId); return; }
        if(templateRoot == null) { templateRoot = new GameObject("Function Block Templates"); templateRoot.SetActive(false); if(Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(templateRoot); }
        foreach(Def d in Defs) {
            GameObject t;
            if(!templates.TryGetValue(d.name, out t) || t == null) {
                t = new GameObject(d.name); t.transform.SetParent(templateRoot.transform, false); t.AddComponent(d.component);
                templates[d.name] = t;
            }
            FunctionBlock b = ScriptableObject.CreateInstance<FunctionBlock>();
            b.name = d.name; b.hideFlags = HideFlags.DontSave; b.def = d; b.gameObject = t; b.icon = d.icon(); b.retired = d.part;
            blocks.Add(b);
        }
    }

    // ------------------------------------------------------------------ shared helpers for the visuals
    static Shader diffuse, glow, beam;
    // Surfaces made by the game itself (no image files): wood grain, stone, brushed metal.
    public enum Surface { Plain, Wood, Stone, Metal }
    static readonly Dictionary<Surface, Texture2D> surfaces = new Dictionary<Surface, Texture2D>();
    public static Material Solid(Color c, Surface s = Surface.Plain) {
        if(diffuse == null) diffuse = Shader.Find("Legacy Shaders/Diffuse");
        var m = new Material(diffuse) { color = s == Surface.Plain ? c : c * 1.25f };
        if(s != Surface.Plain) m.mainTexture = SurfaceTexture(s);
        return m;
    }
    static Texture2D SurfaceTexture(Surface s) {
        Texture2D t; if(surfaces.TryGetValue(s, out t) && t != null) return t;
        const int n = 128; t = new Texture2D(n, n, TextureFormat.RGB24, true) { name = s + " Surface", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 2 };
        var px = new Color[n * n]; float seed = (int)s * 17.3f;
        for(int y = 0; y < n; y++) for(int x = 0; x < n; x++) {
            float u = x / (float)n, v = y / (float)n, g;
            if(s == Surface.Wood) {
                float warp = Mathf.PerlinNoise(u * 3 + seed, v * 1.2f) * 2.5f;
                float ring = Mathf.Repeat(u * 9f + warp, 1f); g = .72f + .18f * Mathf.SmoothStep(0, 1, Mathf.Abs(ring - .5f) * 2) + (Mathf.PerlinNoise(u * 40, v * 3 + seed) - .5f) * .12f;
            } else if(s == Surface.Stone) {
                g = .62f + (Mathf.PerlinNoise(u * 6 + seed, v * 6) - .5f) * .3f + (Mathf.PerlinNoise(u * 24, v * 24 + seed) - .5f) * .18f + (Mathf.PerlinNoise(u * 70 + seed, v * 70) - .5f) * .1f;
                if(x % 64 < 2 || (y + (x / 64) * 32) % 64 < 2) g *= .7f;      // joints of a block pattern
            } else {
                g = .78f + (Mathf.PerlinNoise(u * 2 + seed, v * 90) - .5f) * .16f + (Mathf.PerlinNoise(u * 6, v * 200 + seed) - .5f) * .08f;
            }
            px[y * n + x] = new Color(g, g, g);
        }
        t.SetPixels(px); t.Apply(true); surfaces[s] = t; return t;
    }
    public static Material Glow(Color c, float intensity) {
        if(glow == null) glow = Resources.Load<Shader>("FunctionBlocks/Glow");
        var m = new Material(glow); m.SetColor("_Color", c); m.SetFloat("_Intensity", intensity); return m;
    }
    public static Material Beam(Color c) { if(beam == null) beam = Resources.Load<Shader>("FunctionBlocks/LightBeam"); var m = new Material(beam); m.SetColor("_Color", c); return m; }
    public static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null) {
        var g = GameObject.CreatePrimitive(type); UnityEngine.Object.Destroy(g.GetComponent<Collider>());
        g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale;
        if(rot.HasValue) g.transform.localRotation = rot.Value;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }
    public static bool IsPlaced(Transform t) { return t.parent != null && t.parent.name == "InfiniteSpecialBlocks"; }
    public static Vector3i CellOf(Transform t) { Vector3 p = t.position; return new Vector3i(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z)); }

    // dark evenings and bad weather switch lights on
    public static bool IsDark {
        get {
            TOD_Sky sky = TOD_Sky.Instance;
            bool night = sky != null && (sky.IsNight || sky.SunZenith > 86f);
            var w = InfiniteCreativeWeather.Instance;
            bool gloomy = w != null && VoxelBoxSettings.WeatherEffects && (w.current == InfiniteCreativeWeather.Kind.Rain || w.current == InfiniteCreativeWeather.Kind.Thunderstorm || w.current == InfiniteCreativeWeather.Kind.Blizzard);
            return night || gloomy;
        }
    }
}

// Text values of function blocks per world (sign texts, teleporter names): Documents/Block Island/Funktionsbloecke/<world>.txt
public static class FunctionBlockData {
    public static int Version { get; private set; }
    static string world; static Dictionary<string, string> values;
    static string Key(Vector3i p) { return p.x + " " + p.y + " " + p.z; }
    static string FilePath { get { return Path.Combine(BlockIslandPaths.Folder("Funktionsbloecke"), InfiniteWorldSave.SanitizeName(world) + ".txt"); } }
    static void Load() {
        string w = InfiniteWorldSave.CurrentWorldName ?? "";
        if(values != null && w == world) return;
        world = w; values = new Dictionary<string, string>();
        try { if(File.Exists(FilePath)) foreach(string l in File.ReadAllLines(FilePath)) { int t = l.IndexOf('\t'); if(t > 0) values[l.Substring(0, t)] = l.Substring(t + 1); } }
        catch(Exception e) { Debug.LogWarning("FUNCTION BLOCKS: data not readable: " + e.Message); }
    }
    static void Save() {
        try { var lines = new List<string>(); foreach(var kv in values) lines.Add(kv.Key + "\t" + kv.Value.Replace("\t", " ").Replace("\n", " ")); File.WriteAllLines(FilePath, lines.ToArray()); }
        catch(Exception e) { Debug.LogWarning("FUNCTION BLOCKS: data not writable: " + e.Message); }
    }
    public static string Get(Vector3i p, string fallback) { Load(); string v; return values.TryGetValue(Key(p), out v) ? v : fallback; }
    public static void Set(Vector3i p, string v) { Load(); values[Key(p)] = v; Save(); Version++; }
    public static void Remove(Vector3i p) { Load(); if(values.Remove(Key(p))) { Save(); Version++; } }
    public static List<KeyValuePair<Vector3i, string>> All() {
        Load(); var r = new List<KeyValuePair<Vector3i, string>>();
        foreach(var kv in values) { string[] f = kv.Key.Split(' '); int x, y, z; if(f.Length == 3 && int.TryParse(f[0], out x) && int.TryParse(f[1], out y) && int.TryParse(f[2], out z)) r.Add(new KeyValuePair<Vector3i, string>(new Vector3i(x, y, z), kv.Value)); }
        return r;
    }
}

// Text dialog, short messages and the "use" key (Enter on the block under the crosshair).
public sealed class FunctionBlockUI : MonoBehaviour {
    static FunctionBlockUI instance;
    static FunctionBlockUI Get() { if(instance == null) { var go = new GameObject("Function Block UI"); DontDestroyOnLoad(go); instance = go.AddComponent<FunctionBlockUI>(); } return instance; }
    public static void Ensure() { Get(); }

    bool asking; string title, text; Action<string> done; bool focus;
    string toast; float toastUntil;

    public static void AskText(string title, string initial, string unused, Action<string> onDone) {
        var ui = Get(); ui.asking = true; ui.title = title; ui.text = initial; ui.done = onDone; ui.focus = true;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
    }
    public static void Toast(string message) { var ui = Get(); ui.toast = message; ui.toastUntil = Time.unscaledTime + 3.5f; }
    public static bool Busy { get { return instance != null && (instance.asking || instance.choosing); } }

    // list of choices (waypoints, structures)
    bool choosing; string[] options; Action<int> chosen; Vector2 scroll;
    public static void Choose(string title, string[] options, Action<int> onChosen) {
        var ui = Get(); ui.choosing = true; ui.title = title; ui.options = options; ui.chosen = onChosen; ui.scroll = Vector2.zero;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
    }
    void CloseChoice(int index) {
        choosing = false; Action<int> c = chosen; chosen = null;
        Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
        if(index >= 0 && c != null) c(index);
    }

    void Close(bool ok) {
        asking = false; Action<string> d = done; done = null;
        Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
        if(ok && d != null) d(text.Trim());
    }

    void Update() {
        if(asking || choosing || Map.instance == null || !GameState.IsPlaying || Cursor.visible) return;
        if(Input.GetKeyDown(KeyCode.N)) { FBWaypoint.ShowList(); return; }
        if(!(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) return;
        Camera cam = Camera.main; if(cam == null) return;
        Vector3i pos, prev;
        if(!MapRayIntersection.Raycast(Map.instance, new Ray(cam.transform.position, cam.transform.forward), 8, out pos, out prev)) return;
        Block hit = Map.instance.GetBlock(pos).block;
        if(FunctionBlocks.IsStreetLantern(hit)) { FunctionBlocks.CycleLamp(pos); return; }
        var fb = hit as FunctionBlock;
        if(fb == null || fb.def == null) return;
        if(fb.def.part) { Vector3i b = pos; while(true) { var below = Map.instance.GetBlock(b - new Vector3i(0, 1, 0)).block as FunctionBlock; b -= new Vector3i(0, 1, 0); if(below == null || !below.def.part) { fb = below; pos = b; break; } } if(fb == null) return; }
        if(fb.def.use != null) { fb.def.use(pos); return; }
        if(fb.def.askText != null) {
            Vector3i p = pos;
            AskText(fb.def.askTitle, FunctionBlockData.Get(p, fb.def.askText), "", t => FunctionBlockData.Set(p, string.IsNullOrEmpty(t) ? fb.def.askText : t));
        } else if(fb.def.component == typeof(FBBell)) FBBell.RingAt(pos);
    }

    void OnGUI() {
        GUI.depth = -500;
        if(toast != null && Time.unscaledTime < toastUntil) {
            VoxelBoxUI.BeginResponsive();
            GUILayout.BeginArea(new Rect(VoxelBoxUI.RefW * .5f - 320, VoxelBoxUI.RefH - 170, 640, 60), VoxelBoxUI.Panel);
            GUILayout.Label(toast, VoxelBoxUI.Label); GUILayout.EndArea();
            VoxelBoxUI.EndResponsive();
        }
        if(choosing) {
            VoxelBoxUI.BeginResponsive();
            Rect cr = VoxelBoxUI.Center(620, 560);
            GUILayout.BeginArea(cr, VoxelBoxUI.Panel);
            GUILayout.Label(title, VoxelBoxUI.Header);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            for(int i = 0; i < options.Length; i++) if(GUILayout.Button(options[i], VoxelBoxUI.Button, GUILayout.Height(40))) { CloseChoice(i); break; }
            GUILayout.EndScrollView();
            if(GUILayout.Button("SCHLIESSEN", VoxelBoxUI.Danger, GUILayout.Height(42))) CloseChoice(-1);
            GUILayout.EndArea();
            if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { CloseChoice(-1); Event.current.Use(); }
            VoxelBoxUI.EndResponsive();
            return;
        }
        if(!asking) return;
        VoxelBoxUI.BeginResponsive();
        Rect r = VoxelBoxUI.Center(620, 210);
        GUILayout.BeginArea(r, VoxelBoxUI.Panel);
        GUILayout.Label(title, VoxelBoxUI.Header);
        GUI.SetNextControlName("FunctionBlockText");
        text = GUILayout.TextField(text ?? "", 60, GUILayout.Height(40));
        if(focus) { GUI.FocusControl("FunctionBlockText"); focus = false; }
        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("ABBRECHEN", VoxelBoxUI.Button, GUILayout.Height(44))) Close(false);
        if(GUILayout.Button("ÜBERNEHMEN", VoxelBoxUI.Button, GUILayout.Height(44))) Close(true);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Event e = Event.current;
        if(e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) { Close(true); e.Use(); }
        else if(e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { Close(false); e.Use(); }
        VoxelBoxUI.EndResponsive();
    }
}

// ====================================================================== Glocke
// Hanging bronze bell in a wooden frame. Strikes the full hours of the real Central European time
// (1-12 strokes) and rings once on Enter. Heard within about 80 blocks.
public sealed class FBBell : MonoBehaviour {
    static readonly List<FBBell> all = new List<FBBell>();
    static AudioClip clip;
    Transform bell; AudioSource audioSrc; int lastHour = -1; int strokesLeft; float nextStroke, swing;
    Vector3i cell;

    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        Material wood = FunctionBlocks.Solid(new Color(.36f, .24f, .15f), FunctionBlocks.Surface.Wood), bronze = FunctionBlocks.Solid(new Color(.72f, .52f, .24f), FunctionBlocks.Surface.Metal);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Post L", new Vector3(-.42f, 0, 0), new Vector3(.12f, 1f, .12f), wood);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Post R", new Vector3(.42f, 0, 0), new Vector3(.12f, 1f, .12f), wood);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Beam", new Vector3(0, .44f, 0), new Vector3(.96f, .12f, .14f), wood);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Base", new Vector3(0, -.46f, 0), new Vector3(.96f, .08f, .4f), wood);
        bell = new GameObject("Bell").transform; bell.SetParent(transform, false); bell.localPosition = new Vector3(0, .38f, 0);
        FunctionBlocks.Part(bell, PrimitiveType.Sphere, "Crown", new Vector3(0, -.14f, 0), new Vector3(.34f, .3f, .34f), bronze);
        FunctionBlocks.Part(bell, PrimitiveType.Cylinder, "Waist", new Vector3(0, -.3f, 0), new Vector3(.4f, .14f, .4f), bronze);
        FunctionBlocks.Part(bell, PrimitiveType.Cylinder, "Lip", new Vector3(0, -.44f, 0), new Vector3(.52f, .03f, .52f), bronze);
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.spatialBlend = 1f; audioSrc.rolloffMode = AudioRolloffMode.Linear; audioSrc.minDistance = 6f; audioSrc.maxDistance = 80f; audioSrc.playOnAwake = false; audioSrc.volume = .8f;
        audioSrc.clip = Clip();
        lastHour = BlockIslandRealTime.CentralEurope.Hour;   // no strokes for the hour that has already begun
        all.Add(this);
        FunctionBlockUI.Ensure();
    }
    void OnDestroy() { all.Remove(this); }

    public static void RingAt(Vector3i p) { foreach(FBBell b in all) if(b.cell.Equals(p)) b.Strike(1); }
    void Strike(int n) { strokesLeft += n; if(Time.time > nextStroke) nextStroke = Time.time; }

    void Update() {
        if(audioSrc == null) return;
        DateTime now = BlockIslandRealTime.CentralEurope;
        if(now.Hour != lastHour && now.Minute == 0) { lastHour = now.Hour; int h = now.Hour % 12; Strike(h == 0 ? 12 : h); }
        if(strokesLeft > 0 && Time.time >= nextStroke) { strokesLeft--; nextStroke = Time.time + 2.2f; audioSrc.pitch = UnityEngine.Random.Range(.98f, 1.02f); audioSrc.PlayOneShot(audioSrc.clip); swing = 1f; }
        swing = Mathf.MoveTowards(swing, 0, Time.deltaTime * .45f);
        if(bell != null) bell.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 5.5f) * 22f * swing);
    }

    // A bronze bell synthesised once: hum, prime, minor third, fifth and nominal partials with slow decays.
    static AudioClip Clip() {
        if(clip != null) return clip;
        const int rate = 44100; const float len = 4.5f, f0 = 330f;
        float[] ratio = { .5f, 1f, 1.183f, 1.506f, 2f, 2.514f, 2.662f, 3.011f };
        float[] amp = { .55f, .8f, .45f, .25f, .5f, .2f, .15f, .12f };
        float[] decay = { .9f, 1.3f, 1.6f, 2.2f, 2.6f, 3.5f, 4f, 4.5f };
        int n = (int)(rate * len); var data = new float[n]; float max = 0;
        for(int i = 0; i < n; i++) {
            float t = i / (float)rate, s = 0;
            for(int k = 0; k < ratio.Length; k++) s += amp[k] * Mathf.Exp(-t * decay[k]) * Mathf.Sin(2 * Mathf.PI * f0 * ratio[k] * t);
            s *= Mathf.Min(1f, t * 400f);                 // soft strike, no click
            data[i] = s; max = Mathf.Max(max, Mathf.Abs(s));
        }
        for(int i = 0; i < n; i++) data[i] = data[i] / max * .9f;
        clip = AudioClip.Create("Bell", n, 1, rate, false); clip.SetData(data, 0);
        return clip;
    }
}

// ====================================================================== Laterne (Abendlicht)
// Street lantern on a post. Switches itself on at dusk, at night and in rain, thunderstorm or snowstorm,
// each lantern a few seconds apart like a real street.
public sealed class FBLamp : MonoBehaviour {
    Light lamp; Material glass; float level, delay;
    static readonly Color Warm = new Color(1f, .78f, .45f);

    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        Material iron = FunctionBlocks.Solid(new Color(.12f, .12f, .13f), FunctionBlocks.Surface.Metal);
        glass = FunctionBlocks.Glow(Warm, .25f);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Foot", new Vector3(0, -.46f, 0), new Vector3(.3f, .04f, .3f), iron);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Post", new Vector3(0, -.12f, 0), new Vector3(.08f, .32f, .08f), iron);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Glass", new Vector3(0, .27f, 0), new Vector3(.26f, .3f, .26f), glass);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Bottom", new Vector3(0, .1f, 0), new Vector3(.32f, .04f, .32f), iron);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Roof", new Vector3(0, .44f, 0), new Vector3(.36f, .05f, .36f), iron);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Tip", new Vector3(0, .49f, 0), new Vector3(.1f, .04f, .1f), iron);
        for(int i = 0; i < 4; i++) { float x = (i % 2 == 0 ? -1 : 1) * .135f, z = (i < 2 ? -1 : 1) * .135f; FunctionBlocks.Part(transform, PrimitiveType.Cube, "Bar", new Vector3(x, .27f, z), new Vector3(.03f, .32f, .03f), iron); }
        var lg = new GameObject("Light"); lg.transform.SetParent(transform, false); lg.transform.localPosition = new Vector3(0, .27f, 0);
        lamp = lg.AddComponent<Light>(); lamp.type = LightType.Point; lamp.color = Warm; lamp.range = 10f; lamp.intensity = 0f; lamp.shadows = LightShadows.None; lamp.enabled = false;
        delay = Mathf.Abs((transform.position.x * 7.13f + transform.position.z * 3.71f) % 6f);
    }
    float switchAt = -1; bool want; Vector3i cell; float strength = 1f, nextLevel;
    void Update() {
        if(lamp == null) return;
        if(Time.unscaledTime >= nextLevel) { nextLevel = Time.unscaledTime + .5f; cell = FunctionBlocks.CellOf(transform); strength = FunctionBlocks.LampStrength(cell); }
        bool dark = FunctionBlocks.IsDark && strength > 0f;
        if(dark != want) { want = dark; switchAt = Time.time + delay; }
        float target = (Time.time >= switchAt ? want : !want) ? 1f : 0f;
        level = Mathf.MoveTowards(level, target, Time.deltaTime * .8f);
        lamp.enabled = level > .01f; lamp.intensity = 1.4f * level * Mathf.Min(strength, 1.6f); lamp.range = 10f * Mathf.Max(.6f, strength);
        glass.SetFloat("_Intensity", .25f + 1.6f * level * Mathf.Min(strength, 1.4f));
    }
}

// ====================================================================== Leuchtfeuer
// Lantern room for the top of a lighthouse: a rotating beam (two light cones and a spot light) that shines at dusk,
// at night and in fog, rain or snow.
public sealed class FBBeacon : MonoBehaviour {
    Transform rotor; Material lens; Material beamMat; Light spot, glowLight; float level;
    static readonly Color BeamColor = new Color(1f, .93f, .75f);
    static Mesh cone;

    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        Material frame = FunctionBlocks.Solid(new Color(.18f, .2f, .22f), FunctionBlocks.Surface.Metal), red = FunctionBlocks.Solid(new Color(.6f, .12f, .1f));
        lens = FunctionBlocks.Glow(BeamColor, .3f);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Floor", new Vector3(0, -.45f, 0), new Vector3(.95f, .05f, .95f), frame);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Lens", new Vector3(0, -.05f, 0), new Vector3(.36f, .3f, .36f), lens);
        for(int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; FunctionBlocks.Part(transform, PrimitiveType.Cube, "Bar", new Vector3(Mathf.Cos(a) * .42f, -.05f, Mathf.Sin(a) * .42f), new Vector3(.04f, .78f, .04f), frame); }
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Roof", new Vector3(0, .38f, 0), new Vector3(.95f, .06f, .95f), red);
        FunctionBlocks.Part(transform, PrimitiveType.Sphere, "Dome", new Vector3(0, .44f, 0), new Vector3(.6f, .22f, .6f), red);
        rotor = new GameObject("Rotor").transform; rotor.SetParent(transform, false); rotor.localPosition = new Vector3(0, -.05f, 0);
        beamMat = FunctionBlocks.Beam(BeamColor);
        for(int s = 0; s < 2; s++) {
            var b = new GameObject("Beam"); b.transform.SetParent(rotor, false); b.transform.localRotation = Quaternion.Euler(0, s * 180f, 0);
            b.AddComponent<MeshFilter>().sharedMesh = Cone(); var r = b.AddComponent<MeshRenderer>(); r.sharedMaterial = beamMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }
        var sg = new GameObject("Spot"); sg.transform.SetParent(rotor, false);
        spot = sg.AddComponent<Light>(); spot.type = LightType.Spot; spot.range = 45f; spot.spotAngle = 14f; spot.color = BeamColor; spot.shadows = LightShadows.None; spot.intensity = 0; spot.enabled = false;
        var gg = new GameObject("Glow"); gg.transform.SetParent(transform, false);
        glowLight = gg.AddComponent<Light>(); glowLight.type = LightType.Point; glowLight.range = 6f; glowLight.color = BeamColor; glowLight.shadows = LightShadows.None; glowLight.enabled = false;
    }

    // a long cone along +z: bright at the lamp, fading out at 40 blocks
    static Mesh Cone() {
        if(cone != null) return cone;
        const int seg = 18; const float len = 40f, r0 = .15f, r1 = 3.2f;
        var v = new List<Vector3>(); var c = new List<Color>(); var n = new List<Vector3>(); var tri = new List<int>();
        for(int i = 0; i <= seg; i++) {
            float a = i / (float)seg * Mathf.PI * 2; Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            v.Add(dir * r0); c.Add(new Color(1, 1, 1, 1)); n.Add(dir);
            v.Add(dir * r1 + Vector3.forward * len); c.Add(new Color(1, 1, 1, 0)); n.Add(dir);
        }
        for(int i = 0; i < seg; i++) { int a = i * 2; tri.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 }); }
        cone = new Mesh { name = "Beacon Beam" }; cone.SetVertices(v); cone.SetColors(c); cone.SetNormals(n); cone.SetTriangles(tri, 0); cone.RecalculateBounds();
        return cone;
    }

    void Update() {
        if(rotor == null) return;
        var w = InfiniteCreativeWeather.Instance;
        bool misty = w != null && VoxelBoxSettings.WeatherEffects && w.current != InfiniteCreativeWeather.Kind.Sun;
        level = Mathf.MoveTowards(level, FunctionBlocks.IsDark || misty ? 1f : 0f, Time.deltaTime * .5f);
        rotor.Rotate(0, 36f * Time.deltaTime, 0, Space.Self);
        rotor.gameObject.SetActive(level > .01f);
        beamMat.SetFloat("_Intensity", .38f * level);
        spot.enabled = glowLight.enabled = level > .01f; spot.intensity = 2.2f * level; glowLight.intensity = 1.2f * level;
        lens.SetFloat("_Intensity", .3f + 1.8f * level);
    }
}

// ====================================================================== Schild mit Text
// A full block with a framed plate on its front and back. The text is asked for when placing; Enter while aiming at it edits it.
public sealed class FBTextSign : MonoBehaviour {
    readonly List<TextMesh> texts = new List<TextMesh>();
    Vector3i cell; int shown = -1;
    static Font font; static Material textMat;

    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        cell = FunctionBlocks.CellOf(transform);
        if(font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(textMat == null) { Shader ts = Resources.Load<Shader>("Portal/SignText"); textMat = ts != null ? new Material(ts) { mainTexture = font.material.mainTexture } : font.material; }
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Body", Vector3.zero, Vector3.one, FunctionBlocks.Solid(new Color(.33f, .23f, .15f), FunctionBlocks.Surface.Wood));
        for(int side = 0; side < 2; side++) {
            float z = (side == 0 ? -1 : 1) * .505f;
            var face = FunctionBlocks.Part(transform, PrimitiveType.Cube, "Plate", new Vector3(0, 0, z), new Vector3(.86f, .62f, .004f), FunctionBlocks.Solid(new Color(.95f, .9f, .78f)));
            face.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var g = new GameObject("Text"); g.transform.SetParent(transform, false); g.transform.localPosition = new Vector3(0, 0, z * 1.01f);
            g.transform.localRotation = side == 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            var t = g.AddComponent<TextMesh>(); t.font = font; t.fontSize = 96; t.characterSize = .16f / 9.6f; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = new Color(.18f, .12f, .08f);
            var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = textMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.AddComponent<SignTextFit>().Init(t.characterSize, .78f);
            texts.Add(t);
        }
        FunctionBlockUI.Ensure();
    }
    void Update() {
        if(texts.Count == 0 || shown == FunctionBlockData.Version) return;
        shown = FunctionBlockData.Version;
        string s = Wrap(FunctionBlockData.Get(cell, "Willkommen"), 16);
        foreach(TextMesh t in texts) t.text = s;
    }
    // up to three lines, broken at spaces
    static string Wrap(string s, int width) {
        var words = s.Split(' '); var lines = new List<string>(); string cur = "";
        foreach(string w in words) { if(cur.Length > 0 && cur.Length + 1 + w.Length > width) { lines.Add(cur); cur = w; } else cur = cur.Length > 0 ? cur + " " + w : w; }
        if(cur.Length > 0) lines.Add(cur);
        if(lines.Count > 3) { lines = lines.GetRange(0, 3); lines[2] += " …"; }
        return string.Join("\n", lines.ToArray());
    }
}

// ====================================================================== Teleporter
// A glowing pad. All teleporters of a world with the same name are connected: stand on one for a moment and you are
// taken to the next one with that name (with a short fade). Names are asked for when placing; Enter renames.
public sealed class FBTeleporter : MonoBehaviour {
    public static readonly List<FBTeleporter> All = new List<FBTeleporter>();
    public Vector3i Cell { get; private set; }
    static Vector3i? arrivedOn; static float standSince = -1;
    TextMesh label; int shown = -1; Material ring; ParticleSystem sparks;
    static Font font; static Material textMat;

    void Start() {
        if(!FunctionBlocks.IsPlaced(transform)) return;
        Cell = FunctionBlocks.CellOf(transform);
        Material stone = FunctionBlocks.Solid(new Color(.25f, .26f, .3f), FunctionBlocks.Surface.Stone);
        ring = FunctionBlocks.Glow(new Color(.4f, .8f, 1f), 1.4f);
        FunctionBlocks.Part(transform, PrimitiveType.Cube, "Base", Vector3.zero, new Vector3(1f, 1f, 1f), stone);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Ring", new Vector3(0, .505f, 0), new Vector3(.8f, .006f, .8f), ring);
        FunctionBlocks.Part(transform, PrimitiveType.Cylinder, "Inner", new Vector3(0, .51f, 0), new Vector3(.6f, .006f, .6f), FunctionBlocks.Solid(new Color(.1f, .12f, .18f)));
        // rising sparks
        var pg = new GameObject("Sparks"); pg.transform.SetParent(transform, false); pg.transform.localPosition = new Vector3(0, .52f, 0); pg.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        sparks = pg.AddComponent<ParticleSystem>(); sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = sparks.main; m.startLifetime = 1.6f; m.startSpeed = .7f; m.startSize = new ParticleSystem.MinMaxCurve(.03f, .07f); m.maxParticles = 80; m.simulationSpace = ParticleSystemSimulationSpace.World; m.startColor = new Color(.6f, .9f, 1f, .8f);
        var sh = sparks.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = .35f; sh.radiusThickness = .1f;
        var em = sparks.emission; em.rateOverTime = 18f;
        var stars = Resources.Load<Material>("Portal/PortalStars"); if(stars != null) pg.GetComponent<ParticleSystemRenderer>().sharedMaterial = stars;
        sparks.Play();
        // name floating above the pad
        if(font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(textMat == null) { Shader ts = Resources.Load<Shader>("Portal/SignText"); textMat = ts != null ? new Material(ts) { mainTexture = font.material.mainTexture } : font.material; }
        var lg = new GameObject("Name"); lg.transform.SetParent(transform, false); lg.transform.localPosition = new Vector3(0, 1.6f, 0);
        label = lg.AddComponent<TextMesh>(); label.font = font; label.fontSize = 96; label.characterSize = .13f / 9.6f; label.anchor = TextAnchor.MiddleCenter; label.color = new Color(.55f, .9f, 1f);
        lg.GetComponent<MeshRenderer>().sharedMaterial = textMat;
        All.Add(this);
        FunctionBlockUI.Ensure();
    }
    void OnDestroy() { All.Remove(this); }
    public string Name { get { return FunctionBlockData.Get(Cell, "Teleporter"); } }

    void Update() {
        if(label == null) return;
        if(shown != FunctionBlockData.Version) { shown = FunctionBlockData.Version; label.text = Name; }
        Camera cam = Camera.main; if(cam != null) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
        GameObject pl = GameObject.FindWithTag("Player"); if(pl == null || PortalTransition.Busy || FBTeleportFade.Busy || GameState.IsPause) return;
        Vector3 p = pl.transform.position;
        bool on = p.x >= Cell.x - .1f && p.x <= Cell.x + 1.1f && p.z >= Cell.z - .1f && p.z <= Cell.z + 1.1f && p.y > Cell.y + .8f && p.y < Cell.y + 3.2f;
        if(!on) { if(arrivedOn.HasValue && arrivedOn.Value.Equals(Cell)) arrivedOn = null; if(standSince >= 0 && standingOn == this) standSince = -1; return; }
        if(arrivedOn.HasValue && arrivedOn.Value.Equals(Cell)) return;          // just arrived here: step off first
        if(standingOn != this) { standingOn = this; standSince = Time.time; }
        if(Time.time - standSince < .7f) return;
        standSince = -1;
        Vector3i? target = NextWithSameName();
        if(!target.HasValue) { FunctionBlockUI.Toast("Kein zweiter Teleporter mit dem Namen „" + Name + "“ in dieser Welt."); arrivedOn = Cell; return; }
        arrivedOn = target.Value;
        FBTeleportFade.Go(pl, new Vector3(target.Value.x + .5f, target.Value.y + 2.05f, target.Value.z + .5f));
    }
    static FBTeleporter standingOn;

    Vector3i? NextWithSameName() {
        string name = Name;
        var same = new List<Vector3i>();
        foreach(var kv in FunctionBlockData.All()) {
            if(kv.Value != name || kv.Key.Equals(Cell)) continue;
            var fb = Map.instance != null ? Map.instance.GetBlock(kv.Key).block as FunctionBlock : null;
            if(fb != null && fb.def != null && fb.def.component == typeof(FBTeleporter)) same.Add(kv.Key);
        }
        if(same.Count == 0) return null;
        // the one following this teleporter in a fixed order, so three or more with one name form a round trip
        same.Add(Cell);
        same.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.z != b.z ? a.z.CompareTo(b.z) : a.y.CompareTo(b.y));
        int i = same.FindIndex(c => c.Equals(Cell));
        return same[(i + 1) % same.Count];
    }
}

// Short fade for teleporting inside a world; waits until the destination is built.
public sealed class FBTeleportFade : MonoBehaviour {
    public static bool Busy { get; private set; }
    float black; static Texture2D tex;
    public static void Go(GameObject player, Vector3 to) {
        if(Busy) return; Busy = true;
        var go = new GameObject("Teleport Fade"); DontDestroyOnLoad(go);
        go.AddComponent<FBTeleportFade>().StartCoroutine(go.GetComponent<FBTeleportFade>().Run(player, to));
    }
    System.Collections.IEnumerator Run(GameObject player, Vector3 to) {
        for(float t = 0; t < .35f; t += Time.unscaledDeltaTime) { black = t / .35f; yield return null; }
        black = 1;
        var motor = player.GetComponent<CharacterMotor>(); bool motorOn = motor != null && motor.enabled; if(motor != null) motor.enabled = false;
        player.transform.position = to;
        float until = Time.unscaledTime + 10f;
        while(Time.unscaledTime < until && !PortalTransition.AreaReady(to)) { player.transform.position = to; yield return null; }
        yield return null;
        if(motor != null && motorOn) motor.enabled = true;
        for(float t = 0; t < .5f; t += Time.unscaledDeltaTime) { black = 1 - t / .5f; yield return null; }
        Busy = false; Destroy(gameObject);
    }
    void OnGUI() {
        if(tex == null) { tex = new Texture2D(1, 1); tex.SetPixel(0, 0, Color.black); tex.Apply(); }
        GUI.depth = -9000; GUI.color = new Color(1, 1, 1, black); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), tex); GUI.color = Color.white;
    }
    void OnDestroy() { Busy = false; }
}

// ====================================================================== icons
static class FBIcons {
    delegate Color Painter(float x, float y);       // x, y from -1 to 1
    static Texture2D Paint(string n, Painter p) {
        const int s = 64; var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, name = n };
        for(int y = 0; y < s; y++) for(int x = 0; x < s; x++) t.SetPixel(x, y, p((x + .5f) / s * 2 - 1, (y + .5f) / s * 2 - 1));
        t.Apply(); return t;
    }
    static readonly Color Clear = new Color(0, 0, 0, 0);
    public static Texture2D Bell() {
        Color wood = new Color(.42f, .28f, .17f), bronze = new Color(.85f, .62f, .28f);
        return Paint("Bell Icon", (x, y) => {
            if(Mathf.Abs(x) > .78f && Mathf.Abs(x) < .92f && y > -.85f && y < .8f) return wood;
            if(y > .66f && y < .8f && Mathf.Abs(x) < .92f) return wood;
            float w = .22f + (.62f - y) * .42f; if(y < .62f && y > -.5f && Mathf.Abs(x) < w) return bronze;
            if(y <= -.5f && y > -.62f && Mathf.Abs(x) < .62f) return bronze * .8f;
            return Clear;
        });
    }
    public static Texture2D Lamp() {
        Color iron = new Color(.15f, .15f, .17f), light = new Color(1f, .82f, .45f);
        return Paint("Lamp Icon", (x, y) => {
            if(Mathf.Abs(x) < .07f && y < .1f && y > -.9f) return iron;
            if(Mathf.Abs(x) < .3f && y > -.92f && y < -.82f) return iron;
            if(Mathf.Abs(x) < .32f && y > .66f && y < .78f) return iron;
            if(Mathf.Abs(x) < .26f && y >= .1f && y <= .66f) return Mathf.Abs(x) > .2f ? iron : light;
            float d = Mathf.Sqrt(x * x + (y - .38f) * (y - .38f)); if(d < .7f) return new Color(1f, .85f, .5f, (.7f - d) * .5f);
            return Clear;
        });
    }
    public static Texture2D Beacon() {
        Color red = new Color(.7f, .15f, .12f), beam = new Color(1f, .93f, .7f);
        return Paint("Beacon Icon", (x, y) => {
            if(y > .25f && y < .45f && Mathf.Abs(x) < .35f - (y - .25f)) return red;
            if(y > -.25f && y <= .25f && Mathf.Abs(x) < .22f) return Mathf.Abs(x) > .17f ? new Color(.2f, .2f, .22f) : beam;
            if(y > -.4f && y <= -.25f && Mathf.Abs(x) < .3f) return new Color(.2f, .2f, .22f);
            if(Mathf.Abs(y) < .03f + Mathf.Abs(x) * .22f && Mathf.Abs(x) > .25f) return new Color(beam.r, beam.g, beam.b, .8f - Mathf.Abs(x) * .6f);
            return Clear;
        });
    }
    public static Texture2D Sign() {
        Color wood = new Color(.38f, .26f, .16f), plate = new Color(.95f, .9f, .78f), ink = new Color(.2f, .13f, .08f);
        return Paint("Sign Icon", (x, y) => {
            if(Mathf.Abs(x) > .9f || Mathf.Abs(y) > .62f) return Clear;
            if(Mathf.Abs(x) > .78f || Mathf.Abs(y) > .5f) return wood;
            if((Mathf.Abs(y - .18f) < .06f && Mathf.Abs(x) < .55f) || (Mathf.Abs(y + .14f) < .06f && Mathf.Abs(x) < .4f)) return ink;
            return plate;
        });
    }
    public static Texture2D Teleporter() {
        Color stone = new Color(.27f, .28f, .33f), glow = new Color(.45f, .85f, 1f);
        return Paint("Teleporter Icon", (x, y) => {
            float r = Mathf.Sqrt(x * x / .9f + (y + .25f) * (y + .25f) / .12f);
            if(y < -.25f && y > -.75f && Mathf.Abs(x) < .9f) return stone;
            if(Mathf.Abs(r - .75f) < .12f) return glow;
            if(y > -.25f && Mathf.Abs(x) < .6f && ((int)((x + 1) * 9) % 3 == 0) && (y * 7 + x * 3) % 1 < .25f) return new Color(.6f, .9f, 1f, .7f - y * .5f);
            return Clear;
        });
    }
}
