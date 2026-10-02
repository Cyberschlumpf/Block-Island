using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CactusBuilder {
		
	private static readonly Vector3[][] vertices = new Vector3[][] {
		//Front
		new Vector3[] {
			new Vector3(0, 0, 0.9375f),
			new Vector3(0, 1, 0.9375f),
			new Vector3(1, 1, 0.9375f),
			new Vector3(1, 0, 0.9375f),
		}.Inverse(),
		//Back
		new Vector3[] {
			new Vector3(0, 0, 0.0625f),
			new Vector3(0, 1, 0.0625f),
			new Vector3(1, 1, 0.0625f),
			new Vector3(1, 0, 0.0625f),
		},
		
		//Right
		new Vector3[] {
			new Vector3(0.9375f, 0, 0),
			new Vector3(0.9375f, 1, 0),
			new Vector3(0.9375f, 1, 1),
			new Vector3(0.9375f, 0, 1),
		},
		//Left
		new Vector3[] {
			new Vector3(0.0625f, 0, 0),
			new Vector3(0.0625f, 1, 0),
			new Vector3(0.0625f, 1, 1),
			new Vector3(0.0625f, 0, 1),
		}.Inverse(),
		
		//Top
		new Vector3[] {
			new Vector3(0, 1, 0),
			new Vector3(1, 1, 0),
			new Vector3(1, 1, 1),
			new Vector3(0, 1, 1),
		}.Inverse(),
		//Bottom
		new Vector3[] {
			new Vector3(0, 0, 0),
			new Vector3(1, 0, 0),
			new Vector3(1, 0, 1),
			new Vector3(0, 0, 1),
		},
	};
	
	
	public static void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
		CactusBlock cactus = (CactusBlock) block.block;



        BuildFace( builder, cactus, CubeSide.Front, pos );
        BuildFace( builder, cactus, CubeSide.Back, pos );

        BuildFace( builder, cactus, CubeSide.Right, pos );
        BuildFace( builder, cactus, CubeSide.Left, pos );

        if(IsTopFaceVisible( index, chunk )) {
            BuildFace( builder, cactus, CubeSide.Top, pos );
        }
        if(IsBottomFaceVisible( index, chunk )) {
            BuildFace( builder, cactus, CubeSide.Bottom, pos );
        }
	}

    private static bool IsTopFaceVisible(int index, Chunk chunk) {
        DataBlock block = chunk.GetTopBlock( index );
        if(block.block is CactusBlock) return false;
        return block.IsAlpha();
    }
    private static bool IsBottomFaceVisible(int index, Chunk chunk) {
        DataBlock block = chunk.GetBottomBlock( index );
        if(block.block is CactusBlock) return false;
        return block.IsAlpha();
    }

	
	private static void BuildFace(MeshBuilder builder, CactusBlock cactus, CubeSide side, LocalPosition pos) {
		int iSide = (int) side;
		Face face = cactus.GetFace( side );
		
		builder.AddFaceIndices( face.materialID );
		builder.AddVertices( vertices[iSide], pos );
		builder.AddFaceNormal( BoxBuilder.directions[iSide] );
		builder.AddTexCoords( face.rect );

        builder.topology.Add( new BlockTopology( pos, (BlockTopologyType) iSide ) );
	}
	
	
	public static MeshBuilder Build(CactusBlock cactus) {
		MeshBuilder builder = new MeshBuilder();

        LocalPosition pos = LocalPosition.zero;

        BuildFace( builder, cactus, CubeSide.Front, pos );
        BuildFace( builder, cactus, CubeSide.Back, pos );

        BuildFace( builder, cactus, CubeSide.Right, pos );
        BuildFace( builder, cactus, CubeSide.Left, pos );

        BuildFace( builder, cactus, CubeSide.Top, pos );
        BuildFace( builder, cactus, CubeSide.Bottom, pos );

        return builder;
	}
	
	
}
