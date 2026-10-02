using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public sealed class InfiniteOnlyRenderGuard : MonoBehaviour {
    float nextCheck;
    void LateUpdate() {
        if(SceneManager.GetActiveScene().name!="Game") return;
        if(Time.unscaledTime < nextCheck) return;
        nextCheck=Time.unscaledTime+0.5f;
        // 6.6.9: legacy voxel renderers are forbidden continuously; only InfiniteChunk objects may render voxel geometry.
        foreach(var r in Object.FindObjectsByType<ChunkRenderer>(FindObjectsInactive.Include)) {
            if(r==null) continue;
            Debug.LogWarning("INFINITE-ONLY GUARD: removed late legacy ChunkRenderer: "+r.name);
            r.enabled=false;
            Destroy(r.gameObject);
        }
        foreach(var b in Object.FindObjectsByType<RuntimeBuilder>(FindObjectsInactive.Include)) {
            if(b==null) continue;
            b.StopAllCoroutines();
            b.enabled=false;
        }
    }
}
