using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class FaceEditor {
	
	private static Vector2i startPos;
	
	public static void DrawFaceEditor(Face face, ref Matrix4x4 matrix) {
		using( new VerticalLayout(GUI.skin.box) ) {
			face.material = EditorGUIUtils.AssetField<Material>("Material", face.material);
			EditorGUILayout.Separator();

			Rect position = GUILayoutUtility.GetAspectRect(1);
			using( new Group(position) ) {
                position.position = Vector2.zero;
                Texture2D texture = face.GetTexture();
                if(texture != null) DrawFaceEditor( position, texture, face, ref matrix );
			}
		}
	}
	
	private static void DrawFaceEditor(Rect groupRect, Texture2D texture, Face face, ref Matrix4x4 matrix) {
        int xTileCount = texture.width / 16;
        int yTileCount = texture.height / 16;

        float TILE_WIDTH = 1f / xTileCount;
	    float TILE_HEIGHT = 1f / yTileCount;

        Rect position = new Rect( 0, 0, texture.width, texture.height );
        position = RectUtils.Mul( position, matrix );

        Matrix4x4 atlasToGUIMatrix = MatrixUtils.Create( position.x, position.yMax, position.width, -position.height );

        GUIUtils.FillRect( position, Color.red );
        GUI.DrawTexture( position, texture );
		EditorGUIUtils.DrawRect( RectUtils.Mul(face.rect, atlasToGUIMatrix), Color.green );


        if(!groupRect.Contains( Event.current.mousePosition )) return;

		if(Event.current.IsMouseDrag(1)) {
			Vector3 delta = Event.current.delta;
			Matrix4x4 translate = Matrix4x4.TRS(delta, Quaternion.identity, Vector3.one);
			matrix = translate*matrix;

			Event.current.Use();
		}
		
		if(Event.current.IsScrollWheel()) {
            Vector2 mouse = matrix.inverse.MultiplyPoint( Event.current.mousePosition );

			float s = 0.95f;
			if(Event.current.delta.y < 0) s = 1.0f/s;
				
			matrix *= Matrix4x4.TRS(mouse, Quaternion.identity, Vector3.one);
			matrix *= Matrix4x4.Scale(Vector3.one*s);	
			matrix *= Matrix4x4.TRS(-mouse, Quaternion.identity, Vector3.one);

			Event.current.Use();
		}


        Vector2 pos01 = atlasToGUIMatrix.inverse.MultiplyPoint( Event.current.mousePosition );
        Vector2i pos = new Vector2i( Mathf.FloorToInt( pos01.x * xTileCount ), Mathf.FloorToInt( pos01.y * yTileCount ) );

        if(Event.current.IsMouseDown( 0 ) && position.Contains( Event.current.mousePosition )) {
            if(Event.current.clickCount == 1) {
                startPos = pos;

                face.rect.x = pos.x * TILE_WIDTH;
                face.rect.y = pos.y * TILE_HEIGHT;
                face.rect.width = TILE_WIDTH;
                face.rect.height = TILE_HEIGHT;
            } else {
                face.rect = new Rect( 0, 0, 1, 1 );
                startPos.x = startPos.y = -1;
            }

            GUI.changed = true;
            Event.current.Use();
        }

        if(Event.current.IsMouseDrag( 0 ) && startPos.x != -1 && position.Contains( Event.current.mousePosition )) {
            int sizeX = Mathf.Abs( pos.x - startPos.x ) + 1;
            int sizeY = Mathf.Abs( pos.y - startPos.y ) + 1;
            pos.x = Mathf.Min( startPos.x, pos.x );
            pos.y = Mathf.Min( startPos.y, pos.y );

            face.rect.x = pos.x * TILE_WIDTH;
            face.rect.y = pos.y * TILE_HEIGHT;
            face.rect.width = sizeX * TILE_WIDTH;
            face.rect.height = sizeY * TILE_HEIGHT;

            GUI.changed = true;
            Event.current.Use();
        }
		
	}
	
	
}
