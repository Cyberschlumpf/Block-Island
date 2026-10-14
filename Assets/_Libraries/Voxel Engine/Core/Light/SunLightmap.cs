using UnityEngine;
using System.Collections;

[AddComponentMenu("VoxelEngine/SunLightmap")]
public class SunLightmap : MonoBehaviour, ILightmap {

    private static SunLightmap _instance;
    public static SunLightmap instance {
        get {
            return _instance;
        }
    }

    public readonly SunRayMap raymap = new SunRayMap(); // direct light
    public readonly CommonLightmap lightmap = new CommonLightmap(); // indirect light


    void Awake() {
        _instance = this;
    }

    
	public bool SetMaxLight(int light, Vector3i pos) {
        if(!WorldPosition.Check( pos )) return false;

        if(!raymap.IsRay( pos )) {
            int chunkIndex, localIndex;
            WorldPosition.ToChunkAndLocalIndex( pos, out chunkIndex, out localIndex );

            return lightmap.SetMaxLight( light, chunkIndex, localIndex );
        }
        return false;
	}
	
	public void SetLight(int light, Vector3i pos) {
        if(!WorldPosition.Check( pos )) return;

        int chunkIndex, localIndex;
        WorldPosition.ToChunkAndLocalIndex( pos, out chunkIndex, out localIndex );

        lightmap.SetLight( light, chunkIndex, localIndex );
	}
	
	public int GetLight(Vector3i pos) {
        if(!WorldPosition.Check( pos )) return LightComputerUtils.MAX_LIGHT;

        if(raymap.IsRay( pos )) return LightComputerUtils.MAX_LIGHT;

        int chunkIndex, localIndex;
        WorldPosition.ToChunkAndLocalIndex( pos, out chunkIndex, out localIndex );

        return lightmap.GetLight( chunkIndex, localIndex );
	}

	public int GetSourceLight(Vector3i pos) {
        return raymap.IsRay( pos ) ? LightComputerUtils.MAX_LIGHT : LightComputerUtils.MIN_LIGHT;
	}
	


}

public class SunRayMap {

    private readonly byte[] rays = new byte[Map.X_SIZE * Map.Z_SIZE];

    public bool IsRay(Vector3i pos) {
        return GetRay( pos.x, pos.z ) <= pos.y;
    }

    public void SetRay(int height, int x, int z) {
        int index = ToIndex( x, z );
        rays[index] = (byte) height;
    }

    public int GetRay(int x, int z) {
        int index = ToIndex(x, z);
        if(CheckIndex( index )) return rays[index];
        return 0;
    }

    private static bool CheckIndex(int index) { 
        return index >= 0 && index < Map.X_SIZE * Map.Z_SIZE;
    }

    private static int ToIndex(int x, int z) {
        return z * Map.X_SIZE + x;
    }

}
