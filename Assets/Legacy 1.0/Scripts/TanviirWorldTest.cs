using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Automated check of the Tanviir world with the texture-pack blockset.
// Only active with:  -tvtest -bi1out <folder>
// Loads Tanviir under a separate test save name (real saves stay untouched), writes screenshots
// from several viewpoints, the inventory, and a report with block counts and all errors/warnings.
public class TanviirWorldTest : MonoBehaviour {
    const string TestWorldName = "Tanviir Test";
    string outDir;
    readonly List<string> errors = new List<string>();
    readonly List<string> warnings = new List<string>();
    readonly List<string> report = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] args = Environment.GetCommandLineArgs();
        if(Array.IndexOf(args, "-tvtest") < 0) return;
        Application.runInBackground = true;
        GameObject go = new GameObject("TanviirWorldTest");
        DontDestroyOnLoad(go);
        TanviirWorldTest t = go.AddComponent<TanviirWorldTest>();
        int i = Array.IndexOf(args, "-bi1out");
        t.outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "tvtest");
        Directory.CreateDirectory(t.outDir);
    }

    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string msg, string stack, LogType type) {
        if(msg.Contains("AmbientOcclusion")) return;   // known 2.0 post-processing issue, unrelated
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack);
        else if(type == LogType.Warning && msg.Contains("TANVIIR")) warnings.Add(msg);
        else if(type == LogType.Log && msg.StartsWith("TANVIIR PACK")) report.Add(msg);
    }

    void Start() { StartCoroutine(Run()); }

    IEnumerator Run() {
        yield return new WaitForSecondsRealtime(2f);
        // Main menu must not offer the removed worlds any more.
        report.Add("Removed world types hidden: " + InfiniteWorldSave.IsRemovedWorldType(InfiniteWorldSave.WorldType.Xehanosia));
        InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.Tanviir);
        InfiniteWorldSave.CurrentWorldName = TestWorldName;
        InfiniteWorldSave.LoadRequested = false;
        SceneManager.LoadScene("Game");

        InventoryGUI inv = null;
        float until = Time.realtimeSinceStartup + 60f;
        while((inv = FindAnyObjectByType<InventoryGUI>()) == null && Time.realtimeSinceStartup < until) yield return null;
        if(inv == null) { report.Add("FAILED: InventoryGUI not found"); Finish(); yield break; }
        float settle = Time.realtimeSinceStartup + 25f;
        while(Time.realtimeSinceStartup < settle) { DisableAutosave(); yield return null; }

        BlockSet set = BlockSet.instance;
        int pack = 0, retired = 0, packAlpha = 0;
        for(int i = 0; i < set.Count; i++) {
            Block b = set[i]; if(b == null) continue;
            if(b.retired) retired++;
            if(b.name.StartsWith(TanviirPackBlocks.Prefix)) { pack++; if(b.IsAlpha()) packAlpha++; }
        }
        report.Add("World: " + InfiniteWorldSave.CurrentWorldType + "  native: " + TanviirNativeWorld.Available + "  BlockSet: " + set.Count
            + "  Tanviir pack blocks: " + pack + " (alpha " + packAlpha + ")  retired old Tanviir blocks: " + retired);

        GameObject player = GameObject.FindWithTag("Player");
        Camera cam = player != null ? player.GetComponentInChildren<Camera>() : null;
        if(cam == null) { report.Add("FAILED: no player camera"); Finish(); yield break; }
        report.Add("Player start: " + player.transform.position);
        yield return Shot("1_start.png");

        foreach(MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>())
            if(mb != null && (mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController)) mb.enabled = false;
        Vector3 s = player.transform.position;
        // Compass views at eye level, then raised overviews.
        string[] dirs = { "nord", "ost", "sued", "west" };
        Vector3[] fwd = { Vector3.back, Vector3.right, Vector3.forward, Vector3.left };
        for(int d = 0; d < 4; d++) yield return View(player, cam, s, fwd[d], 8f, "2_blick_" + dirs[d] + ".png", 3f);
        yield return View(player, cam, s + new Vector3(0, 40, 0) - fwd[0] * 40f, fwd[0], 35f, "3_ueberblick_nord.png", 8f);
        yield return View(player, cam, s + new Vector3(0, 40, 0) - fwd[2] * 40f, fwd[2], 35f, "3_ueberblick_sued.png", 8f);
        yield return View(player, cam, s + new Vector3(0, 90, 0), fwd[1], 80f, "3_ueberblick_hoch.png", 10f);
        // Sample what is actually in the world around the start point.
        CountAround(s);
        yield return View(player, cam, s, fwd[0], 8f, "4_zurueck.png", 2f);

        // Inventory: building blocks, nature, shapes.
        FieldInfo show = typeof(InventoryGUI).GetField("show", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo category = typeof(InventoryGUI).GetField("category", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo scroll = typeof(InventoryGUI).GetField("scrollPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        show.SetValue(inv, true); Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        foreach(int c in new[] { 0, 1, 3 }) {
            category.SetValue(inv, c);
            scroll.SetValue(inv, new Vector2(0, 100000f));
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("5_inventar_" + c + "_ende.png");
        }
        show.SetValue(inv, false);
        Finish();
    }

    IEnumerator View(GameObject player, Camera cam, Vector3 pos, Vector3 dir, float pitch, string file, float wait) {
        player.transform.position = pos;
        player.transform.rotation = Quaternion.LookRotation(dir);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        yield return new WaitForSecondsRealtime(wait);
        yield return Shot(file);
    }

    void CountAround(Vector3 c) {
        var counts = new Dictionary<string, int>();
        InfiniteVoxelWorld w = Map.instance != null ? Map.instance.infiniteWorld : null;
        if(w == null) { report.Add("No infinite world for sampling"); return; }
        int cx = Mathf.FloorToInt(c.x), cz = Mathf.FloorToInt(c.z);
        for(int x = cx - 48; x <= cx + 48; x += 2) for(int z = cz - 48; z <= cz + 48; z += 2) for(int y = 40; y < 200; y++) {
            DataBlock b = w.GetBlock(x, y, z);
            if(b.IsEmpty() || b.block == null) continue;
            string n = b.block.name; int k; counts.TryGetValue(n, out k); counts[n] = k + 1;
        }
        var list = new List<KeyValuePair<string, int>>(counts);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        var lines = new List<string>();
        int nonPack = 0;
        foreach(var kv in list) { lines.Add(kv.Value + "  " + kv.Key); if(!kv.Key.StartsWith(TanviirPackBlocks.Prefix)) nonPack += kv.Value; }
        report.Add("Blocks sampled around start (97x97 every 2nd column): " + list.Count + " kinds, non-pack voxels: " + nonPack);
        File.WriteAllText(Path.Combine(outDir, "bloecke_um_start.txt"), string.Join("\n", lines));
    }

    void Finish() {
        report.Add("Tanviir warnings: " + warnings.Count);
        report.Add("Total errors/exceptions: " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- TANVIIR WARNINGS ---\n" + string.Join("\n", warnings) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        Application.Quit();
    }

    void DisableAutosave() {
        foreach(InfiniteWorldAutosave a in FindObjectsByType<InfiniteWorldAutosave>()) Destroy(a);
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) { a.saveOnQuit = false; Destroy(a); }
    }

    IEnumerator Shot(string file) {
        yield return new WaitForEndOfFrame();
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(outDir, file), t.EncodeToPNG());
        Destroy(t);
    }
}
