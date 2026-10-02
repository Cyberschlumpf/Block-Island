using UnityEngine;
using System.Collections;

public class ChunkRenderer : MonoBehaviour {

	private Chunk chunk;
    private BlockTopology[] topology;
	private MeshFilter filter;
	private bool dirty = false, dirtyLight = false;
	
	
	public static ChunkRenderer CreateChunkRenderer(ChunkPosition pos, Chunk chunk) {
		GameObject go = new GameObject( string.Format("({0} {1} {2})", pos.x, pos.y, pos.z), typeof(MeshFilter), typeof(MeshRenderer), typeof(ChunkRenderer));
		go.transform.parent = Map.instance.transform;
		go.transform.localPosition = new Vector3i(pos.x, pos.y, pos.z).Mul(Chunk.SIZE);
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
		
		ChunkRenderer chunkRenderer = go.GetComponent<ChunkRenderer>();
		chunkRenderer.chunk = chunk;
		chunkRenderer.filter = go.GetComponent<MeshFilter>();
		
		go.GetComponent<Renderer>().castShadows = true; // Stage 6.11.6: never suppress world shadows
		go.GetComponent<Renderer>().receiveShadows = true; // Stage 6.11.6: terrain receives sun shadows
		
		return chunkRenderer;
	}
	
	
	void LateUpdate() {
		if(dirty) Build();
		if(dirtyLight) BuildLight();
	}


	public void Build() {
		dirty = dirtyLight = false;

		MeshBuilder builder = ChunkBuilder.BuildChunk(chunk, Map.instance);
        Build(builder);
	}

    public void Build(MeshBuilder builder) {
        dirty = dirtyLight = false;

        //if(Application.isEditor) CheckTopology(builder);


        Mesh mesh = builder.ToMesh( filter.sharedMesh );
        if( mesh ) {
            topology = builder.topology.ToArray();
            mesh.colors32 = ChunkBuilder.BuildLight(topology, builder.vertices, chunk.position, Map.instance);
            filter.sharedMesh = mesh;
            filter.GetComponent<Renderer>().materials = builder.GetMaterials( BlockSet.instance.GetMaterials() );
        } else {
            Destroy(gameObject);
        }
    }

    private static void CheckTopology(MeshBuilder builder) {
        int count = 0;
        foreach(BlockTopology block in builder.topology) {
            if(block.type == BlockTopologyType.Vertex) {
                count += block.count;
            } else {
                count += 4;
            }
        }

        if(count != builder.vertices.Count) {
            Debug.LogError( "Error chunk topology " + count + "  " + builder.vertices.Count );
        }
    }
	
	private void BuildLight() {
		dirtyLight = false;

		if(filter.sharedMesh != null) {
            Mesh mesh = filter.sharedMesh;
            mesh.colors32 = ChunkBuilder.BuildLight( topology, mesh.vertices, chunk.position, Map.instance );
		}
	}
	
	public void SetDirty() {
		dirty = true;
	}

	public void SetDirtyLight() {
		dirtyLight = true;
	}
	
		
}

