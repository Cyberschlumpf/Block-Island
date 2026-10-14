using System.Collections.Generic;
using UnityEngine;

// Block Island 1.0 migration: placement fixes for 1.0 prefab blocks (figures, objects, rides),
// called by InfiniteChunkMeshRenderer for every instantiated GameObjectBlock prefab.
public static class BlockIsland1Placement {
    static HashSet<Block> legacy;
    static int legacyCount = -1;
    // prefab -> height of its lowest animated vertex above the instance pivot (measured once)
    static readonly Dictionary<GameObject, float> footOffset = new Dictionary<GameObject, float>();

    public static bool IsLegacy(Block b) {
        Block[] slots = BlockSet.blockIsland1Slots;
        if(legacy == null || legacyCount != slots.Length) { legacy = new HashSet<Block>(slots); legacyCount = slots.Length; }
        return b != null && legacy.Contains(b);
    }

    // Stand on the block floor: the lowest point of the ANIMATED model sits on the bottom of the voxel,
    // X/Z stay at the block centre. Rides (prefabs with their own camera, e.g. the roller coaster) keep
    // their original 1.0 set point. The first instance of a prefab is measured after its animation has
    // run (bind-pose bounds put animals in the air); later instances reuse that offset immediately.
    // A height chosen by hand (key J, BlockIsland1FigureHeights) overrides the automatic floor.
    public static void SnapToFloor(GameObject instance, Block block, GameObject prefab, float floorY) {
        BlockIsland1Figure figure = instance.AddComponent<BlockIsland1Figure>();
        figure.block = block; figure.prefab = prefab; figure.floorY = floorY;
        if(prefab != null && !footOffset.ContainsKey(prefab) && instance.GetComponentInChildren<Camera>(true) != null)
            footOffset[prefab] = float.NaN;                              // rides: never measured
        if(prefab == null || footOffset.ContainsKey(prefab)) { figure.Apply(); return; }
        BlockIsland1FloorSnap s = instance.AddComponent<BlockIsland1FloorSnap>();
        s.prefab = prefab;
    }

    // Measured contact point of a prefab relative to its pivot. NaN = keeps its 1.0 set point.
    internal static bool TryContact(GameObject prefab, out float contact) {
        contact = float.NaN;
        return prefab != null && footOffset.TryGetValue(prefab, out contact);
    }

    // True when the prefab was measured and keeps its original 1.0 set point (strongly animated objects).
    public static bool KeepsOriginalSetPoint(GameObject prefab) { float o; return prefab != null && footOffset.TryGetValue(prefab, out o) && float.IsNaN(o); }

    internal static void Remember(GameObject prefab, float offset) { if(prefab != null) footOffset[prefab] = offset; }

    // Lowest world-space point of all visible meshes, from Unity's own renderer bounds. Skinned meshes
    // must have updateWhenOffscreen enabled (see BlockIsland1FloorSnap) so their bounds follow the
    // current animation pose instead of the imported bind-pose box.
    internal static float LowestPoint(GameObject root) {
        float low = float.PositiveInfinity;
        foreach(Renderer r in root.GetComponentsInChildren<Renderer>()) {
            if(!r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            low = Mathf.Min(low, r.bounds.min.y);
        }
        return float.IsPositiveInfinity(low) ? float.NaN : low;
    }

    // 1.0 rides (e.g. the roller coaster) carry their own "Main Camera". Never let it take over the
    // player's view or Camera.main; it is only used while riding (key G, see BlockIsland1Ride).
    public static void PrepareRideCameras(GameObject instance) {
        foreach(Camera c in instance.GetComponentsInChildren<Camera>(true)) {
            c.enabled = false;
            if(c.CompareTag("MainCamera")) c.tag = "Untagged";
            AudioListener l = c.GetComponent<AudioListener>(); if(l != null) l.enabled = false;
            BlockIsland1Ride.Register(c, instance.transform);
        }
    }
}
