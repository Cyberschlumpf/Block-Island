using UnityEngine;
using System.Collections.Generic;

public class MeshBlock : Block {
	
	public Face face;
	
	public Mesh mesh;
	
	internal Vector3[] vertices;
    internal Vector3[] normals;
	internal Vector2[] uvs;
	internal int[] indices;
	
	internal Vector3 min, max;
	
	
	public override void Init(BlockSet blockSet, int blockId) {
		base.Init(blockSet, blockId);
		
		if(mesh) {
			vertices = mesh.vertices;
			normals = mesh.normals;
            uvs = mesh.uv;
			indices = mesh.triangles;

            for(int i = 0; i < vertices.Length; i++) {
                vertices[i] += Vector3.one / 2f;
            }
			
			var bounds = mesh.bounds;
            min = bounds.min + Vector3.one / 2f;
            max = bounds.max + Vector3.one / 2f;
		}
	}

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        BlockDirection dir = block.direction;

        builder.AddIndices( indices, face.materialID );
		builder.AddVertices( vertices, pos, dir );
		builder.AddNormals( normals, dir );
        builder.AddTexCoords( uvs, face.rect );
        builder.topology.Add( new BlockTopology(pos, BlockTopologyType.Vertex, vertices.Length) );
	}
	
	public override MeshBuilder Build() {
		if(mesh == null) return null;
		
		MeshBuilder builder = new MeshBuilder();

        builder.AddIndices( indices, face.materialID );
		builder.AddVertices( vertices, Vector3.zero );
		builder.AddNormals( normals );
        builder.AddTexCoords( uvs, face.rect );
		
		return builder;
	}

    public override Face GetPreviewFace() {
        return face;
    }
	
}
