using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CubeBuilder {

    // 1.0.22: Standard voxel cubes must meet exactly at their cell borders.
    // The legacy BoxBuilder vertices use -0.001..1.001 for non-cube detail geometry;
    // using those for adjacent full cubes makes coplanar top/side faces overlap and
    // produces the bright/dark seams seen on grass and walls. Keep that legacy path
    // untouched and use exact 0..1 geometry only for CubeBlock rendering + preview.
    private static readonly Vector3[][] cubeVertices = new Vector3[][] {
        new Vector3[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,0,1) }.Inverse(),
        new Vector3[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) },
        new Vector3[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
        new Vector3[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(0,0,1) }.Inverse(),
        new Vector3[] { new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,1,1) }.Inverse(),
        new Vector3[] { new Vector3(0,0,1), new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1) },
    };

	public static void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
		CubeBlock cube = (CubeBlock) block.block;
		BlockDirection dir = block.direction;

        if(IsFrontVisible( pos, index, chunk )) { // Front
            Face face = cube.GetFace(dir, CubeSide.Front);
            BuildFace( builder, 0, face, pos );
        }

        if(IsBackVisible( pos, index, chunk )) { // Back
            Face face = cube.GetFace( dir, CubeSide.Back );
            BuildFace( builder, 1, face, pos );
        }

        if(IsRightVisible( pos, index, chunk )) { // Right
            Face face = cube.GetFace( dir, CubeSide.Right);
            BuildFace( builder, 2, face, pos );
        }

        if(IsLeftVisible( pos, index, chunk )) { // Left
            Face face = cube.GetFace( dir, CubeSide.Left );
            BuildFace( builder, 3, face, pos );
        }

        if(IsTopVisible( pos, index, chunk )) { // Top
            Face face = cube.top;
            BuildFace( builder, 4, face, pos, dir );
        }

        if(IsBottomVisible( pos, index, chunk )) { // Bottom
            Face face = cube.bottom;
            BuildFace( builder, 5, face, pos, dir );
        }
	}


    public static bool IsFrontVisible(LocalPosition pos, int index, Chunk chunk) {
        return chunk.GetFrontBlock( index ).IsAlpha();
    }
    public static bool IsBackVisible(LocalPosition pos, int index, Chunk chunk) {
        return chunk.GetBackBlock( index ).IsAlpha();
    }

    public static bool IsRightVisible(LocalPosition pos, int index, Chunk chunk) {
        return chunk.GetRightBlock( index ).IsAlpha();
    }
    public static bool IsLeftVisible(LocalPosition pos, int index, Chunk chunk) {
        return chunk.GetLeftBlock( index ).IsAlpha();
    }

    public static bool IsTopVisible(LocalPosition pos, int index, Chunk chunk) {
        return chunk.GetTopBlock( index ).IsAlpha();
    }
    public static bool IsBottomVisible(LocalPosition pos, int index, Chunk chunk) {
        if(pos.y == 0 && chunk.position.y == 0) return false;
        return chunk.GetBottomBlock( index ).IsAlpha();
    }

    private static Face GetFace(this CubeBlock block, BlockDirection dir, CubeSide side) {
        //if(side == CubeSide.Top) return block.top;
        //if(side == CubeSide.Bottom) return block.bottom;

        if(dir == BlockDirection.FORWARD) {
            if(side == CubeSide.Front) return block.front;
            if(side == CubeSide.Back) return block.back;
            if(side == CubeSide.Right) return block.right;
            if(side == CubeSide.Left) return block.left;
        }

        //    left
        //back    front
        //    right
        if(dir == BlockDirection.RIGHT) {
            if(side == CubeSide.Front) return block.left;
            if(side == CubeSide.Back) return block.right;
            if(side == CubeSide.Right) return block.front;
            if(side == CubeSide.Left) return block.back;
        }

        //     back
        //right    left
        //    front
        if(dir == BlockDirection.BACKWARD) {
            if(side == CubeSide.Front) return block.back;
            if(side == CubeSide.Back) return block.front;
            if(side == CubeSide.Right) return block.left;
            if(side == CubeSide.Left) return block.right;
        }

        //     right
        //front     back
        //     left
        if(dir == BlockDirection.LEFT) {
            if(side == CubeSide.Front) return block.right;
            if(side == CubeSide.Back) return block.left;
            if(side == CubeSide.Right) return block.back;
            if(side == CubeSide.Left) return block.front;
        }

        return default( Face );
    }


    public static void BuildFace(MeshBuilder builder, int iSide, Face face, LocalPosition pos) {
        builder.AddFaceIndices( face.materialID );
        builder.AddVertices( cubeVertices[iSide], pos );
        builder.AddFaceNormal( BoxBuilder.directions[iSide] );
        builder.AddTexCoords( face.rect );

        builder.topology.Add( new BlockTopology( pos, (BlockTopologyType) iSide ) );
    }

    public static void BuildFace(MeshBuilder builder, int iSide, Rect texCoord, int materialID, LocalPosition pos) {
        builder.AddFaceIndices( materialID );
        builder.AddVertices( cubeVertices[iSide], pos );
        builder.AddFaceNormal( BoxBuilder.directions[iSide] );
        builder.AddTexCoords( texCoord );

        builder.topology.Add( new BlockTopology( pos, (BlockTopologyType) iSide ) );
    }

    public static void BuildFace(MeshBuilder builder, int iSide, Face face, LocalPosition pos, BlockDirection dir) {
        builder.AddFaceIndices( face.materialID );
        builder.AddVertices( cubeVertices[iSide], pos, dir );
        builder.AddFaceNormal( BoxBuilder.directions[iSide], dir );
        builder.AddTexCoords( face.rect );

        builder.topology.Add( new BlockTopology( pos, (BlockTopologyType) iSide ) );
    }


	
	public static MeshBuilder Build(CubeBlock cube) {
		MeshBuilder builder = new MeshBuilder();

        LocalPosition pos = LocalPosition.zero;

        BuildFace( builder, 0, cube.front, pos );
        BuildFace( builder, 1, cube.back, pos );
        BuildFace( builder, 2, cube.right, pos );
        BuildFace( builder, 3, cube.left, pos );
        BuildFace( builder, 4, cube.top, pos );
        BuildFace( builder, 5, cube.bottom, pos );

		return builder;
	}
	
	
}