using UnityEngine;

public static class InfiniteWorldMath {
    public const int ChunkSizeX = Chunk.X_SIZE;
    public const int ChunkSizeY = Chunk.Y_SIZE;
    public const int ChunkSizeZ = Chunk.Z_SIZE;
    public static int FloorDiv(int v, int d) {
        int q=v/d, r=v%d;
        if (r!=0 && ((r<0)!=(d<0))) q--;
        return q;
    }
    public static int Mod(int v, int d) { int r=v%d; return r<0?r+d:r; }
    public static InfiniteChunkKey WorldToChunk(int x,int y,int z) =>
        new InfiniteChunkKey(FloorDiv(x,ChunkSizeX),FloorDiv(y,ChunkSizeY),FloorDiv(z,ChunkSizeZ));
    public static Vector3i WorldToLocal(int x,int y,int z) =>
        new Vector3i(Mod(x,ChunkSizeX),Mod(y,ChunkSizeY),Mod(z,ChunkSizeZ));
}
