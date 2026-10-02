using UnityEngine;
using System.Collections.Generic;

public class MapLightComputer {

    private const int MIN_LIGHT = LightComputerUtils.MIN_LIGHT;
    private const int MAX_LIGHT = LightComputerUtils.MAX_LIGHT;

    public static void ComputeSunLight(SunLightmap lightmap, Map map) {
        for(int x=0; x < Map.X_SIZE; x++) {
            for(int z=0; z < Map.Z_SIZE; z++) {
                SunLightComputer.ComputeRayAtPosition( lightmap, map, x, z );
            }
        }

        List<Vector3i> scatterList = LightComputerUtils.scatterList;
        scatterList.Clear();
        for(int x = -1; x <= Map.X_SIZE; x++) {
            for(int z = -1; z <= Map.Z_SIZE; z++) {
                int minY = lightmap.raymap.GetRay( x, z );
                int maxY = ComputeMaxY( x, z, lightmap.raymap );

                for(int y=minY; y <= maxY; y++) {
                    Vector3i pos = new Vector3i( x, y, z );
                    if(lightmap.GetLight( pos ) > MIN_LIGHT) {
                        scatterList.Add( pos );
                    }
                }
            }
        }

        LightComputerUtils.ScatterLight( map, lightmap, scatterList );
    }


    private static int ComputeMaxY(int x, int z, SunRayMap raymap) {
        int y0 = raymap.GetRay( x, z );
        int y1 = raymap.GetRay( x - 1, z );
        int y2 = raymap.GetRay( x + 1, z );
        int y3 = raymap.GetRay( x, z - 1 );
        int y4 = raymap.GetRay( x, z + 1 );
        
        return Mathf.Max( y0, y1, y2, y3, y4 ); // высота на которой все соседи освещены
    }


}
