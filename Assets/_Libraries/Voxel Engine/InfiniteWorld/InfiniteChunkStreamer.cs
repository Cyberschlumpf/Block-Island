using UnityEngine;
using System.Collections.Generic;

// Stage 6.14.6 Tanviir mesh-path performance + completeness coordinator. Keeps a bounded set of chunk coordinates around the player.
// Mesh adapter is deliberately isolated so the legacy ChunkBuilder can be migrated safely.
public class InfiniteChunkStreamer : MonoBehaviour {
    public Transform target;
    [Range(2,16)] public int horizontalViewDistance=3;
    [Range(1,8)] public int verticalViewDistance=2;
    public int maxChunkTransitionsPerFrame=1;
    [Range(2,24)] public int tanviirViewDistance=16;
    // 6.14.11: hard runtime safety cap. Older scenes can have the former value 16 serialized,
    // so changing the field default alone would not fix existing projects.
    [Range(3,24)] public int tanviirRuntimeViewDistanceCap=16;
    [Range(1,16)] public int tanviirColumnsPerFrame=2;
    [Header("Tanviir adaptive streaming")] public float tanviirTargetFps=60f;
    [Range(1,8)] public int tanviirMinColumnsPerFrame=1;
    [Range(4,16)] public int tanviirMaxColumnsPerFrame=10;
    [Range(1,6)] public int tanviirMaxChunkBuildsPerFrame=1;
    // Stage 6.14.3: radius 16, but NEVER create empty repair chunks across the whole view radius.
    // Missing chunks are created on demand by the builder/dirty path only. This avoids thousands of empty meshes.
    // Discovery is still frame-budgeted; cache is enlarged in TanviirNativeWorld.
    public int belowSurfaceChunks=2;
    public int aboveSurfaceChunks=2;
    [Header("Tanviir diagnostics")] public bool tanviirHoleDiagnostics=true;
    InfiniteTerrainGenerator terrain;

    readonly HashSet<InfiniteChunkKey> active=new HashSet<InfiniteChunkKey>();
    readonly Queue<InfiniteChunkKey> loadQueue=new Queue<InfiniteChunkKey>();
    readonly HashSet<InfiniteChunkKey> queued=new HashSet<InfiniteChunkKey>();
    readonly Queue<Vector3i> columnQueue=new Queue<Vector3i>();
    readonly HashSet<long> queuedColumns=new HashSet<long>();
    InfiniteChunkKey lastCenter; bool haveLastCenter;
    public int ActiveChunkCount { get { return active.Count; } }
    public int QueuedChunkCount { get { return queued.Count; } }

    void Start() {
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        terrain=GetComponent<InfiniteTerrainGenerator>();
        // 0.9.9.9c: Lichtgarten is a large authored sky world. Keep the same 16-chunk
        // horizontal visibility target used by the large fixed worlds instead of the
        // procedural-island default of 3 chunks.
        if(terrain!=null && terrain.lightGarden) horizontalViewDistance=16;
    }
    void Update() {
        if(target==null && Camera.main!=null) target=Camera.main.transform;
        if(target==null) return;
        var p=target.position;
        var center=InfiniteWorldMath.WorldToChunk(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z));
        // 6.13.13: discovering a Tanviir column can decode an Anvil chunk. Never discover an
        // entire view radius in one frame; that was the source of the 2-10 FPS / frozen loading bursts.
        if(!haveLastCenter || center.x!=lastCenter.x || center.z!=lastCenter.z || (!BlockIslandWorldSource.FixedWorldActive && center.y!=lastCenter.y)) {
            // 6.14.5 completeness fix: Tanviir architecture is column-based. Do NOT throw away
            // unfinished native-world work whenever the player crosses a chunk boundary. That
            // could permanently leave visible holes while walking/flying through the city.
            // Existing queued work is retained; HashSets suppress duplicates and distance-based
            // release still removes geometry that is truly outside the radius.
            QueueWanted(center); lastCenter=center; haveLastCenter=true;
        }
        // 6.14.12: the old periodic Tanviir completeness sweep was a workaround for the
        // former 16/32 chunk-mapping bug.  6.14.11.4 fixed that bug at the source, so repeating
        // discovery across the whole radius every two seconds only burns CPU/cache locks.
        // Normal movement-driven QueueWanted() remains the sole discovery path.
        int columnBudget=BlockIslandWorldSource.FixedWorldActive ? AdaptiveColumnBudget() : int.MaxValue;
        while(columnBudget-->0 && columnQueue.Count>0) {
            Vector3i col=columnQueue.Dequeue(); queuedColumns.Remove(ColumnKey(col.x,col.z));
            QueueColumn(col.x,col.z,col.y);
        }
        int n=BlockIslandWorldSource.FixedWorldActive ? AdaptiveChunkBudget() : maxChunkTransitionsPerFrame;
        // Native Tanviir meshes are dense. At very low FPS, one mesh build every frame keeps
        // the game permanently saturated. Back off for a few frames so input/rendering can recover.
        if(BlockIslandWorldSource.FixedWorldActive && n>0) {
            float fps=1f/Mathf.Max(0.001f,Time.unscaledDeltaTime);
            int stride = fps<12f ? 8 : (fps<24f ? 4 : (fps<45f ? 2 : 1));
            if((Time.frameCount % stride)!=0) n=0;
        }
        while(n-->0 && loadQueue.Count>0) {
            var k=loadQueue.Dequeue(); queued.Remove(k);
            if(active.Add(k)) SendMessage("OnInfiniteChunkNeeded", k, SendMessageOptions.DontRequireReceiver);
        }
        var remove=new List<InfiniteChunkKey>();
        foreach(var k in active)
            if(Mathf.Abs(k.x-center.x)>(BlockIslandWorldSource.FixedWorldActive?EffectiveTanviirViewDistance():horizontalViewDistance)+1 ||
               Mathf.Abs(k.z-center.z)>(BlockIslandWorldSource.FixedWorldActive?EffectiveTanviirViewDistance():horizontalViewDistance)+1) remove.Add(k);
            else if(!BlockIslandWorldSource.FixedWorldActive) {
                int wx=k.x*Chunk.X_SIZE+Chunk.X_SIZE/2, wz=k.z*Chunk.Z_SIZE+Chunk.Z_SIZE/2;
                int sy=terrain!=null ? InfiniteWorldMath.FloorDiv(terrain.SurfaceY(wx,wz),Chunk.Y_SIZE) : center.y;
                int minY=Mathf.Min(sy-belowSurfaceChunks-1,center.y-verticalViewDistance-1);
                int maxY=Mathf.Max(sy+aboveSurfaceChunks+1,center.y+verticalViewDistance+1);
                if(k.y<minY || k.y>maxY) remove.Add(k);
            }
        foreach(var k in remove) { active.Remove(k); SendMessage("OnInfiniteChunkReleased",k,SendMessageOptions.DontRequireReceiver); }
    }


    int EffectiveTanviirViewDistance() {
        // Serialized scenes from 6.14.10 may still contain 16. Keep the inspector setting,
        // but cap actual streaming work until a later performance pass proves larger radii safe.
        return Mathf.Clamp(Mathf.Min(tanviirViewDistance,tanviirRuntimeViewDistanceCap),2,24);
    }

    int AdaptiveColumnBudget() {
        float fps=1f/Mathf.Max(0.001f,Time.unscaledDeltaTime);
        if(fps < 18f) return 1;
        if(fps < tanviirTargetFps*0.72f) return tanviirMinColumnsPerFrame;
        if(fps < tanviirTargetFps) return Mathf.Max(tanviirMinColumnsPerFrame,2);
        if(fps > tanviirTargetFps*2.0f) return tanviirMaxColumnsPerFrame;
        if(fps > tanviirTargetFps*1.35f) return Mathf.Min(tanviirMaxColumnsPerFrame,6);
        return Mathf.Clamp(tanviirColumnsPerFrame,tanviirMinColumnsPerFrame,tanviirMaxColumnsPerFrame);
    }
    int AdaptiveChunkBudget() {
        float fps=1f/Mathf.Max(0.001f,Time.unscaledDeltaTime);
        if(fps < tanviirTargetFps*0.78f) return 1;
        if(fps > tanviirTargetFps*1.75f) return tanviirMaxChunkBuildsPerFrame;
        if(fps > tanviirTargetFps*1.20f) return Mathf.Min(2,tanviirMaxChunkBuildsPerFrame);
        return 1;
    }

    static long ColumnKey(int x,int z) { return ((long)x<<32) ^ (uint)z; }
    void QueueWanted(InfiniteChunkKey c) {
        int wantedDistance=BlockIslandWorldSource.FixedWorldActive ? EffectiveTanviirViewDistance() : horizontalViewDistance;
        // Near-to-far order. Tanviir columns are only DISCOVERED here; expensive Anvil access is
        // spread over subsequent frames by tanviirColumnsPerFrame.
        for(int r=0;r<=wantedDistance;r++)
        for(int x=-r;x<=r;x++) {
            int dz=r-Mathf.Abs(x);
            QueueColumnRequest(c.x+x,c.z+dz,c.y);
            if(dz!=0) QueueColumnRequest(c.x+x,c.z-dz,c.y);
        }
    }
    void QueueColumnRequest(int cx,int cz,int py) {
        if(!BlockIslandWorldSource.FixedWorldActive) { QueueColumn(cx,cz,py); return; }
        long k=ColumnKey(cx,cz);
        if(queuedColumns.Add(k)) columnQueue.Enqueue(new Vector3i(cx,py,cz));
    }
    void QueueColumn(int cx,int cz,int playerChunkY) {
        // 6.13.14: never decode .mca/NBT synchronously from Update. Ask the background prefetcher
        // for this Minecraft column and revisit it on a later frame once decoding is complete.
        if(BlockIslandWorldSource.FixedWorldActive && BlockIslandWorldSource.FixedWorldAvailable && !BlockIslandWorldSource.IsColumnReady(cx,cz)) {
            BlockIslandWorldSource.RequestColumn(cx,cz);
            QueueColumnRequest(cx,cz,playerChunkY);
            return;
        }
        // 6.13.12: Tanviir is architecture, not a height-field. A column can contain a bridge,
        // tower, room and street in different Y sections. Queue every NON-EMPTY Block Island 32-high chunk.
        // This fixes missing lower storeys / walls without blindly meshing all 16 vertical chunks.
        if(BlockIslandWorldSource.FixedWorldActive && BlockIslandWorldSource.FixedWorldAvailable) {
            for(int cy=0;cy<Mathf.CeilToInt(256f/Chunk.Y_SIZE);cy++) if(BlockIslandWorldSource.HasBlocksInChunk(cx,cy,cz))
                Enqueue(new InfiniteChunkKey(cx,cy,cz));

            // 6.14.3: DO NOT enqueue empty chunks just to make holes editable. At radius 16 that
            // created several thousand empty chunk builds and crushed the main thread to ~2 FPS.
            // InfiniteChunkMeshRenderer.MarkDirtyAtWorld() creates a missing chunk immediately
            // when the player actually places the first repair block, so editability is preserved.
            return;
        }
        int wx=cx*Chunk.X_SIZE+Chunk.X_SIZE/2;
        int wz=cz*Chunk.Z_SIZE+Chunk.Z_SIZE/2;
        int surfaceY=BlockIslandWorldSource.FixedWorldActive
            ? TanviirImportWorld.SurfaceY(wx,wz)
            : ((terrain!=null && terrain.worldCoordinateProof && terrain.hardCoordinatePlane)
                ? terrain.seaLevel+1
                : (terrain!=null ? terrain.SurfaceY(wx,wz) : playerChunkY*Chunk.Y_SIZE));
        int surfaceChunkY=InfiniteWorldMath.FloorDiv(surfaceY,Chunk.Y_SIZE);
        int seaChunkY=terrain!=null ? InfiniteWorldMath.FloorDiv(terrain.seaLevel,Chunk.Y_SIZE) : surfaceChunkY;

        // There are no caves yet, so fully buried chunks have no visible faces and need not be
        // meshed. Queue only the terrain surface, the chunk above it for trees/flora, the sea
        // surface, and the player's own vertical neighborhood when flying.
        Enqueue(new InfiniteChunkKey(cx,surfaceChunkY,cz));
        Enqueue(new InfiniteChunkKey(cx,surfaceChunkY+1,cz));
        if(seaChunkY!=surfaceChunkY) Enqueue(new InfiniteChunkKey(cx,seaChunkY,cz));
        int liveVertical=BlockIslandWorldSource.FixedWorldActive ? Mathf.Min(verticalViewDistance,1) : verticalViewDistance;
        for(int dy=-liveVertical;dy<=liveVertical;dy++)
            Enqueue(new InfiniteChunkKey(cx,playerChunkY+dy,cz));
    }

    void Enqueue(InfiniteChunkKey k) { if(!active.Contains(k) && queued.Add(k)) loadQueue.Enqueue(k); }
}
