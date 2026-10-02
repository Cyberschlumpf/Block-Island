using UnityEngine;
using System.Collections;

public abstract class GUIScreen : MonoBehaviour {

	public void SetScreen<T>() {
		foreach(GUIScreen screen in GetComponents<GUIScreen>()) {
			screen.enabled = screen is T;
		}
	}

}
