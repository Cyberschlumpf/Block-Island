using UnityEngine;
using System.Collections;

public class GameState : MonoBehaviour {
	
	private static GameState _instance;
	private static GameState instance {
		get {
			if(_instance == null) _instance = FindFirstObjectByType<GameState>();
			return _instance;
		}
	}
	
	public static bool IsPause {
		set {
			if(value) {
				Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
			} else {
				Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
			}
		}
		get {
			return Time.timeScale <= 0.0001f;
		}
	}

	public static bool IsPlaying {
		set {
			IsPause = !value;
		}
		get {
			return !IsPause;
		}
	}

	
	void Start() {
        IsPlaying = true;
	}
	
	void Update() {
        // 0.9.9.5e: TAB is owned by Builder while the Custom Workshop is active.
        // The old global TAB handler always forced the cursor open again in the same frame,
        // which made the second workshop TAB appear to do nothing.
		if(Input.GetKeyDown(KeyCode.Escape)) {
			IsPause = !IsPause;
		}
	}
	
}
