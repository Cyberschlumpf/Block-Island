using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Complete automated function test. Only active with:  -bi1fulltest -bi1out <folder> [-bi1mcworld <Minecraft world folder>]
// Test worlds use their own save names ("BI1 Test …"); the player's saves are never loaded or written.
public class BlockIsland1FullTest : MonoBehaviour {
    string outDir, mcWorld;
    const string GreenfieldName = "BI1 Test Greenfield";
    readonly List<string> report = new List<string>();
    readonly List<string> errors = new List<string>();
    int phaseErrors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] a = Environment.GetCommandLineArgs();
        if(Array.IndexOf(a, "-bi1fulltest") < 0) return;
        Application.runInBackground = true;   // keep testing when the window is not in front
        var go = new GameObject("BlockIsland1FullTest"); DontDestroyOnLoad(go);
        var t = go.AddComponent<BlockIsland1FullTest>();
        int i = Array.IndexOf(a, "-bi1out"); t.outDir = i >= 0 ? a[i + 1] : Path.Combine(Application.persistentDataPath, "bi1fulltest");
        i = Array.IndexOf(a, "-bi1mcworld"); t.mcWorld = i >= 0 ? a[i + 1] : null;
        Directory.CreateDirectory(t.outDir);
    }

    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string msg, string stack, LogType type) {
        if(type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if(stack != null && stack.Contains("AmbientOcclusion")) return;          // known old 2.0 post-processing error
        errors.Add(type + ": " + msg + "\n" + stack); phaseErrors++;
    }

    void Start() { StartCoroutine(Run()); }

    void Add(string s) { report.Add(s); Debug.Log("BI1 FULLTEST " + s); Write(); }
    void Phase(string name) { report.Add(""); report.Add("== " + name); phaseErrors = 0; }
    void EndPhase() { report.Add("   errors in this phase: " + phaseErrors); Write(); }
    void Write() { try { File.WriteAllText(Path.Combine(outDir, "funktionstest.txt"), string.Join("\n", report.ToArray()) + "\n\n--- ERRORS (without known AO error) ---\n" + string.Join("\n\n", errors.ToArray())); } catch { } }

    IEnumerator Shot(string file) {
        yield return new WaitForEndOfFrame();
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(outDir, file), t.EncodeToPNG()); Destroy(t);
    }

    static void NoAutosave() {
        foreach(InfiniteWorldAutosave a in FindObjectsByType<InfiniteWorldAutosave>()) Destroy(a);
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) { a.saveOnQuit = false; Destroy(a); }
    }

    IEnumerator LoadWorld(InfiniteWorldSave.WorldType type, string testName, float settle) {
        InfiniteWorldSave.BeginNewWorld(type);
        InfiniteWorldSave.CurrentWorldName = testName; InfiniteWorldSave.LoadRequested = false;
        SceneManager.LoadScene("Game");
        yield return WaitForWorld(settle);
    }

    IEnumerator WaitForWorld(float settle) {
        float until = Time.realtimeSinceStartup + 60f;
        while((Map.instance == null || Map.instance.infiniteWorld == null || GameObject.FindWithTag("Player") == null) && Time.realtimeSinceStartup < until) yield return null;
        float s = Time.realtimeSinceStartup + settle;
        while(Time.realtimeSinceStartup < s) { NoAutosave(); yield return null; }
    }

    IEnumerator Run() {
        yield return new WaitForSecondsRealtime(2f);
        Add("Block Island 2.0 function test  " + DateTime.Now);

        // ---------- 1. Minecraft import ----------
        if(!string.IsNullOrEmpty(mcWorld)) {
            Phase("1. Minecraft-Import: " + mcWorld);
            var sw = Stopwatch.StartNew(); MinecraftWorldImporter.Result res = null; MinecraftWorldImporter.Progress last = null; float nextLog = 0;
            StartCoroutine(MinecraftWorldImporter.ImportAsync(mcWorld, GreenfieldName, 0, true, p => {
                last = p; if(Time.realtimeSinceStartup > nextLog) { nextLog = Time.realtimeSinceStartup + 15f; Debug.Log("BI1 FULLTEST import " + p.doneChunks + "/" + p.totalChunks); } }, r => res = r));
            while(res == null) yield return null;
            Add("result: ok=" + res.ok + "  " + res.message);
            Add("time: " + sw.Elapsed.TotalSeconds.ToString("0") + " s   chunks " + (last != null ? last.doneChunks + "/" + last.totalChunks : "?") + "   blocks " + res.importedBlocks.ToString("N0"));
            string info = InfiniteWorldSave.PathFor(GreenfieldName) + ".native/import.txt";
            if(File.Exists(info)) Add("import.txt:\n" + File.ReadAllText(info));
            long bytes = 0; string dir = InfiniteWorldSave.NativeRegionDirFor(GreenfieldName);
            if(Directory.Exists(dir)) foreach(string f in Directory.GetFiles(dir)) bytes += new FileInfo(f).Length;
            Add("native size: " + (bytes / 1048576.0).ToString("0") + " MB in " + (Directory.Exists(dir) ? Directory.GetFiles(dir).Length : 0) + " region files");
            EndPhase();
        }

        // ---------- 2.-5. in a test copy of the Block Island world ----------
        yield return LoadWorld(InfiniteWorldSave.WorldType.BlockIsland, "BI1 Test Funktionstest", 10f);
        InfiniteVoxelWorld world = Map.instance.infiniteWorld; GameObject player = GameObject.FindWithTag("Player");
        player.transform.position = new Vector3(600.5f, 70f, 704.5f);
        yield return new WaitForSecondsRealtime(6f);
        yield return BuildAndSave(world);
        yield return CustomBlocks(world);
        yield return CopyPaste(world, player);
        Phase("5. Teleport (T)");
        Vector3 sp = InfiniteWorldSpawn.StartPoint(world); player.transform.position = sp;
        yield return new WaitForSecondsRealtime(6f);
        Add("start point " + sp + "  ground below: " + (world.GetBlock(Mathf.FloorToInt(sp.x), Mathf.FloorToInt(sp.y) - 3, Mathf.FloorToInt(sp.z)).IsEmpty() ? "air" : "solid"));
        yield return Shot("fulltest_teleport.png");
        EndPhase();

        // ---------- 6. imported Greenfield world ----------
        if(!string.IsNullOrEmpty(mcWorld) && File.Exists(InfiniteWorldSave.PathFor(GreenfieldName))) {
            Phase("6. Greenfield laden");
            InfiniteWorldSave.RequestLoad(GreenfieldName);
            SceneManager.LoadScene("Game");
            yield return WaitForWorld(14f);
            player = GameObject.FindWithTag("Player");
            Add("world type " + InfiniteWorldSave.CurrentWorldType + ", native active " + TanviirImportWorld.active + ", player " + (player != null ? player.transform.position.ToString() : "-"));
            if(player != null) {
                Vector3 p = player.transform.position; int solid = 0;
                for(int x = -24; x <= 24; x += 2) for(int z = -24; z <= 24; z += 2) for(int y = -30; y <= 10; y++) if(!Map.instance.infiniteWorld.GetBlock(Mathf.FloorToInt(p.x) + x, Mathf.FloorToInt(p.y) + y, Mathf.FloorToInt(p.z) + z).IsEmpty()) solid++;
                Add("solid voxels sampled around spawn: " + solid);
                yield return Shot("fulltest_greenfield_1.png");
                player.transform.rotation = Quaternion.Euler(0, 120, 0);
                yield return new WaitForSecondsRealtime(3f);
                yield return Shot("fulltest_greenfield_2.png");
                player.transform.position = p + new Vector3(0, 40, 0); player.transform.rotation = Quaternion.Euler(0, 45, 0);
                yield return new WaitForSecondsRealtime(6f);
                yield return Shot("fulltest_greenfield_3.png");
            }
            EndPhase();
        }

        // ---------- 7. all other worlds ----------
        var worlds = new[] { InfiniteWorldSave.WorldType.Tanviir, InfiniteWorldSave.WorldType.Islands };
        foreach(var w in worlds) {
            Phase("7. Welt " + w);
            yield return LoadWorld(w, "BI1 Test " + w, 12f);
            Add("loaded: map " + (Map.instance != null) + ", player " + (GameObject.FindWithTag("Player") != null) + ", blocks " + (BlockSet.instance != null ? BlockSet.instance.Count : -1));
            yield return Shot("fulltest_welt_" + w + ".png");
            EndPhase();
        }

        Add(""); Add("TOTAL errors (without known AO error): " + errors.Count);
        Write();
        // Leave the player's save folder as it was: move every test save (and imported regions) to the output folder.
        string keep = Path.Combine(outDir, "testsaves"); Directory.CreateDirectory(keep);
        foreach(string f in Directory.GetFiles(InfiniteWorldSave.SaveDirectory, "BI1 Test*")) { try { File.Move(f, Path.Combine(keep, Path.GetFileName(f))); } catch(Exception e) { Add("cleanup: " + e.Message); } }
        foreach(string d in Directory.GetDirectories(InfiniteWorldSave.SaveDirectory, "BI1 Test*")) { try { Directory.Move(d, Path.Combine(keep, Path.GetFileName(d))); } catch(Exception e) { Add("cleanup: " + e.Message); } }
        Write();
        Application.Quit();
    }

    // ---------- 2. build and save ----------
    IEnumerator BuildAndSave(InfiniteVoxelWorld world) {
        Phase("2. Bauen und Speichern");
        string[] names = { "Stone", "Glass", "Locomotive", "Dark Wood", "Water" };
        var placed = new List<KeyValuePair<Vector3i, DataBlock>>();
        int bx = 600, bz = 690, by = Map.instance.infiniteWorld.generator.SurfaceY(bx, bz) + 2;
        for(int i = 0; i < names.Length; i++) {
            Block b = BlockSet.instance.FindBlock(names[i]); if(b == null) { Add("block missing: " + names[i]); continue; }
            var p = new Vector3i(bx + i * 2, by, bz); var d = new DataBlock(b, BlockDirection.RIGHT);
            Map.instance.SetBlockAndRebuild(p, d); InfiniteWorldSave.RecordPlayerEdit(p, d); placed.Add(new KeyValuePair<Vector3i, DataBlock>(p, d));
        }
        var removed = new Vector3i(bx, by - 3, bz);                   // remove a ground block
        Map.instance.SetBlockAndRebuild(removed, new DataBlock()); InfiniteWorldSave.RecordPlayerEdit(removed, new DataBlock());
        bool saved = InfiniteWorldSave.SaveCurrent(world);
        string file = InfiniteWorldSave.PathFor(InfiniteWorldSave.CurrentWorldName);
        Add("saved: " + saved + "  file " + (File.Exists(file) ? new FileInfo(file).Length + " bytes" : "missing"));
        foreach(var kv in placed) world.SetBlock(kv.Key.x, kv.Key.y, kv.Key.z, new DataBlock());     // tamper with the loaded world
        world.SetBlock(removed.x, removed.y, removed.z, new DataBlock(BlockSet.instance.FindBlock("Dirt")));
        bool loaded = InfiniteWorldSave.Load(world, InfiniteWorldSave.CurrentWorldName);
        int ok = 0;
        foreach(var kv in placed) { DataBlock g = world.GetBlock(kv.Key.x, kv.Key.y, kv.Key.z); if(g.blockID == kv.Value.blockID && g.direction == kv.Value.direction) ok++; }
        bool removedOk = world.GetBlock(removed.x, removed.y, removed.z).IsEmpty();
        Add("loaded: " + loaded + "  placed blocks restored " + ok + "/" + placed.Count + "  removed block stays removed: " + removedOk);
        yield return null; EndPhase();
    }

    // ---------- 3. custom blocks ----------
    IEnumerator CustomBlocks(InfiniteVoxelWorld world) {
        Phase("3. Custom Blocks");
        CustomVoxelBlock slot = BlockSet.instance.FindBlock<CustomVoxelBlock>("Custom Voxel 32");
        if(slot == null) { Add("Custom Voxel 32 missing"); EndPhase(); yield break; }
        string path = (string)typeof(CustomVoxelBlock).GetProperty("FilePath", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(slot, null);
        string backup = File.Exists(path) ? File.ReadAllText(path) : null;
        DataBlock[] backupDesign = slot.design;
        Block stone = BlockSet.instance.FindBlock("Stone"), glass = BlockSet.instance.FindBlock("Glass");
        var design = new DataBlock[CustomVoxelBlock.COUNT]; int solid = 0, i = 0;
        for(int z = 0; z < 8; z++) for(int y = 0; y < 8; y++) for(int x = 0; x < 8; x++, i++)
            if(x == 0 || y == 0 || z == 0 || (x + y + z) % 5 == 0) { design[i] = new DataBlock((x + z) % 2 == 0 ? stone : glass, BlockDirection.FORWARD); solid++; }
        slot.SetDesign(design);
        Add("design file written: " + File.Exists(path) + " (" + solid + " mini voxels)");
        CustomVoxelBlock fresh = ScriptableObject.CreateInstance<CustomVoxelBlock>(); fresh.slot = slot.slot;
        typeof(CustomVoxelBlock).GetMethod("LoadDesign", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fresh, null);
        int same = 0; for(int k = 0; k < CustomVoxelBlock.COUNT; k++) if(fresh.design[k].blockID == design[k].blockID) same++;
        Add("design reloaded from file: " + same + "/" + CustomVoxelBlock.COUNT + " identical");
        Destroy(fresh);
        var pos = new Vector3i(612, world.generator.SurfaceY(612, 690) + 2, 690);
        var cd = new DataBlock(slot, BlockDirection.FORWARD);
        Map.instance.SetBlockAndRebuild(pos, cd); InfiniteWorldSave.RecordPlayerEdit(pos, cd);
        bool saved = InfiniteWorldSave.SaveCurrent(world); world.SetBlock(pos.x, pos.y, pos.z, new DataBlock());
        InfiniteWorldSave.Load(world, InfiniteWorldSave.CurrentWorldName);
        Add("custom block placed, saved " + saved + ", after reload in world: " + (world.GetBlock(pos.x, pos.y, pos.z).block == slot));
        GameObject player = GameObject.FindWithTag("Player"); player.transform.position = new Vector3(pos.x - 3f, pos.y + 1f, pos.z + .5f); player.transform.rotation = Quaternion.LookRotation(Vector3.right);
        yield return new WaitForSecondsRealtime(3f);
        yield return Shot("fulltest_custom_block.png");
        // restore the player's slot
        if(backup != null) File.WriteAllText(path, backup); else if(File.Exists(path)) File.Delete(path);
        slot.design = backupDesign;
        EndPhase();
    }

    // ---------- 4. copy & paste ----------
    IEnumerator CopyPaste(InfiniteVoxelWorld world, GameObject player) {
        Phase("4. Copy & Paste");
        Builder b = player.GetComponentInChildren<Builder>();
        BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Instance;
        Func<string, FieldInfo> F = n => typeof(Builder).GetField(n, bf);
        MethodInfo capture = typeof(Builder).GetMethod("CaptureSelection", bf), paste = typeof(Builder).GetMethod("PasteSelection", bf);

        // small: exact copy check
        int gy = world.generator.SurfaceY(600, 704);
        var a0 = new Vector3i(590, gy - 4, 694); var a1 = new Vector3i(601, gy + 5, 705);
        yield return Capture(b, F, capture, a0, a1);
        Add("small copy: " + F("copyStatus").GetValue(b));
        var dst = new Vector3i(700, gy + 20, 694);
        F("copyRotationQuarterTurns").SetValue(b, 0);
        yield return Run(b, F, (IEnumerator)paste.Invoke(b, new object[] { dst }));
        int diff = 0, total = 0;
        for(int x = 0; x <= a1.x - a0.x; x++) for(int y = 0; y <= a1.y - a0.y; y++) for(int z = 0; z <= a1.z - a0.z; z++) {
            DataBlock s = world.GetBlock(a0.x + x, a0.y + y, a0.z + z), d = world.GetBlock(dst.x + x, dst.y + y, dst.z + z); total++;
            if(s.IsEmpty() != d.IsEmpty() || (!s.IsEmpty() && s.blockID != d.blockID)) diff++;
        }
        Add("small paste: " + F("copyStatus").GetValue(b) + "   voxel differences " + diff + "/" + total);

        // limit
        yield return Capture(b, F, capture, new Vector3i(0, 0, 0), new Vector3i(512, 10, 10));
        Add("513 wide: " + F("copyStatus").GetValue(b));

        // large: 512 x 256 x 512 over the island
        long mem0 = GC.GetTotalMemory(true); var sw = Stopwatch.StartNew();
        yield return Capture(b, F, capture, new Vector3i(256, 0, 256), new Vector3i(767, 255, 767));
        double tCopy = sw.Elapsed.TotalSeconds; object clip = F("copyBuffer").GetValue(b);
        int runs = clip != null ? (int)clip.GetType().GetProperty("RunCount").GetValue(clip, null) : -1;
        Add("large copy 512x256x512: " + F("copyStatus").GetValue(b) + "   " + tCopy.ToString("0.0") + " s, runs " + runs.ToString("N0") + ", memory +" + ((GC.GetTotalMemory(false) - mem0) / 1048576) + " MB");
        sw = Stopwatch.StartNew();
        var big = new Vector3i(1100, 0, 256);
        yield return Run(b, F, (IEnumerator)paste.Invoke(b, new object[] { big }));
        Add("large paste: " + F("copyStatus").GetValue(b) + "   " + sw.Elapsed.TotalSeconds.ToString("0.0") + " s");
        var rnd = new System.Random(5); int sampleDiff = 0;
        for(int k = 0; k < 20000; k++) {
            int x = rnd.Next(512), y = rnd.Next(256), z = rnd.Next(512);
            DataBlock s = world.GetBlock(256 + x, y, 256 + z), d = world.GetBlock(big.x + x, y, big.z + z);
            if(s.IsEmpty() != d.IsEmpty() || (!s.IsEmpty() && s.blockID != d.blockID)) sampleDiff++;
        }
        Add("large paste random sample: " + sampleDiff + " differences in 20000 voxels");
        player.transform.position = new Vector3(big.x + 256, 140, big.z - 60); player.transform.rotation = Quaternion.Euler(0, 0, 0);
        yield return new WaitForSecondsRealtime(8f);
        yield return Shot("fulltest_paste_gross.png");
        EndPhase();
    }

    IEnumerator Capture(Builder b, Func<string, FieldInfo> F, MethodInfo capture, Vector3i s, Vector3i e) {
        F("copyStart").SetValue(b, s); F("copyEnd").SetValue(b, e); F("copyStartSet").SetValue(b, true);
        yield return Run(b, F, (IEnumerator)capture.Invoke(b, null));
    }
    IEnumerator Run(Builder b, Func<string, FieldInfo> F, IEnumerator routine) {
        b.StartCoroutine(routine);
        yield return null;
        while((bool)F("copyBusy").GetValue(b)) yield return null;
        yield return null;
    }
}
