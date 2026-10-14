using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor( typeof(Map) )]
public class MapEditor : Editor {


	void OnEnable() {
		Map map = (Map) target;
		foreach(Transform chunk in map.transform) {
			EditorUtility.SetSelectedRenderState(chunk.GetComponent<Renderer>(), EditorSelectedRenderState.Hidden);
		}
	}

	void OnDisable() {
		Map map = (Map) target;
		foreach(Transform chunk in map.transform) {
			EditorUtility.SetSelectedRenderState(chunk.GetComponent<Renderer>(), EditorSelectedRenderState.Wireframe);
		}
	}
	
	
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		if(!Application.isPlaying) return;
		Map map = (Map) target;
		
		int chunkCount = 0;
        Vector3i size = Vector3i.zero;
        foreach(var chunk in map.grid.chunks) {
            if(chunk != null) {
                chunkCount++;
                size = Vector3i.Max(size, (Vector3i) chunk.position + Vector3i.one);
            }
        }

        //int blockMemory = System.Runtime.InteropServices.Marshal.SizeOf(typeof(DataBlock));
        //int chunkMemory = blockMemory * Chunk.X_SIZE * Chunk.Y_SIZE * Chunk.Z_SIZE;
        //int mapMemory = chunkCount * chunkMemory;
		
        using( new VerticalLayout(GUI.skin.box) ) {
            GUILayout.Label("Chunk Count: " + chunkCount);
            GUILayout.Label("ChunkRenderer Count: " + map.transform.childCount);
            GUILayout.Label("Map Size "+size);

            /*EditorGUILayout.Separator();

            GUILayout.Label("Block Memory "+blockMemory+" B");
            GUILayout.Label("Chunk Memory " + (chunkMemory/1000f) + " KB");
            GUILayout.Label("Map Memory " + (mapMemory / 1000f / 1000f) + " MB");*/
        }

        if( GUILayout.Button("Compute Vertex Count") ) {
            int vertexCount = ComputeVertexCount();
            Debug.Log("Vertex Count "+vertexCount);
        }
        if(GUILayout.Button( "Compute Face Count" )) {
            int vertexCount = ComputeVertexCount();
            Debug.Log("Face Count " + (vertexCount/4));
        }
	}

    private int ComputeVertexCount() {
        Map map = target as Map;
        int vertexCount = 0;
        foreach(Transform child in map.transform) {
            MeshFilter filter = child.GetComponent<MeshFilter>();
            vertexCount += filter.sharedMesh.vertexCount;
        }
        return vertexCount;
    }


	void OnSceneGUI() {
		Map map = (Map) target;

        if(!Application.isPlaying) return;
		if(Event.current.type != EventType.Repaint) return;

		Ray ray = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );

		float distance = 100;
		if( MapRayIntersection.Raycast(map, ray, ref distance) ) {
			Vector3 point = ray.GetPoint( distance );
			Handles.color = Color.red;
			Handles.SphereHandleCap(0, point, Quaternion.identity, 0.1f, EventType.Repaint);
			Handles.DrawLine( point, point+Vector3.up*0.2f );

			//Vector3i pos = Vector3i.Round( point+Vector3.up/2f );
			//bool direct = map.GetSunLightmap().IsSunRay(pos);
			//int light = map.GetSunLightmap().GetLight(pos);
			//Debug.Log(direct +"   "+ light);
		}

	}
	
	
}
