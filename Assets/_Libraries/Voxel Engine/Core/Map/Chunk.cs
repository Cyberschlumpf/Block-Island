using UnityEngine;
using System.Collections;

public class Chunk {
	
    public const int X_BITS = 5;
    public const int Y_BITS = 5;
    public const int Z_BITS = 5;

	public const int X_SIZE = 1 << X_BITS; // 32
    public const int Y_SIZE = 1 << Y_BITS; // 32
    public const int Z_SIZE = 1 << Z_BITS; // 32

    public const int X_MASK = X_SIZE - 1;
    public const int Y_MASK = Y_SIZE - 1;
    public const int Z_MASK = Z_SIZE - 1;

    public const int X_MAX = X_SIZE - 1;
    public const int Y_MAX = Y_SIZE - 1;
    public const int Z_MAX = Z_SIZE - 1;

    public const int ARRAY_SIZE = Z_SIZE * Y_SIZE * X_SIZE;

	public static readonly Vector3i SIZE = new Vector3i(X_SIZE, Y_SIZE, Z_SIZE);

    private const int LOCAL_MAX_X_MASK = Chunk.X_MASK; // 001
    private const int LOCAL_MIN_X_MASK = ~LOCAL_MAX_X_MASK; // 110

    private const int LOCAL_MAX_Y_MASK = Chunk.Y_MASK << Chunk.X_BITS; // 010
    private const int LOCAL_MIN_Y_MASK = ~LOCAL_MAX_Y_MASK; //101

    private const int LOCAL_MAX_Z_MASK = Chunk.Z_MASK << Chunk.Y_BITS << Chunk.X_BITS; // 100
    private const int LOCAL_MIN_Z_MASK = ~LOCAL_MAX_Z_MASK; // 011

    private const int CHUNK_DX = 1;
    private const int CHUNK_DY = ChunkGrid.X_SIZE;
    private const int CHUNK_DZ = ChunkGrid.X_SIZE * ChunkGrid.Y_SIZE;

    private const int LOCAL_DX = 1;
    private const int LOCAL_DY = Chunk.X_SIZE;
    private const int LOCAL_DZ = Chunk.X_SIZE * Chunk.Y_SIZE;


    public readonly ChunkPosition position;
    private readonly int index;
    public readonly DataBlock[] blocks = new DataBlock[ARRAY_SIZE];
	private ChunkRenderer chunkRenderer;
	
	public Chunk(ChunkPosition position) {
		this.position = position;
        this.index = position.ToIndex();
	}
	
	public ChunkRenderer GetChunkRendererInstance() {
		if(chunkRenderer == null) chunkRenderer = ChunkRenderer.CreateChunkRenderer(position, this);
		return chunkRenderer;
	}
	public ChunkRenderer GetChunkRenderer() {
		return chunkRenderer;
	}

	
	public void SetBlock(LocalPosition pos, DataBlock block) {
        int index = pos.ToIndex();
        blocks[index] = block;
	}
	public void SetBlock(int x, int y, int z, DataBlock block) {
        int index = LocalPosition.ToIndex( x, y, z );
        blocks[index] = block;
	}
    public void SetBlock(int index, DataBlock block) {
        blocks[index] = block;
    }
	
	public DataBlock GetBlock(LocalPosition pos) {
        int index = pos.ToIndex();
        return blocks[index];
	}
	public DataBlock GetBlock(int x, int y, int z) {
        int index = LocalPosition.ToIndex( x, y, z );
        return blocks[index];
	}
    public DataBlock GetBlock(int index) { 
        return blocks[index];
    }


    /*public DataBlock SafeGetBlock(LocalPosition localPos) {
        if(localPos.Check()) {
            int index = localPos.ToIndex();
            return blocks[index];
        }

        ChunkPosition chunkPos = position;
        WorldPosition.FixCoord( ref chunkPos, ref localPos );

        Chunk chunk = Map.instance.grid.Get(chunkPos);
        if(chunk != null) return chunk.GetBlock(localPos);
        return default(DataBlock);
    }
	*/


    public DataBlock GetAdjacentBlock(int localIndex, CubeSide side) {
        switch(side) {
            case CubeSide.Front: return GetFrontBlock( localIndex );
            case CubeSide.Back: return GetBackBlock( localIndex );

            case CubeSide.Right: return GetRightBlock( localIndex );
            case CubeSide.Left: return GetLeftBlock( localIndex );

            case CubeSide.Top: return GetTopBlock( localIndex );
            case CubeSide.Bottom: return GetBottomBlock( localIndex );
        }
        return default(DataBlock);
    }


    public DataBlock GetFrontBlock(int localIndex) {
        if((localIndex | LOCAL_MAX_Z_MASK) == localIndex) { // z == max
            if(position.z == ChunkGrid.Z_MAX) return default( DataBlock );

            int chunkIndex = this.index + CHUNK_DZ;
            localIndex -= LOCAL_DZ * Chunk.Z_MAX;
            return Map.instance.GetBlock( chunkIndex, localIndex );
        } else {
            return blocks[localIndex + LOCAL_DZ];
        }
    }

    public DataBlock GetBackBlock(int localIndex) {
        if((localIndex & LOCAL_MIN_Z_MASK) == localIndex) { // z == min
            if(position.z == 0) return default( DataBlock );

            int chunkIndex = this.index - CHUNK_DZ;
            localIndex += LOCAL_DZ * Chunk.Z_MAX;
            return Map.instance.GetBlock( chunkIndex, localIndex );
        } else {
            return blocks[localIndex - LOCAL_DZ];
        }
    }

    public DataBlock GetRightBlock(int localIndex) {
        if((localIndex | LOCAL_MAX_X_MASK) == localIndex) { // x == max
            if(position.x == ChunkGrid.X_MAX) return default( DataBlock );

            int chunkIndex = this.index + CHUNK_DX;
            localIndex -= LOCAL_DX * Chunk.X_MAX;
            return Map.instance.GetBlock( chunkIndex, localIndex );
        } else {
            return blocks[localIndex + LOCAL_DX];
        }
    }

    public DataBlock GetLeftBlock(int localIndex) {
        if((localIndex & LOCAL_MIN_X_MASK) == localIndex) { // x == min
            if(position.x == 0) return default( DataBlock );

            int chunkIndex = this.index - CHUNK_DX;
            localIndex += LOCAL_DX * Chunk.X_MAX;
            return Map.instance.GetBlock( chunkIndex, localIndex );
        } else {
            return blocks[localIndex - LOCAL_DX];
        }
    }

    public DataBlock GetTopBlock(int localIndex) {
        if((localIndex | LOCAL_MAX_Y_MASK) == localIndex) { // y == max
            if(position.y == ChunkGrid.Y_MAX) return default( DataBlock );

            int chunkIndex = this.index + CHUNK_DY;
            localIndex -= LOCAL_DY * Chunk.Y_MAX;
            return Map.instance.GetBlock( chunkIndex, localIndex );
        } else {
            return blocks[localIndex + LOCAL_DY];
        }
    }

    public DataBlock GetBottomBlock(int localIndex) {
        if((localIndex & LOCAL_MIN_Y_MASK) == localIndex) { // y == min
            if(position.y == 0) return default( DataBlock );

            int chunkIndex = this.index - CHUNK_DY;
            localIndex += LOCAL_DY * Chunk.Y_MAX;
            return Map.instance.GetBlock( chunkIndex, localIndex );
        } else {
            return blocks[localIndex - LOCAL_DY];
        }
    }


}
