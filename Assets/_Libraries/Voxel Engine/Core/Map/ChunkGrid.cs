using UnityEngine;
using System.Collections;

public class ChunkGrid {

    public const int X_BITS = 5;
    public const int Y_BITS = 1;
    public const int Z_BITS = 5;

    public const int X_SIZE = 1 << X_BITS; // 32
    public const int Y_SIZE = 1 << Y_BITS; // 2 chunks = 64 blocks high
    public const int Z_SIZE = 1 << Z_BITS; // 32

    public const int X_MASK = X_SIZE - 1;
    public const int Y_MASK = Y_SIZE - 1;
    public const int Z_MASK = Z_SIZE - 1;

    public const int X_MAX = X_SIZE - 1;
    public const int Y_MAX = Y_SIZE - 1;
    public const int Z_MAX = Z_SIZE - 1;

    public const int ARRAY_SIZE = X_SIZE * Y_SIZE * Z_SIZE;

    public readonly Chunk[] chunks = new Chunk[ARRAY_SIZE];


    public void Set(ChunkPosition pos, Chunk chunk) {
        if(pos.Check())
            chunks[pos.ToIndex()] = chunk;
    }
    public void Set(int x, int y, int z, Chunk chunk) {
        if(ChunkPosition.Check( x, y, z ))
            chunks[ChunkPosition.ToIndex( x, y, z )] = chunk;
    }
    public void Set(int index, Chunk chunk) {
        chunks[index] = chunk;
    }


    public Chunk Get(ChunkPosition pos) {
        if(pos.Check()) return chunks[pos.ToIndex()];
        return null;
    }
    public Chunk Get(int x, int y, int z) {
        if(ChunkPosition.Check( x, y, z )) return chunks[ChunkPosition.ToIndex( x, y, z )];
        return null;
    }
    public Chunk Get(int index) {
        return chunks[index];
    }

    public Chunk GetChunkInstance(ChunkPosition pos) {
        if(!pos.Check()) return null;

        int index = pos.ToIndex();
        Chunk chunk = Get( index );
        if(chunk == null) {
            chunk = new Chunk( pos );
            Set( index, chunk );
        }
        return chunk;
    }

}
