using UnityEngine;
using System.Collections.Generic;

public class MeshBuilder {

    // Stage 6.10.1: the project already has more than 30 registered materials.
    // 6.10.0 appended rail/minecart materials after the legacy set, so their material IDs
    // landed outside the old fixed 0..29 MeshBuilder table. The block Build() then failed
    // when adding indices, leaving the placed rail/cart invisible. Keep a generous fixed
    // table for this legacy renderer; this does not change any existing block/material IDs.
    private const int MAX_MATERIAL_COUNT = 512;
	
	public readonly List<Vector3> vertices = new List<Vector3>();
	public readonly List<Vector3> normals = new List<Vector3>();
    public readonly List<Vector2> texCoords = new List<Vector2>();
    public readonly List<BlockTopology> topology = new List<BlockTopology>();
    public readonly List<int>[] indices = new List<int>[MAX_MATERIAL_COUNT];

    public MeshBuilder() {
        // 6.14.6: material buckets are lazy. Tanviir chunks normally use only a small
        // fraction of the 512 legacy slots; allocating 512 List<int> objects for every
        // streamed chunk created substantial GC pressure during initial world loading.
    }

    List<int> MaterialIndices(int materialIndex) {
        if(materialIndex < 0 || materialIndex >= indices.Length) return null;
        List<int> list=indices[materialIndex];
        if(list==null) indices[materialIndex]=list=new List<int>(64);
        return list;
    }


    public void AddVertex(Vector3 vertex) {
        vertices.Add( vertex );
    }
	public void AddVertices(Vector3[] vertices, Vector3 pos) {
		foreach(Vector3 v in vertices) {
			this.vertices.Add( v + pos );
		}
	}
	public void AddVertices(Vector3[] vertices, Vector3 pos, BlockDirection dir) {
		foreach(Vector3 v in vertices) {
            this.vertices.Add( BlockDirectionUtils.TransformBlockVertex( v, dir ) + pos );
		}
	}
	
	public void AddNormals(Vector3[] normals) {
        this.normals.AddRange( normals );
	}
	public void AddNormals(Vector3[] normals, BlockDirection dir) {
		foreach(Vector3 n in normals) {
            this.normals.Add( BlockDirectionUtils.TransformVector( n, dir ) );
		}
	}
    public void AddNormals(Vector3 normal, int count, BlockDirection dir) {
        normal = BlockDirectionUtils.TransformVector( normal, dir );
        for(int i=0; i < count; i++ ) {
            normals.Add( normal );
        }
    }
    public void AddFaceNormal(Vector3 normal) {
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
    }
    public void AddFaceNormal(Vector3 normal, BlockDirection dir) {
        normal = BlockDirectionUtils.TransformVector( normal, dir );
        normals.Add( normal );
        normals.Add( normal );
        normals.Add( normal );
        normals.Add( normal );
    }
	
	public void AddTexCoords(Rect rect) {
        texCoords.Add( new Vector2( rect.xMin, rect.yMin ) );
        texCoords.Add( new Vector2( rect.xMin, rect.yMax ) );
        texCoords.Add( new Vector2( rect.xMax, rect.yMax ) );
        texCoords.Add( new Vector2( rect.xMax, rect.yMin ) );
	}
	
	public void AddTexCoords(Vector2[] uvs, Rect rect) {
		foreach(Vector2 uv in uvs) {
			float u = Mathf.Lerp(rect.xMin, rect.xMax, uv.x);
			float v = Mathf.Lerp(rect.yMin, rect.yMax, uv.y);
			texCoords.Add( new Vector2(u, v) );
		}
	}
	
	public void AddTexCoord(Vector2 uv, Rect rect) {
		float u = Mathf.Lerp(rect.xMin, rect.xMax, uv.x);
		float v = Mathf.Lerp(rect.yMin, rect.yMax, uv.y);
		texCoords.Add( new Vector2(u, v) );
	}
	
	public void AddFaceIndices(int materialIndex) {
		if(materialIndex == -1) return;

        List<int> inds = MaterialIndices(materialIndex);
        if(inds==null) return;
		int offset = vertices.Count;

        //0  1
        //3  2  

        inds.Add( offset + 0 );
        inds.Add( offset + 1 );
        inds.Add( offset + 2 );

        inds.Add( offset + 0 );
        inds.Add( offset + 2 );
        inds.Add( offset + 3 );
	}
	
	public void AddIndices(int[] newIndices, int materialIndex) {
		if(materialIndex == -1) return;
        if(materialIndex < 0 || materialIndex >= indices.Length) {
            Debug.LogError("MeshBuilder material index out of range: " + materialIndex + " / " + indices.Length);
            return;
        }

        List<int> materialIndices = MaterialIndices(materialIndex);
        if(materialIndices==null) return;
		int offset = vertices.Count;
		
		foreach(int index in newIndices) {
			materialIndices.Add( index + offset );
		}
	}

    public void AddInvIndices(int[] newIndices, int materialIndex) {
		if(materialIndex == -1) return;

        List<int> indices = MaterialIndices(materialIndex);
        if(indices==null) return;
		int offset = vertices.Count;
		
		for(int i=newIndices.Length-1; i>=0; i--) {
			indices.Add( newIndices[i] + offset );
		}
	}
	
	public void Clear() {
		vertices.Clear();
		normals.Clear();
		texCoords.Clear();
        topology.Clear();
		foreach(List<int> list in indices) {
            if(list!=null) list.Clear();
		}
	}
	
	public Mesh ToMesh(Mesh mesh) {
		if(vertices.Count == 0) {
			if(mesh) GameObject.Destroy(mesh);
			return null;
		}
		
		if(mesh == null) mesh = new Mesh();

        mesh.Clear();
        // 6.14.12: streamed Tanviir chunks are normally static for many frames. MarkDynamic()
        // asks Unity to optimize buffers for continuous rewriting, which is the opposite usage.
        // Edited chunks can still be cleared and rebuilt normally.
        // 6.14.6: dense Tanviir architecture can exceed Unity's 16-bit index ceiling.
        // A UInt16 mesh above 65,535 vertices can disappear or render corruptly, which
        // looked like random 16x16 holes. Select the index width before assigning data.
        mesh.indexFormat = vertices.Count > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        // 6.14.12: feed Unity the existing Lists directly.  ToArray() duplicated every dense
        // chunk vertex/normal/UV buffer and produced large short-lived GC allocations.
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0,texCoords);

        int count = 0;
        foreach(var inds in indices) {
            if(inds!=null && inds.Count > 0) count++;
        }

        mesh.subMeshCount = count;
        int index = 0;
        foreach(var inds in indices) {
            if(inds!=null && inds.Count > 0) {
                mesh.SetIndices( inds, MeshTopology.Triangles, index, false );
                index++;
            }
        }

        // SetIndices above deliberately skips a full bounds pass per material/submesh.
        // Calculate bounds once after all submeshes are installed.
        mesh.RecalculateBounds();
        //mesh.Optimize();
		return mesh;
	}
	
	public Material[] GetMaterials(List<Material> materials) {
		List<Material> list = new List<Material>(indices.Length);
		for(int i=0; i<indices.Length; i++) {
			if(indices[i]!=null && indices[i].Count != 0) {
				list.Add( materials[i] );
			}
		}
		return list.ToArray();
	}
	

    public static int[] Triangulate(int i1, int i2, int i3, int i4) { 
        return new int[] {
            i1, i2, i3,
            i1, i3, i4,
        };
    }

    public static int[] Triangulate(int i1, int i2, int i3, int i4, 
                                    int i5, int i6, int i7, int i8) {
        return new int[] {
            i1, i2, i3,
            i1, i3, i4,

            i5, i6, i7,
            i5, i7, i8,
        };
    }
	
}
