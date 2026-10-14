using UnityEngine;
using System.Collections;

public class GroundBuilder {

    private const int TILE_SIZE = 4;

    private static readonly Vector3[][] wingsVertices;
    private static readonly Vector3[][] wingsNormals;
    private static readonly Vector2[][] wingsTexCoords;
	
	static GroundBuilder() {
        const float _0 = BoxBuilder._0;
        const float _1 = BoxBuilder._1;

		Vector3[,] vts = new Vector3[,] {
			{ new Vector3(_1, _1, _1), new Vector3(_0, _1, _1) },
			{ new Vector3(_0, _1, _0), new Vector3(_1, _1, _0) },
			{ new Vector3(_1, _1, _0), new Vector3(_1, _1, _1) },
			{ new Vector3(_0, _1, _1), new Vector3(_0, _1, _0) }
		};
		
		wingsVertices = new Vector3[4][];
		wingsNormals = new Vector3[4][];
        wingsTexCoords = new Vector2[4][];

		for(int i=0; i<4; i++) {
			Vector3 v1 = vts[i, 0];
			Vector3 v2 = vts[i, 1];
            Vector3 dir = (BoxBuilder.directions[i] - Vector3.up / 2f).normalized / 3f;

			wingsVertices[i] = new Vector3[] {
				v1, v2,
				v2+dir, v1+dir
			};

            Vector3 normal = Vector3.Cross( v2 - v1, dir ).normalized;
			wingsNormals[i] = new Vector3[] {
				normal, normal, normal, normal
			};

            float y1 = i / 4f + 0.001f;
            float y2 = (i + 1) / 4f;
            wingsTexCoords[i] = new Vector2[] {
				new Vector2(0, y2),
				new Vector2(1, y2),
				new Vector2(1, y1),
				new Vector2(0, y1),
			};
		}

	}
	
	
	public static void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
		GroundBlock ground = (GroundBlock) block.block;
		bool isWingsVisible = ground.wings.materialID != -1 && chunk.GetTopBlock(index).IsAlpha();


        if(CubeBuilder.IsFrontVisible( pos, index, chunk )) {
            Face face = ground.side;
            Rect rect = Offset( face.rect, CubeSide.Front, pos );

            CubeBuilder.BuildFace( builder, 0, rect, face.materialID, pos );
            if(isWingsVisible) BuildWing( builder, 0, ground.wings, pos );
        }

        if(CubeBuilder.IsBackVisible( pos, index, chunk )) {
            Face face = ground.side;
            Rect rect = Offset( face.rect, CubeSide.Back, pos );

            CubeBuilder.BuildFace( builder, 1, rect, face.materialID, pos );
            if(isWingsVisible) BuildWing( builder, 1, ground.wings, pos );
        }

        if(CubeBuilder.IsRightVisible( pos, index, chunk )) {
            Face face = ground.side;
            Rect rect = Offset( face.rect, CubeSide.Right, pos );

            CubeBuilder.BuildFace( builder, 2, rect, face.materialID, pos );
            if(isWingsVisible) BuildWing( builder, 2, ground.wings, pos );
        }

        if(CubeBuilder.IsLeftVisible( pos, index, chunk )) {
            Face face = ground.side;
            Rect rect = Offset( face.rect, CubeSide.Left, pos );

            CubeBuilder.BuildFace( builder, 3, rect, face.materialID, pos );
            if(isWingsVisible) BuildWing( builder, 3, ground.wings, pos );
        }

        if(CubeBuilder.IsTopVisible( pos, index, chunk )) {
            Face face = ground.top;
            Rect rect = Offset( face.rect, CubeSide.Top, pos );

            CubeBuilder.BuildFace( builder, 4, rect, face.materialID, pos );
        }

        if(CubeBuilder.IsBottomVisible( pos, index, chunk )) {
            Face face = ground.bottom;
            Rect rect = Offset( face.rect, CubeSide.Bottom, pos );

            CubeBuilder.BuildFace( builder, 5, rect, face.materialID, pos );
        }

	}

    private static void BuildWing(MeshBuilder builder, int iSide, Face face, LocalPosition pos) {
        builder.AddFaceIndices( face.materialID );
        builder.AddVertices( wingsVertices[iSide], pos );
        builder.AddNormals( wingsNormals[iSide] );
        builder.AddTexCoords( wingsTexCoords[iSide], face.rect );

        builder.topology.Add( new BlockTopology( pos, (BlockTopologyType) iSide ) );
    }


    private static Rect Offset(Rect rect, CubeSide side, LocalPosition pos) {
        int offsetU, offsetV;
        GetOffsetUV( side, pos, out offsetU, out offsetV );

        rect.width /= TILE_SIZE;
        rect.height /= TILE_SIZE;
        rect.x += rect.width * offsetU;
        rect.y += rect.height * offsetV;

        return rect;
    }

    private static void GetOffsetUV(CubeSide face, LocalPosition pos, out int offsetU, out int offsetV) {
        switch(face) {
            case CubeSide.Front:  offsetU = -pos.x; offsetV = pos.y; break;
            case CubeSide.Back:   offsetU =  pos.x; offsetV = pos.y; break;

            case CubeSide.Left:   offsetU = -pos.z; offsetV = pos.y; break;
            case CubeSide.Right:  offsetU =  pos.z; offsetV = pos.y; break;

            case CubeSide.Top:    offsetU = -pos.x; offsetV = -pos.z; break;
            case CubeSide.Bottom: offsetU =  pos.x; offsetV = -pos.z; break;

            default: offsetU = 0; offsetV = 0; break;
        }

        offsetU %= TILE_SIZE;
        offsetV %= TILE_SIZE;
        if(offsetU < 0) offsetU += TILE_SIZE;
        if(offsetV < 0) offsetV += TILE_SIZE;
	}
	
	
	public static MeshBuilder Build(GroundBlock block) {
		MeshBuilder builder = new MeshBuilder();

        LocalPosition pos = LocalPosition.zero;


        Face face = block.side;
        CubeBuilder.BuildFace( builder, 0, face, pos );
        CubeBuilder.BuildFace( builder, 1, face, pos );
        CubeBuilder.BuildFace( builder, 2, face, pos );
        CubeBuilder.BuildFace( builder, 3, face, pos );

        face = block.top;
        CubeBuilder.BuildFace( builder, 4, face, pos );

        face = block.bottom;
        CubeBuilder.BuildFace( builder, 5, face, pos );


        if(block.wings.materialID != -1) {
            BuildWing( builder, 0, block.wings, pos );
            BuildWing( builder, 1, block.wings, pos );
            BuildWing( builder, 2, block.wings, pos );
            BuildWing( builder, 3, block.wings, pos );
		}
		return builder;
	}
	
}
