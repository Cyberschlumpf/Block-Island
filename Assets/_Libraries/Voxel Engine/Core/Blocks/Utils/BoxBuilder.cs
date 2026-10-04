using UnityEngine;
using System.Collections;

public static class BoxBuilder {

    // Legacy voxel bounds restored in 1.0.21. Atlas selection now uses the engine's
    // native BlockSet material + Face.rect path instead of altering global geometry.
    public const float _0 = -0.001f, _1 = 1.001f;

	public static readonly Vector3i[] directions = new Vector3i[] {
		Vector3i.forward, Vector3i.back,
		Vector3i.right,   Vector3i.left,
		Vector3i.up,      Vector3i.down
	};

    public static readonly CubeSide[] sides = new CubeSide[] {
		CubeSide.Front, CubeSide.Back,
		CubeSide.Right, CubeSide.Left,
		CubeSide.Top,   CubeSide.Bottom,
	};
	
	// 1   2
	//   /
	// 0   3
	public static readonly Vector3[][] vertices = new Vector3[][] {
		//Front
		new Vector3[] {
			new Vector3(_0, _0, _1),
			new Vector3(_0, _1, _1),
			new Vector3(_1, _1, _1),
			new Vector3(_1, _0, _1),
		}.Inverse(),
		//Back
		new Vector3[] {
			new Vector3(_0, _0, _0),
			new Vector3(_0, _1, _0),
			new Vector3(_1, _1, _0),
			new Vector3(_1, _0, _0),
		},
		
		//Right
		new Vector3[] {
			new Vector3(_1, _0, _0),
			new Vector3(_1, _1, _0),
			new Vector3(_1, _1, _1),
			new Vector3(_1, _0, _1),
		},
		//Left
		new Vector3[] {
			new Vector3(_0, _0, _0),
			new Vector3(_0, _1, _0),
			new Vector3(_0, _1, _1),
			new Vector3(_0, _0, _1),
		}.Inverse(),
		
		//Top
		new Vector3[] {
            new Vector3(_0, _1, _1),
            new Vector3(_0, _1, _0),
            new Vector3(_1, _1, _0),
            new Vector3(_1, _1, _1),

		}.Inverse(),
		//Bottom
		new Vector3[] {
            new Vector3(_0, _0, _1),
            new Vector3(_0, _0, _0),
            new Vector3(_1, _0, _0),
            new Vector3(_1, _0, _1),
		},
	};
    
	
	public static T[] Inverse<T>(this T[] array) {
		System.Array.Reverse(array);
		return array;
	}


    public static void AddBox(this MeshBuilder builder, Box box, Rect face, LocalPosition pos, int materialIndex) {
        for(int i=0; i < 6; i++) {
            builder.AddFaceIndices( materialIndex );
            AddFace( builder, box, i, face, pos );
        }
    }

    private static void AddFace(MeshBuilder builder, Box box, int iSide, Rect face, LocalPosition pos) {
		Vector3 min = box.min;
		Vector3 max = box.max;
		Vector3[] vts = vertices[iSide];
		Vector3i dir = directions[iSide];
        CubeSide side = sides[iSide];
		foreach(Vector3 vertex in vts) {
			float x = Mathf.Lerp(min.x, max.x, vertex.x);
			float y = Mathf.Lerp(min.y, max.y, vertex.y);
			float z = Mathf.Lerp(min.z, max.z, vertex.z);
			Vector3 ver = new Vector3(x, y, z);
            Vector2 uv = ComputeTexCoord( ver, side );
			
			builder.AddVertex( pos+ver );
			builder.AddTexCoord( uv, face );
		}
        builder.AddFaceNormal( dir );

        builder.topology.Add( new BlockTopology( pos, BlockTopologyType.Vertex, vts.Length ) );
	}

    public static Vector2 ComputeTexCoord(Vector3 v, CubeSide side) {
        if(side == CubeSide.Front || side == CubeSide.Back) return new Vector2( v.x, v.y );
        if(side == CubeSide.Left || side == CubeSide.Right) return new Vector2( v.z, v.y );
        return new Vector2( v.x, v.z );
    }

	
}
