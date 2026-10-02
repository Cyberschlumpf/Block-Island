using UnityEngine;

// Stage 6.14.14: dedicated Tanviir persistence. The BIR1 base world is immutable;
// only Block Island player edits (including air/removals) plus player transform are saved.
public sealed class InfiniteWorldAutosave : MonoBehaviour {
    public float intervalSeconds = 60f;
    float nextSave;
    InfiniteVoxelWorld world;

    void Start() {
        world = GetComponent<InfiniteVoxelWorld>();
        nextSave = Time.unscaledTime + intervalSeconds;
    }

    void Update() {
        if(InfiniteWorldSave.CurrentWorldType != InfiniteWorldSave.WorldType.Tanviir && InfiniteWorldSave.CurrentWorldType != InfiniteWorldSave.WorldType.BlockIsland && InfiniteWorldSave.CurrentWorldType != InfiniteWorldSave.WorldType.LightGarden) return;
        if(Time.unscaledTime < nextSave) return;
        nextSave = Time.unscaledTime + intervalSeconds;
        SaveFixedWorld();
    }

    public bool SaveFixedWorld() {
        if(world==null) world=GetComponent<InfiniteVoxelWorld>();
        if(world==null) return false;
        string n=InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.BlockIsland?InfiniteWorldSave.BlockIslandSaveName:(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.LightGarden?InfiniteWorldSave.LightGardenSaveName:InfiniteWorldSave.TanviirSaveName);
        bool ok=InfiniteWorldSave.Save(world,n);
        if(ok && InfiniteDayNightCycle.Instance!=null) InfiniteDayNightCycle.Instance.SavePrefs();
        return ok;
    }

    void OnApplicationQuit() {
        if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Tanviir || InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.BlockIsland || InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.LightGarden) SaveFixedWorld();
    }
}
