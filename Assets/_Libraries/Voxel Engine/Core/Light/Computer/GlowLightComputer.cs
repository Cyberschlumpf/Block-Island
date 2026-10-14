using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlowLightComputer {

    private const int MIN_LIGHT = LightComputerUtils.MIN_LIGHT;
    private const int MAX_LIGHT = LightComputerUtils.MAX_LIGHT;


    public static void AddBlock(GlowLightmap lightmap, Map map, Vector3i pos) {
		int oldLight = lightmap.GetLight(pos);

		int newLight = map.GetBlock(pos).GetGlow();
		if( map.GetBlock(pos).IsAlpha() ) newLight = Mathf.Max(newLight, oldLight);


		if( newLight > oldLight ) {
			LightComputerUtils.ScatterLight( map, lightmap, newLight, pos );
		}
		if( newLight < oldLight ) {
			LightComputerUtils.RemoveLight(map, lightmap, pos);
			lightmap.SetLight( newLight, pos );
		}
	}

	public static void RemoveBlock(GlowLightmap lightmap, Map map, Vector3i pos) {
		int oldLight = lightmap.GetLight(pos);
        int newLight = LightComputerUtils.ComputeLight( pos, lightmap, map );

		if( newLight > oldLight ) {
			LightComputerUtils.ScatterLight( map, lightmap, newLight, pos );
		}
		if( newLight < oldLight ) {
			LightComputerUtils.RemoveLight(map, lightmap, pos);
		}
	}
	
	
}


