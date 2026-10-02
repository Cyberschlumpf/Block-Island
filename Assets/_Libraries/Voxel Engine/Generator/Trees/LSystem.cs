using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class LSystem {


	private struct Trans {
		public Vector3 pos;
		public Quaternion rot;
		
		public Trans(Vector3 pos, Quaternion rot) {
			this.pos = pos;
			this.rot = rot;
		}
		
	}
	
	private readonly Stack<Trans> stack = new Stack<Trans> ();

	private Map map;
	private Block wood;
	private Block leaf;

	public LSystem(Map map, Block wood, Block leaf) {
		this.map = map;
		this.wood = wood;
		this.leaf = leaf;
	}
	
	
	public void Generate (Vector3i pos, string axiom, Dictionary<char, string> ruleSet, Dictionary<char, float> probabilities, int iterations, int angle) {
		stack.Clear();
		
		for (int i = 0; i < iterations; i++) {
			StringBuilder temp = new StringBuilder();
			
			foreach(char c in axiom) {
				if (ruleSet.ContainsKey (c) && probabilities [c] > Random.value)
					temp.Append( ruleSet[c] );
				else
					temp.Append( c );
			}
			
			axiom = temp.ToString();
		}
		
		Trans trans = new Trans(pos, Quaternion.identity);
		angle += Random.Range (-4, 4);
		
		foreach (char c in axiom) {
			if(c == '1') {
				GenerateWood(map, trans);
				trans.pos += trans.rot * Vector3.up;
			}
			if(c == '2') {
				GenerateLeaf(map, trans);
				trans.pos += trans.rot * Vector3.up;
			}
			if(c == 'F') {
				GenerateWoodAndLeaf(map, trans);
				trans.pos += trans.rot * Vector3.up;
			}
			if(c == '-') { // empty step
				trans.pos += trans.rot * Vector3.up;
			}

			
			if(c == '[') {
				stack.Push( trans );
			}
			if(c == ']') {
				trans = stack.Pop();
			}
			
			if(c == 'Z') {
				Quaternion rot = Quaternion.AngleAxis (angle, Vector3.forward);
				trans.rot *= rot;
			}
			if(c == 'z') {
				Quaternion rot = Quaternion.AngleAxis (angle, -Vector3.forward);
				trans.rot *= rot;
			}
			
			if(c == 'Y') {
				Quaternion rot = Quaternion.AngleAxis (angle, Vector3.up);
				trans.rot *= rot;
			}
			if(c == 'y') {
				Quaternion rot = Quaternion.AngleAxis (angle, -Vector3.up);
				trans.rot *= rot;
			}
			
			if(c == 'X') {
				Quaternion rot = Quaternion.AngleAxis (angle, Vector3.right);
				trans.rot *= rot;
			}
			if(c == 'x') {
				Quaternion rot = Quaternion.AngleAxis (angle, -Vector3.right);
				trans.rot *= rot;
			}
			
		}
	}
	
	private void GenerateWood(Map map, Trans trans) {
		Vector3i pos = Vector3i.Round( trans.pos );
		map.SetBlock (pos, wood);
        map.SetBlock( pos + Vector3i.right,   wood );
        map.SetBlock( pos - Vector3i.right,   wood );
        map.SetBlock( pos + Vector3i.forward, wood );
        map.SetBlock( pos - Vector3i.forward, wood );
	}
	
	private void GenerateLeaf(Map map, Trans trans) {
		Vector3i pos = Vector3i.Round( trans.pos );
		foreach (Vector3i dir in Vector3i.directions) {
            map.SetBlock( pos + dir + Vector3i.right, leaf );
            map.SetBlock( pos + dir - Vector3i.right, leaf );
            map.SetBlock( pos + dir + Vector3i.forward, leaf );
            map.SetBlock( pos + dir - Vector3i.forward, leaf );
		}
	}
	
	private void GenerateWoodAndLeaf(Map map, Trans trans) {
		GenerateWood(map, trans);
		if(stack.Count > 1) GenerateLeaf(map, trans);
	}

}
