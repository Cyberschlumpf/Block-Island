using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor( typeof(GameObjectBlock) )]
public class GameObjectBlockEditor : Editor {
	
	private Vector3 angles;

	public override void OnInspectorGUI() {
		GameObjectBlock block = (GameObjectBlock) target;

        MonoScript script = MonoScript.FromScriptableObject( block );
        EditorGUIUtils.AssetField( "Script", script );

		using( new VerticalLayout(GUI.skin.box) ) {
			block.gameObject = EditorGUIUtils.AssetField<GameObject>("GameObject", block.gameObject);
			
			block.glow = EditorGUILayout.IntField("Light", block.glow);
			block.glow = Mathf.Clamp(block.glow, 0, 15);
			
			block.icon = EditorGUIUtils.AssetField<Texture2D>("Icon", block.icon);
		}
		
		if(GUI.changed) {
			EditorUtility.SetDirty( target );
		}
	}
	
	
	public override bool HasPreviewGUI() {
		GameObjectBlock block = (GameObjectBlock) target;
		return block.gameObject != null;
	}
	
	public override void OnPreviewGUI(Rect rect, GUIStyle background) {
		var block = (Block) target;
		block.DrawPreview(rect);
	}
	
	public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background) {
		if(Event.current.IsMouseDrag()) {
			angles.x += Event.current.delta.y;
			angles.x = Mathf.Clamp(angles.x, -60, 60);
			angles.y += Event.current.delta.x;
			Event.current.Use();
		}
		
		GameObjectBlock block = (GameObjectBlock) target;
		Quaternion rotation = Quaternion.Euler(angles);
		EditorGUIUtils.DrawObjectPreview(rect, block.gameObject, rotation, 2);
	}
	
	
}
