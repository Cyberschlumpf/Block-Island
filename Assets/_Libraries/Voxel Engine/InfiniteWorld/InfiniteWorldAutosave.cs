using UnityEngine;

// Stage 6.14.14: dedicated Tanviir persistence. The BIR1 base world is immutable;
// only Block Island player edits (including air/removals) plus player transform are saved.
public sealed class InfiniteWorldAutosave : MonoBehaviour {
    public float intervalSeconds = 30f;
    float nextSave;
    InfiniteVoxelWorld world;

    void Start() { world=GetComponent<InfiniteVoxelWorld>(); nextSave=Time.unscaledTime+intervalSeconds; }
    void Update() { if(Time.unscaledTime<nextSave) return; nextSave=Time.unscaledTime+intervalSeconds; SaveNow(); }

    public bool SaveNow() {
        if(world==null) world=GetComponent<InfiniteVoxelWorld>();
        if(world==null || string.IsNullOrEmpty(InfiniteWorldSave.CurrentWorldName)) return false;
        bool ok=InfiniteWorldSave.SaveCurrent(world);
        if(ok && InfiniteDayNightCycle.Instance!=null) InfiniteDayNightCycle.Instance.SavePrefs();
        return ok;
    }
    void OnApplicationPause(bool paused) { if(paused) SaveNow(); }
    void OnApplicationFocus(bool focused) { if(!focused) SaveNow(); }
    void OnApplicationQuit() { SaveNow(); }
}
