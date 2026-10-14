using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("VoxelEngine/Map")]
[RequireComponent( typeof(SunLightmap) )]
[RequireComponent( typeof(GlowLightmap) )]
public class Map : MonoBehaviour {

    public const int X_SIZE = ChunkGrid.X_SIZE * Chunk.X_SIZE;
    public const int Y_SIZE = ChunkGrid.Y_SIZE * Chunk.Y_SIZE;
    public const int Z_SIZE = ChunkGrid.Z_SIZE * Chunk.Z_SIZE;

    public const int X_MASK = X_SIZE - 1;
    public const int Y_MASK = Y_SIZE - 1;
    public const int Z_MASK = Z_SIZE - 1;
	
	private static Map _instance;
	public static Map instance {
		get {
			return _instance;
		}
	}

    public readonly ChunkGrid grid = new ChunkGrid();

    [System.NonSerialized] public InfiniteVoxelWorld infiniteWorld;
    [System.NonSerialized] public InfiniteChunkMeshRenderer infiniteRenderer;


    public static Map Create() {
        GameObject go = new GameObject( "Map", typeof(Map), typeof(SunLightmap), typeof(GlowLightmap) );
        return go.GetComponent<Map>();
    }

	
	void Awake() {
        _instance = this;
	}

	
	public void SetBlock(Vector3i pos, Block block) {
		SetBlock(pos, new DataBlock(block));
	}
	public void SetBlock(int x, int y, int z, Block block) {
		SetBlock(x, y, z, new DataBlock(block));
	}
	
	public void SetBlock(Vector3i pos, DataBlock block) {
        SetBlock( pos.x, pos.y, pos.z, block );
	}
	public void SetBlock(int x, int y, int z, DataBlock block) {
        if(infiniteWorld != null) { infiniteWorld.SetBlock(x,y,z,block); if(infiniteRenderer!=null) infiniteRenderer.MarkDirtyAtWorld(x,y,z); return; }
        Chunk chunk = grid.GetChunkInstance( WorldPosition.ToChunkPosition( x, y, z ) );
        if(chunk != null) chunk.SetBlock( WorldPosition.ToLocalPosition( x, y, z ), block );
	}
	
	public DataBlock GetBlock(Vector3i pos) {
		return GetBlock(pos.x, pos.y, pos.z);
	}
	public DataBlock GetBlock(int x, int y, int z) {
        if(infiniteWorld != null) return infiniteWorld.GetBlock(x,y,z);
        if(WorldPosition.Check( x, y, z )) {
            int chunkIndex, localIndex;
            WorldPosition.ToChunkAndLocalIndex( x, y, z, out chunkIndex, out localIndex );

            Chunk chunk = grid.Get( chunkIndex );
            if(chunk != null) return chunk.GetBlock( localIndex );
        }
        return default(DataBlock);
	}

    public DataBlock GetBlock(ChunkPosition chunkPos, LocalPosition localPos) {
        Chunk chunk = grid.chunks[ chunkPos.ToIndex() ];
        if(chunk != null) return chunk.blocks[ localPos.ToIndex() ];
        return default( DataBlock );
    }
    public DataBlock GetBlock(int chunkIndex, int localIndex) {
        Chunk chunk = grid.chunks[ chunkIndex ];
        if(chunk != null) return chunk.blocks[ localIndex ];
        return default( DataBlock );
    }

	
	public int GetMaxY(int x, int z) {
        ChunkPosition chunkPos = WorldPosition.ToChunkPosition( x, Map.Y_SIZE, z );
        LocalPosition localPos = WorldPosition.ToLocalPosition( x, 0,          z );
		
		for(; chunkPos.y >= 0; chunkPos.y--) {
            Chunk chunk = grid.Get( chunkPos );
			if(chunk == null) continue;
			
			localPos.y = Chunk.Y_SIZE-1;
			for(;localPos.y >= 0; localPos.y--) {
				DataBlock block = chunk.GetBlock(localPos);
                if(!block.IsEmpty()) return WorldPosition.ToWorldPosition( chunkPos, localPos ).y;
			}
		}
		
		return 0;
	}
	
}