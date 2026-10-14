using UnityEngine;
using System.Collections;

// position in map
public static class WorldPosition {


    public static ChunkPosition ToChunkPosition(Vector3i pos) {
        return ToChunkPosition( pos.x, pos.y, pos.z );
    }
    public static ChunkPosition ToChunkPosition(int x, int y, int z) {
        ChunkPosition pos;
        pos.x = (sbyte) (x >> Chunk.X_BITS);
        pos.y = (sbyte) (y >> Chunk.Y_BITS);
        pos.z = (sbyte) (z >> Chunk.Z_BITS);
        return pos;
    }


    public static LocalPosition ToLocalPosition(Vector3i pos) {
        return ToLocalPosition( pos.x, pos.y, pos.z );
    }
    public static LocalPosition ToLocalPosition(int x, int y, int z) {
        LocalPosition pos;
        pos.x = (sbyte) (x & Chunk.X_MASK);
        pos.y = (sbyte) (y & Chunk.Y_MASK);
        pos.z = (sbyte) (z & Chunk.Z_MASK);
        return pos;
    }



    public static bool Check(Vector3i pos) {
        return Check( pos.x, pos.y, pos.z );
    }
    public static bool Check(int x, int y, int z) {
        return (x & Map.X_MASK) == x &&
                (y & Map.Y_MASK) == y &&
                (z & Map.Z_MASK) == z;
    }

    public static void ToChunkAndLocalIndex(Vector3i pos, out int chunkIndex, out int localIndex) {
        ToChunkAndLocalIndex( pos.x, pos.y, pos.z, out chunkIndex, out localIndex );
    }

    public static void ToChunkAndLocalIndex(int x, int y, int z, out int chunkIndex, out int localIndex) {
        int cx = x >> Chunk.X_BITS;
        int cy = y >> Chunk.Y_BITS;
        int cz = z >> Chunk.Z_BITS;

        int lx = x & Chunk.X_MASK;
        int ly = y & Chunk.Y_MASK;
        int lz = z & Chunk.Z_MASK;

        const int GRID_XY_BITS = ChunkGrid.X_BITS + ChunkGrid.Y_BITS;
        chunkIndex = (cz << GRID_XY_BITS) | (cy << ChunkGrid.X_BITS) | cx;

        const int CHUNK_XY_BITS = Chunk.X_BITS + Chunk.Y_BITS;
        localIndex = (lz << CHUNK_XY_BITS) | (ly << Chunk.X_BITS) | lx;
    }

    public static Vector3i ToWorldPosition(ChunkPosition chunkPos, LocalPosition localPos) {
        Vector3i worldPos;
        worldPos.x = (chunkPos.x << Chunk.X_BITS) + localPos.x;
        worldPos.y = (chunkPos.y << Chunk.Y_BITS) + localPos.y;
        worldPos.z = (chunkPos.z << Chunk.Z_BITS) + localPos.z;
        return worldPos;
    }

    public static void FixCoord(ref ChunkPosition chunkPos, ref LocalPosition localPos) {
        if(localPos.x < 0) {
            chunkPos.x--;
            localPos.x += Chunk.X_SIZE;
        }
        if(localPos.y < 0) {
            chunkPos.y--;
            localPos.y += Chunk.Y_SIZE;
        }
        if(localPos.z < 0) {
            chunkPos.z--;
            localPos.z += Chunk.Z_SIZE;
        }

        if(localPos.x >= Chunk.X_SIZE) {
            chunkPos.x++;
            localPos.x -= Chunk.X_SIZE;
        }
        if(localPos.y >= Chunk.Y_SIZE) {
            chunkPos.y++;
            localPos.y -= Chunk.Y_SIZE;
        }
        if(localPos.z >= Chunk.Z_SIZE) {
            chunkPos.z++;
            localPos.z -= Chunk.Z_SIZE;
        }
    }
	
}
