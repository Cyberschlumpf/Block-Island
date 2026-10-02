using UnityEngine;
using System.Collections;

[AddComponentMenu("VoxelEngine/GlowLightmap")]
public class GlowLightmap : MonoBehaviour, ILightmap {

    private static GlowLightmap _instance;
    public static GlowLightmap instance {
        get {
            return _instance;
        }
    }

	private Map map;
    public readonly CommonLightmap lightmap = new CommonLightmap();

    void Awake() {
        _instance = this;
        map = GetComponent<Map>();
    }


    public bool SetMaxLight(int light, Vector3i pos) {
        if(!WorldPosition.Check( pos )) return false;

        int chunkIndex, localIndex;
        WorldPosition.ToChunkAndLocalIndex( pos, out chunkIndex, out localIndex );

        return lightmap.SetMaxLight( light, chunkIndex, localIndex );
	}

	public void SetLight(int light, Vector3i pos) {
        if(WorldPosition.Check( pos )) {
            int chunkIndex, localIndex;
            WorldPosition.ToChunkAndLocalIndex( pos, out chunkIndex, out localIndex );

            lightmap.SetLight( light, chunkIndex, localIndex );
        }
	}
	
	public int GetLight(Vector3i pos) {
        if(!WorldPosition.Check( pos )) return 0;

        int chunkIndex, localIndex;
        WorldPosition.ToChunkAndLocalIndex( pos, out chunkIndex, out localIndex );

        return lightmap.GetLight( chunkIndex, localIndex );
	}

	public int GetSourceLight(Vector3i pos) {
		return map.GetBlock(pos).GetGlow();
	}
	
	
}
