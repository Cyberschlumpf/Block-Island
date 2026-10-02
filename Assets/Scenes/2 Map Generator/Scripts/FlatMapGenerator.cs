using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable 0414 // private field assigned but not used.
public class FlatMapGenerator : MonoBehaviour {
	
	public int sizeX = 128, sizeZ = 128;

	public Block[] plants;
	public float[] plantProbabilities;

	public VoxelTree[] trees;

    private readonly PerlinNoise3D noise3d = new PerlinNoise3D(1 / 20f);

	private Block dirt;
	private Block grass;

    void Start() {
        Map map = Map.instance;
        SunLightmap sunmap = SunLightmap.instance;
        BlockSet blockSet = BlockSet.instance;

        dirt = blockSet.FindBlock<GroundBlock>("Dirt");
        grass = blockSet.FindBlock<GroundBlock>("Grass");
		

		float startTime = Time.realtimeSinceStartup;
        float lightTime = 0;
		{
			GenerateTerrain(map);
			GenerateTrees(map);
            float t1 = Time.realtimeSinceStartup;
            MapLightComputer.ComputeSunLight( sunmap, map );
            lightTime = Time.realtimeSinceStartup - t1;
			GeneratePlants(map);
		}
		float generationTime = Time.realtimeSinceStartup - startTime;
		
        // if without RuntimeBuilder 
		/*startTime = Time.realtimeSinceStartup;
		{
            map.Build();
		}
		float buildingTime = Time.realtimeSinceStartup - startTime;
        */
		Debug.Log( "Generation Time "+generationTime+"   (Light "+lightTime+")" );
	    //Debug.Log( "Building Time "+buildingTime );
		//Debug.Log( "Total Time "+(generationTime + buildingTime) );
	}
	
	private void GenerateTerrain(Map map) {
		for(int x=0; x<sizeX; x++) {
			for(int z=0; z<sizeZ; z++) {
				for(int y=0; y<50; y++) {
                    if (noise3d.Noise(x, y, z) > 0.3f) {
                        map.SetBlock( x, y, z, dirt );
                    }
				}
			}
		}
	}
	
	private void GenerateTrees(Map map) {
		for(int x=0; x<sizeX; x++) {
			for(int z=0; z<sizeZ; z++) {
				if( Random.Range(0, 700) == 0 ) {
					int y = map.GetMaxY(x, z);
					Vector3i pos = new Vector3i(x, y, z);
					DataBlock block = map.GetBlock( pos );
					
					if( block.block == dirt ) {
						VoxelTree tree = trees[ Random.Range(0, trees.Length) ];
						tree.Generate( map, pos+Vector3i.up );
					}
				}
			}
		}
	}
	
	private void GeneratePlants(Map map) {
        SunLightmap sunmap = SunLightmap.instance;

		for(int x=0; x<sizeX; x++) {
			for(int z=0; z<sizeZ; z++) {
				int maxY = map.GetMaxY(x, z);
				for(int y=0; y<=maxY; y++) {
					Vector3i pos = new Vector3i(x, y, z);
					if( !map.GetBlock( pos+Vector3i.up ).IsEmpty() ) continue;
					
					DataBlock block = map.GetBlock( pos );
                    int light = sunmap.GetLight( pos + Vector3i.up );
					
					if(block.block == dirt && light >= 5) {
                        map.SetBlock( pos, grass );

						Block plant = GetRandomPlant();
						if(plant != null) SetPlant(map, plant, pos+Vector3i.up);
					}
				}
			}
		}
	}

	private Block GetRandomPlant() {
		int rnd = Random.Range(0, 100);
		float sum = 0;
		for(int i=0; i<plants.Length; i++) {
			sum += plantProbabilities[i];
			if(rnd < sum) return plants[i];
		}
		return null;
	}
	
	private void SetPlant(Map map, Block block, Vector3i pos) {
		DataBlock oldBlock = map.GetBlock(pos);
		oldBlock.block = block;
        map.SetBlock( pos, oldBlock );
	}
	
}
