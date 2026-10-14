using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SunLightComputer {

    private const int MIN_LIGHT = LightComputerUtils.MIN_LIGHT;
    private const int MAX_LIGHT = LightComputerUtils.MAX_LIGHT;


	public static int ComputeRayAtPosition(SunLightmap lightmap, Map map, int x, int z) {
		int y = map.GetMaxY(x, z);
		for(; y>=0; y--) {
			DataBlock block = map.GetBlock( x, y, z );
			int delta = block.GetLightStep();
			if(delta >= 2) {
                lightmap.raymap.SetRay( y + 1, x, z );
                return y + 1;
			}
		}
        return 0;
	}


	public static void AddBlock(SunLightmap lightmap, Map map, Vector3i pos) {
		int oldSun = lightmap.raymap.GetRay(pos.x, pos.z);
        int newSun = ComputeRayAtPosition( lightmap, map, pos.x, pos.z );

		if(oldSun == newSun) {
			LightComputerUtils.RemoveLight(map, lightmap, pos);
		} else { // oldSun < newSun // pos.y == newSun-1
			// луч света поднялся
			List<Vector3i> removeList = LightComputerUtils.removeList;
			removeList.Clear();
			for (int ty = oldSun; ty < newSun; ty++) {
				pos.y = ty;
				lightmap.SetLight(MIN_LIGHT, pos);
				removeList.Add( pos );
			}
			LightComputerUtils.RemoveLight(map, lightmap, removeList);
		}

	}
	
	public static void RemoveBlock(SunLightmap lightmap, Map map, Vector3i pos) {
		int oldSun = lightmap.raymap.GetRay(pos.x, pos.z);
        int newSun = ComputeRayAtPosition( lightmap, map, pos.x, pos.z );

		if(oldSun == newSun) {
            UpdateLight( map, lightmap, pos );
		} else { // newSun < oldSun
			// луч света опустился
			List<Vector3i> scatterList = LightComputerUtils.scatterList;
			scatterList.Clear();
			for (int ty = newSun; ty < oldSun; ty++) {
				pos.y = ty;
				lightmap.SetLight(MIN_LIGHT, pos);
				scatterList.Add( pos );
			}
			LightComputerUtils.ScatterLight(map, lightmap, scatterList);
		}
	}


    private static void UpdateLight(Map map, SunLightmap lightmap, Vector3i pos) {
		int light = LightComputerUtils.ComputeLight(pos, lightmap, map);
		LightComputerUtils.ScatterLight( map, lightmap, light, pos );
	}

	
}
