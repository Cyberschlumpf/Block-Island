using UnityEngine;
using System.Collections.Generic;

public class PineTree : VoxelTree {

	public Block wood;
	public Block leaf;

	
	public override void Generate (Map map, Vector3i pos) {
		Dictionary<char, float> probs = new Dictionary<char, float> ();
		probs.Add ('A', 1.0f);
		probs.Add ('B', 0.8f);
		
		Dictionary<char, string> rules = new Dictionary<char, string> ();
		rules.Add ('A', "y[B]AAA");
		rules.Add ('B', "xxx22222");
		
		string axiom = "111111A 1111A 1111A 1111A";

		LSystem lsystem = new LSystem(map, wood, leaf);
		lsystem.Generate (pos, axiom, rules, probs, 4, 40);
	}
	
}