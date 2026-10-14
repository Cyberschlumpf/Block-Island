using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Automated check of the infinite Block Island world (1.0 island + generated islands around it).
// Only active with:  -biinfinite -bi1out <folder>
// Uses a separate test save name; the player's Block Island save is never loaded or written.
public class BlockIslandInfiniteTest : MonoBehaviour {
    const string TestWorldName = "BI1 Test Unendlich";
    string outDir;
    readonly List<string> errors = new List<string>();
    readonly List<string> report = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] args = Environment.GetCommandLineArgs();
        if(Array.IndexOf(args, "-biinfinite") < 0) return;
        Application.runInBackground = true;
        GameObject go = new GameObject("BlockIslandInfiniteTest");
        DontDestroyOnLoad(go);
        var t = go.AddComponent<BlockIslandInfiniteTest>();
        int i = Array.IndexOf(args, "-bi1out");
        t.outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "biinfinite");
        Directory.CreateDirectory(t.outDir);
    }

    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string msg, string stack, LogType type) {
        if(stack != null && stack.Contains("AmbientOcclusion")) return;   // known 2.0 post-processing issue
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack);
    }

    void Start() { StartCoroutine(Run()); }

    IEnumerator Run() {
        yield return new WaitForSecondsRealtime(2f);
        InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland);
        InfiniteWorldSave.CurrentWorldName = TestWorldName;
        InfiniteWorldSave.LoadRequested = false;
        SceneManager.LoadScene("Game");
        float until = Time.realtimeSinceStartup + 60f;
        while((Map.instance == null || Map.instance.infiniteWorld == null) && Time.realtimeSinceStartup < until) yield return null;
        float settle = Time.realtimeSinceStartup + 15f;
        while(Time.realtimeSinceStartup < settle) { DisableAutosave(); yield return null; }
        InfiniteVoxelWorld w = Map.instance.infiniteWorld;
        InfiniteTerrainGenerator g = w.generator;
        report.Add("World " + InfiniteWorldSave.CurrentWorldType + "  seaLevel " + g.seaLevel + "  seed " + g.seed + "  reserve " + g.reserveBlockIsland);

        // Seam: the 1.0 border is water at y 49; the generated side must continue it.
        int seamOk = 0, seamTotal = 0;
        for(int t = 0; t < 1024; t += 16) {
            foreach(var pr in new[] { new[] { 1023, t, 1024, t }, new[] { 0, t, -1, t }, new[] { t, 1023, t, 1024 }, new[] { t, 0, t, -1 } }) {
                seamTotal++;
                bool a = w.GetBlock(pr[0], 49, pr[1]).IsFluid(), b = w.GetBlock(pr[2], 49, pr[3]).IsFluid();
                bool landOutside = !w.GetBlock(pr[2], 50, pr[3]).IsEmpty();
                if(a && b && !landOutside) seamOk++;
            }
        }
        report.Add("Seam water at y49 on both sides, no land directly at the border: " + seamOk + "/" + seamTotal);
        // Old vs. new coast shaping on the same columns.
        foreach(bool wide in new[] { false, true }) {
            g.wideBeaches = wide; g.ClearColumnCache();
            int aboveSea = 0, high = 0, sandy = 0, n = 0;
            for(int x = -1500; x <= 2500; x += 25) for(int z = -1500; z <= 2500; z += 25) {
                if(BlockIslandWorldSource.InsideBlockIsland(x, z)) continue;
                n++; int top = g.SurfaceY(x, z);
                if(top >= g.seaLevel) aboveSea++; if(top > g.seaLevel + 3) high++;
                DataBlock tb = w.GetBlock(x, top, z); if(top >= g.seaLevel - 1 && tb.block != null && tb.block.name.ToLowerInvariant().Contains("sand")) sandy++;
            }
            report.Add((wide ? "NEW wide beaches" : "OLD coast       ") + ": land (top >= sea) " + (100f * aboveSea / n).ToString("0.0") + " %, higher than sea+3 " + (100f * high / n).ToString("0.0") + " %, sand surface " + (100f * sandy / n).ToString("0.0") + " %");
        }
        g.wideBeaches = true; g.ClearColumnCache();
        // Generated land around the island.
        int land = 0, samples = 0, maxTop = 0;
        for(int x = -1500; x <= 2500; x += 25) for(int z = -1500; z <= 2500; z += 25) {
            if(BlockIslandWorldSource.InsideBlockIsland(x, z)) continue;
            samples++; int top = g.SurfaceY(x, z);
            if(top > g.seaLevel) { land++; if(top > maxTop) maxTop = top; }
        }
        report.Add("Generated land outside the 1.0 area: " + (100f * land / samples).ToString("0.0") + " % of " + samples + " samples, highest top y " + maxTop);
        // Inside stays 1.0 (spot check: same as the native source).
        int diff = 0;
        for(int i = 0; i < 4000; i++) { int x = UnityEngine.Random.Range(0, 1024), z = UnityEngine.Random.Range(0, 1024), y = UnityEngine.Random.Range(30, 140);
            if(w.GetBlock(x, y, z).block != BlockIslandNativeWorld.Sample(x, y, z).block) diff++; }
        report.Add("1.0 area unchanged (4000 random voxels vs. native 1.0 data): " + diff + " differences");

        GameObject player = GameObject.FindWithTag("Player");
        Camera cam = player != null ? player.GetComponentInChildren<Camera>() : null;
        if(cam == null) { report.Add("FAILED: no player camera"); Finish(); yield break; }
        foreach(MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>())
            if(mb != null && (mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController)) mb.enabled = false;
        // Find the nearest generated island beyond the east border and look at it from the 1.0 coast.
        yield return View(player, cam, new Vector3(980, 85, 512), Vector3.right, 10f, "1_rand_ost.png", 10f);
        yield return View(player, cam, new Vector3(512, 85, 1000), Vector3.forward, 10f, "2_rand_sued.png", 10f);
        yield return View(player, cam, new Vector3(-60, 85, 512), Vector3.right, 12f, "3_von_aussen_auf_alte_insel.png", 10f);
        // Close-ups of generated islands: walk outward from the border until land, then look at it.
        string[] names = { "ost", "nord", "west", "sued" };
        Vector3[] dirs = { Vector3.right, Vector3.back, Vector3.left, Vector3.forward };
        Vector3[] starts = { new Vector3(1024, 0, 300), new Vector3(700, 0, -1), new Vector3(-1, 0, 800), new Vector3(200, 0, 1024) };
        for(int d = 0; d < 4; d++) {
            Vector3 p = starts[d]; int found = -1;
            for(int step = 0; step < 300; step++) { p += dirs[d] * 8f; if(g.SurfaceY(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)) > g.seaLevel + 3) { found = step; break; } }
            report.Add("First generated land " + names[d] + " of the border: " + (found < 0 ? "none within 2400" : (found * 8 + 8) + " blocks out at " + p));
            if(found < 0) continue;
            Vector3 eye = p - dirs[d] * 45f; eye.y = g.seaLevel + 18;
            yield return View(player, cam, eye, dirs[d], 12f, "4_neue_insel_" + names[d] + ".png", 10f);
        }
        yield return View(player, cam, new Vector3(1060, 75, 1060), new Vector3(-1, 0, -1).normalized, 8f, "5_ecke_blick_auf_alte_insel.png", 10f);
        Finish();
    }

    IEnumerator View(GameObject player, Camera cam, Vector3 pos, Vector3 dir, float pitch, string file, float wait) {
        player.transform.position = pos;
        player.transform.rotation = Quaternion.LookRotation(dir);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        yield return new WaitForSecondsRealtime(wait);
        int inside = 0, outside = 0, outsideWithMesh = 0;
        foreach(MeshFilter mf in FindObjectsByType<MeshFilter>()) {
            if(!mf.gameObject.name.StartsWith("InfiniteChunk")) continue;
            Vector3 o = mf.transform.position;
            if(BlockIslandWorldSource.InsideBlockIsland(Mathf.FloorToInt(o.x), Mathf.FloorToInt(o.z))) inside++; else { outside++; if(mf.sharedMesh != null && mf.sharedMesh.vertexCount > 0) outsideWithMesh++; }
        }
        InfiniteVoxelWorld w = Map.instance.infiniteWorld;
        report.Add(file + ": pos " + pos + "  chunks inside " + inside + ", outside " + outside + " (with mesh " + outsideWithMesh + ")  block at y49 below: " + (w.GetBlock(Mathf.FloorToInt(pos.x), 49, Mathf.FloorToInt(pos.z)).block != null ? w.GetBlock(Mathf.FloorToInt(pos.x), 49, Mathf.FloorToInt(pos.z)).block.name : "air")
            + "  streamer target " + (Camera.main != null ? Camera.main.transform.position.ToString() : "-"));
        yield return new WaitForEndOfFrame();
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(outDir, file), t.EncodeToPNG());
        Destroy(t);
    }

    void Finish() {
        report.Add("Total errors/exceptions (without known AO error): " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        string keep = Path.Combine(outDir, "testsaves"); Directory.CreateDirectory(keep);
        foreach(string f in Directory.GetFiles(InfiniteWorldSave.SaveDirectory, "BI1 Test*")) { try { File.Move(f, Path.Combine(keep, Path.GetFileName(f))); } catch { } }
        Application.Quit();
    }

    void DisableAutosave() {
        foreach(InfiniteWorldAutosave a in FindObjectsByType<InfiniteWorldAutosave>()) Destroy(a);
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) { a.saveOnQuit = false; Destroy(a); }
    }
}
