using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor( typeof(BlockSet) )]
public class BlockSetEditor : Editor {
	
	private int selectedBlock;
	
	[MenuItem( "GameObject/Create Other/BlockSet" )]
	private static void Create() {
		new GameObject("BlockSet", typeof(BlockSet));
	}
	
	
	public override void OnInspectorGUI() {
		BlockSet blockSet = (BlockSet) target;

		DrawBlockSet(blockSet, ref selectedBlock);

		EditorGUILayout.Separator();
		
		if( blockSet.GetMaterials().Count > 0 ) {
			using( new VerticalLayout(GUI.skin.box) ) {
				foreach(Material mat in blockSet.GetMaterials()) {
					EditorGUIUtils.AssetField<Material>(mat);
				}
			}
		}
		
		if(GUI.changed) EditorUtility.SetDirty( blockSet );
	}


	private static void DrawBlockSet(BlockSet blockSet, ref int selectedBlock) {
		List<Block> list = blockSet.GetBlocks();

		using( new VerticalLayout(GUI.skin.box, GUILayout.ExpandWidth(true)) ) {
			if(list.Count > 0) {
				selectedBlock = BlockListViewer.DrawGrid(list, selectedBlock);
			} else {
				GUILayout.Label("List is empty");
			}
			
			GUILayout.Space(64);
			GUILayout.Label("Drag And Drop", GUILayout.ExpandWidth(true));
			
			using( new HorizontalLayout() ) {
				list[selectedBlock] = EditorGUIUtils.AssetField<Block>( list[selectedBlock] );
				if(GUILayout.Button("Remove") && list.Count > 0) {
					Undo.RecordObject( blockSet, "BlockSet" );
					list.RemoveAt(selectedBlock);
					selectedBlock = Mathf.Clamp(selectedBlock, 0, list.Count-1);
				}
			}
		}
		Rect rect = GUILayoutUtility.GetLastRect();

		if( rect.Contains(Event.current.mousePosition) ) {
			DragAndDropReceiver( list );
		}
	}
	
	private static void DragAndDropReceiver(List<Block> list) {
		if(Event.current.type == EventType.DragUpdated) {
			DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			foreach(Object obj in DragAndDrop.objectReferences) {
				if( obj is Block == false ) {
					DragAndDrop.visualMode = DragAndDropVisualMode.None;
					break;
				}
			}
		}
		
		if(Event.current.type == EventType.DragPerform) {
			foreach(Object obj in DragAndDrop.objectReferences) {
				if(!list.Contains( (Block) obj )) {
					list.Add( (Block) obj );
					GUI.changed = true;
				}
			}
		}
	}
	
	
}
