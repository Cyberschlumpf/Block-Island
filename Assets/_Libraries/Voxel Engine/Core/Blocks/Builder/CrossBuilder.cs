using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrossBuilder {
	
	private static Vector3[] vertices = new Vector3[] {
		// face a
		new Vector3(1, 0, 1),
		new Vector3(1, 1, 1) - Vector3.up*0.001f,
		new Vector3(0, 1, 0) - Vector3.up*0.001f,
		new Vector3(0, 0, 0),
		
		//face b
		new Vector3(1, 0, 0),
		new Vector3(1, 1, 0) - Vector3.up*0.001f,
		new Vector3(0, 1, 1) - Vector3.up*0.001f,
		new Vector3(0, 0, 1),
	};
	
	private static Vector3[] normals = new Vector3[] {
		//face a
		new Vector3(-0.7f, 0, 0.7f),
		new Vector3(-0.7f, 0, 0.7f),
		new Vector3(-0.7f, 0, 0.7f),
		new Vector3(-0.7f, 0, 0.7f),
		
		//face b
		new Vector3(0.7f, 0, 0.7f),
		new Vector3(0.7f, 0, 0.7f),
		new Vector3(0.7f, 0, 0.7f),
		new Vector3(0.7f, 0, 0.7f),
	};

    private static Vector2[] texCoords = new Vector2[] {
        new Vector2(0.01f, 0),
        new Vector2(0.01f, 0.99f),
        new Vector2(0.99f, 0.99f),
        new Vector2(0.99f, 0),

        new Vector2(0.01f, 0),
        new Vector2(0.01f, 0.99f),
        new Vector2(0.99f, 0.99f),
        new Vector2(0.99f, 0),
    };
	
	private static int[] indices = MeshBuilder.Triangulate(0, 1, 2, 3,
                                                           4, 5, 6, 7);
	
	private static Vector3[] secondFaceNormals = new Vector3[] {
		//face a
		-new Vector3(-0.7f, 0, 0.7f),
		-new Vector3(-0.7f, 0, 0.7f),
		-new Vector3(-0.7f, 0, 0.7f),
		-new Vector3(-0.7f, 0, 0.7f),
		
		//face b
		-new Vector3(0.7f, 0, 0.7f),
		-new Vector3(0.7f, 0, 0.7f),
		-new Vector3(0.7f, 0, 0.7f),
		-new Vector3(0.7f, 0, 0.7f),
	};


    public static void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, Chunk chunk) {
        CrossBlock cross = (CrossBlock) block.block;

        BlockTopology topology = new BlockTopology(pos, BlockTopologyType.Vertex, 8);

        Face face = cross.face;
        builder.AddIndices( indices, face.materialID );
        builder.AddVertices( vertices, pos );
        builder.AddNormals( normals );
        builder.AddTexCoords( texCoords, face.rect );

        if(cross.doubleSided) {
            builder.AddInvIndices( indices, face.materialID );
            builder.AddVertices( vertices, pos );
            builder.AddNormals( secondFaceNormals );
            builder.AddTexCoords( texCoords, face.rect );
            topology.count += 8;
        }

        builder.topology.Add( topology );
    }
	
	
	public static MeshBuilder Build(CrossBlock cross) {
		MeshBuilder builder = new MeshBuilder();
		
		Face face = cross.face;

        builder.AddIndices( indices, face.materialID );
		builder.AddVertices( vertices, Vector3.zero );
		builder.AddNormals( normals );
        builder.AddTexCoords( texCoords, face.rect );
		
		if(cross.doubleSided) {
            builder.AddInvIndices( indices, face.materialID );
			builder.AddVertices( vertices, Vector3.zero );
			builder.AddNormals( secondFaceNormals );
            builder.AddTexCoords( texCoords, face.rect );
		}
		
		return builder;
	}
	
}
