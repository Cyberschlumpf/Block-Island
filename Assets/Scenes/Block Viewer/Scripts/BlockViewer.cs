using UnityEngine;
using System.Collections.Generic;

public class BlockViewer : MonoBehaviour {

#if UNITY_EDITOR

    private Block oldBlock;


    void Update() {
        Block block = UnityEditor.Selection.activeObject as Block;

        if(oldBlock != block && block) {
            GameObject target = GameObject.Find( "Target" );
            MeshFilter filter = target.GetComponent<MeshFilter>();

            MeshBuilder builder = block.Build();
            if(builder != null && builder.vertices.Count > 0) {
                Mesh mesh = builder.ToMesh( filter.sharedMesh );
                MoveMesh( mesh, -Vector3.one/2f );
                CreateColors( mesh );
                filter.sharedMesh = mesh;
                target.GetComponent<Renderer>().sharedMaterials = builder.GetMaterials( BlockSet.instance.GetMaterials() );
            } else {
                Destroy( filter.sharedMesh );
            }
        }
        
        oldBlock = block;
    }

    private static void MoveMesh(Mesh mesh, Vector3 delta) {
        Vector3[] vts = mesh.vertices;
        for(int i = 0; i < vts.Length; i++) {
            vts[i] += delta;
        }
        mesh.vertices = vts;
    }

    private static void CreateColors(Mesh mesh) { 
        Color32[] colors = new Color32[mesh.vertexCount];
        for(int i = 0; i < colors.Length; i++) {
            colors[i] = new Color32(200, 0, 0, 0);
        }
        mesh.colors32 = colors;
    }


    void OnDrawGizmos() {
        GameObject target = GameObject.Find( "Target" );
        if(target == null) return;
        MeshFilter filter = target.GetComponent<MeshFilter>();
        Mesh mesh = filter.sharedMesh;
        if(mesh == null) return;

        var vertices = mesh.vertices;
        var normals = mesh.normals;

        Gizmos.matrix = target.transform.localToWorldMatrix;
        for(int i = 0; i < vertices.Length; i++) {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(vertices[i], normals[i] * 0.1f);
        }
    }



#endif


}
