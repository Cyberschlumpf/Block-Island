using UnityEngine;

// Dependency-free diagnostics: no IMGUI, UnityEngine.UI or TextMeshPro.
public sealed class InfiniteWorldDebugHUD : MonoBehaviour {
    InfiniteChunkStreamer streamer;
    InfiniteTerrainGenerator terrain;
    Transform target;
    float nextReport;

    void Start() {
        streamer=GetComponent<InfiniteChunkStreamer>();
        terrain=GetComponent<InfiniteTerrainGenerator>();
        target=streamer!=null ? streamer.target : null;
        Debug.LogWarning("=== INFINITE WORLD DIAGNOSTICS ACTIVE ===");
    }

    void Update() {
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        if(Time.unscaledTime < nextReport) return;
        nextReport=Time.unscaledTime+5f;
        if(target==null) {
            Debug.LogWarning("INFINITE WORLD: waiting for Camera.main / streaming target");
            return;
        }
        Vector3 p=target.position;
        InfiniteChunkKey c=InfiniteWorldMath.WorldToChunk(
            Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z));
        int sy=terrain!=null ? terrain.SurfaceY(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z)) : 0;
        Debug.Log("INFINITE STATUS world="+p.ToString("F1")+" chunk="+c+
            " surfaceY="+sy+" loaded="+(streamer!=null?streamer.ActiveChunkCount:0)+
            " queued="+(streamer!=null?streamer.QueuedChunkCount:0)+
            " seed="+(terrain!=null?terrain.seed:0));
    }
}
