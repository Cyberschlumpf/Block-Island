using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// Automated test of all function blocks (player built with -fbtest -bi1out <dir>, environment BI_DATA_DIR = test data folder).
// Builds a stone platform in the sky over Block Island, places every function block on it and checks what each one does.
public sealed class FunctionBlocksTest : MonoBehaviour {
    string outDir; readonly List<string> report = new List<string>(); readonly List<string> errors = new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] a = Environment.GetCommandLineArgs(); if(Array.IndexOf(a, "-fbtest") < 0) return;
        Application.runInBackground = true;
        var go = new GameObject("FunctionBlocksTest"); DontDestroyOnLoad(go); var t = go.AddComponent<FunctionBlocksTest>();
        int i = Array.IndexOf(a, "-bi1out"); t.outDir = i >= 0 ? a[i + 1] : Application.persistentDataPath; Directory.CreateDirectory(t.outDir);
    }
    void OnEnable() { Application.logMessageReceived += OnLog; } void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string m, string s, LogType t) { if(s != null && s.Contains("AmbientOcclusion")) return; if(t == LogType.Error || t == LogType.Exception) errors.Add(t + ": " + m + "\n" + s); }
    void Start() { StartCoroutine(Run()); Invoke(nameof(Finish), 420f); }
    void Check(bool ok, string what) { report.Add((ok ? "OK    " : "FEHLER ") + what); if(!ok) errors.Add("ERROR: " + what); }

    const int Y = 100, Z = 760, X0 = 700;
    Map map; GameObject pl; Camera cam;

    IEnumerator Run() {
        string data = Environment.GetEnvironmentVariable("BI_DATA_DIR");
        if(string.IsNullOrEmpty(data)) { errors.Add("ERROR: BI_DATA_DIR missing"); Finish(); yield break; }
        // a picture for the info column
        string pics = Path.Combine(data, "Bilder"); Directory.CreateDirectory(pics);
        string src = Path.Combine(Application.streamingAssetsPath, "..", "..", "..", "..");
        var tex = new Texture2D(64, 40); for(int y = 0; y < 40; y++) for(int x = 0; x < 64; x++) tex.SetPixel(x, y, Color.HSVToRGB(x / 64f, .7f, .9f)); tex.Apply();
        File.WriteAllBytes(Path.Combine(pics, "testbild.png"), tex.EncodeToPNG());
        yield return new WaitForSecondsRealtime(2f);
        InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland); SceneManager.LoadScene("Game");
        float until = Time.realtimeSinceStartup + 60f;
        while((Map.instance == null || GameObject.FindWithTag("Player") == null || Camera.main == null) && Time.realtimeSinceStartup < until) yield return null;
        yield return new WaitForSecondsRealtime(8f);
        map = Map.instance; pl = GameObject.FindWithTag("Player"); cam = Camera.main;
        foreach(InfiniteWorldAutosave s in FindObjectsByType<InfiniteWorldAutosave>()) s.intervalSeconds = 100000f;
        foreach(MonoBehaviour mb in pl.GetComponentsInChildren<MonoBehaviour>()) if(mb is MouseLook || mb is CharacterInputController) mb.enabled = false;
        if(InfiniteCreativeWeather.Instance != null) InfiniteCreativeWeather.Instance.SetWeather(InfiniteCreativeWeather.Kind.Sun);
        InfiniteDayNightCycle.Instance.SetTime(12f, true);

        // platform
        Block stone = BlockSet.instance.FindBlock("Stone");
        for(int x = X0 - 3; x <= X0 + 52; x++) for(int z = Z - 6; z <= Z + 6; z++) map.infiniteWorld.SetBlock(x, Y, z, new DataBlock(stone));
        map.infiniteRenderer.MarkDirtyChunks(Chunks(X0 - 3, X0 + 52, Z - 6, Z + 6));
        pl.transform.position = new Vector3(X0 + 24, Y + 2.1f, Z - 5);
        yield return new WaitForSecondsRealtime(6f);

        // all function blocks in a row
        Check(BlockSet.instance[FunctionBlocks.FirstId] is FunctionBlock && BlockSet.instance[FunctionBlocks.FirstId + FunctionBlocks.Defs.Length - 1] is FunctionBlock, "Blocknummern 4004–" + (FunctionBlocks.FirstId + FunctionBlocks.Defs.Length - 1) + " belegt");
        var cells = new Dictionary<string, Vector3i>();
        int xi = X0;
        for(int i = 0; i < FunctionBlocks.Defs.Length; i++) {
            var d = FunctionBlocks.Defs[i]; if(d.part) continue;
            if(d.component == typeof(FBTeleporter) || d.component == typeof(FBElevator)) continue;   // placed separately
            var c = new Vector3i(xi, Y + 1, Z); xi += 2; cells[d.name] = c;
            Place(c, BlockSet.instance[FunctionBlocks.FirstId + i]);
            yield return null; CloseDialogs();
        }
        // texts
        FunctionBlockData.Set(cells["Schild mit Text"], "Hafen Old Harbor");
        FunctionBlockData.Set(cells["Wegpunkt"], "Testplattform");
        // teleporters (same name) and an elevator column
        var t1 = new Vector3i(X0 + 2, Y + 1, Z + 4); var t2 = new Vector3i(X0 + 40, Y + 1, Z + 4);
        Place(t1, Def(typeof(FBTeleporter))); yield return null; CloseDialogs(); Place(t2, Def(typeof(FBTeleporter))); yield return null; CloseDialogs();
        FunctionBlockData.Set(t1, "Strand"); FunctionBlockData.Set(t2, "Strand");
        var e1 = new Vector3i(X0 + 20, Y + 1, Z - 4); var e2 = new Vector3i(X0 + 20, Y + 9, Z - 4);
        Place(e1, Def(typeof(FBElevator))); Place(e2, Def(typeof(FBElevator)));
        yield return new WaitForSecondsRealtime(8f);
        report.Add("placed: " + cells.Count + " blocks + 2 teleporters + 2 elevator floors");
        foreach(var kv in cells) Check(Find(kv.Value) != null || kv.Key == "Bauwerk-Spawner", "sichtbar: " + kv.Key);
        // multi-cell blocks
        Check(IsPart(cells["Tür (automatisch)"] + new Vector3i(0, 1, 0)), "Tür füllt 2 Blöcke");
        Check(IsPart(cells["Info-Säule (eigene Bilder)"] + new Vector3i(0, 2, 0)), "Info-Säule füllt 3 Blöcke");
        Check(map.GetBlock(cells["Schild mit Text"]).IsSolid(), "Funktionsblöcke sind fest");

        // day screenshot
        Look(new Vector3(X0 + 22, Y + 4, Z - 9), new Vector3(X0 + 22, Y + 1, Z));
        yield return new WaitForSecondsRealtime(4f); yield return Shot("1_tag.png");

        // door
        Vector3i door = cells["Tür (automatisch)"];
        pl.transform.position = new Vector3(door.x + .5f, Y + 2.1f, door.z - 4f); yield return new WaitForSecondsRealtime(1f);
        Check(!FunctionBlocks.IsOpen(door), "Tür zu, wenn niemand da ist");
        pl.transform.position = new Vector3(door.x + .5f, Y + 2.1f, door.z - 1.6f); yield return new WaitForSecondsRealtime(1f);
        Check(FunctionBlocks.IsOpen(door) && FunctionBlocks.IsOpen(door + new Vector3i(0, 1, 0)), "Tür öffnet sich beim Näherkommen");
        // spring
        Vector3i spring = cells["Sprungfeder"];
        pl.transform.position = new Vector3(spring.x + .5f, Y + 2.15f, spring.z + .5f);
        float y0 = pl.transform.position.y, ymax = y0;
        for(float t = 0; t < 1.5f; t += Time.deltaTime) { ymax = Mathf.Max(ymax, pl.transform.position.y); yield return null; }
        Check(ymax > y0 + 2.5f, "Sprungfeder schleudert hoch (" + (ymax - y0).ToString("0.0") + " Blöcke)");
        // conveyor
        Vector3i conv = cells["Förderband"];
        yield return new WaitForSecondsRealtime(1.5f);
        Vector3 c0 = new Vector3(conv.x + .5f, Y + 2.1f, conv.z + .5f);
        pl.transform.position = c0; yield return new WaitForSecondsRealtime(.3f);
        Vector3 moved = pl.transform.position - c0; moved.y = 0;
        Check(moved.magnitude > .3f, "Förderband schiebt (" + moved.magnitude.ToString("0.00") + " Blöcke in 0,3 s)");
        // teleporter
        pl.transform.position = new Vector3(t1.x + .5f, Y + 2.1f, t1.z + .5f);
        until = Time.realtimeSinceStartup + 8f; while(Time.realtimeSinceStartup < until && Vector3.Distance(pl.transform.position, new Vector3(t2.x + .5f, pl.transform.position.y, t2.z + .5f)) > 1.5f) yield return null;
        yield return new WaitForSecondsRealtime(1f);
        Check(Vector3.Distance(new Vector3(pl.transform.position.x, 0, pl.transform.position.z), new Vector3(t2.x + .5f, 0, t2.z + .5f)) < 1.5f, "Teleporter „Strand“ bringt zum zweiten Teleporter");
        yield return new WaitForSecondsRealtime(2f);
        Check(Vector3.Distance(new Vector3(pl.transform.position.x, 0, pl.transform.position.z), new Vector3(t2.x + .5f, 0, t2.z + .5f)) < 1.5f, "kein sofortiges Zurückspringen");
        // elevator
        pl.transform.position = new Vector3(e1.x + .5f, Y + 2.1f, e1.z + .5f);
        until = Time.realtimeSinceStartup + 8f; while(Time.realtimeSinceStartup < until && pl.transform.position.y < e2.y + 1.5f) yield return null;
        Check(pl.transform.position.y > e2.y + 1.5f, "Aufzug fährt zur oberen Etage");
        // bell, fireworks
        FBBell.RingAt(cells["Glocke"]); yield return new WaitForSecondsRealtime(.5f);
        var bell = Find(cells["Glocke"]); Check(bell != null && bell.GetComponent<AudioSource>() != null && bell.GetComponent<AudioSource>().isPlaying, "Glocke läutet");
        FBFireworks.LaunchAt(cells["Feuerwerk"]); yield return new WaitForSecondsRealtime(2.2f);
        Check(GameObject.Find("Burst") != null, "Feuerwerk explodiert");
        // switch
        FBSwitch.ToggleAt(cells["Weiche (Minecart)"]); Check(FunctionBlockData.Get(cells["Weiche (Minecart)"], "") == "abbiegen", "Weiche schaltet auf abbiegen");
        // internet data
        until = Time.realtimeSinceStartup + 25f; TideData.Event ev; while(Time.realtimeSinceStartup < until && !(BlockIslandRealTime.HasWeather && TideData.Next(BlockIslandRealTime.BlockIsland, true, out ev))) yield return null;
        Check(BlockIslandRealTime.HasWeather, "Wetterdaten Block Island (" + BlockIslandRealTime.Temperature + " °C, Wind " + BlockIslandRealTime.Wind + " km/h aus " + BlockIslandRealTime.WindDirection + "°)");
        Check(TideData.Next(BlockIslandRealTime.BlockIsland, true, out ev), "Gezeiten Block Island (nächstes Hochwasser " + ev.time.ToString("HH:mm") + ")");
        report.Add("Mondphase: " + FBMoon.Name(FBMoon.Phase) + " (" + FBMoon.Phase.ToString("0.00") + ")");
        // lamp brightness and night
        FunctionBlocks.CycleLamp(cells["Laterne (Abendlicht)"]);
        Check(FunctionBlockData.Get(cells["Laterne (Abendlicht)"], "") == "Hell", "Laterne: Helligkeit mit Enter einstellbar (Mittel → Hell)");
        InfiniteDayNightCycle.Instance.SetTime(23f, true);
        yield return new WaitForSecondsRealtime(12f);
        var lamp = Find(cells["Laterne (Abendlicht)"]); Light ll = lamp != null ? lamp.GetComponentInChildren<Light>() : null;
        Check(ll != null && ll.enabled && ll.intensity > .5f, "Laterne leuchtet nachts");
        Look(new Vector3(X0 + 22, Y + 4, Z - 9), new Vector3(X0 + 22, Y + 1, Z));
        yield return new WaitForSecondsRealtime(2f); yield return Shot("2_nacht.png");
        // old street lanterns in town
        int on = 0, total = 0;
        foreach(LampControl lc in FindObjectsByType<LampControl>()) { total++; if(lc.fire != null && lc.fire.activeSelf) on++; }
        pl.transform.position = new Vector3(762.5f, 54.5f, 806f); pl.transform.rotation = Quaternion.LookRotation(Vector3.back);
        yield return new WaitForSecondsRealtime(8f);
        on = 0; total = 0; foreach(LampControl lc in FindObjectsByType<LampControl>()) { total++; if(lc.fire != null && lc.fire.activeSelf) on++; }
        Check(total == 0 || on == total, "alte Straßenlaternen leuchten nachts (" + on + " von " + total + ")");
        yield return Shot("3_strassenlaternen_nacht.png");
        InfiniteDayNightCycle.Instance.SetTime(12f, true); yield return new WaitForSecondsRealtime(10f);
        on = 0; foreach(LampControl lc in FindObjectsByType<LampControl>()) if(lc.fire != null && lc.fire.activeSelf) on++;
        Check(on == 0, "alte Straßenlaternen sind am Tag aus (" + on + " an)");
        // picture column, waypoint list
        var column = Find(cells["Info-Säule (eigene Bilder)"]);
        Check(column != null && column.GetComponentsInChildren<Renderer>().Length > 0, "Info-Säule steht");
        var picField = typeof(FBPictureColumn).GetField("pictures", BindingFlags.NonPublic | BindingFlags.Static);
        Check(((List<Texture2D>)picField.GetValue(null)).Count > 0, "Info-Säule lädt Bilder aus dem Ordner");
        // ---------------- minecart: station stop and switch
        for(int x = X0 - 3; x <= X0 + 34; x++) for(int z = Z + 7; z <= Z + 17; z++) map.infiniteWorld.SetBlock(x, Y, z, new DataBlock(stone));
        map.infiniteRenderer.MarkDirtyChunks(Chunks(X0 - 3, X0 + 34, Z + 7, Z + 17));
        Block rail = BlockSet.instance.FindBlock("Rail Straight"), cartBlock = BlockSet.instance.FindBlock("Minecart");
        for(int x = X0; x <= X0 + 30; x++) Place(new Vector3i(x, Y + 1, Z + 8), rail);
        Place(new Vector3i(X0 + 15, Y + 1, Z + 9), Def(typeof(FBStation)));
        for(int x = X0; x <= X0 + 20; x++) Place(new Vector3i(x, Y + 1, Z + 12), rail);
        for(int z = Z + 13; z <= Z + 16; z++) Place(new Vector3i(X0 + 10, Y + 1, z), rail);
        var sw = new Vector3i(X0 + 10, Y + 1, Z + 11); Place(sw, Def(typeof(FBSwitch))); FunctionBlockData.Set(sw, "abbiegen");
        pl.transform.position = new Vector3(X0 + 12, Y + 6, Z + 4); yield return new WaitForSecondsRealtime(3f);
        Check(MinecartRuntimeController.TrySpawnFromPlacedBlock(map, new Vector3i(X0 + 1, Y + 2, Z + 8), new DataBlock(cartBlock, BlockDirection.RIGHT)), "Minecart 1 gestartet");
        Check(MinecartRuntimeController.TrySpawnFromPlacedBlock(map, new Vector3i(X0 + 1, Y + 2, Z + 12), new DataBlock(cartBlock, BlockDirection.RIGHT)), "Minecart 2 gestartet");
        MinecartRuntimeController cartA = null, cartB = null;
        foreach(var c in FindObjectsByType<MinecartRuntimeController>()) { if(c.railPos.z == Z + 8) cartA = c; if(c.railPos.z == Z + 12) cartB = c; }
        float stoppedFor = 0, lastX = -999, startT = Time.realtimeSinceStartup; float maxZ = Z + 12;
        while(Time.realtimeSinceStartup - startT < 25f) {
            yield return new WaitForSecondsRealtime(.25f);
            if(cartA != null) { float x = cartA.transform.position.x; if(Mathf.Abs(x - lastX) < .01f && Mathf.Abs(x - (X0 + 15.5f)) < 1.2f) stoppedFor += .25f; lastX = x; }
            if(cartB != null) maxZ = Mathf.Max(maxZ, cartB.transform.position.z);
        }
        Check(stoppedFor >= 4f, "Minecart hält am Bahnhof (" + stoppedFor.ToString("0.0") + " s)");
        Check(maxZ > Z + 14f, "Minecart biegt an der Weiche ab (bis z " + maxZ.ToString("0.0") + ")");
        // ---------------- music box (an MP3 was copied into the test music folder)
        FBJukebox.NextAt(cells["Musikbox"]);
        until = Time.realtimeSinceStartup + 20f; AudioSource jb = null;
        while(Time.realtimeSinceStartup < until) { var j = Find(cells["Musikbox"]); jb = j != null ? j.GetComponent<AudioSource>() : null; if(jb != null && jb.isPlaying) break; yield return null; }
        Check(jb != null && jb.isPlaying, "Musikbox spielt " + (jb != null && jb.clip != null ? jb.clip.length.ToString("0") + " s Titel" : "nichts"));
        if(jb != null) jb.Stop();
        // ---------------- structure spawner
        var clip = new VoxelClipboard(3, 4, 3); for(int i = 0; i < 36; i++) clip.Append(new DataBlock(stone));
        string msg; Check(VoxelStructureLibrary.Save(clip, "Testturm", out msg), "Test-Bauwerk gespeichert");
        Vector3i sp = cells["Bauwerk-Spawner"];
        if(map.GetBlock(sp).IsEmpty()) Place(sp, Def(typeof(FBSpawner)));
        FBSpawner.Placed(sp, BlockDirection.FORWARD); yield return null;
        var ui = FindFirstObjectByType<FunctionBlockUI>();
        typeof(FunctionBlockUI).GetMethod("CloseChoice", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new object[] { 0 });
        yield return new WaitForSecondsRealtime(3f);
        Check(map.GetBlock(sp + new Vector3i(0, 3, 0)).block == stone && map.GetBlock(sp + new Vector3i(2, 0, 2)).block == stone, "Bauwerk-Spawner baut das Bauwerk an seiner Stelle");
        // ---------------- waypoint list (key N)
        pl.transform.position = new Vector3(X0 + 30, Y + 2.1f, Z - 5);
        FBWaypoint.ShowList(); yield return null;
        typeof(FunctionBlockUI).GetMethod("CloseChoice", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new object[] { 0 });
        yield return new WaitForSecondsRealtime(3f);
        Vector3i wp = cells["Wegpunkt"];
        Check(Vector3.Distance(new Vector3(pl.transform.position.x, 0, pl.transform.position.z), new Vector3(wp.x + .5f, 0, wp.z + .5f)) < 2f, "Wegpunkt-Liste bringt zum Wegpunkt");
        // ---------------- guides in the pause menu
        var pause = FindFirstObjectByType<PauseGUI>(FindObjectsInactive.Include);
        if(pause != null) {
            GameState.IsPause = true; yield return null;
            typeof(PauseGUI).GetField("tab", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pause, 6);
            typeof(BlockIslandGuides).GetField("guideTab", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 1);
            yield return new WaitForSecondsRealtime(1f); yield return Shot("4_pause_anleitungen.png");
            GameState.IsPlaying = true;
        }
        Check(pause != null, "Pause-Menü gefunden (Anleitungen-Reiter)");
        // removing the door removes its upper part
        map.SetBlockAndRebuild(door + new Vector3i(0, 1, 0), new DataBlock());
        Check(map.GetBlock(door).IsEmpty(), "Abbauen der oberen Türhälfte entfernt die ganze Tür");
        Finish();
    }

    FunctionBlock Def(Type component) { foreach(var d in FunctionBlocks.Defs) if(d.component == component) return (FunctionBlock)BlockSet.instance[FunctionBlocks.FirstId + Array.IndexOf(FunctionBlocks.Defs, d)]; return null; }
    void Place(Vector3i c, Block b) { var db = new DataBlock(b, BlockDirection.BACKWARD); map.SetBlockAndRebuild(c, db); InfiniteWorldSave.RecordPlayerEdit(c, db); }
    bool IsPart(Vector3i c) { var fb = map.GetBlock(c).block as FunctionBlock; return fb != null && fb.def.part; }
    static void CloseDialogs() {
        var ui = FindFirstObjectByType<FunctionBlockUI>(); if(ui == null) return;
        typeof(FunctionBlockUI).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new object[] { false });
        typeof(FunctionBlockUI).GetMethod("CloseChoice", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new object[] { -1 });
    }
    GameObject Find(Vector3i c) {
        foreach(Transform t in FindObjectsByType<Transform>()) if(t.parent != null && t.parent.name == "InfiniteSpecialBlocks" && FunctionBlocks.CellOf(t).Equals(c)) return t.gameObject;
        return null;
    }
    static HashSet<InfiniteChunkKey> Chunks(int x0, int x1, int z0, int z1) {
        var s = new HashSet<InfiniteChunkKey>(); for(int x = x0; x <= x1; x += 8) for(int z = z0; z <= z1; z += 8) s.Add(InfiniteWorldMath.WorldToChunk(x, Y, z)); s.Add(InfiniteWorldMath.WorldToChunk(x1, Y, z1)); return s;
    }
    void Look(Vector3 from, Vector3 at) {
        pl.transform.position = from - Vector3.up * 1.6f; Vector3 d = at - from; Vector3 flat = new Vector3(d.x, 0, d.z);
        pl.transform.rotation = Quaternion.LookRotation(flat); cam.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg, 0, 0);
    }
    void Finish() {
        report.Add("errors: " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) a.saveOnQuit = false;
        Application.Quit();
    }
    IEnumerator Shot(string f) { yield return new WaitForEndOfFrame(); Texture2D t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(outDir, f), t.EncodeToPNG()); Destroy(t); }
}
