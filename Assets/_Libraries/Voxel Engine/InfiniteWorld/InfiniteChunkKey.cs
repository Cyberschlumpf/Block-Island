using System;

[Serializable]
public struct InfiniteChunkKey : IEquatable<InfiniteChunkKey> {
    public int x, y, z;
    public InfiniteChunkKey(int x, int y, int z) { this.x=x; this.y=y; this.z=z; }
    public bool Equals(InfiniteChunkKey o) => x==o.x && y==o.y && z==o.z;
    public override bool Equals(object obj) => obj is InfiniteChunkKey && Equals((InfiniteChunkKey)obj);
    public override int GetHashCode() { unchecked { int h=17; h=h*31+x; h=h*31+y; h=h*31+z; return h; } }
    public override string ToString() => x+","+y+","+z;
}
