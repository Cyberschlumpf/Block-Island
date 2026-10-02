using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

public class EditorGUIUtils {
	
	public static readonly Color SELECT_COLOR = new Color32( 61, 96, 145, 255 );
	
	public static void DrawRect(Rect rect, Color color) {
		Vector3 a = new Vector3(rect.xMin, rect.yMin);
		Vector3 b = new Vector3(rect.xMax, rect.yMin);
		Vector3 c = new Vector3(rect.xMax, rect.yMax);
		Vector3 d = new Vector3(rect.xMin, rect.yMax);
		
		Handles.color = color;
		Handles.DrawLine(a, b);
		Handles.DrawLine(b, c);
		Handles.DrawLine(c, d);
		Handles.DrawLine(d, a);
	}
	
	public static void FillRect(Rect rect, Color color) {
        if(Event.current.type == EventType.Repaint) {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture( rect, EditorGUIUtility.whiteTexture );
            GUI.color = oldColor;
        }
	}
	
	public static int Popup(string label, int selected, IList items, params GUILayoutOption[] options) {
		string[] strings = new string[items.Count];
		for(int i=0; i<items.Count; i++) {
			if(items[i] != null) strings[i] = items[i].ToString();
		}
		return EditorGUILayout.Popup(label, selected, strings, options);
	}
	
	public static int DrawList(int selected, IList list) {
		float labelHeight = GUI.skin.label.CalcHeight( GUIContent.none, 0 );
		Rect rect = GUILayoutUtility.GetRect(0, labelHeight*list.Count, GUILayout.ExpandWidth(true));
		Rect[] rects = GUIUtils.Separate(rect, 1, list.Count);
		for(int i=0; i<list.Count; i++) {
			Rect position = rects[i];
			object item = list[i];
			
			if(i == selected) FillRect(position, SELECT_COLOR);
			string name = item != null ? item.ToString() : "Null";
			GUI.Label(position, name);
		}
		
		
		GUI.BeginGroup(rect);
		if(Event.current.type == EventType.MouseDown) {
			float mouseY = Event.current.mousePosition.y;
			selected = Mathf.FloorToInt( mouseY / labelHeight );
			if(selected < 0 || selected >= list.Count) selected = -1;
            EditorGUIUtility.hotControl = 0;
            EditorGUIUtility.keyboardControl = 0;
			Event.current.Use();
		}
		GUI.EndGroup();
		
		return selected;
	}
	
	public static T AssetField<T>(T obj) where T : UnityEngine.Object {
		return (T) EditorGUILayout.ObjectField(obj, typeof(T), false);
	}
	
	public static T AssetField<T>(string label, T obj) where T : UnityEngine.Object {
		return (T) EditorGUILayout.ObjectField(label, obj, typeof(T), false);
	}
	
	public static void DrawMeshPreview(Rect rect, Mesh mesh, Material material, Quaternion rotation, float distance) {
		if(Event.current.type != EventType.Repaint) return;
		
		Material[] materials = new Material[] {material};
		DrawMeshPreview( rect, mesh, materials, rotation, distance );
	}
	
	public static void DrawMeshPreview(Rect rect, Mesh mesh, Material[] materials, Quaternion rotation, float distance) {
		if(Event.current.type != EventType.Repaint) return;

        GameObject tmpObject = new GameObject( "Model", typeof( MeshFilter ), typeof( MeshRenderer ) );
        tmpObject.GetComponent<MeshFilter>().mesh = mesh;
        tmpObject.GetComponent<Renderer>().materials = materials;

        DrawObjectPreview(rect, tmpObject, rotation, distance);
	}
	
	public static void DrawObjectPreview(Rect rect, GameObject tmpObject, Quaternion rotation, float distance) {
		if(Event.current.type != EventType.Repaint) return;
		
		int layer = LayerMask.NameToLayer("Preview");
		
		GameObject camera = new GameObject("Camera", typeof(Camera));
		camera.hideFlags = HideFlags.HideAndDontSave;

        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.Depth;
		camera.GetComponent<Camera>().cullingMask = 1 << layer;
		camera.transform.position = Vector3.zero;
		camera.transform.rotation = rotation;
		camera.transform.Translate(0, 0, -distance);
		
        if( !tmpObject.activeInHierarchy ) {
            tmpObject = (GameObject) GameObject.Instantiate( tmpObject );
        }
		tmpObject.hideFlags = HideFlags.HideAndDontSave;
		SetLayer(tmpObject, layer);

        if(tmpObject.GetComponent<Renderer>() != null) {
            Bounds bounds = tmpObject.GetComponent<Renderer>().bounds;
            tmpObject.transform.position = -bounds.center;
        }
		
		Handles.DrawCamera(rect, camera.GetComponent<Camera>());
		
		GameObject.DestroyImmediate(camera);
		GameObject.DestroyImmediate(tmpObject);
	}
	
	private static void SetLayer(GameObject obj, int layer) {
		obj.layer = layer;
		foreach(Transform child in obj.transform) {
			SetLayer(child.gameObject, layer);
		}
	}
	
	
}
