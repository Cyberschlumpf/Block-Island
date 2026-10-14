using UnityEngine;
using System.Collections.Generic;

public class RedTree : VoxelTree {

	public Block wood;
	public Block leaf;

	
	public override void Generate (Map map, Vector3i pos) {
		Dictionary<char, float> probs = new Dictionary<char, float> ();
		probs.Add ('A', 1.0f);
		probs.Add ('B', 0.8f);
		
		Dictionary<char, string> rules = new Dictionary<char, string> ();
		rules.Add ('A', "YY[x 1F 1F AA]");
		
		string axiom = "1111 AAAA";

		LSystem lsystem = new LSystem(map, wood, leaf);
		lsystem.Generate (pos, axiom, rules, probs, 4, 40);
	}
	
}
