using UnityEngine;
using System.Collections.Generic;

public class LSystemTree : VoxelTree {

	public Block wood;
	public Block leaf;
	
	public int seed = 0;
	public float probabilityA = 1.0f, probabilityB = 0.8f;
	public string rulesA = "[FFBFA]////[BFFFA]////[FBFFA]";
	public string rulesB = "[FFFA]////[FFFA]////[FFFA]";
	public string axiom = "FFFFFFA";


	public override void Generate(Map map, Vector3i pos) {
		int oldSeed = Random.seed;
		Random.seed = seed;

		Dictionary<char, float> probs = new Dictionary<char, float> ();
		probs.Add ('A', probabilityA);
		probs.Add ('B', probabilityB);
		
		Dictionary<char, string> rules = new Dictionary<char, string> ();
		rules.Add ('A', rulesA);
		rules.Add ('B', rulesB);

		LSystem lsystem = new LSystem(map, wood, leaf);
		lsystem.Generate(pos, axiom, rules, probs, 4, 40);

		Random.seed = oldSeed;
	}
	
	
}
