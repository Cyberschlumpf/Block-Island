using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Automated portal test (player built with -portaltest -bi1out <dir>, environment BI_DATA_DIR = own test data folder):
// Block Island -> build "Portal → Tanviir" -> walk in -> Tanviir (start point) -> build "Portal → Block Island" -> walk in
// -> back in Block Island in front of the first portal. Screenshots of every stage, 360° pictures, registry and saves.
public sealed class PortalTest : MonoBehaviour {
    string outDir;
    readonly List<string> report = new List<string>();
    readonly List<string> errors = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] a = Environment.GetCommandLineArgs();
        if(Array.IndexOf(a, "-portaltest") < 0) return;
        Application.runInBackground = true;
        var go = new GameObject("PortalTest"); DontDestroyOnLoad(go);
        var t = go.AddComponent<PortalTest>();
        int i = Array.IndexOf(a, "-bi1out"); t.outDir = i >= 0 ? a[i + 1] : Path.Combine(Application.persistentDataPath, "portaltest");
        Directory.CreateDirectory(t.outDir);
    }
    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string msg, string stack, LogType type) {
        if(stack != null && stack.Contains("AmbientOcclusion")) return;
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack);
        if(msg.StartsWith("PORTALS:")) report.Add("  log: " + msg);
    }
    void Start() { StartCoroutine(Run()); Invoke(nameof(Timeout), 400f); }
    void Timeout() { report.Add("TIMEOUT"); Finish(); }

    IEnumerator Run() {
        string data = Environment.GetEnvironmentVariable("BI_DATA_DIR");
        report.Add("data folder: " + BlockIslandPaths.Root + " (BI_DATA_DIR " + data + ")");
        if(string.IsNullOrEmpty(data) || !BlockIslandPaths.Root.StartsWith(data)) { errors.Add("ERROR: test must run with its own BI_DATA_DIR"); Finish(); yield break; }
        yield return new WaitForSecondsRealtime(2f);

        // 1. a fresh Block Island world
        InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland);
        SceneManager.LoadScene("Game");
        yield return WaitWorld();
        report.Add("start: " + InfiniteWorldSave.CurrentWorldName + " player " + Player().transform.position);

        // 2. build "Portal → Tanviir" five blocks in front of the player
        Block toTanviir = BlockSet.instance[PortalBlocks.ToTanviirId], toBI = BlockSet.instance[PortalBlocks.ToBlockIslandId];
        report.Add("blocks: " + (toTanviir != null ? toTanviir.name : "missing") + " / " + (toBI != null ? toBI.name : "missing"));
        if(!(toTanviir is PortalBlock) || !(toBI is PortalBlock)) { errors.Add("ERROR: portal blocks missing"); Finish(); yield break; }
        Vector3i p1 = PlaceInFront(toTanviir, 6);
        yield return WaitPortal(p1);
        BlockIslandPortal portal1 = Find(p1);
        report.Add("portal 1 (→ Tanviir) at " + p1 + ", instance " + (portal1 != null) + ", registry " + Registry());
        LookAt(portal1 != null ? portal1.Center : (Vector3)p1); yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("1_portal_block_island.png");

        // 3. walk into it and watch the journey
        string realBefore = RealSavesStamp();
        yield return Enter(portal1, "2");
        report.Add("after journey 1: world " + InfiniteWorldSave.CurrentWorldName + " type " + InfiniteWorldSave.CurrentWorldType + " player " + Player().transform.position);
        if(InfiniteWorldSave.CurrentWorldType != InfiniteWorldSave.WorldType.Tanviir) errors.Add("ERROR: did not arrive in Tanviir");
        yield return Shot("3_ankunft_tanviir.png");

        // 4. in Tanviir: "Portal → Block Island"; it shows the picture taken at the Block Island portal
        Vector3i p2 = PlaceInFront(toBI, 6);
        yield return WaitPortal(p2);
        BlockIslandPortal portal2 = Find(p2);
        report.Add("portal 2 (→ Block Island) at " + p2 + ", instance " + (portal2 != null) + ", registry " + Registry());
        LookAt(portal2 != null ? portal2.Center : (Vector3)p2); yield return new WaitForSecondsRealtime(1.5f);
        yield return Shot("4_portal_tanviir_zeigt_block_island.png");

        // 5. back to Block Island: arrival in front of portal 1
        yield return Enter(portal2, "5");
        Vector3 pp = Player().transform.position;
        report.Add("after journey 2: world " + InfiniteWorldSave.CurrentWorldName + " player " + pp);
        if(InfiniteWorldSave.CurrentWorldType != InfiniteWorldSave.WorldType.BlockIsland) errors.Add("ERROR: did not arrive back in Block Island");
        BlockIslandPortal back = null; float waited = 0;
        while(back == null && waited < 20f) { back = Find(p1); yield return new WaitForSecondsRealtime(.5f); waited += .5f; }
        if(back == null) errors.Add("ERROR: portal 1 not found after returning");
        else {
            Vector3 d = pp - back.Center; d.y = 0;
            report.Add("distance to the Block Island portal on arrival: " + d.magnitude.ToString("0.0") + " (portal inside: " + back.Contains(Player().transform.position) + ")");
            if(d.magnitude > 6f) errors.Add("ERROR: arrival not in front of the portal (" + d.magnitude + ")");
            Vector3 lookBack = back.Center; LookAt(lookBack); yield return new WaitForSecondsRealtime(2f);
            yield return Shot("6_portal_block_island_zeigt_tanviir.png");
        }
        yield return new WaitForSecondsRealtime(3f);
        report.Add("still in Block Island 3 s later (no bounce back): " + (InfiniteWorldSave.CurrentWorldType == InfiniteWorldSave.WorldType.BlockIsland && !PortalTransition.Busy));

        // files
        string portale = BlockIslandPaths.Folder("Portale");
        foreach(string f in Directory.GetFiles(portale)) report.Add("file " + Path.GetFileName(f) + " " + new FileInfo(f).Length / 1024 + " KB");
        foreach(string f in Directory.GetFiles(InfiniteWorldSave.SaveDirectory, "*.infinite")) report.Add("save " + Path.GetFileName(f));
        if(!File.Exists(PortalSnapshots.PathOf("Block Island")) || !File.Exists(PortalSnapshots.PathOf("Tanviir"))) errors.Add("ERROR: 360° pictures missing");
        string realAfter = RealSavesStamp();
        report.Add("real saves unchanged: " + (realBefore == realAfter));
        if(realBefore != realAfter) errors.Add("ERROR: the player's real saves were touched");
        Finish();
    }

    IEnumerator Enter(BlockIslandPortal p, string prefix) {
        if(p == null) { errors.Add("ERROR: no portal to enter"); yield break; }
        GameObject pl = Player();
        pl.transform.position = new Vector3(p.Center.x, p.Cell.y + 1.1f, p.Center.z);
        float t0 = Time.realtimeSinceStartup;
        while(!PortalTransition.Busy && Time.realtimeSinceStartup - t0 < 5f) yield return null;
        if(!PortalTransition.Busy) { errors.Add("ERROR: walking into the portal did not start the journey"); yield break; }
        yield return new WaitForSecondsRealtime(.7f); yield return Shot(prefix + "a_wird_schwarz.png");
        yield return new WaitForSecondsRealtime(1.8f); yield return Shot(prefix + "b_sterne.png");
        yield return new WaitForSecondsRealtime(1.5f); yield return Shot(prefix + "c_sterne_laden.png");
        float tl = Time.realtimeSinceStartup; bool dissolveShot = false;
        while(PortalTransition.Busy && Time.realtimeSinceStartup - tl < 60f) {
            if(!dissolveShot && PortalTransition.Phase == "dissolve") {
                dissolveShot = true; yield return new WaitForSecondsRealtime(.9f); yield return Shot(prefix + "d_sterne_loesen_sich.png");
            }
            yield return null;
        }
        report.Add("journey " + prefix + " took " + (Time.realtimeSinceStartup - t0).ToString("0.0") + " s");
        if(PortalTransition.Busy) errors.Add("ERROR: journey " + prefix + " did not finish");
        yield return WaitWorld();
        foreach(MonoBehaviour mb in Player().GetComponentsInChildren<MonoBehaviour>()) if(mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController) mb.enabled = false;
    }

    IEnumerator WaitWorld() {
        float until = Time.realtimeSinceStartup + 60f;
        while((Map.instance == null || Map.instance.infiniteWorld == null || Player() == null || Camera.main == null) && Time.realtimeSinceStartup < until) yield return null;
        yield return new WaitForSecondsRealtime(6f);
        foreach(InfiniteWorldAutosave s in FindObjectsByType<InfiniteWorldAutosave>()) s.intervalSeconds = 100000f;
        foreach(MonoBehaviour mb in Player().GetComponentsInChildren<MonoBehaviour>()) if(mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController) mb.enabled = false;
        if(InfiniteDayNightCycle.Instance != null) InfiniteDayNightCycle.Instance.SetTime(11f, true);
    }

    IEnumerator WaitPortal(Vector3i cell) {
        float until = Time.realtimeSinceStartup + 20f;
        while(Find(cell) == null && Time.realtimeSinceStartup < until) yield return null;
        if(Find(cell) == null) errors.Add("ERROR: portal at " + cell + " did not appear");
    }

    static BlockIslandPortal Find(Vector3i cell) { foreach(BlockIslandPortal p in BlockIslandPortal.All) if(p.Cell.Equals(cell)) return p; return null; }
    static GameObject Player() { return GameObject.FindWithTag("Player"); }

    Vector3i PlaceInFront(Block b, int dist) {
        GameObject pl = Player(); Vector3 f = Camera.main.transform.forward; f.y = 0; if(f.sqrMagnitude < .01f) f = Vector3.forward; f.Normalize();
        InfiniteVoxelWorld w = Map.instance.infiniteWorld;
        // a flat, open 4x4 spot (same ground height, 5 free blocks above) as close as possible to 'dist' blocks ahead
        Vector3i cell = default(Vector3i); bool found = false;
        for(int ring = 0; ring <= 24 && !found; ring++) for(int a = 0; a < Mathf.Max(1, ring * 8) && !found; a++) {
            float ang = a * Mathf.PI * 2f / Mathf.Max(1, ring * 8);
            Vector3 at = pl.transform.position + f * dist + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * ring;
            int x = Mathf.FloorToInt(at.x), z = Mathf.FloorToInt(at.z);
            int top = TopAt(w, x, z, Mathf.FloorToInt(pl.transform.position.y) + 40); bool ok = top > 0;
            for(int dx = -1; dx <= 2 && ok; dx++) for(int dz = -1; dz <= 2 && ok; dz++) {
                if(TopAt(w, x + dx, z + dz, top + 8) != top) ok = false;
                if(w.GetBlock(x + dx, top, z + dz).IsFluid()) ok = false;
            }
            if(ok) { cell = new Vector3i(x, top + 1, z); found = true; }
        }
        if(!found) { errors.Add("ERROR: no open spot for a portal"); cell = new Vector3i(Mathf.FloorToInt(pl.transform.position.x), Mathf.FloorToInt(pl.transform.position.y), Mathf.FloorToInt(pl.transform.position.z)); }
        Map.instance.SetBlockAndRebuild(cell, new DataBlock(b));
        return cell;
    }

    static int TopAt(InfiniteVoxelWorld w, int x, int z, int from) {
        for(int y = Mathf.Min(250, from); y > 0; y--) { var b = w.GetBlock(x, y, z); if(!b.IsEmpty()) return b.block is CrossBlock ? -1 : y; }
        return -1;
    }

    void LookAt(Vector3 target) {
        GameObject pl = Player(); Camera cam = Camera.main;
        Vector3 d = target - cam.transform.position; Vector3 flat = d; flat.y = 0;
        // step back so the whole cube is in view
        if(flat.magnitude < 9f || Mathf.Abs(d.y) > 6f) {
            if(flat.sqrMagnitude < .01f) flat = Vector3.forward;
            pl.transform.position = new Vector3(target.x, target.y, target.z) - flat.normalized * 10f;
            d = target - cam.transform.position; flat = d; flat.y = 0;
        }
        pl.transform.rotation = Quaternion.LookRotation(flat.normalized);
        cam.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg, 0, 0);
    }

    static string Registry() {
        string f = Path.Combine(BlockIslandPaths.Folder("Portale"), "portale.txt");
        return File.Exists(f) ? File.ReadAllText(f).Replace("\n", " | ").Replace("\t", " ") : "(none)";
    }
    static string RealSavesStamp() {
        string real = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Block Island", "Saves");
        if(!Directory.Exists(real)) return "none";
        var sb = new System.Text.StringBuilder();
        foreach(string f in Directory.GetFiles(real)) sb.Append(Path.GetFileName(f) + File.GetLastWriteTimeUtc(f).Ticks + ";");
        return sb.ToString();
    }

    void Finish() {
        report.Add("errors: " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) a.saveOnQuit = false;
        Application.Quit();
    }

    IEnumerator Shot(string file) {
        yield return new WaitForEndOfFrame();
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(outDir, file), t.EncodeToPNG());
        Destroy(t);
    }
}
