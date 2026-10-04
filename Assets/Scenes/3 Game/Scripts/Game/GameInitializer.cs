using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameInitializer : MonoBehaviour {

    public GameInitializer instance {
        get {
            return FindFirstObjectByType<GameInitializer>();
        }
    }

	public BlockSet defaultBlockSet;
	public Map defaultMap;

    IEnumerator Start() {
        if (defaultBlockSet != null) defaultBlockSet.gameObject.SetActive(true);
        if (defaultMap != null) defaultMap.gameObject.SetActive(true);

        Map map = Map.instance;

        // Autosave is attached at runtime so old binary scenes do not need manual editing.
        if (FindFirstObjectByType<AutosaveManager>() == null) {
            gameObject.AddComponent<AutosaveManager>();
        }

        // This initializer is serialized in the original Game scene and therefore is our reliable
        // bridge from the Unity-5.5 binary scene to the Unity-6000 infinite-world pipeline.
        InfiniteVoxelWorld infinite = InfiniteWorldBootstrap.InstallNow();
        if (infinite == null) infinite = FindFirstObjectByType<InfiniteVoxelWorld>();
        if (infinite != null && infinite.generator != null) {
            // 6.14.14.2: restore a saved Tanviir transform BEFORE the first frame is yielded.
            // With radius 16, yielding first allowed InfiniteChunkStreamer.Update() to queue the
            // entire 33x33 area around the scene/default camera position. The player was then
            // teleported to the save position while that stale queue was deliberately retained,
            // making the loaded world look empty until a block edit forced a local rebuild.
            GameObject infinitePlayer = GameObject.FindWithTag("Player");
            if (infinitePlayer != null) {
                if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity && !InfiniteWorldSave.HasLoadedPlayerTransform) { infinitePlayer.transform.position=new Vector3(0,0,0); infinitePlayer.transform.rotation=Quaternion.identity; }
                if(InfiniteWorldSave.HasLoadedPlayerTransform) {
                    infinitePlayer.transform.position = InfiniteWorldSave.LoadedPlayerPosition;
                    infinitePlayer.transform.rotation = InfiniteWorldSave.LoadedPlayerRotation;
                    InfiniteWorldSave.HasLoadedPlayerTransform=false;
                } else if(InfiniteWorldSave.CurrentWorldType==InfiniteWorldSave.WorldType.Relativity) {
                    infinitePlayer.transform.position = new Vector3(0,0,0);
                    infinitePlayer.transform.rotation = Quaternion.identity;
                } else {
                    int sx = 0, sz = 0;
                    int sy = infinite.generator.SurfaceY(sx, sz) + 3;
                    infinitePlayer.transform.position = new Vector3(sx, sy, sz);
                }
                infinitePlayer.SetActive(true);
            }
            // Give camera-follow scripts one frame to catch up only AFTER the player is already
            // at the correct saved position. Streaming therefore starts from the correct area.
            yield return null;
            yield break;
        }
        yield return null;

        Vector3i size = ComputeSize(map);
        while(size.x <= 1) {
            yield return null;
            size = ComputeSize( map );
        }

        GameObject player = GameObject.FindWithTag("Player");
        player.SetActive(false);

        Vector3i pos = (size / 2).Mul(Chunk.SIZE);
        pos.y = map.GetMaxY(pos.x, pos.z) + 2;

        player.transform.position = pos;
        player.SetActive(true);

        RuntimeBuilder[] builder = map.GetComponentsInChildren<RuntimeBuilder>(true);
        if (builder.Length == 1) builder[0].enabled = true;
    }

    private static Vector3i ComputeSize(Map map) {
        Vector3i max = Vector3i.zero;
        foreach(var chunk in map.grid.chunks) {
            if(chunk != null) max = Vector3i.Max(max, (Vector3i) chunk.position);
        }
        return max + Vector3i.one;
    }

    public void OnDestroy() {
        if (BlockSet.instance) Destroy(BlockSet.instance.gameObject);
        if (Map.instance) Destroy(Map.instance.gameObject);
    }

}
