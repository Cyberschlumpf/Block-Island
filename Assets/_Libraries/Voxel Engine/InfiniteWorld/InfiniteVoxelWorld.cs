using UnityEngine;
using System.Collections.Generic;

// Sparse voxel storage. Unmodified blocks are regenerated from seed; only edits are retained.
public class InfiniteVoxelWorld : MonoBehaviour {
    public InfiniteTerrainGenerator generator;
    readonly Dictionary<InfiniteChunkKey, Dictionary<int,DataBlock>> edits =
        new Dictionary<InfiniteChunkKey, Dictionary<int,DataBlock>>();

    void Awake() { if(generator==null) generator=GetComponent<InfiniteTerrainGenerator>(); }
    void Start() {
        // Stage 6.6.2: autosave is active again. Save loader uses the new clean-world slot.
        Debug.LogWarning("STAGE 6.7.1: save loading disabled for clean coastline validation; procedural data only.");
    }

    static int LocalIndex(Vector3i p) => p.x + Chunk.X_SIZE*(p.y + Chunk.Y_SIZE*p.z);

    public DataBlock GetBlock(int x,int y,int z) {
        if(generator==null) generator=GetComponent<InfiniteTerrainGenerator>();
        if(generator==null) return default(DataBlock);
        var ck=InfiniteWorldMath.WorldToChunk(x,y,z);
        var lp=InfiniteWorldMath.WorldToLocal(x,y,z);
        Dictionary<int,DataBlock> c; DataBlock b;
        if(edits.TryGetValue(ck,out c) && c.TryGetValue(LocalIndex(lp),out b)) return b;
        return generator.Sample(x,y,z);
    }
    // 6.14.13: fill a complete render chunk in one pass. Tanviir can read its immutable
    // BIR1 base directly; only the sparse player edits for this chunk are overlaid afterwards.
    public void FillChunk(InfiniteChunkKey key, DataBlock[] dst) {
        if(dst==null || dst.Length < Chunk.X_SIZE*Chunk.Y_SIZE*Chunk.Z_SIZE) return;
        int ox=key.x*Chunk.X_SIZE, oy=key.y*Chunk.Y_SIZE, oz=key.z*Chunk.Z_SIZE;
        if(BlockIslandWorldSource.BlockIslandNativeWorldActive && BlockIslandNativeWorld.Available) {
            BlockIslandNativeWorld.FillChunk(dst,ox,oy,oz);
            Dictionary<int,DataBlock> bc;
            if(edits.TryGetValue(key,out bc)) foreach(var e in bc) if((uint)e.Key < (uint)dst.Length) dst[e.Key]=e.Value;
            return;
        }
        if(TanviirImportWorld.active && TanviirNativeWorld.Available) {
            TanviirImportWorld.FillChunk(dst,ox,oy,oz);
            Dictionary<int,DataBlock> c;
            if(edits.TryGetValue(key,out c)) foreach(var e in c)
                if((uint)e.Key < (uint)dst.Length) dst[e.Key]=e.Value;
            return;
        }
        int i=0;
        for(int z=0;z<Chunk.Z_SIZE;z++) for(int y=0;y<Chunk.Y_SIZE;y++) for(int x=0;x<Chunk.X_SIZE;x++,i++)
            dst[i]=GetBlock(ox+x,oy+y,oz+z);
    }

    public void SetBlock(int x,int y,int z,DataBlock b) {
        var ck=InfiniteWorldMath.WorldToChunk(x,y,z);
        var lp=InfiniteWorldMath.WorldToLocal(x,y,z);
        Dictionary<int,DataBlock> c;
        if(!edits.TryGetValue(ck,out c)) edits[ck]=c=new Dictionary<int,DataBlock>();
        c[LocalIndex(lp)]=b;
    }
    public void SetEditByIndex(InfiniteChunkKey key,int index,DataBlock b) { Dictionary<int,DataBlock> c; if(!edits.TryGetValue(key,out c)) edits[key]=c=new Dictionary<int,DataBlock>(); c[index]=b; }
    public Dictionary<InfiniteChunkKey, Dictionary<int,DataBlock>> GetEdits() => edits;
}
