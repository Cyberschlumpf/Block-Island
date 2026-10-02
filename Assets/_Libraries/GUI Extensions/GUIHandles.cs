using UnityEngine;

public class AreaLayout : System.IDisposable {

	public AreaLayout(Rect rect) {
		GUILayout.BeginArea(rect);
	}
	
	public void Dispose() {
		GUILayout.EndArea();
	}
}


public class Group : System.IDisposable {

	public Group(Rect rect) {
		GUI.BeginGroup(rect);
	}
	
	public void Dispose() {
		GUI.EndGroup();
	}

}


public class ScrollView : System.IDisposable {
	
	public ScrollView(ref Vector2 position) {
		position = GUILayout.BeginScrollView(position);
	}
	
	public void Dispose() {
		GUILayout.EndScrollView();
	}
}


public class HorizontalLayout : System.IDisposable {

	private int right = 0;

	public HorizontalLayout(params GUILayoutOption[] options) {
		GUILayout.BeginHorizontal(options);
	}

	public HorizontalLayout(GUIStyle style, params GUILayoutOption[] options) {
		GUILayout.BeginHorizontal(style, options);
	}

	public HorizontalLayout(int left, int right) {
		this.right = right;
		GUILayout.BeginHorizontal();
		for(int i=0; i<left; i++) GUILayout.FlexibleSpace(); 
	}
	
	public void Dispose() {
		for(int i=0; i<right; i++) GUILayout.FlexibleSpace(); 
		GUILayout.EndHorizontal();
	}
}



public class VerticalLayout : System.IDisposable {

	private int bottom = 0;

	public VerticalLayout(params GUILayoutOption[] options) {
		GUILayout.BeginVertical(options);
	}

	public VerticalLayout(GUIStyle style, params GUILayoutOption[] options) {
		GUILayout.BeginVertical(style, options);
	}

	public VerticalLayout(int top, int bottom) {
		this.bottom = bottom;
		GUILayout.BeginVertical();
		for(int i=0; i<top; i++) GUILayout.FlexibleSpace(); 
	}
	
	public void Dispose() {
		for(int i=0; i<bottom; i++) GUILayout.FlexibleSpace(); 
		GUILayout.EndVertical();
	}
}
