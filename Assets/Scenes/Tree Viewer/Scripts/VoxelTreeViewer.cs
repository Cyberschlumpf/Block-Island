using UnityEngine;
using System.Collections;
using System;

public class VoxelTreeViewer : MonoBehaviour {


#if UNITY_EDITOR


	void OnGUI() {
		Map map = Map.instance;
        SunLightmap sunmap = SunLightmap.instance;

		GameObject obj = UnityEditor.Selection.activeGameObject;
		VoxelTree tree = null;
		if(obj) tree = obj.GetComponent<VoxelTree>();


        using(new AreaLayout( new Rect( 0, 0, Screen.width, Screen.height ) )) {
            using(new VerticalLayout( 1, 0 )) {
                using(new HorizontalLayout( 1, 1 )) {

                    if(GUILayout.Button( "Clear" )) {
                        map.Clear();
                    }

                    GUI.enabled = tree != null;
                    if(GUILayout.Button( "Generate" )) {
                        map.Clear();

                        float t1 = Time.realtimeSinceStartup;
                        tree.Generate( map, new Vector3i( 20, 0, 20 ) );

                        float t2 = Time.realtimeSinceStartup;
                        MapLightComputer.ComputeSunLight( sunmap, map );

                        float t3 = Time.realtimeSinceStartup;
                        map.Build();

                        float t4 = Time.realtimeSinceStartup;

                        Debug.Log( "Tree Generation Time " + (t2 - t1) );
                        Debug.Log( "Light Computing Time " + (t3 - t2) );
                        Debug.Log( "Building Time " + (t4 - t3) );
                        Debug.Log( "Total Time " + (t4 - t1) );
                    }
                    GUI.enabled = true;

                }
            }
        }

	}

#endif

}
