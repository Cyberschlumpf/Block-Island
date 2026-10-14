using UnityEngine;

public class GameInputManager : MonoBehaviour {
    private static GameInputManager instance;
    private const string SensitivityKey = "VoxelBox.MouseSensitivity";
    private static float cachedSensitivity = -1f;
    private float sensitivity = 5f;

    public static float Sensitivity {
        get {
            if (instance != null) return instance.sensitivity;
            if (cachedSensitivity < 0f) cachedSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 5f);
            return cachedSensitivity;
        }
        set {
            float v = Mathf.Clamp(value, 1f, 10f);
            cachedSensitivity = v;
            if (instance != null) instance.sensitivity = v;
            PlayerPrefs.SetFloat(SensitivityKey, v);
        }
    }

    void Awake(){
        instance = this;
        sensitivity = cachedSensitivity >= 0f ? cachedSensitivity : PlayerPrefs.GetFloat(SensitivityKey, 5f);
        cachedSensitivity = sensitivity;
    }

    void OnDestroy(){ if(instance == this) instance = null; }
}
