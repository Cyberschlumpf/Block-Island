using UnityEngine;

/// <summary>
/// Stage 6.13.1: Reduces rectilinear edge stretching on ultrawide displays.
/// Unity normally keeps vertical FOV fixed, so 32:9 becomes an extremely wide
/// horizontal projection. Above the reference aspect this component preserves
/// the horizontal FOV that the game has at 16:9 instead.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class UltrawideCameraNormalizer : MonoBehaviour
{
    [Tooltip("Aspect ratio at which the authored camera FOV is used unchanged.")]
    public float referenceAspect = 16f / 9f;

    [Tooltip("Only normalize screens wider than the reference aspect.")]
    public bool ultrawideOnly = true;

    [Tooltip("0 = normal Unity vertical-FOV behavior, 1 = full 16:9 horizontal-FOV preservation.")]
    [Range(0f, 1f)] public float strength = 1f;

    private Camera cam;
    private float authoredVerticalFov;
    private int lastWidth = -1;
    private int lastHeight = -1;

    void Awake()
    {
        cam = GetComponent<Camera>();
        authoredVerticalFov = cam.fieldOfView;
        Apply();
    }

    void OnEnable()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (authoredVerticalFov <= 0f) authoredVerticalFov = cam.fieldOfView;
        Apply();
    }

    void LateUpdate()
    {
        // Also reacts correctly when switching resolution/fullscreen/windowed mode.
        if (Screen.width != lastWidth || Screen.height != lastHeight) Apply();
    }

    public void Apply()
    {
        if (cam == null || Screen.height <= 0) return;
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float aspect = (float)Screen.width / Screen.height;
        if (ultrawideOnly && aspect <= referenceAspect + 0.001f)
        {
            cam.fieldOfView = authoredVerticalFov;
            return;
        }

        // Horizontal FOV at the game's reference 16:9 presentation.
        float halfV = authoredVerticalFov * Mathf.Deg2Rad * 0.5f;
        float referenceHalfHorizontal = Mathf.Atan(Mathf.Tan(halfV) * referenceAspect);

        // Convert that same horizontal FOV back to the vertical FOV required by
        // the actual display aspect. This avoids the exaggerated 32:9 edges.
        float normalizedVertical = 2f * Mathf.Atan(Mathf.Tan(referenceHalfHorizontal) / aspect) * Mathf.Rad2Deg;
        cam.fieldOfView = Mathf.Lerp(authoredVerticalFov, normalizedVertical, Mathf.Clamp01(strength));
    }
}
