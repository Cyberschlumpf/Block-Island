using UnityEngine;
using System.Collections.Generic;

public class BirchTree : VoxelTree {

	public Block wood;
	public Block leaf;

	
	public override void Generate (Map map, Vector3i pos) {
		Dictionary<char, float> probs = new Dictionary<char, float> ();
		probs.Add ('A', 1.0f);
		probs.Add ('B', 0.8f);
		
		Dictionary<char, string> rules = new Dictionary<char, string> ();
		rules.Add ('A', "111B");
		rules.Add ('B', "yy[XXX-2-2-2-2-2]BB");
		
		string axiom = "11111AAA";

		LSystem lsystem = new LSystem(map, wood, leaf);
		lsystem.Generate (pos, axiom, rules, probs, 4, 40);
	}
	
}