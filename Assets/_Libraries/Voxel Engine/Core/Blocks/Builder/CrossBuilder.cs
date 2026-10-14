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
	
    // 1.0.31: Builds a CrossBlock so its local up axis points toward the centre
    // of the inverted Relativity sphere. The orientation is derived from world
    // position, so no extra rotation data has to be stored in savegames.
    public static void BuildRadialInward(MeshBuilder builder, DataBlock block, LocalPosition pos, Vector3 worldCenter) {
        CrossBlock cross = (CrossBlock)block.block;
        Face face = cross.face;

        Vector3 inward = worldCenter.sqrMagnitude > 0.0001f ? -worldCenter.normalized : Vector3.up;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, inward);
        Vector3 pivot = new Vector3(0.5f, 0f, 0.5f);

        Vector3[] radialVertices = new Vector3[vertices.Length];
        Vector3[] radialNormals = new Vector3[normals.Length];
        Vector3[] radialSecondNormals = new Vector3[secondFaceNormals.Length];
        for(int i=0;i<vertices.Length;i++) radialVertices[i] = rotation * (vertices[i]-pivot) + pivot;
        for(int i=0;i<normals.Length;i++) radialNormals[i] = rotation * normals[i];
        for(int i=0;i<secondFaceNormals.Length;i++) radialSecondNormals[i] = rotation * secondFaceNormals[i];

        builder.AddIndices(indices, face.materialID);
        builder.AddVertices(radialVertices, pos);
        builder.AddNormals(radialNormals);
        builder.AddTexCoords(texCoords, face.rect);

        if(cross.doubleSided) {
            builder.AddInvIndices(indices, face.materialID);
            builder.AddVertices(radialVertices, pos);
            builder.AddNormals(radialSecondNormals);
            builder.AddTexCoords(texCoords, face.rect);
        }
    }

    // 1.0.32: In the hollow sphere the player chooses the plant/cross-block axis manually.
    // DataBlock.direction is stored as a byte, so values 0..5 persist without changing the save format.
    // 0 Up, 1 Right, 2 Down, 3 Left, 4 Forward, 5 Back.
    public static void BuildManualAxis(MeshBuilder builder, DataBlock block, LocalPosition pos) {
        CrossBlock cross=(CrossBlock)block.block;
        Face face=cross.face;
        int axis=(int)block.direction;
        Quaternion rotation=Quaternion.identity;
        if(axis==1) rotation=Quaternion.FromToRotation(Vector3.up,Vector3.right);
        else if(axis==2) rotation=Quaternion.FromToRotation(Vector3.up,Vector3.down);
        else if(axis==3) rotation=Quaternion.FromToRotation(Vector3.up,Vector3.left);
        else if(axis==4) rotation=Quaternion.FromToRotation(Vector3.up,Vector3.forward);
        else if(axis==5) rotation=Quaternion.FromToRotation(Vector3.up,Vector3.back);
        Vector3 pivot=new Vector3(.5f,0f,.5f);
        Vector3[] vv=new Vector3[vertices.Length], nn=new Vector3[normals.Length], sn=new Vector3[secondFaceNormals.Length];
        for(int i=0;i<vertices.Length;i++) vv[i]=rotation*(vertices[i]-pivot)+pivot;
        for(int i=0;i<normals.Length;i++) nn[i]=rotation*normals[i];
        for(int i=0;i<secondFaceNormals.Length;i++) sn[i]=rotation*secondFaceNormals[i];
        builder.AddIndices(indices,face.materialID); builder.AddVertices(vv,pos); builder.AddNormals(nn); builder.AddTexCoords(texCoords,face.rect);
        if(cross.doubleSided){builder.AddInvIndices(indices,face.materialID);builder.AddVertices(vv,pos);builder.AddNormals(sn);builder.AddTexCoords(texCoords,face.rect);}
    }

}
