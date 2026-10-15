using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Block Island 1.0 migration: automated in-game test of the BlockSet menu (key E).
// Only active when the player is started with:  -bi1inventorytest -bi1out <folder>
// Starts the Block Island world under a separate test save name (real saves stay untouched),
// waits for the world, then waits for a real E key press (sent from outside); writes screenshots
// of every inventory category and a report of all errors/exceptions to <folder>.
public class BlockIsland1InventoryTest : MonoBehaviour {
    const string TestWorldName = "BI1 Migrationstest";
    string outDir;
    readonly List<string> errors = new List<string>();
    readonly List<string> report = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] args = Environment.GetCommandLineArgs();
        if(Array.IndexOf(args, "-bi1inventorytest") < 0) return;
        Application.runInBackground = true;   // keep testing when the window is not in front
        GameObject go = new GameObject("BlockIsland1InventoryTest");
        DontDestroyOnLoad(go);
        BlockIsland1InventoryTest t = go.AddComponent<BlockIsland1InventoryTest>();
        int i = Array.IndexOf(args, "-bi1out");
        t.outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "bi1test");
        Directory.CreateDirectory(t.outDir);
    }

    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string msg, string stack, LogType type) {
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack);
    }

    void Start() { StartCoroutine(Run()); }

    IEnumerator Run() {
        yield return new WaitForSecondsRealtime(2f);
        bool features = Array.IndexOf(Environment.GetCommandLineArgs(), "-bi1features") >= 0;
        if(features) yield return DesignerShot();
        report.Add("Main menu errors before start: " + errors.Count);
        InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland);
        InfiniteWorldSave.CurrentWorldName = TestWorldName;   // never load or overwrite the real Block Island save
        InfiniteWorldSave.LoadRequested = false;
        SceneManager.LoadScene("Game");

        InventoryGUI inv = null;
        float until = Time.realtimeSinceStartup + 60f;
        while((inv = FindAnyObjectByType<InventoryGUI>()) == null && Time.realtimeSinceStartup < until) yield return null;
        if(inv == null) { Fail("InventoryGUI not found in Game scene"); yield break; }
        float settle = Time.realtimeSinceStartup + 25f;
        while(Time.realtimeSinceStartup < settle) { DisableAutosave(); yield return null; }
        report.Add("World: " + InfiniteWorldSave.CurrentWorldType + ", BlockSet blocks: " + (BlockSet.instance != null ? BlockSet.instance.Count : -1)
            + ", 1.0 slots: " + BlockSet.blockIsland1Slots.Length);
        yield return Shot("1_welt.png");

        FieldInfo show = typeof(InventoryGUI).GetField("show", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo category = typeof(InventoryGUI).GetField("category", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo scroll = typeof(InventoryGUI).GetField("scrollPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo names = typeof(InventoryGUI).GetField("categoryNames", BindingFlags.NonPublic | BindingFlags.Static);

        // Signal readiness, then wait for the real E key press from outside.
        File.WriteAllText(Path.Combine(outDir, "ready_for_e"), "1");
        int errorsBeforeE = errors.Count;
        until = Time.realtimeSinceStartup + 25f;
        while(!(bool)show.GetValue(inv) && Time.realtimeSinceStartup < until) yield return null;
        bool realKey = (bool)show.GetValue(inv);
        if(!realKey) {
            // Fallback: exactly what the E handler in InventoryGUI.Update() does.
            show.SetValue(inv, true);
            typeof(InventoryGUI).GetMethod("FocusSelectedBlock", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(inv, null);
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        }
        report.Add("Menu opened by " + (realKey ? "REAL E key press" : "simulated E (same code path; no OS key permission)"));
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("2_menu_geoeffnet.png");

        string[] cats = (string[])names.GetValue(null);
        for(int c = 0; c < cats.Length; c++) {
            category.SetValue(inv, c); scroll.SetValue(inv, Vector2.zero);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("3_kategorie_" + c + "_" + cats[c] + ".png");
            if(cats[c] == "FIGUREN") {
                scroll.SetValue(inv, new Vector2(0f, 100000f));
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Shot("3_kategorie_" + c + "_" + cats[c] + "_ende.png");
            }
        }
        report.Add("Errors while menu was open: " + (errors.Count - errorsBeforeE));

        show.SetValue(inv, false); Cursor.visible = false; Cursor.lockState = CursorLockMode.Locked;
        yield return new WaitForSecondsRealtime(.5f);
        if(features) {
            yield return FeatureTest();
            report.Add("Total errors/exceptions: " + errors.Count);
            WriteReport();
            Application.Quit();
            yield break;
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(), "-bi1ring") >= 0) {
            yield return RingTest();
            report.Add("Total errors/exceptions: " + errors.Count);
            WriteReport();
            Application.Quit();
            yield break;
        }
        bool quick = Array.IndexOf(Environment.GetCommandLineArgs(), "-bi1quick") >= 0;
        if(!quick) {
            yield return PlaceFigureTest();
            WriteReport();
            yield return RideTest();
            WriteReport();
            yield return WorldFigureAudit();
            WriteReport();
            yield return CloseUps();
            WriteReport();
        }
        yield return HeightMenuTest();

        report.Add("Total errors/exceptions: " + errors.Count);
        WriteReport();
        Application.Quit();
    }

    // Every 1.0 figure/object: exactly one instance, feet on the block floor after animating.
    IEnumerator PlaceFigureTest() {
        GameObject player = GameObject.FindWithTag("Player");
        if(player == null || Map.instance == null) { report.Add("Figure test skipped"); yield break; }
        var figs = new List<GameObjectBlock>();
        foreach(Block b in BlockSet.blockIsland1Slots) {
            GameObjectBlock g = b as GameObjectBlock;
            if(g != null && g.gameObject != null && g.gameObject.GetComponentInChildren<Camera>(true) == null) figs.Add(g);
        }
        Vector3 o = player.transform.position;
        int cols = 12; var placed = new List<KeyValuePair<GameObjectBlock, Vector3i>>(); var before = new List<DataBlock>();
        for(int i = 0; i < figs.Count; i++) {
            Vector3i pos = new Vector3i(Mathf.FloorToInt(o.x) + 6 + (i % cols) * 4, Mathf.FloorToInt(o.y) + 3, Mathf.FloorToInt(o.z) - 22 + (i / cols) * 4);
            before.Add(Map.instance.GetBlock(pos));
            Map.instance.SetBlockAndRebuild(pos, new DataBlock(figs[i], BlockDirection.FORWARD));
            placed.Add(new KeyValuePair<GameObjectBlock, Vector3i>(figs[i], pos));
        }
        yield return new WaitForSecondsRealtime(6f);
        yield return ExactBounds();
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
        int ok = 0, bad = 0, noMesh = 0, missing = 0, doubled = 0, pivot = 0;
        foreach(var kv in placed) {
            string n = kv.Key.gameObject.name + " (Infinite)"; Vector3i pos = kv.Value; GameObject inst = null; int copies = 0;
            foreach(GameObject g in all)
                if(g.name == n && new Vector2(g.transform.position.x - pos.x - .5f, g.transform.position.z - pos.z - .5f).magnitude < .1f) { copies++; inst = g; }
            if(copies == 0) { missing++; report.Add("  MISSING " + kv.Key.name); continue; }
            if(copies > 1) { doubled++; report.Add("  DOUBLE " + kv.Key.name + " x" + copies); }
            if(BlockIsland1Placement.KeepsOriginalSetPoint(kv.Key.gameObject)) { pivot++; report.Add("  1.0 set point kept (animated): " + kv.Key.name + " (" + kv.Key.gameObject.name + ")"); continue; }
            float low = BlockIsland1Placement.LowestPoint(inst);
            if(float.IsNaN(low)) { noMesh++; continue; }
            float off = low - pos.y;
            if(off >= -.05f && off <= .15f) ok++; else { bad++; report.Add("  OFF " + kv.Key.name + " (" + kv.Key.gameObject.name + "): " + off.ToString("0.00")); }
        }
        report.Add("Figures placed: " + figs.Count + " | on floor (-0.05..+0.15): " + ok + " | 1.0 set point kept: " + pivot + " | off: " + bad + " | effects without mesh: " + noMesh + " | missing: " + missing + " | doubled: " + doubled);
        yield return Shot("4_figuren_raster.png");
        for(int i = 0; i < placed.Count; i++) Map.instance.SetBlockAndRebuild(placed[i].Value, before[i]);
        yield return new WaitForSecondsRealtime(1f);
    }

    // Roller coaster: placing it must not take over the view; riding (G) and getting off restore everything.
    IEnumerator RideTest() {
        Block coaster = null;
        foreach(Block b in BlockSet.blockIsland1Slots) {
            GameObjectBlock g = b as GameObjectBlock;
            if(g != null && g.gameObject != null && g.gameObject.GetComponentInChildren<CoasterControllerSWLXR>(true) != null) { coaster = b; break; }
        }
        GameObject player = GameObject.FindWithTag("Player");
        if(coaster == null || player == null) { report.Add("Ride test skipped (coaster block: " + (coaster != null) + ")"); yield break; }
        Camera playerCam = player.GetComponentInChildren<Camera>();
        Vector3 p = player.transform.position + player.transform.forward * 6f;
        Vector3i pos = new Vector3i(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
        DataBlock before = Map.instance.GetBlock(pos);
        Map.instance.SetBlockAndRebuild(pos, new DataBlock(coaster, BlockDirection.FORWARD));
        yield return new WaitForSecondsRealtime(3f);
        int foreign = 0;
        foreach(Camera c in FindObjectsByType<Camera>(FindObjectsInactive.Exclude)) if(c.enabled && c != playerCam && c.transform.root != player.transform && c.targetTexture == null) foreign++;
        report.Add("Coaster '" + coaster.name + "' placed: Camera.main is player camera = " + (Camera.main == playerCam) + ", other active cameras = " + foreign + " (expected True / 0)");
        yield return Shot("5_achterbahn_gesetzt.png");

        BlockIsland1Ride ride = FindAnyObjectByType<BlockIsland1Ride>();
        MethodInfo nearest = typeof(BlockIsland1Ride).GetMethod("Nearest", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo enter = typeof(BlockIsland1Ride).GetMethod("Enter", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo exit = typeof(BlockIsland1Ride).GetMethod("Exit", BindingFlags.NonPublic | BindingFlags.Instance);
        if(ride == null) { report.Add("Ride manager missing"); yield break; }
        int mask = playerCam.cullingMask;
        enter.Invoke(ride, new object[] { nearest.Invoke(ride, null) });   // same as pressing G near the coaster
        yield return new WaitForSecondsRealtime(4f);
        Builder builder = player.GetComponentInChildren<Builder>();
        report.Add("Riding: builder paused = " + (builder != null && !builder.enabled) + ", Camera.main still player = " + (Camera.main == playerCam));
        yield return Shot("6_mitfahren.png");
        exit.Invoke(ride, null);                                           // same as pressing G while riding
        yield return new WaitForSecondsRealtime(1.5f);
        report.Add("Got off: builder active = " + (builder != null && builder.enabled) + ", player view restored = " + (playerCam.cullingMask == mask));
        yield return Shot("7_ausgestiegen.png");
        Map.instance.SetBlockAndRebuild(pos, before);
        yield return new WaitForSecondsRealtime(.5f);
    }

    void WriteReport() {
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
    }

    // In the real 1.0 world: every loaded figure's lowest point vs. the top of the ground below it.
    static readonly Vector3[] AuditSpots = {
        new Vector3(547,81,677), new Vector3(784,101,749), new Vector3(696,90,712), new Vector3(610,131,771),
        new Vector3(640,67,406), new Vector3(793,91,823), new Vector3(683,90,800), new Vector3(691,93,493) };

    IEnumerator WorldFigureAudit() {
        GameObject player = GameObject.FindWithTag("Player");
        if(player == null) yield break;
        var seen = new HashSet<string>();
        var byPrefab = new Dictionary<string, List<float>>();
        int measured = 0, onGround = 0, floating = 0, sunk = 0, overWater = 0, overAir = 0, keptPivot = 0;
        for(int si = 0; si < AuditSpots.Length; si++) {
            player.transform.position = AuditSpots[si];
            yield return new WaitForSecondsRealtime(9f);
            yield return ExactBounds();
            foreach(GameObject g in FindObjectsByType<GameObject>(FindObjectsInactive.Exclude)) {
                if(!g.name.EndsWith(" (Infinite)") || g.transform.parent == null || g.transform.parent.name != "InfiniteSpecialBlocks") continue;
                int x = Mathf.FloorToInt(g.transform.position.x), z = Mathf.FloorToInt(g.transform.position.z);
                if(Mathf.Abs(x - AuditSpots[si].x) > 40 || Mathf.Abs(z - AuditSpots[si].z) > 40) continue;
                // find the figure's own voxel in this column
                int vy = -1; GameObjectBlock gob = null; float best = float.MaxValue;
                for(int y = 0; y < 256; y++) {
                    GameObjectBlock b = Map.instance.GetBlock(new Vector3i(x, y, z)).block as GameObjectBlock;
                    if(b == null || b.gameObject == null || b.gameObject.name + " (Infinite)" != g.name) continue;
                    float d = Mathf.Abs(y + .5f - g.transform.position.y); if(d < best) { best = d; vy = y; gob = b; }
                }
                if(gob == null || !BlockIsland1Placement.IsLegacy(gob)) continue;
                if(!seen.Add(x + "," + vy + "," + z)) continue;
                if(BlockIsland1Placement.KeepsOriginalSetPoint(gob.gameObject)) { keptPivot++; continue; }
                float low = BlockIsland1Placement.LowestPoint(g);
                if(float.IsNaN(low)) continue;
                Block below = Map.instance.GetBlock(new Vector3i(x, vy - 1, z)).block;
                if(below == null) overAir++; else if(below is FluidBlock) overWater++;
                float off = low - vy;      // 0 = standing on the top of the block below
                measured++;
                if(off > .15f) floating++; else if(off < -.15f) sunk++; else onGround++;
                string key = gob.name + " (" + gob.gameObject.name + ")";
                if(!byPrefab.ContainsKey(key)) byPrefab[key] = new List<float>();
                byPrefab[key].Add(off);
            }
            yield return Shot("8_welt_" + si + ".png");
        }
        report.Add("World audit: measured " + measured + " | on ground " + onGround + " | floating " + floating + " | sunk " + sunk
            + " | over air " + overAir + " | over water " + overWater + " | 1.0 set point kept " + keptPivot);
        foreach(var kv in byPrefab) {
            float mn = float.MaxValue, mx = float.MinValue; foreach(float f in kv.Value) { mn = Mathf.Min(mn, f); mx = Mathf.Max(mx, f); }
            if(mn < -.15f || mx > .15f) report.Add("  " + kv.Key + " x" + kv.Value.Count + ": offset " + mn.ToString("0.00") + " .. " + mx.ToString("0.00"));
        }
    }

    // Key J height menu: one setting moves every copy of the block; "Automatisch" returns to the floor.
    IEnumerator HeightMenuTest() {
        GameObject player = GameObject.FindWithTag("Player");
        Block alfred = null;
        foreach(Block b in BlockSet.blockIsland1Slots) if(b != null && b.name == "Alfred") alfred = b;
        if(player == null || alfred == null) { report.Add("Height test skipped"); yield break; }
        string file = Path.Combine(Application.persistentDataPath, "BlockIsland1_figure_heights.txt");
        string backup = File.Exists(file) ? File.ReadAllText(file) : null;

        Vector3 o = player.transform.position;
        var cells = new List<Vector3i>(); var before = new List<DataBlock>();
        for(int i = 0; i < 3; i++) {
            Vector3i c = new Vector3i(Mathf.FloorToInt(o.x) + 4 + i * 3, Mathf.FloorToInt(o.y) + 1, Mathf.FloorToInt(o.z) + 4);
            cells.Add(c); before.Add(Map.instance.GetBlock(c));
            Map.instance.SetBlockAndRebuild(c, new DataBlock(alfred, BlockDirection.FORWARD));
        }
        yield return new WaitForSecondsRealtime(4f);
        yield return ExactBounds();

        // open the menu on the first copy exactly like key J does, and screenshot it
        BlockIsland1HeightMenu menu = FindAnyObjectByType<BlockIsland1HeightMenu>();
        BlockIsland1Figure first = null;
        foreach(BlockIsland1Figure f in BlockIsland1Figure.All) if(f != null && f.block == alfred && Mathf.FloorToInt(f.transform.position.x) == cells[0].x) first = f;
        if(menu == null || first == null) { report.Add("Height test: menu " + (menu != null) + ", figure " + (first != null)); yield break; }
        BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(BlockIsland1HeightMenu).GetField("target", bf).SetValue(menu, first);
        typeof(BlockIsland1HeightMenu).GetField("value", bf).SetValue(menu, .6f);
        typeof(BlockIsland1HeightMenu).GetField("open", bf).SetValue(menu, true);
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        BlockIsland1FigureHeights.Set(alfred, .6f);                 // what the slider does
        BlockIsland1Figure.ApplyAll(alfred);
        yield return new WaitForSecondsRealtime(1f);
        yield return Shot("10_hoehenmenue.png");
        report.Add("Height 60% (expected +0.60 each): " + Offsets(alfred, cells));

        BlockIsland1FigureHeights.Clear(alfred);                  // "AUTOMATISCH"
        BlockIsland1Figure.ApplyAll(alfred);
        yield return new WaitForSecondsRealtime(.5f);
        report.Add("Height automatic (expected ~0.00 each): " + Offsets(alfred, cells));
        BlockIsland1FigureHeights.Set(alfred, -1f); BlockIsland1Figure.ApplyAll(alfred);
        yield return new WaitForSecondsRealtime(.5f);
        report.Add("Height -1 (expected -1.00 each): " + Offsets(alfred, cells));
        BlockIsland1FigureHeights.Clear(alfred); BlockIsland1Figure.ApplyAll(alfred);
        typeof(BlockIsland1HeightMenu).GetMethod("Close", bf).Invoke(menu, new object[] { true });
        report.Add("Saved file written: " + File.Exists(file));

        if(backup != null) File.WriteAllText(file, backup); else if(File.Exists(file)) File.Delete(file);   // leave player data untouched
        for(int i = 0; i < cells.Count; i++) Map.instance.SetBlockAndRebuild(cells[i], before[i]);
        yield return new WaitForSecondsRealtime(.5f);
    }

    static string Offsets(Block block, List<Vector3i> cells) {
        var parts = new List<string>();
        foreach(BlockIsland1Figure f in BlockIsland1Figure.All) {
            if(f == null || f.block != block) continue;
            foreach(Vector3i c in cells)
                if(Mathf.FloorToInt(f.transform.position.x) == c.x && Mathf.FloorToInt(f.transform.position.z) == c.z)
                    parts.Add((BlockIsland1Placement.LowestPoint(f.gameObject) - c.y).ToString("0.00"));
        }
        return string.Join(", ", parts.ToArray());
    }

    // Diagnosis for the turquoise ring around the player: same view, one effect switched off per shot.
    static Vector3 RingPos {
        get {
            string[] a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-bi1ringpos");
            if(i >= 0 && i + 3 < a.Length) return new Vector3(float.Parse(a[i + 1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(a[i + 2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(a[i + 3], System.Globalization.CultureInfo.InvariantCulture));
            return new Vector3(145.5f, 54f, 160f);
        }
    }

    IEnumerator RingTest() {
        GameObject player = GameObject.FindWithTag("Player");
        Camera cam = player != null ? player.GetComponentInChildren<Camera>() : null;
        if(cam == null) { report.Add("Ring test: no player camera"); yield break; }
        foreach(MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>())
            if(mb != null && (mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController)) mb.enabled = false;
        player.transform.position = RingPos;
        player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        cam.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
        yield return new WaitForSecondsRealtime(10f);
        var names = new List<string>();
        foreach(Component c in cam.GetComponents<Component>()) { Behaviour b = c as Behaviour; names.Add(c == null ? "MISSING SCRIPT" : c.GetType().Name + (b != null && !b.enabled ? "(off)" : "")); }
        report.Add("Camera effects: " + string.Join(", ", names.ToArray()));
        report.Add("Fog: " + RenderSettings.fog + " mode " + RenderSettings.fogMode + " color " + RenderSettings.fogColor + " density " + RenderSettings.fogDensity
            + " start " + RenderSettings.fogStartDistance + " | shadows " + QualitySettings.shadows + " dist " + QualitySettings.shadowDistance + " cascades " + QualitySettings.shadowCascades);
        yield return Shot("11_ring_A_normal.png");

        TOD_Scattering scat = cam.GetComponent<TOD_Scattering>();
        if(scat != null) { scat.enabled = false; yield return new WaitForSecondsRealtime(1f); yield return Shot("11_ring_B_ohne_Atmosphaere.png"); scat.enabled = true; }

        var layers = cam.GetComponents<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
        foreach(var l in layers) l.enabled = false;
        yield return new WaitForSecondsRealtime(1f); yield return Shot("11_ring_C_ohne_PostProcessing.png");
        foreach(var l in layers) l.enabled = true;
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        yield return null; RenderSettings.fog = false;
        yield return new WaitForSecondsRealtime(1f); yield return Shot("11_ring_D_ohne_Nebel.png");
        RenderSettings.fog = fog;
        ShadowQuality sq = QualitySettings.shadows; QualitySettings.shadows = ShadowQuality.Disable;
        yield return new WaitForSecondsRealtime(1f); yield return Shot("11_ring_E_ohne_Schatten.png");
        QualitySettings.shadows = sq;
        Light sun = TOD_Sky.Instance != null && TOD_Sky.Instance.Components != null ? TOD_Sky.Instance.Components.LightSource : null;
        if(sun != null) {
            report.Add("Sun: shadows " + sun.shadows + " bias " + sun.shadowBias + " normalBias " + sun.shadowNormalBias + " strength " + sun.shadowStrength + " | ambient " + RenderSettings.ambientMode + " " + RenderSettings.ambientLight);
            float b = sun.shadowBias, nb = sun.shadowNormalBias;
            sun.shadowBias = .3f; sun.shadowNormalBias = .6f;
            yield return new WaitForSecondsRealtime(1f); yield return Shot("11_ring_G_hoeherer_Bias.png");
            sun.shadowBias = b; sun.shadowNormalBias = nb;
        }
        int casc = QualitySettings.shadowCascades; QualitySettings.shadowCascades = 1;
        yield return new WaitForSecondsRealtime(1f); yield return Shot("11_ring_H_eine_Kaskade.png");
        QualitySettings.shadowCascades = casc;
        foreach(Behaviour b in cam.GetComponents<Behaviour>()) if(b != null && !(b is Camera) && !(b is AudioListener) && !(b is FlareLayer)) b.enabled = false;
        RenderSettings.fog = false;
        yield return new WaitForSecondsRealtime(1f); yield return Shot("11_ring_F_ohne_alle_Effekte.png");
    }

    // Main menu: Block-Designer panel (list column layout).
    IEnumerator DesignerShot() {
        MainMenuGUI menu = FindAnyObjectByType<MainMenuGUI>();
        if(menu == null) { report.Add("Designer: main menu not found"); yield break; }
        FieldInfo side = typeof(MainMenuGUI).GetField("side", BindingFlags.NonPublic | BindingFlags.Instance);
        side.SetValue(menu, Enum.Parse(side.FieldType, "BlockDesigner"));
        yield return new WaitForSecondsRealtime(2f);
        yield return Shot("20_block_designer.png");
        side.SetValue(menu, Enum.Parse(side.FieldType, "None"));
        yield return null;
    }

    // In game: start point (key T), minecart graphics, inventory FUNCTION tab, pause graphics settings.
    IEnumerator FeatureTest() {
        GameObject player = GameObject.FindWithTag("Player");
        if(player == null || Map.instance == null || Map.instance.infiniteWorld == null) { report.Add("Feature test: no player/world"); yield break; }
        Vector3 start = InfiniteWorldSpawn.StartPoint(Map.instance.infiniteWorld);
        player.transform.position = start;                       // exactly what key T does
        yield return new WaitForSecondsRealtime(9f);
        report.Add("Start point (T): " + start + " -> player at " + player.transform.position);
        yield return Shot("21_startpunkt.png");

        // rails and cart on the ground next to the player, photographed by a separate camera
        Block straight = BlockSet.instance.FindBlock("Rail Straight"), curve = BlockSet.instance.FindBlock("Rail Curve"), cart = BlockSet.instance.FindBlock("Minecart");
        int bx = Mathf.FloorToInt(start.x) + 4, bz = Mathf.FloorToInt(start.z);
        int gy = Map.instance.infiniteWorld.generator.SurfaceY(bx, bz) + 1;
        var cells = new List<Vector3i>(); var before = new List<DataBlock>();
        System.Action<int, int, Block, BlockDirection> put = (dx, dz, b, d) => {
            Vector3i c = new Vector3i(bx + dx, gy, bz + dz); cells.Add(c); before.Add(Map.instance.GetBlock(c));
            Map.instance.SetBlockAndRebuild(c, new DataBlock(b, d)); };
        // a track as the player builds it: looking +z the Builder stores BACKWARD, looking -x it stores RIGHT
        Block loco = BlockSet.instance.FindBlock("Locomotive");
        for(int i = 0; i < 3; i++) put(0, i, straight, BlockDirection.BACKWARD);
        put(0, 3, curve, BlockDirection.BACKWARD);
        for(int i = 1; i <= 3; i++) put(-i, 3, straight, BlockDirection.RIGHT);
        put(3, 0, cart, BlockDirection.BACKWARD);
        put(3, 2, loco, BlockDirection.BACKWARD);
        Camera cam = new GameObject("BI1 Feature Camera").AddComponent<Camera>();
        cam.depth = 100; cam.fieldOfView = 50f;
        cam.transform.position = new Vector3(bx + 4.5f, gy + 3.6f, bz - 2.5f);
        cam.transform.LookAt(new Vector3(bx - .5f, gy + .2f, bz + 2.2f));
        yield return new WaitForSecondsRealtime(2f);
        yield return Shot("22_schienen_minecart.png");
        cam.transform.position = new Vector3(bx + .5f, gy + 5.5f, bz + 1.6f);
        cam.transform.LookAt(new Vector3(bx - .3f, gy, bz + 2.6f));
        yield return new WaitForSecondsRealtime(1f);
        yield return Shot("22b_schienen_von_oben.png");

        cam.transform.position = new Vector3(bx + 5.2f, gy + 1.4f, bz + 1.2f);
        cam.transform.LookAt(new Vector3(bx + 3.5f, gy + .5f, bz + 2.5f));
        yield return new WaitForSecondsRealtime(1f);
        yield return Shot("22d_lok_nah.png");

        // locomotive placed on the track drives like the minecart
        Vector3i lp = new Vector3i(bx, gy + 1, bz);
        DataBlock ld = new DataBlock(loco, BlockDirection.BACKWARD);
        Map.instance.SetBlockAndRebuild(lp, ld);
        bool spawned = MinecartRuntimeController.TrySpawnFromPlacedBlock(Map.instance, lp, ld);
        GameObject moving = GameObject.Find("Moving Locomotive");
        Vector3 p0 = moving != null ? moving.transform.position : Vector3.zero;
        yield return new WaitForSecondsRealtime(1.6f);
        report.Add("Locomotive on rail: spawned " + spawned + ", moved " + (moving != null ? Vector3.Distance(p0, moving.transform.position).ToString("0.00") : "-") + " blocks in 1.6 s");
        yield return Shot("22c_lok_faehrt.png");
        if(moving != null) Destroy(moving);
        Destroy(cam.gameObject);
        for(int i = 0; i < cells.Count; i++) Map.instance.SetBlockAndRebuild(cells[i], before[i]);

        // inventory, FUNKTION tab
        InventoryGUI inv = FindAnyObjectByType<InventoryGUI>();
        if(inv != null) {
            BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(InventoryGUI).GetField("show", bf).SetValue(inv, true);
            typeof(InventoryGUI).GetField("category", bf).SetValue(inv, 2);
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("23_inventar_funktion.png");
            typeof(InventoryGUI).GetField("show", bf).SetValue(inv, false);
        }

        // pause menu, GRAFIK tab
        GameState.IsPause = true;
        yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("24_pause_grafik.png");
        GameState.IsPause = false;
        yield return new WaitForSecondsRealtime(.5f);
    }

    // Skinned bounds follow the animated pose only with updateWhenOffscreen.
    IEnumerator ExactBounds() {
        foreach(SkinnedMeshRenderer r in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude)) r.updateWhenOffscreen = true;
        yield return null; yield return null;
    }

    // Independent check: eye-level photos of real 1.0 figures in the world (not using the measuring code).
    static readonly string[] CloseNames = { "Martha", "Jenny", "Stephanie", "Alfred", "Darius", "Hund (Duck 2)", "Boar", "Parkchair" };
    static readonly Vector3[] ClosePos = { new Vector3(158,50,770), new Vector3(144,50,749), new Vector3(137,50,748), new Vector3(159,50,764),
        new Vector3(145,52,195), new Vector3(512,53,514), new Vector3(790,53,682), new Vector3(514,53,515) };

    IEnumerator CloseUps() {
        GameObject player = GameObject.FindWithTag("Player");
        if(player == null) yield break;
        Camera cam = new GameObject("BI1 CloseUp Camera").AddComponent<Camera>();
        cam.depth = 100; cam.nearClipPlane = .05f; cam.fieldOfView = 40f;
        for(int i = 0; i < ClosePos.Length; i++) {
            Vector3 c = ClosePos[i] + new Vector3(.5f, 0f, .5f);
            player.transform.position = c + new Vector3(6f, 3f, 0f);
            cam.transform.position = c + new Vector3(3.2f, .55f, 0f);
            cam.transform.LookAt(c + new Vector3(0f, .6f, 0f));
            yield return new WaitForSecondsRealtime(9f);
            yield return Shot("9_nah_" + i + "_" + CloseNames[i] + ".png");
        }
        Destroy(cam.gameObject);
    }

    void DisableAutosave() {
        // Destroyed (not just disabled): focus/pause messages would still reach disabled components.
        foreach(InfiniteWorldAutosave a in FindObjectsByType<InfiniteWorldAutosave>()) Destroy(a);
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) { a.saveOnQuit = false; Destroy(a); }
    }

    IEnumerator Shot(string file) {
        yield return new WaitForEndOfFrame();
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(outDir, file), t.EncodeToPNG());
        Destroy(t);
    }

    void Fail(string msg) {
        report.Add("FAILED: " + msg);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        Application.Quit();
    }
}
