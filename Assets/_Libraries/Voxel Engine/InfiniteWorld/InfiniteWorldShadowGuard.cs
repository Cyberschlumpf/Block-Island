using UnityEngine;
using UnityEngine.Rendering;

// 1.0.8: Runtime shadow-state guard.
// Some gameplay/runtime transitions can leave Unity's global shadow state or the TOD
// directional light with shadows disabled. The startup visual setup repairs this only once,
// so the fault otherwise remains until the scene is restarted.
// This guard is intentionally cheap: it checks the global/TOD state periodically and only
// writes settings when they differ. Chunk renderer state is still owned by ChunkRenderer.
public sealed class InfiniteWorldShadowGuard : MonoBehaviour {
    const float CheckInterval = 0.5f;
    float nextCheck;

    void OnEnable() { nextCheck = 0f; }

    void Update() {
        if(Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckInterval;
        RepairShadowState();
    }

    static void RepairShadowState() {
        bool wanted = VoxelBoxSettings.Shadows;
        ShadowQuality wantedQuality = wanted ? ShadowQuality.All : ShadowQuality.Disable;
        int wantedCascades = wanted ? 4 : 0;

        if(QualitySettings.shadows != wantedQuality) QualitySettings.shadows = wantedQuality;
        if(QualitySettings.shadowCascades != wantedCascades) QualitySettings.shadowCascades = wantedCascades;
        if(Mathf.Abs(QualitySettings.shadowDistance - VoxelBoxSettings.ShadowDistance) > 0.1f)
            QualitySettings.shadowDistance = VoxelBoxSettings.ShadowDistance;

        ShadowResolution wantedResolution = VoxelBoxSettings.ShadowQualityLevel == 0
            ? ShadowResolution.Low
            : VoxelBoxSettings.ShadowQualityLevel == 1 ? ShadowResolution.High : ShadowResolution.VeryHigh;
        if(QualitySettings.shadowResolution != wantedResolution) QualitySettings.shadowResolution = wantedResolution;

        TOD_Sky sky = TOD_Sky.Instance;
        if(sky == null || sky.Components == null || sky.Components.LightSource == null) return;
        Light sun = sky.Components.LightSource;
        LightShadows wantedLightShadows = wanted ? LightShadows.Soft : LightShadows.None;
        if(!sun.enabled) sun.enabled = true;
        if(sun.shadows != wantedLightShadows) sun.shadows = wantedLightShadows;
        if(sun.cullingMask != ~0) sun.cullingMask = ~0;
    }
}
