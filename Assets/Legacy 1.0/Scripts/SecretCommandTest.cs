using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// Test of the main menu: no Tanviir button, the guide page "TASTEN & PORTALE", and the secret command "lemuria"
// (player built with -secrettest -bi1out <dir>, environment BI_DATA_DIR = own test data folder).
public sealed class SecretCommandTest : MonoBehaviour {
    string outDir; readonly List<string> report = new List<string>(); readonly List<string> errors = new List<string>();
    MainMenuGUI menu; Queue<char> toType = new Queue<char>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        string[] a = Environment.GetCommandLineArgs(); if(Array.IndexOf(a, "-secrettest") < 0) return;
        Application.runInBackground = true;
        var go = new GameObject("SecretCommandTest"); DontDestroyOnLoad(go); var t = go.AddComponent<SecretCommandTest>();
        int i = Array.IndexOf(a, "-bi1out"); t.outDir = i >= 0 ? a[i + 1] : Application.persistentDataPath; Directory.CreateDirectory(t.outDir);
    }
    void OnEnable() { Application.logMessageReceived += OnLog; } void OnDisable() { Application.logMessageReceived -= OnLog; }
    void OnLog(string m, string s, LogType t) { if(s != null && s.Contains("AmbientOcclusion")) return; if(t == LogType.Error || t == LogType.Exception) errors.Add(t + ": " + m + "\n" + s); }
    void Start() { StartCoroutine(Run()); Invoke(nameof(Finish), 150f); }
    IEnumerator Run() {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BI_DATA_DIR"))) { errors.Add("ERROR: BI_DATA_DIR missing"); Finish(); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        menu = FindFirstObjectByType<MainMenuGUI>();
        report.Add("scene " + SceneManager.GetActiveScene().name + ", main menu " + (menu != null));
        if(menu == null) { errors.Add("ERROR: main menu not found"); Finish(); yield break; }
        yield return Shot("1_hauptmenue.png");
        // guide page in the settings
        var mt = typeof(MainMenuGUI); var bf = BindingFlags.NonPublic | BindingFlags.Instance;
        mt.GetField("side", bf).SetValue(menu, Enum.Parse(mt.GetNestedType("SidePanel", BindingFlags.NonPublic), "Settings"));
        mt.GetField("tab", bf).SetValue(menu, 6); mt.GetField("guideTab", bf).SetValue(menu, 0);
        yield return new WaitForSecondsRealtime(1f); yield return Shot("2_anleitung_tasten_portale.png");
        mt.GetField("side", bf).SetValue(menu, Enum.Parse(mt.GetNestedType("SidePanel", BindingFlags.NonPublic), "None"));
        yield return new WaitForSecondsRealtime(.5f);
        // a wrong word first, then the secret one
        foreach(char c in "lemurix") toType.Enqueue(c);
        yield return new WaitForSecondsRealtime(1f);
        report.Add("after 'lemurix': scene " + SceneManager.GetActiveScene().name);
        if(SceneManager.GetActiveScene().name != "Main Menu" && SceneManager.GetActiveScene().name != SceneManager.GetSceneAt(0).name) errors.Add("ERROR: wrong word opened something");
        string before = SceneManager.GetActiveScene().name;
        foreach(char c in "LEMURIA") toType.Enqueue(c);
        float until = Time.realtimeSinceStartup + 60f;
        while(SceneManager.GetActiveScene().name == before && Time.realtimeSinceStartup < until) yield return null;
        report.Add("after 'LEMURIA': scene " + SceneManager.GetActiveScene().name + ", world " + InfiniteWorldSave.CurrentWorldName + " type " + InfiniteWorldSave.CurrentWorldType);
        if(InfiniteWorldSave.CurrentWorldType != InfiniteWorldSave.WorldType.Tanviir) errors.Add("ERROR: secret command did not open Tanviir");
        while((Map.instance == null || Camera.main == null) && Time.realtimeSinceStartup < until) yield return null;
        yield return new WaitForSecondsRealtime(12f); yield return Shot("3_lemuria_tanviir.png");
        Finish();
    }
    // typed characters arrive as IMGUI key events in the menu's own OnGUI handling
    void OnGUI() {
        if(toType.Count == 0 || menu == null || Event.current.type != EventType.Layout) return;
        char c = toType.Dequeue();
        Event old = Event.current;
        Event.current = new Event { type = EventType.KeyDown, character = c };
        typeof(MainMenuGUI).GetMethod("SecretCommand", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, null);
        Event.current = old;
    }
    void Finish() {
        report.Add("errors: " + errors.Count);
        File.WriteAllText(Path.Combine(outDir, "bericht.txt"), string.Join("\n", report) + "\n\n--- ERRORS ---\n" + string.Join("\n\n", errors));
        foreach(AutosaveManager a in FindObjectsByType<AutosaveManager>()) a.saveOnQuit = false;
        Application.Quit();
    }
    IEnumerator Shot(string f) { yield return new WaitForEndOfFrame(); Texture2D t = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(outDir, f), t.EncodeToPNG()); Destroy(t); }
}
