using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Automated test of the prefab importer (key P). Only active with:  -prefabtest -bi1out <folder>
// (run with BI_PREFAB_DIR pointing at a test prefab folder). Uses a separate test world.
public class PrefabImporterTest : MonoBehaviour {
    const string World = "BI1 Test Prefab";
    string outDir;
    readonly List<string> report = new List<string>();
    readonly List<string> errors = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] a = Environment.GetCommandLineArgs();
        if(Array.IndexOf(a, "-prefabtest") < 0) return;
        Application.runInBackground = true;
        var go = new GameObject("PrefabImporterTest"); DontDestroyOnLoad(go);
        var t = go.AddComponent<PrefabImporterTest>();
        int i = Array.IndexOf(a, "-bi1out"); t.outDir = i >= 0 ? a[i + 1] : Path.Combine(Application.persistentDataPath, "prefabtest");
        Directory.CreateDirectory(t.outDir);
    }
    void OnEnable() { Application.logMessageReceived += OnLog; }
    void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string msg, string stack, LogType type) {
        if(stack != null && stack.Contains("AmbientOcclusion")) return;
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack);
    }
    void Start() { StartCoroutine(Run()); }

    IEnumerator LoadGame(bool existing) {
        if(existing) InfiniteWorldSave.RequestLoad(World);
        else { InfiniteWorldSave.BeginNewWorld(InfiniteWorldSave.WorldType.BlockIsland); InfiniteWorldSave.CurrentWorldName = World; InfiniteWorldSave.LoadRequested = false; }
        SceneManager.LoadScene("Game");
        yield return null;
        float until = Time.realtimeSinceStartup + 60f;
        while((Map.instance == null || Map.instance.infiniteWorld == null || GameObject.FindWithTag("Player") == null) && Time.realtimeSinceStartup < until) yield return null;
        float settle = Time.realtimeSinceStartup + 12f;
        while(Time.realtimeSinceStartup < settle) { foreach(InfiniteWorldAutosave s in FindObjectsByType<InfiniteWorldAutosave>()) Destroy(s); foreach(AutosaveManager s in FindObjectsByType<AutosaveManager>()) { s.saveOnQuit = false; Destroy(s); } yield return null; }
    }

    IEnumerator Run() {
        yield return new WaitForSecondsRealtime(2f);
        report.Add("prefab folder: " + PrefabBlockLibrary.Folder + "  files: " + string.Join(", ", PrefabBlockLibrary.ListBundles()));
        yield return LoadGame(false);
        GameObject player = GameObject.FindWithTag("Player");
        foreach(MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>()) if(mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController) mb.enabled = false;
        Camera cam = player.GetComponentInChildren<Camera>();

        // 1. importer: open, select, size 3 blocks high, first animation clip, save to blockset
        var gui = FindAnyObjectByType<PrefabImporterGUI>();
        BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Instance;
        Func<string, FieldInfo> F = n => typeof(PrefabImporterGUI).GetField(n, bf);
        typeof(PrefabImporterGUI).GetMethod("Open", bf).Invoke(gui, null);
        yield return new WaitForSecondsRealtime(2f);
        var edit = (PrefabBlockLibrary.Entry)F("edit").GetValue(gui);
        var clips = (List<AnimationClip>)F("clips").GetValue(gui);
        report.Add("importer: selected " + F("selected").GetValue(gui) + ", prefab " + (F("prefab").GetValue(gui) as GameObject) + ", clips: " + string.Join(", ", clips.ConvertAll(c => c.name).ToArray()));
        Vector3 s0 = (Vector3)typeof(PrefabImporterGUI).GetMethod("ModelSize", bf).Invoke(gui, null);
        edit.scale *= 3f / Mathf.Max(.01f, s0.y);
        string clip = clips.Count > 0 ? clips[clips.Count > 1 ? 1 : 0].name : PrefabBlockLibrary.StandardAnimation;
        edit.animation = clip; edit.speed = 1f; edit.loop = true; edit.name = "Test Bogenschütze";
        ((GameObject)F("model").GetValue(gui)).GetComponent<PrefabBlockInstance>().Apply();
        yield return new WaitForSecondsRealtime(2.5f);
        Vector3 s1 = (Vector3)typeof(PrefabImporterGUI).GetMethod("ModelSize", bf).Invoke(gui, null);
        report.Add("preview size before " + s0.ToString("0.00") + " -> after scaling " + s1.ToString("0.00") + " (wanted height 3)");
        yield return Shot("1_importer_vorschau.png");
        typeof(PrefabImporterGUI).GetMethod("SaveNew", bf).Invoke(gui, null);
        report.Add("save: " + F("status").GetValue(gui));
        yield return new WaitForSecondsRealtime(1f);
        yield return Shot("2_importer_gespeichert.png");
        typeof(PrefabImporterGUI).GetMethod("Close", bf).Invoke(gui, null);

        var entry = PrefabBlockLibrary.Entries.Find(e => e.name == "Test Bogenschütze" && !e.removed);
        Block block = entry != null ? BlockSet.instance[PrefabBlockLibrary.FirstIndex + entry.slot] : null;
        Builder builder = FindAnyObjectByType<Builder>();
        report.Add("block: " + (block != null ? block.name + " id " + block.blockID + " prefab " + ((GameObjectBlock)block).gameObject + " icon " + (block.icon != null) + " retired " + block.retired : "MISSING")
            + "  selected in builder: " + (builder != null && builder.GetSelectedBlock() == block));

        // 2. build three copies on the ground in front of the start point
        InfiniteVoxelWorld w = Map.instance.infiniteWorld;
        var placed = new List<Vector3i>();
        for(int k = 0; k < 3; k++) {
            int x = 520 + k * 4, z = 520; int y = w.generator.SurfaceY(x, z) + 1;
            var p = new Vector3i(x, y, z); var db = new DataBlock(block, BlockDirection.FORWARD);
            w.SetBlock(x, y, z, db); InfiniteWorldSave.RecordPlayerEdit(p, db); Map.instance.infiniteRenderer.MarkDirtyAtWorld(x, y, z);
            placed.Add(p);
        }
        player.transform.position = new Vector3(524.5f, placed[0].y + 3f, 510f); player.transform.rotation = Quaternion.identity;
        cam.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        yield return new WaitForSecondsRealtime(4f);
        CheckInstances("built", 3);
        yield return CheckMotion("built");
        yield return Shot("3_gebaut.png");

        // 3. save world, reload it, the blocks must come back
        bool saved = InfiniteWorldSave.Save(w, World);
        report.Add("world saved: " + saved);
        yield return LoadGame(true);
        w = Map.instance.infiniteWorld; player = GameObject.FindWithTag("Player");
        foreach(MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>()) if(mb is MouseLook || mb is CharacterMotor || mb is CharacterInputController) mb.enabled = false;
        cam = player.GetComponentInChildren<Camera>();
        player.transform.position = new Vector3(524.5f, placed[0].y + 3f, 510f); player.transform.rotation = Quaternion.identity;
        cam.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        yield return new WaitForSecondsRealtime(5f);
        int same = 0; foreach(var p in placed) { Block b = w.GetBlock(p.x, p.y, p.z).block; if(b != null && b.name == "Test Bogenschütze") same++; }
        report.Add("after reload: " + same + "/3 blocks are the prefab block (id " + (block != null ? block.blockID : -1) + ")");
        CheckInstances("reloaded", 3);
        yield return CheckMotion("reloaded");
        yield return Shot("4_nach_neuladen.png");

        // 4. change settings: all copies follow (double size, no animation)
        entry = PrefabBlockLibrary.Entries.Find(e => e.name == "Test Bogenschütze" && !e.removed);
        float before = Height();
        entry.scale *= 2f; entry.animation = PrefabBlockLibrary.NoAnimation;
        PrefabBlockLibrary.Update(entry, null);
        yield return new WaitForSecondsRealtime(1.5f);
        report.Add("settings changed: height " + before.ToString("0.00") + " -> " + Height().ToString("0.00") + " blocks; animators enabled: " + AnyAnimatorEnabled());
        yield return Shot("5_doppelt_ohne_animation.png");

        // 5. Animator prefab (cow): chosen clip, then the prefab's own controller ("Standard")
        string[] files = PrefabBlockLibrary.ListBundles(); int cowIndex = Array.FindIndex(files, f => f.Contains("Kuh"));
        if(cowIndex >= 0) {
            typeof(PrefabImporterGUI).GetMethod("Open", bf).Invoke(gui, null);
            typeof(PrefabImporterGUI).GetMethod("Select", bf).Invoke(gui, new object[] { cowIndex });
            yield return new WaitForSecondsRealtime(1f);
            var ce = (PrefabBlockLibrary.Entry)F("edit").GetValue(gui); var cc = (List<AnimationClip>)F("clips").GetValue(gui);
            report.Add("cow clips: " + string.Join(", ", cc.ConvertAll(c => c.name).ToArray()));
            ce.name = "Test Kuh"; ce.animation = cc.Count > 0 ? cc[cc.Count - 1].name : PrefabBlockLibrary.StandardAnimation; ce.speed = 1.5f;
            ((GameObject)F("model").GetValue(gui)).GetComponent<PrefabBlockInstance>().Apply();
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("6_kuh_vorschau.png");
            typeof(PrefabImporterGUI).GetMethod("SaveNew", bf).Invoke(gui, null);
            report.Add("cow save: " + F("status").GetValue(gui));
            typeof(PrefabImporterGUI).GetMethod("Close", bf).Invoke(gui, null);
            var cowEntry = PrefabBlockLibrary.Entries.Find(e => e.name == "Test Kuh" && !e.removed);
            Block cow = BlockSet.instance[PrefabBlockLibrary.FirstIndex + cowEntry.slot];
            int cx = 520, cz = 528, cy = w.generator.SurfaceY(cx, cz) + 1;
            w.SetBlock(cx, cy, cz, new DataBlock(cow, BlockDirection.FORWARD)); InfiniteWorldSave.RecordPlayerEdit(new Vector3i(cx, cy, cz), new DataBlock(cow, BlockDirection.FORWARD)); Map.instance.infiniteRenderer.MarkDirtyAtWorld(cx, cy, cz);
            player.transform.position = new Vector3(cx + .5f, cy + 2f, cz - 7f);
            yield return new WaitForSecondsRealtime(3f);
            report.Add("cow block id " + cow.blockID + ", animation '" + cowEntry.animation + "'");
            yield return CheckMotion("cow clip", cowEntry);
            yield return Shot("7_kuh_gebaut.png");
            cowEntry.animation = PrefabBlockLibrary.StandardAnimation; PrefabBlockLibrary.Update(cowEntry, null);
            yield return new WaitForSecondsRealtime(1f);
            yield return CheckMotion("cow standard", cowEntry);
        }

        report.Add("errors: " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        foreach(string f in Directory.GetFiles(InfiniteWorldSave.SaveDirectory, "BI1 Test*")) { try { File.Move(f, Path.Combine(outDir, Path.GetFileName(f))); } catch { } }
        foreach(string d in Directory.GetDirectories(InfiniteWorldSave.SaveDirectory, "BI1 Test*")) { try { Directory.Move(d, Path.Combine(outDir, Path.GetFileName(d))); } catch { } }
        Application.Quit();
    }

    void CheckInstances(string when, int expected) {
        var list = new List<PrefabBlockInstance>();
        foreach(var p in PrefabBlockInstance.All) if(p != null && p.gameObject.name.EndsWith("(Infinite)")) list.Add(p);
        string detail = "";
        foreach(var p in list) {
            float low = BlockIsland1Placement.LowestPoint(p.gameObject);
            Animator a = p.GetComponentInChildren<Animator>();
            detail += "  [floor " + p.floorY.ToString("0.00") + " lowest " + low.ToString("0.00") + " scale " + p.transform.localScale.y.ToString("0.00") + " animator " + (a != null && a.enabled) + "]";
        }
        report.Add(when + ": " + list.Count + "/" + expected + " placed copies" + detail);
    }
    // Does the model move? Compare all child positions over half a second.
    IEnumerator CheckMotion(string when, PrefabBlockLibrary.Entry only = null) {
        PrefabBlockInstance p = null;
        foreach(var q in PrefabBlockInstance.All) if(q != null && q.isActiveAndEnabled && q.gameObject.name.EndsWith("(Infinite)") && (only == null || q.entry == only)) { p = q; break; }
        if(p == null) { report.Add(when + " motion: no copy"); yield break; }
        Transform[] t = p.GetComponentsInChildren<Transform>(); var a = new Vector3[t.Length];
        for(int i = 0; i < t.Length; i++) a[i] = t[i].localPosition + t[i].localEulerAngles;
        yield return new WaitForSecondsRealtime(.5f);
        float moved = 0f; for(int i = 0; i < t.Length; i++) if(t[i] != null) moved += (t[i].localPosition + t[i].localEulerAngles - a[i]).magnitude;
        Animation an = p.GetComponentInChildren<Animation>(); Animator am = p.GetComponentInChildren<Animator>();
        report.Add(when + " motion: " + (moved > .01f ? "ANIMATED" : "still") + " (" + moved.ToString("0.00") + ")  legacy playing: " + (an != null && an.isPlaying) + ", animator enabled: " + (am != null && am.enabled));
    }

    float Height() {
        foreach(var p in PrefabBlockInstance.All) if(p != null && p.gameObject.name.EndsWith("(Infinite)")) {
            Bounds b = new Bounds(); bool any = false;
            foreach(Renderer r in p.GetComponentsInChildren<Renderer>()) { if(!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return b.size.y;
        }
        return 0f;
    }
    bool AnyAnimatorEnabled() { foreach(var p in PrefabBlockInstance.All) if(p != null && p.gameObject.name.EndsWith("(Infinite)")) foreach(Animator a in p.GetComponentsInChildren<Animator>()) if(a.enabled) return true; return false; }

    IEnumerator Shot(string file) {
        yield return new WaitForEndOfFrame();
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(outDir, file), t.EncodeToPNG());
        Destroy(t);
    }
}
