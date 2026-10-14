using UnityEngine;

// Start point of the current world. Used for a new world without saved player position
// (GameInitializer) and by the teleport key T (CharacterInputController).
public static class InfiniteWorldSpawn {
    // Block Island: the 1.0 world is 1024 x 1024; 1.0 started the player in the map centre.
    const int BlockIslandCentre = 512;
    // Tanviir level.dat spawn height.
    const int TanviirSpawnY = 108;

    public static Vector3 StartPoint(InfiniteVoxelWorld world) {
        if(InfiniteWorldSave.CurrentWorldType == InfiniteWorldSave.WorldType.Relativity) return Vector3.zero;
        int sx = 0, sz = 0;
        if(InfiniteWorldSave.CurrentWorldType == InfiniteWorldSave.WorldType.BlockIsland) sx = sz = BlockIslandCentre;
        if(InfiniteWorldSave.CurrentWorldType == InfiniteWorldSave.WorldType.Tanviir && TanviirNativeWorld.Available) {
            // Tanviir: the Minecraft spawn (level.dat y 108) maps to x/z 0; stand on the first free floor from there
            // instead of on the roof of the tower above it.
            for(int y = TanviirSpawnY; y < 254; y++) {
                // Same clearance as the generic start (top block + 3): floor at y-1, three free blocks above.
                ushort below, a0, a1, a2; byte m;
                if(TanviirNativeWorld.Raw(sx, y - 1, sz, out below, out m) && below != 0 && TanviirNativeWorld.Raw(sx, y, sz, out a0, out m) && a0 == 0
                    && TanviirNativeWorld.Raw(sx, y + 1, sz, out a1, out m) && a1 == 0 && TanviirNativeWorld.Raw(sx, y + 2, sz, out a2, out m) && a2 == 0)
                    return new Vector3(sx + .5f, y + 2, sz + .5f);
            }
        }
        int sy = world != null && world.generator != null ? world.generator.SurfaceY(sx, sz) + 3 : 64;
        return new Vector3(sx + .5f, sy, sz + .5f);
    }
}
