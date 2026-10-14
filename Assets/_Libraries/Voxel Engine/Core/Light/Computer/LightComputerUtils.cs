using UnityEngine;
using System.Collections.Generic;

public static class LightComputerUtils {

    public const int MIN_LIGHT = 1;
    public const int MAX_LIGHT = 15;

    public static readonly List<Vector3i> scatterList = new List<Vector3i>();
    public static readonly List<Vector3i> removeList = new List<Vector3i>();

    public static void ScatterLight(Map map, ILightmap lightmap, int light, Vector3i pos) {
        scatterList.Clear();
        scatterList.Add( pos );

        lightmap.SetMaxLight( light, pos );
        LightComputerUtils.ScatterLight( map, lightmap, scatterList );
    }

    public static void ScatterLight(Map map, ILightmap lightmap, List<Vector3i> scatterList) { // рассеивание
        for(int i=0; i < scatterList.Count; i++) {
            Vector3i pos = scatterList[i];
            if(pos.y < 0) continue;

            int light = lightmap.GetLight( pos );
            if(light <= MIN_LIGHT) continue;

            foreach(Vector3i dir in Vector3i.directions) {
                Vector3i nextPos = pos + dir;
                DataBlock block = map.GetBlock( nextPos );
                int newLight = light - block.GetLightStep();

                if(newLight >= MIN_LIGHT && block.IsAlpha() && lightmap.SetMaxLight( newLight, nextPos )) {
                    scatterList.Add( nextPos );
                }
                if(!block.IsEmpty()) map.MarkBlockLightAsDirty( nextPos );
            }
        }
    }


    public static void RemoveLight(Map map, ILightmap lightmap, Vector3i pos) {
        removeList.Clear();
        removeList.Add( pos );

        lightmap.SetLight( MIN_LIGHT, pos );
        RemoveLight( map, lightmap, removeList );
    }

    public static void RemoveLight(Map map, ILightmap lightmap, List<Vector3i> removeList) {
        scatterList.Clear();

        for(int i=0; i < removeList.Count; i++) {
            Vector3i pos = removeList[i];

            foreach(Vector3i dir in Vector3i.directions) {
                Vector3i nextPos = pos + dir;
                int light = lightmap.GetLight( nextPos );
                int glow = lightmap.GetSourceLight( nextPos );
                DataBlock block = map.GetBlock( nextPos );
                if(!block.IsEmpty()) map.MarkBlockLightAsDirty( nextPos );

                if(glow > MIN_LIGHT) scatterList.Add( nextPos );
                if(light <= MIN_LIGHT || !block.IsAlpha()) continue;


                if(ComputeLight( nextPos, lightmap, map ) < light) {
                    lightmap.SetLight( MIN_LIGHT, nextPos );
                    if(light > MIN_LIGHT + 1) removeList.Add( nextPos );
                } else {
                    scatterList.Add( nextPos );
                }
            }
        }

        foreach(Vector3i pos in scatterList) { // only for GlowLightmap
            int glow = lightmap.GetSourceLight( pos );
            lightmap.SetMaxLight( glow, pos );
        }

        ScatterLight( map, lightmap, scatterList );
    }



    public static int ComputeLight(Vector3i pos, ILightmap lightmap, Map map) {
        int delta = map.GetBlock( pos ).GetLightStep();
        int glow = lightmap.GetSourceLight( pos );

        int light1 = lightmap.GetLight( pos + Vector3i.right ) - delta;
        int light2 = lightmap.GetLight( pos - Vector3i.right ) - delta;
        int light3 = lightmap.GetLight( pos + Vector3i.up ) - delta;
        int light4 = lightmap.GetLight( pos - Vector3i.up ) - delta;
        int light5 = lightmap.GetLight( pos + Vector3i.forward ) - delta;
        int light6 = lightmap.GetLight( pos - Vector3i.forward ) - delta;

        return Mathf.Max( glow, light1, light2, light3, light4, light5, light6 );
    }

    /*public static Color32 GetSmoothVertexLight(CubeSide face, Vector3 vertex, Vector3i pos, Map map) {
		// pos - позиция блока 
		// vertex - локальная позиция вершины т.е. от 0 до 1
		
		//d b
		// .-------.
		//c|a      |
		// |       |
		// .-------.

        int dx = vertex.x < 0.5f ? -1 : 1;
        int dy = vertex.y < 0.5f ? -1 : 1;
        int dz = vertex.z < 0.5f ? -1 : 1;
		
		Vector3i a, b, c, d;
		if(face == CubeSide.Left || face == CubeSide.Right) { // X
			a = pos + new Vector3i(dx, 0,  0);
			b = pos + new Vector3i(dx, dy, 0);
			c = pos + new Vector3i(dx, 0,  dz);
			d = pos + new Vector3i(dx, dy, dz);
		} else 
		if(face == CubeSide.Bottom || face == CubeSide.Top) { // Y
			a = pos + new Vector3i(0,  dy, 0);
			b = pos + new Vector3i(dx, dy, 0);
			c = pos + new Vector3i(0,  dy, dz);
			d = pos + new Vector3i(dx, dy, dz);
		} else { // Z
			a = pos + new Vector3i(0,  0,  dz);
			b = pos + new Vector3i(dx, 0,  dz);
			c = pos + new Vector3i(0,  dy, dz);
			d = pos + new Vector3i(dx, dy, dz);
		}
		
		if(map.GetBlock(b).IsAlpha() || map.GetBlock(c).IsAlpha()) {
            Color32 c1 = GetBlockLight( a, map );
            Color32 c2 = GetBlockLight( b, map );
            Color32 c3 = GetBlockLight( c, map );
            Color32 c4 = GetBlockLight( d, map );

            int glow = (c1.r + c2.r + c3.r + c4.r) / 4;
            int sun = (c1.g + c2.g + c3.g + c4.g) / 4;

            Color32 color = new Color32();
            color.r = (byte) glow;
            color.g = (byte) sun;
            return color;
		} else {
			Color32 c1 = GetBlockLight( a, map );
            Color32 c2 = GetBlockLight( b, map );
            Color32 c3 = GetBlockLight( c, map );

            int glow = (c1.r + c2.r + c3.r) / 3;
            int sun = (c1.g + c2.g + c3.g) / 3;

            Color32 color = new Color32();
            color.r = (byte) glow;
            color.g = (byte) sun;
            return color;
		}
	}*/


    public static Color32 GetBlockLight(Vector3i pos) {
        //Block block = map.GetBlock(pos).block;
        //if(block is CubeBlock && block.IsAlpha()) return new Color32(); // внутри блока листьев полностью темно

        const int scaler = 255 / LightComputerUtils.MAX_LIGHT;

        int glow = GlowLightmap.instance.GetLight( pos ) * scaler;
        int sun = SunLightmap.instance.GetLight( pos ) * scaler;
        Color32 color = default( Color32 );
        color.r = (byte) glow;
        color.g = (byte) sun;
        return color;
    }

}
