using UnityEngine;
using System.Collections;

// position in chunk
public struct LocalPosition {

    public static readonly LocalPosition zero = default( LocalPosition );

    public sbyte x, y, z;
	
    public LocalPosition(int x, int y, int z) {
        this.x = (sbyte) x;
        this.y = (sbyte) y;
        this.z = (sbyte) z;
    }


    public int ToIndex() {
        return ToIndex(x, y, z);
    }
    public static int ToIndex(int x, int y, int z) {
        const int XY_BITS = Chunk.X_BITS + Chunk.Y_BITS;
        return (z << XY_BITS) | (y << Chunk.X_BITS) | x;
    }

    public bool Check() {
        return Check(x, y, z);
    }
    public static bool Check(int x, int y, int z) {
        return (x & Chunk.X_MASK) == x &&
               (y & Chunk.Y_MASK) == y &&
               (z & Chunk.Z_MASK) == z;
    }


    public static LocalPosition operator -(LocalPosition a, Vector3i b) {
        return new LocalPosition( a.x - b.x, a.y - b.y, a.z - b.z );
    }
    public static LocalPosition operator +(LocalPosition a, Vector3i b) {
        return new LocalPosition( a.x + b.x, a.y + b.y, a.z + b.z );
    }


    public static explicit operator Vector3i(LocalPosition pos) {
        return new Vector3i( pos.x, pos.y, pos.z );
    }

    public static implicit operator Vector3(LocalPosition pos) {
        return new Vector3( pos.x, pos.y, pos.z );
    }


    public override string ToString() {
        return string.Format( "LocalPosition({0} {1} {2})", x, y, z );
    }

}
