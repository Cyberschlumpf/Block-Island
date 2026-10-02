using System.Collections;
using UnityEngine;

/// <summary>
/// Periodically writes a rolling autosave of the current voxel world.
/// Only one save can run at a time. Autosave uses a fixed filename so it
/// does not create hundreds of save slots during long play sessions.
/// </summary>
public sealed class AutosaveManager : MonoBehaviour {
    public const string AUTOSAVE_FILE = "autosave.map";

    [Tooltip("Seconds between automatic saves.")]
    public float intervalSeconds = 120f;

    [Tooltip("Write one final autosave when leaving the application.")]
    public bool saveOnQuit = true;

    private bool isSaving;
    private Coroutine autosaveRoutine;

    void Start() {
        // 6.14.1: Tanviir is a persistent restoration world. Save edits frequently without
        // writing on every mouse click (which would cause visible I/O hitches while building).
        if (TanviirImportWorld.active) intervalSeconds = 60f;
        else intervalSeconds = Mathf.Max(30f, intervalSeconds);
        autosaveRoutine = StartCoroutine(AutosaveLoop());
    }

    private IEnumerator AutosaveLoop() {
        while (true) {
            yield return new WaitForSecondsRealtime(intervalSeconds);
            SaveNow();
        }
    }

    public void SaveNow() {
        if (isSaving || Map.instance == null || BlockSet.instance == null) return;

        isSaving = true;
        try {
            float started = Time.realtimeSinceStartup;
            if(Map.instance.infiniteWorld != null) { if(InfiniteWorldSave.SaveCurrent(Map.instance.infiniteWorld)) Debug.Log("Named infinite-world autosave complete: " + InfiniteWorldSave.CurrentWorldName); return; }
            byte[] bytes = MapSaver.Export(BlockSet.instance, Map.instance);
            IOUtils.Save(AUTOSAVE_FILE, bytes);
            Debug.Log(string.Format("Autosave complete: {0:0.00} MB in {1:0.00}s", bytes.Length / 1048576f, Time.realtimeSinceStartup - started));
        }
        catch (System.Exception ex) {
            Debug.LogError("Autosave failed: " + ex);
        }
        finally {
            isSaving = false;
        }
    }

    void OnApplicationQuit() {
        if (saveOnQuit) SaveNow();
    }

    void OnDestroy() {
        if (autosaveRoutine != null) StopCoroutine(autosaveRoutine);
    }
}
