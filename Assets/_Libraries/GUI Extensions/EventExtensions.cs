using UnityEngine;
using System.Collections;

public static class EventExtensions {

	public static bool IsMouseDown(this Event evt) {
		return evt.type == EventType.MouseDown;
	}
	public static bool IsMouseDown(this Event evt, int button) {
		return evt.type == EventType.MouseDown && evt.button == button;
	}

	public static bool IsMouseDrag(this Event evt) {
		return evt.type == EventType.MouseDrag;
	}
	public static bool IsMouseDrag(this Event evt, int button) {
		return evt.type == EventType.MouseDrag && evt.button == button;
	}

	public static bool IsMouseUp(this Event evt) {
		return evt.type == EventType.MouseUp;
	}
	public static bool IsMouseUp(this Event evt, int button) {
		return evt.type == EventType.MouseUp && evt.button == button;
	}

	public static bool IsScrollWheel(this Event evt) {
		return evt.type == EventType.ScrollWheel;
	}

}
