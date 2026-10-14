using UnityEngine;
using System.Collections;

public class StairBuilder {
	
	// Y
	//1---2
	//|   |
	//|   3---4
	//|       |
	//0-------5 Z
	
	private static readonly Vector3[][] vertices = new Vector3[][] {
		new Vector3[] { // front
			new Vector3(1, 1,    0.5f), new Vector3(0, 1,    0.5f),
			new Vector3(0, 0.5f, 0.5f), new Vector3(1, 0.5f, 0.5f),
			
			new Vector3(1, 0.5f, 1), new Vector3(0, 0.5f, 1),
			new Vector3(0, 0,    1), new Vector3(1, 0,    1)
		},
		new Vector3[] { // back
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0),
			new Vector3(1, 0, 0),
		},
		new Vector3[] { //right
			new Vector3(1, 0, 0),       new Vector3(1, 1, 0),    new Vector3(1, 1, 0.5f), 
			new Vector3(1, 0.5f, 0.5f), new Vector3(1, 0.5f, 1), new Vector3(1, 0, 1),
		},
		new Vector3[] { // left
			new Vector3(0, 0, 0),       new Vector3(0, 1, 0),    new Vector3(0, 1, 0.5f), 
			new Vector3(0, 0.5f, 0.5f), new Vector3(0, 0.5f, 1), new Vector3(0, 0, 1),
		},
		new Vector3[] { // top
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0.5f),
            new Vector3(1, 1, 0.5f),

            new Vector3(0, 0.5f, 1),
            new Vector3(1, 0.5f, 1),
            new Vector3(1, 0.5f, 0.5f),
            new Vector3(0, 0.5f, 0.5f),
		},
		new Vector3[] { // bottom
            new Vector3(1, 0, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 0),
		},
	};

    private static readonly Vector3i[] directions = BoxBuilder.directions;
	
	private static readonly Vector2[][] texCoords = new Vector2[][] {
        ComputeTexCoords( vertices[0], CubeSide.Front ),
        ComputeTexCoords( vertices[1], CubeSide.Back ),

        ComputeTexCoords( vertices[2], CubeSide.Right ),
        ComputeTexCoords( vertices[3], CubeSide.Left ),

        ComputeTexCoords( vertices[4], CubeSide.Top ),
        ComputeTexCoords( vertices[5], CubeSide.Bottom ),
    };
	
	private static readonly int[][] indices = new int[][] {
        MeshBuilder.Triangulate(0, 1, 2, 3,
                                4, 5, 6, 7), // front
        
        MeshBuilder.Triangulate(0, 1, 2, 3), // back

        MeshBuilder.Triangulate(0, 1, 2, 3,
                                0, 3, 4, 5), // right
        
        MeshBuilder.Triangulate(3, 2, 1, 0, 
                                5, 4, 3, 0), // left
        
        MeshBuilder.Triangulate(0, 1, 2, 3,
                                4, 5, 6, 7), // top
        
        MeshBuilder.Triangulate(0, 1, 2, 3), // bottom
	};


    private static Vector2[] ComputeTexCoords(Vector3[] vertices, CubeSide side) {
        Vector2[] texCoords = new Vector2[vertices.Length];
        for(int i = 0; i < texCoords.Length; i++) {
            texCoords[i] = BoxBuilder.ComputeTexCoord( vertices[i], side );
        }
        return texCoords;
    }



	public static void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
		StairBlock stair = (StairBlock) block.block;
        BlockDirection dir = block.direction;

        BuildFace( builder, stair, 0, dir, pos ); // Front

        if(IsFaceVisible( index, CubeSide.Back, dir, chunk )) {
            BuildFace( builder, stair, 1, dir, pos ); // Back
		}

        if(IsFaceVisible( index, CubeSide.Right, dir, chunk )) {
            BuildFace( builder, stair, 2, dir, pos ); // Right
		}

        if(IsFaceVisible( index, CubeSide.Left, dir, chunk )) {
            BuildFace( builder, stair, 3, dir, pos ); // Left
		}

        BuildFace( builder, stair, 4, dir, pos ); // Top

        if(IsFaceVisible( index, CubeSide.Bottom, dir, chunk )) {
            BuildFace( builder, stair, 5, dir, pos ); // Bottom
		}
	}


    private static bool IsFaceVisible(int index, CubeSide side, BlockDirection dir, Chunk chunk) {
        side = CubeBlock.TransformSide(side, dir);

        if(side == CubeSide.Front) return chunk.GetFrontBlock( index ).IsAlpha();
        if(side == CubeSide.Back) return chunk.GetBackBlock( index ).IsAlpha();

        if(side == CubeSide.Right) return chunk.GetRightBlock( index ).IsAlpha();
        if(side == CubeSide.Left) return chunk.GetLeftBlock( index ).IsAlpha();

        if(side == CubeSide.Top) return chunk.GetTopBlock( index ).IsAlpha();
        if(side == CubeSide.Bottom) return chunk.GetBottomBlock( index ).IsAlpha();

        return false;
    }

	
	private static void BuildFace(MeshBuilder builder, StairBlock block, int index, BlockDirection direction, LocalPosition pos) {
		Face face = block.face;
        int vCount = vertices[index].Length;

        builder.AddIndices( indices[index], face.materialID );
        builder.AddVertices( vertices[index], pos, direction );
        builder.AddTexCoords( texCoords[index], face.rect );
        builder.AddNormals( directions[index], vCount, direction );

        builder.topology.Add( new BlockTopology( pos, BlockTopologyType.Vertex, vCount ) );
	}
	
	
	public static MeshBuilder Build(StairBlock stair) {
		MeshBuilder builder = new MeshBuilder();
		BlockDirection dir = BlockDirection.FORWARD;

        BuildFace( builder, stair, 0, dir, LocalPosition.zero ); // Front
        BuildFace( builder, stair, 1, dir, LocalPosition.zero ); // Back

        BuildFace( builder, stair, 2, dir, LocalPosition.zero ); // Right
        BuildFace( builder, stair, 3, dir, LocalPosition.zero ); // Left

        BuildFace( builder, stair, 4, dir, LocalPosition.zero ); // Top
        BuildFace( builder, stair, 5, dir, LocalPosition.zero ); // Bottom
		
		return builder;
	}
	
	
}
