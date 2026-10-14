using UnityEngine;
using System.Collections.Generic;

public class SakuraTree : VoxelTree {

	public Block wood;
	public Block leaf;

	
	public override void Generate (Map map, Vector3i pos) {
		Dictionary<char, float> probs = new Dictionary<char, float> ();
		probs.Add ('A', 0.8f);
		probs.Add ('B', 0.8f);
		
		Dictionary<char, string> rules = new Dictionary<char, string> ();
		rules.Add ('A', "YY[B]");
		rules.Add ('B', "yxx22222B");
		
		string axiom = "11111111 AAAAA";

		LSystem lsystem = new LSystem(map, wood, leaf);
		lsystem.Generate (pos, axiom, rules, probs, 4, 40);
	}
	
	
}
