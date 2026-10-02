using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BlockListViewer {
	
	private static string DRAG_AND_DROP = "drag and drop block";
	
	public static int DrawGrid(List<Block> items, int index) {
		Rect rect;
		int xCount, yCount;
		index = DrawGrid(items, index, out rect, out xCount, out yCount);
		float itemWidth = rect.width/xCount;
		float itemHeight = rect.height/yCount;

		using( new Group(rect) ) {
			Vector2 mouse = Event.current.mousePosition;
			int posX = Mathf.FloorToInt(mouse.x/itemWidth);
			int posY = Mathf.FloorToInt(mouse.y/itemHeight);
			int realIndex = -1; // номер элемента под курсором
			if(posX >= 0 && posX < xCount && posY >= 0 && posY < yCount) realIndex = posY*xCount + posX;
			
			int dropX = Mathf.Clamp(posX, 0, xCount-1);
			int dropY = Mathf.Clamp(posY, 0, yCount-1);
			int dropIndex = dropY*xCount + dropX; // ближайший элемент к курсору
			dropIndex = Mathf.Clamp(dropIndex, 0, items.Count);
			
			if(Event.current.IsMouseDrag(0) && realIndex == index) {
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.objectReferences = new Object[0];
				DragAndDrop.paths = new string[0];
				DragAndDrop.SetGenericData(DRAG_AND_DROP, index);
				DragAndDrop.StartDrag("DragAndDrop");
				Event.current.Use();
			}
			
			if(Event.current.type == EventType.DragUpdated) {
				object oldIndex = DragAndDrop.GetGenericData(DRAG_AND_DROP);
				if(oldIndex != null) {
					DragAndDrop.visualMode = DragAndDropVisualMode.Link;
					Event.current.Use();
				}
			}
			
			if(Event.current.type == EventType.DragPerform) {
				object oldIndex = DragAndDrop.GetGenericData(DRAG_AND_DROP);
				if(oldIndex != null) {
					index = Swap(items, dropIndex, (int)oldIndex);
					DragAndDrop.AcceptDrag();
                    GUI.changed = true;
					Event.current.Use();
				}
			}
			
			if(Event.current.type == EventType.Repaint && DragAndDrop.visualMode != DragAndDropVisualMode.None) {
				object oldIndex = DragAndDrop.GetGenericData(DRAG_AND_DROP);
				if(oldIndex != null) {
					int x = dropIndex % xCount;
					int y = dropIndex / xCount;
					Rect lineRect = new Rect(x*itemWidth, y*itemHeight+2, 2, itemWidth-2);
					EditorGUIUtils.FillRect(lineRect, Color.red);
				}
			}
		}
		
		return index;
	}
	
	private static int DrawGrid(List<Block> items, int index, out Rect rect, out int xCount, out int yCount) {
		xCount = Mathf.FloorToInt( Screen.width/50f );
		yCount = Mathf.CeilToInt( (float) items.Count/xCount );
		
		rect = GUILayoutUtility.GetAspectRect((float)xCount/yCount);
		float labelHeight = GUI.skin.label.CalcHeight(GUIContent.none, 0); // высота текста
		GUILayout.Space(labelHeight*yCount);
		rect.height += labelHeight*yCount;
		
		Rect[] rects = GUIUtils.Separate(rect, xCount, yCount);
		for(int i=0; i<items.Count; i++) {
			Rect position = rects[i];
			position.xMin += 2;
			position.yMin += 2;
				
			bool selected = DrawItem(position, items[i], i == index, i);
			if(selected) index = i;
		}
		
		return index;
	}
	
	private static bool DrawItem(Rect position, Block block, bool selected, int index) {
		Rect texturePosition = position;
		texturePosition.height = texturePosition.width;
		Rect labelPosition = position;
		labelPosition.yMin += texturePosition.height;
		
		if(selected) EditorGUIUtils.FillRect(labelPosition, EditorGUIUtils.SELECT_COLOR);
		if(block != null) {
			block.DrawPreview(texturePosition);
			GUI.Label(labelPosition, block.name);
		} else {
			EditorGUIUtils.FillRect(texturePosition, Color.grey);
			GUI.Label(labelPosition, "Null");
		}
		
		if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && position.Contains(Event.current.mousePosition)) {
			Event.current.Use();
			return true;
		}
		return false;
	}
	
	private static int Swap(List<Block> items, int newIndex, int oldIndex) {
		items.Insert(newIndex, items[oldIndex]);
		if( newIndex < oldIndex ) {
			items.RemoveAt( oldIndex+1 );
		} else {
			items.RemoveAt( oldIndex );
			newIndex--;
		}
		return Mathf.Clamp(newIndex, 0, items.Count-1);
	}
	
}
