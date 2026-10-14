using UnityEngine;

// Stage 6.14.7 - single Block Island world-source facade.
// The engine consumes Block Island DataBlocks regardless of whether they originate
// from the procedural Island Generator or the fixed native BIR1 Tanviir world.
// BIR1 remains the proven storage format; this layer does not reconvert world data.
public static class BlockIslandWorldSource {
    public static bool BlockIslandNativeWorldActive { get; set; }
    public static bool FixedWorldActive { get { return TanviirImportWorld.active || BlockIslandNativeWorldActive; } }
    public static bool FixedWorldAvailable { get { return BlockIslandNativeWorldActive ? BlockIslandNativeWorld.Available : TanviirNativeWorld.Available; } }

    public static DataBlock Sample(InfiniteTerrainGenerator generator,int x,int y,int z) {
        if(BlockIslandNativeWorldActive) return BlockIslandNativeWorld.Sample(x,y,z);
        if(TanviirImportWorld.active) return TanviirImportWorld.Sample(x,y,z);
        return generator!=null ? generator.SampleProcedural(x,y,z) : default(DataBlock);
    }

    public static int SurfaceY(InfiniteTerrainGenerator generator,int x,int z) {
        if(BlockIslandNativeWorldActive) return BlockIslandNativeWorld.SurfaceY(x,z);
        if(TanviirImportWorld.active) return TanviirImportWorld.SurfaceY(x,z);
        return generator!=null ? generator.SurfaceYProcedural(x,z) : 0;
    }

    // Sparse/native helpers used by the proven 6.14.6 streamer. Keeping these here
    // lets the streamer retain its performant column discovery without owning BIR1 details.
    public static bool IsColumnReady(int cx,int cz) { if(BlockIslandNativeWorldActive)return BlockIslandNativeWorld.IsWorldColumnReady(cx,cz); return !FixedWorldActive || !FixedWorldAvailable || TanviirNativeWorld.IsWorldColumnReady(cx,cz); }
    public static void RequestColumn(int cx,int cz) { if(BlockIslandNativeWorldActive){BlockIslandNativeWorld.RequestWorldColumn(cx,cz);return;} if(FixedWorldActive && FixedWorldAvailable) TanviirNativeWorld.RequestWorldColumn(cx,cz); }
    public static bool HasBlocksInChunk(int cx,int cy,int cz) { if(BlockIslandNativeWorldActive)return BlockIslandNativeWorld.HasBlocksInWorldChunk(cx,cy,cz); return FixedWorldActive && FixedWorldAvailable && TanviirNativeWorld.HasBlocksInWorldChunk(cx,cy,cz); }
}
