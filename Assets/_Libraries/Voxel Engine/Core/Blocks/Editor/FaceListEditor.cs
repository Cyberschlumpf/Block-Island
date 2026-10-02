using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FaceListEditor {
	

	public static Face DrawFaceList(Block block, ref int selected) {
		Face[] faces = Face.GetFaceList(block);
		string[] names = Face.GetFaceNameList(block);

		if(faces.Length > 0) {
			using( new VerticalLayout(GUI.skin.box) ) {
				selected = Mathf.Clamp(selected, 0, faces.Length-1);
				selected = DrawFaceList(faces, names, selected);
			}
			return faces[selected];
		}

		return null;
	}
	
	private static int DrawFaceList(Face[] faces, string[] names, int selected) {
		using( new HorizontalLayout(1, 1) ) {
			using( new VerticalLayout() ) {
				Rect rect = GUILayoutUtility.GetAspectRect( faces.Length, GUILayout.MaxWidth(64*faces.Length) );
				Rect[] rectList = GUIUtils.Separate(rect, faces.Length, 1);
				for(int i=0; i<rectList.Length; i++) {
					bool pressed = DrawFace(rectList[i], faces[i], names[i], i == selected);
					if(pressed) selected = i;
				}
				
				if(faces.Length > 1) {
					GUIStyle style = new GUIStyle(GUI.skin.button);
					style.margin.left = 0;
					style.margin.right = 0;
					selected = GUILayout.Toolbar(selected, names, style);
				}
			}
		}
		
		return selected;
	}
	
	private static bool DrawFace(Rect position, Face face, string name, bool selected) {
		Texture2D texture = face.GetTexture();
		if(texture) {
			GUI.DrawTextureWithTexCoords(position, texture, face.rect);
		} else {
			GUIUtils.FillRect(position, Color.gray);
		}

		if(!position.Contains(Event.current.mousePosition)) return false;

		if( Event.current.type == EventType.MouseDrag ) {
			DragAndDrop.PrepareStartDrag();
			DragAndDrop.SetGenericData("Face", face);
			DragAndDrop.StartDrag("Dragging block");
			Event.current.Use();
		}

		if(Event.current.type == EventType.DragUpdated) {
			Face dropFace = (Face) DragAndDrop.GetGenericData("Face");
			if(dropFace != null) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				Event.current.Use();
			}
		}
		
		if(Event.current.type == EventType.DragPerform) {
			Face dropFace = (Face) DragAndDrop.GetGenericData("Face");
			if(dropFace != null) {
				face.Set( dropFace );
				DragAndDrop.AcceptDrag();
				GUI.changed = true;
				Event.current.Use();
                return true;
			}
		}
		
		if(Event.current.type == EventType.MouseDown) {
			Event.current.Use();
			return true;
		}
		return false;
	}
	
}
