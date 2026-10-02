using UnityEngine;

// Carries only the Island Generator parameters into the infinite world.
// Stage 6.5.6 deliberately does NOT copy the legacy finite voxel raster: that raster
// contains the old hard map boundary and was the source of the giant sand plateau.
public static class InfiniteWorldLaunchConfig {
    public static bool pending;
    public static int seed = 1337;
    public static float noiseScale = 1f/20f;
    public static Vector2 noiseOffset = Vector2.zero;
    public static int previewSizeX = 0;
    public static int previewSizeZ = 0;
    public static AnimationCurve previewCurve;
    // 0.9.9.8a - first native Sky Islands world mode.
    public static bool skyIslands = false;
    // 0.9.9.8b: main-menu shortcut opens the generator already in Sky-Islands mode.
    public static bool openSkyGenerator = false;

    public static void Capture(IslandGenerator island) {
        pending=true;
        noiseScale=island.noise.scale;
        noiseOffset=island.noise.offset;
        previewSizeX=island.sizeX;
        previewSizeZ=island.sizeZ;
        previewCurve=new AnimationCurve(island.curve.keys);
        unchecked { seed=1337 ^ Mathf.RoundToInt(noiseOffset.x*10007f) ^ (Mathf.RoundToInt(noiseOffset.y*30011f)<<1); }
        skyIslands=island.skyIslands;
        Debug.LogWarning("INFINITE START ISLAND PARAMETERS CAPTURED: "+previewSizeX+"x"+previewSizeZ+" seed="+seed+" (legacy raster disabled)");
    }
}
