using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Diagnostics/test for the rotating history column on the town square (1.0 block "26", fireSpot prefab):
// player built with -columntest -bi1out <dir>, environment BI_DATA_DIR = own test data folder.
public sealed class ColumnTest : MonoBehaviour {
    string outDir; readonly List<string> report = new List<string>(); readonly List<string> errors = new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] a = Environment.GetCommandLineArgs(); if(Array.IndexOf(a, "-columntest") < 0) return;
        Application.runInBackground = true;
        var go = new GameObject("ColumnTest"); DontDestroyOnLoad(go); var t = go.AddComponent<ColumnTest>();
        int i = Array.IndexOf(a, "-bi1out"); t.outDir = i >= 0 ? a[i + 1] : Application.persistentDataPath; Directory.CreateDirectory(t.outDir);
    }
    void OnEnable() { Application.logMessageReceived += OnLog; } void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string m, string s, LogType t) { if(s != null && s.Contains("AmbientOcclusion")) return; if(t == LogType.Error || t == LogType.Exception) errors.Add(t + ": " + m + "\n" + s); }
    void Start() { StartCoroutine(Run()); Invoke(nameof(Finish), 240f); }
    IEnumerator Run() {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BI_DATA_DIR"))) { errors.Add("ERROR: BI_DATA_DIR missing"); Finish(); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland); SceneManager.LoadScene("Game");
        float until = Time.realtimeSinceStartup + 60f;
        while((Map.instance == null || GameObject.FindWithTag("Player") == null || Camera.main == null) && Time.realtimeSinceStartup < until) yield return null;
        yield return new WaitForSecondsRealtime(8f);
        GameObject pl = GameObject.FindWithTag("Player"); Camera cam = Camera.main;
        foreach(MonoBehaviour mb in pl.GetComponentsInChildren<MonoBehaviour>()) if(mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController) mb.enabled = false;
        if(InfiniteDayNightCycle.Instance != null) InfiniteDayNightCycle.Instance.SetTime(11f, true);
        if(InfiniteCreativeWeather.Instance != null) InfiniteCreativeWeather.Instance.SetWeather(InfiniteCreativeWeather.Kind.Sun);
        pl.transform.position = new Vector3(762.6f, 54.3f, 804.5f); pl.transform.rotation = Quaternion.Euler(0, 187f, 0); cam.transform.localRotation = Quaternion.Euler(0, 0, 0);
        yield return new WaitForSecondsRealtime(20f);
        yield return Shot("saeule_1.png");
        GameObject col = null;
        foreach(Transform t in FindObjectsByType<Transform>()) if(t.name.StartsWith("fireSpot") && Vector3.Distance(t.position, new Vector3(762.5f, 53.5f, 798.5f)) < 3f) { col = t.gameObject; break; }
        report.Add("column instance: " + (col != null ? col.name + " at " + col.transform.position + " active " + col.activeInHierarchy : "NOT FOUND"));
        if(col != null) foreach(Transform t in col.GetComponentsInChildren<Transform>(true)) {
            string line = "  " + t.name + " active " + t.gameObject.activeInHierarchy + " pos " + t.position + " rot " + t.eulerAngles + " scale " + t.lossyScale;
            var r = t.GetComponent<Renderer>();
            if(r != null) { line += " renderer " + r.enabled; foreach(Material m in r.sharedMaterials) line += "\n      mat " + (m != null ? m.name + " [" + (m.shader != null ? m.shader.name + " supported " + m.shader.isSupported : "no shader") + "] tex " + (m.mainTexture != null ? m.mainTexture.name + " " + m.mainTexture.width : "none") : "null"); }
            foreach(Component c in t.GetComponents<Component>()) if(c == null) line += " MISSING SCRIPT"; else if(c is MonoBehaviour) line += " " + c.GetType().Name + (((MonoBehaviour)c).enabled ? "" : "(off)");
            report.Add(line);
        }
        Transform rotating = null; if(col != null) foreach(Transform t in col.GetComponentsInChildren<Transform>(true)) if(t.name == "Adshel_Cylinder_Rotating") rotating = t;
        float a0 = rotating != null ? rotating.eulerAngles.y : 0;
        yield return new WaitForSecondsRealtime(3f);
        report.Add("rotating part turned by " + (rotating != null ? Mathf.DeltaAngle(a0, rotating.eulerAngles.y).ToString("0.0") : "-") + " degrees in 3 s");
        { Clock ck = FindFirstObjectByType<Clock>();
          report.Add("clock: " + (ck != null ? ck.transform.position + " rot " + ck.transform.eulerAngles + " forward " + ck.transform.forward + " CET " + BlockIslandRealTime.CentralEurope.ToString("HH:mm:ss") + " Block Island " + BlockIslandRealTime.BlockIsland.ToString("HH:mm") : "none"));
          if(ck != null) {
            InfiniteVoxelWorld w = Map.instance.infiniteWorld; Vector3 c = ck.transform.position;
            foreach(Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) { Vector3 q = c + d * 1.2f; report.Add("  block " + d + ": " + (w.GetBlock(Mathf.FloorToInt(q.x), Mathf.FloorToInt(q.y), Mathf.FloorToInt(q.z)).IsEmpty() ? "air" : "solid")); }
            Vector3 front = -ck.transform.forward; foreach(Vector3 d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) { Vector3 q = c + d * 1.2f; if(w.GetBlock(Mathf.FloorToInt(q.x), Mathf.FloorToInt(q.y), Mathf.FloorToInt(q.z)).IsEmpty() && !w.GetBlock(Mathf.FloorToInt(c.x - d.x * 1.2f), Mathf.FloorToInt(q.y), Mathf.FloorToInt(c.z - d.z * 1.2f)).IsEmpty()) front = d; }
            Vector3 eye = c + Vector3.forward * 7f;
            pl.transform.position = eye - Vector3.up * .8f;
            pl.transform.rotation = Quaternion.LookRotation(Vector3.back); cam.transform.localRotation = Quaternion.Euler(0, 0, 0);
            yield return new WaitForSecondsRealtime(10f);
            report.Add("clock screenshot at CET " + BlockIslandRealTime.CentralEurope.ToString("HH:mm:ss"));
            yield return Shot("uhr.png");
            // info sign on the square, seen from both sides
            Block sign = BlockSet.instance[InfoSignBlocks.Id];
            report.Add("info sign block: " + (sign != null ? sign.name + " " + sign.GetType().Name : "missing"));
            var cell = new Vector3i(762, 53, 794);
            // placed like the Builder does: facing the player who stands south of it (player looks north = +z... here: block direction BACKWARD = toward -z)
            var placed = new DataBlock(sign, BlockDirection.BACKWARD);
            Map.instance.SetBlockAndRebuild(cell, placed); InfiniteWorldSave.RecordPlayerEdit(cell, placed);
            yield return new WaitForSecondsRealtime(4f);
            Vector3i off = InfoSignBlock.PartnerOffset(BlockDirection.BACKWARD, false);
            DataBlock half = Map.instance.GetBlock(cell + off);
            report.Add("second half at " + (cell + off) + ": " + (half.block != null ? half.block.name + " solid " + half.block.IsSolid() : "EMPTY"));
            if(!(half.block is InfoSignBlock)) errors.Add("ERROR: second half missing");
            if(!Map.instance.GetBlock(cell).IsSolid() || !half.IsSolid()) errors.Add("ERROR: sign blocks are not solid");
            for(int side = 0; side < 2; side++) {
                Vector3 look = side == 0 ? Vector3.forward : Vector3.back;
                Vector3 centre = new Vector3(cell.x + .5f + off.x * .5f, cell.y + .5f, cell.z + .5f);
                pl.transform.position = centre - look * 4f + Vector3.down * .9f;
                pl.transform.rotation = Quaternion.LookRotation(look); cam.transform.localRotation = Quaternion.Euler(0, 0, 0);
                yield return new WaitForSecondsRealtime(2f); yield return Shot("infotafel_seite" + (side + 1) + ".png");
            }
            foreach(TextMesh t in FindObjectsByType<TextMesh>()) report.Add("  text '" + t.text + "'");
            // removing the second half removes the whole sign
            Map.instance.SetBlockAndRebuild(cell + off, new DataBlock());
            report.Add("after removing the second half: first half " + (Map.instance.GetBlock(cell).IsEmpty() ? "removed" : "STILL THERE"));
            if(!Map.instance.GetBlock(cell).IsEmpty()) errors.Add("ERROR: first half not removed");
          } }
        pl.transform.position = new Vector3(762.5f, 54.3f, 801.6f); yield return new WaitForSecondsRealtime(1f); yield return Shot("saeule_2_nah.png");
        yield return new WaitForSecondsRealtime(8f); yield return Shot("saeule_3_spaeter.png");
        Finish();
    }
    void Finish() {
        report.Add("errors: " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) a.saveOnQuit = false;
        Application.Quit();
    }
    IEnumerator Shot(string f) { yield return new WaitForEndOfFrame(); Texture2D t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(outDir, f), t.EncodeToPNG()); Destroy(t); }
}
