using UnityEngine;
using System.Collections;

// position in grid
public struct ChunkPosition {

    public static readonly ChunkPosition zero = default( ChunkPosition );

    public sbyte x, y, z;

    public ChunkPosition(int x, int y, int z) {
        this.x = (sbyte) x;
        this.y = (sbyte) y;
        this.z = (sbyte) z;
    }


    public int ToIndex() {
        return ToIndex(x, y, z);
    }
    public static int ToIndex(int x, int y, int z) {
        const int XY_BITS = ChunkGrid.X_BITS + ChunkGrid.Y_BITS;
        return (z << XY_BITS) | (y << ChunkGrid.X_BITS) | x;
    }

    public bool Check() {
        return Check(x, y, z);
    }
    public static bool Check(int x, int y, int z) {
        return (x & ChunkGrid.X_MASK) == x &&
                (y & ChunkGrid.Y_MASK) == y &&
                (z & ChunkGrid.Z_MASK) == z;
    }


    public static ChunkPosition operator -(ChunkPosition a, Vector3i b) {
        return new ChunkPosition( a.x - b.x, a.y - b.y, a.z - b.z );
    }
    public static ChunkPosition operator +(ChunkPosition a, Vector3i b) {
        return new ChunkPosition( a.x + b.x, a.y + b.y, a.z + b.z );
    }


    public static explicit operator Vector3i(ChunkPosition pos) {
        return new Vector3i( pos.x, pos.y, pos.z );
    }

    public override string ToString() {
        return string.Format( "ChunkPosition({0} {1} {2})", x, y, z );
    }
	
}
