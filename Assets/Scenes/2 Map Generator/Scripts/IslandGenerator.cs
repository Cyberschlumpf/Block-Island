using UnityEngine;
using System.Collections.Generic;

#pragma warning disable 0414 // private field assigned but not used.
public class IslandGenerator : MonoBehaviour {

	public const float WATER_LEVEL = 0.5f;
	public const float SAND_LEVEL = 0.52f;
	public const float DIRT_LEVEL = 0.83f;
	public const float ROCK_LEVEL = 0.9f;

	public enum BlockType {
		AIR, WATER, SAND, DIRT, ROCK
	}

	public int sizeX = 1024;
	public int sizeZ = 1024;

	// 0.9.9.8a: UI/launch flag only. Runtime generation is handled by InfiniteTerrainGenerator.
	public bool skyIslands = false;

	internal FastPerlinNoise2D noise;
	public AnimationCurve curve;

	private Block water;
	private Block sand;
	private Block dirt;
	private Block grass;
	private Block rock;

	public Block[] plants;
	public float[] plantProbabilities;

	public VoxelTree[] trees;


	void Start() {
        // 0.9.9.8b: SKY ISLANDS is a first-class main-menu entry below Block Island.
        if(InfiniteWorldLaunchConfig.openSkyGenerator) { skyIslands=true; InfiniteWorldLaunchConfig.openSkyGenerator=false; }
        // Stage 6.8.3: do not force a 512x512 finite world at runtime.
        // The Island Generator remains a preview/seed selector only.
		noise = new FastPerlinNoise2D(1f/20, 5);

		BlockSet blockSet = BlockSet.instance;

		water = blockSet.FindBlock("Water");
		sand = blockSet.FindBlock("Sand");
		dirt = blockSet.FindBlock("Dirt");
		grass = blockSet.FindBlock("Grass");
		rock = blockSet.FindBlock("Rock");
	}


	public void Generate(Map map) {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game") {
            Debug.LogError("LEGACY GENERATOR BLOCKED: IslandGenerator.Generate() was called in Game. InfiniteTerrainGenerator is the only allowed world source.");
            return;
        }
		//GeneratorUtils.Clear( map );

		float startTime = Time.realtimeSinceStartup;
		{
			GenerateTerrain( map );
			GenerateTrees( map );
			MapLightComputer.ComputeSunLight( SunLightmap.instance, map );
			GeneratePlants( map );
		}
		float generationTime = Time.realtimeSinceStartup - startTime;

		/*startTime = Time.realtimeSinceStartup;
		{
			GeneratorUtils.Build( map );
		}
		float buildingTime = Time.realtimeSinceStartup - startTime;*/


		Debug.Log( "Generation time "+generationTime );
		//Debug.Log( "Building time "+buildingTime );
		//Debug.Log( "Total time "+(generationTime+buildingTime) );
	}


	private void GenerateTerrain(Map map) {
		// Keep generated terrain inside the 64-block vertical world.
		float scale = Map.Y_SIZE - 4f;
		for(int x = 0; x<sizeX; x++) {
			for(int z = 0; z<sizeZ; z++) {
				int maxY = (int) (GetNoise(x, z) * scale);
				
				for(int y=0; y<maxY; y++) {
					Vector3i pos = new Vector3i(x, y, z);
                    map.SetBlock( pos, GetBlock( y / scale ) );
				}
				
				for(int y=maxY; y<WATER_LEVEL*scale; y++) {
					Vector3i pos = new Vector3i(x, y, z);
                    map.SetBlock( pos, water );
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


	private Block GetBlock(float y) {
		if(y < SAND_LEVEL) return sand;
		if(y < DIRT_LEVEL) return dirt;
		if(y < ROCK_LEVEL) return rock;
		return null;
	}

	public float GetNoise(float x, float z) {
		float v = noise.Noise (x, z);
		v *= 1f/0.8f/2f;
		v += 0.5f;
		return curve.Evaluate(v);
	}


}
