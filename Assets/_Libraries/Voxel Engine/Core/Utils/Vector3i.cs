using UnityEngine;

public struct Vector3i {
	
	public int x, y, z;
	
	public static readonly Vector3i zero    = new Vector3i(0, 0, 0);
	public static readonly Vector3i one     = new Vector3i(1, 1, 1);

	public static readonly Vector3i forward = new Vector3i(0, 0, 1);
	public static readonly Vector3i back    = new Vector3i(0, 0, -1);
	public static readonly Vector3i up      = new Vector3i(0, 1, 0);
	public static readonly Vector3i down    = new Vector3i(0, -1, 0);
	public static readonly Vector3i left    = new Vector3i(-1, 0, 0);
	public static readonly Vector3i right   = new Vector3i(1, 0, 0);
	
	public static readonly Vector3i[] directions = new Vector3i[] {
		left, right,
		back, forward,
		down, up,
	};

	
	public Vector3i(int x, int y, int z) {
		this.x = x;
		this.y = y;
		this.z = z;
	}
	
	public Vector3i(int x, int y) {
		this.x = x;
		this.y = y;
		this.z = 0;
	}
	

	public Vector3i Mul(Vector3i factor) {
		return new Vector3i(x*factor.x, y*factor.y, z*factor.z);
	}

    public Vector3i Div(Vector3i factor) {
        return new Vector3i(x / factor.x, y / factor.y, z / factor.z);
    }

	public static Vector3i operator - (Vector3i a) {
		return new Vector3i( -a.x, -a.y, -a.z );
	}
	
	public static Vector3i operator - (Vector3i a, Vector3i b) {
		return new Vector3i( a.x-b.x, a.y-b.y, a.z-b.z);
	}
	
	public static Vector3i operator + (Vector3i a, Vector3i b) {
		return new Vector3i( a.x+b.x, a.y+b.y, a.z+b.z);
	}

	public static Vector3i operator * (Vector3i v, int factor) {
		return new Vector3i (v.x*factor, v.y*factor, v.z*factor);
	}

	public static Vector3i operator / (Vector3i v, int factor) {
		return new Vector3i (v.x/factor, v.y/factor, v.z/factor);
	}
	
	public static  implicit operator Vector3(Vector3i v) {
		return new Vector3(v.x, v.y, v.z);
	}

	public static bool operator == (Vector3i a, Vector3i b) {
		return a.x == b.x && 
				a.y == b.y && 
				a.z == b.z;
	}
	
	public static bool operator != (Vector3i a, Vector3i b) {
		return a.x != b.x ||
				a.y != b.y ||
				a.z != b.z;
	}

	
	public override bool Equals(object other) {
		if (!(other is Vector3i)) return false;
		Vector3i vector = (Vector3i) other;
		return x == vector.x && 
			    y == vector.y && 
				z == vector.z;
	}

	public override int GetHashCode () {
		return x.GetHashCode () ^ y.GetHashCode () << 2 ^ z.GetHashCode () >> 2;
	}
	
	public override string ToString() {
		return string.Format("Vector3i({0} {1} {2})", x, y, z);
	}




	public static Vector3i Min(Vector3i a, Vector3i b) {
		return new Vector3i(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
	}
	public static Vector3i Max(Vector3i a, Vector3i b) {
		return new Vector3i(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
	}
	
	public static Vector3i Floor(Vector3 v) {
		return new Vector3i(
			Mathf.FloorToInt(v.x),
			Mathf.FloorToInt(v.y),
			Mathf.FloorToInt(v.z)
			);
	}
	
	public static Vector3i Ceil(Vector3 v) {
		return new Vector3i(
			Mathf.CeilToInt(v.x),
			Mathf.CeilToInt(v.y),
			Mathf.CeilToInt(v.z)
			);
	}
	
	public static Vector3i Round(Vector3 v) {
		return new Vector3i(
			Mathf.RoundToInt(v.x),
			Mathf.RoundToInt(v.y),
			Mathf.RoundToInt(v.z)
			);
	}

}
