using UnityEngine;
using System.Collections;

public static class MapBuilder {

    public static void SetBlockAndRebuild(this Map map, Vector3i pos, DataBlock block) {
        if(map.infiniteWorld != null) {
            if(!block.IsEmpty() && !block.block.CanCreateBlock(pos)) return;
            DataBlock old=map.GetBlock(pos);
            if(!old.IsEmpty()) old.block.OnBlockDestroy(pos);
            if(!block.IsEmpty()) block.block.OnBlockCreate(pos, block.direction);
            map.SetBlock(pos, block);
            foreach(Vector3i dir in Vector3i.directions) { Block n=map.GetBlock(pos+dir).block; if(n!=null) n.OnNeighborBlockChanged(pos+dir,pos); }
            return;
        }
        SunLightmap sunmap = SunLightmap.instance;
        GlowLightmap glowmap = GlowLightmap.instance;

        if(!block.IsEmpty() && !block.block.CanCreateBlock( pos )) return;

        DataBlock oldBlock = map.GetBlock( pos );
        if(!oldBlock.IsEmpty()) oldBlock.block.OnBlockDestroy( pos );
        if(!block.IsEmpty()) block.block.OnBlockCreate( pos, block.direction );

        map.SetBlock( pos, block );

        foreach(Vector3i dir in Vector3i.directions) {
            Block nblock = map.GetBlock( pos + dir ).block;
            if(nblock != null) nblock.OnNeighborBlockChanged( pos + dir, pos );
        }

        if(block.IsEmpty()) {
            SunLightComputer.RemoveBlock( sunmap, map, pos );
            GlowLightComputer.RemoveBlock( glowmap, map, pos );
        } else {
            SunLightComputer.AddBlock( sunmap, map, pos );
            GlowLightComputer.AddBlock( glowmap, map, pos );
        }

        MarkBlockAsDirty( map, pos );

        LocalPosition localPos = WorldPosition.ToLocalPosition( pos );

        if(localPos.x == 0) MarkBlockAsDirty( map, pos - Vector3i.right );
        if(localPos.y == 0) MarkBlockAsDirty( map, pos - Vector3i.up );
        if(localPos.z == 0) MarkBlockAsDirty( map, pos - Vector3i.forward );

        if(localPos.x == Chunk.X_SIZE - 1) MarkBlockAsDirty( map, pos + Vector3i.right );
        if(localPos.y == Chunk.Y_SIZE - 1) MarkBlockAsDirty( map, pos + Vector3i.up );
        if(localPos.z == Chunk.Z_SIZE - 1) MarkBlockAsDirty( map, pos + Vector3i.forward );
    }

    public static void MarkBlockAsDirty(this Map map, Vector3i worldPos) {
        Chunk chunk = map.grid.Get( WorldPosition.ToChunkPosition( worldPos ) );
        if(chunk != null) chunk.GetChunkRendererInstance().SetDirty();
    }

    public static void MarkBlockLightAsDirty(this Map map, Vector3i worldPos) {
        Chunk chunk = map.grid.Get( WorldPosition.ToChunkPosition( worldPos ) );
        if(chunk != null) {
            ChunkRenderer renderer = chunk.GetChunkRenderer();
            if(renderer != null) renderer.SetDirtyLight();
        }
    }


    public static void Clear(this Map map) {
        foreach(var chunk in map.grid.chunks) {
            if(chunk != null) {
                System.Array.Clear( chunk.blocks, 0, Chunk.ARRAY_SIZE );

                ChunkRenderer chunkRenderer = chunk.GetChunkRenderer();
                if(chunkRenderer) Object.DestroyImmediate( chunkRenderer.gameObject );
            }
        }
    }

    public static void Build(this Map map) {
        foreach(var chunk in map.grid.chunks) {
            if(chunk != null) chunk.GetChunkRendererInstance().Build();
        }
    }


}
