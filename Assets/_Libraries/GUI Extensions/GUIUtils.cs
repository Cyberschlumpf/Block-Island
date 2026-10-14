using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class GUIUtils {
	
	
	private static Texture2D _whiteTexture;
	public static Texture2D whiteTexture {
		get {
			if(_whiteTexture == null) {
				_whiteTexture = new Texture2D(1,1);
				_whiteTexture.SetPixel(0, 0, Color.white);
				_whiteTexture.Apply();
			}
			return _whiteTexture;
		}
	}
	
	public static T GetStateObject<T>(int controlID) {
		return (T) GUIUtility.GetStateObject(typeof(T), controlID);
	}
	
	public static void FillRect(Rect rect, Color color) {
        if(Event.current.type == EventType.Repaint) {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture( rect, whiteTexture );
            GUI.color = oldColor;
        }
	}
	
	public static Rect[] Separate(Rect mainRect, int xCount, int yCount) {
		float itemWidth = mainRect.width / xCount;
		float itemHeight = mainRect.height / yCount;
		List<Rect> list = new List<Rect>();
		for(int y=0; y<yCount; y++) {
			for(int x=0; x<xCount; x++) {
				Rect rect = new Rect(mainRect.x+itemWidth*x, mainRect.y+itemHeight*y, itemWidth, itemHeight);
				list.Add(rect);
			}
		}
		return list.ToArray();
	}
	
	
}