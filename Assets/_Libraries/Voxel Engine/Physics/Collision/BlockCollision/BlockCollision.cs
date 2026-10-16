using UnityEngine;
using System.Collections;

public class BlockCollision {

	
	public static Contact GetContactBlockCharacter(DataBlock block, Vector3 blockPos, CharacterCollider collider) {
		if(!block.IsSolid()) return null;
		if(block.block is FunctionBlock && FunctionBlocks.IsOpen(new Vector3i(Mathf.FloorToInt(blockPos.x + .01f), Mathf.FloorToInt(blockPos.y + .01f), Mathf.FloorToInt(blockPos.z + .01f)))) return null;   // an open automatic door
		
		if(block.block is StairBlock) {
			return StairCollision.GetContactStairCharacter(blockPos, block.direction, collider);
		}
		if(block.block is MeshBlock) {
			return MeshCollision.GetContactMeshCharacter(block, blockPos, collider);
		}
		return BoxCollision.GetContactBoxCharacter(blockPos, collider);
	}
	
}