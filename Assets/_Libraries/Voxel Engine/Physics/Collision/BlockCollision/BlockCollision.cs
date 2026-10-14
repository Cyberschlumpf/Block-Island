using UnityEngine;
using System.Collections;

public class BlockCollision {

	
	public static Contact GetContactBlockCharacter(DataBlock block, Vector3 blockPos, CharacterCollider collider) {
		if(!block.IsSolid()) return null;
		
		if(block.block is StairBlock) {
			return StairCollision.GetContactStairCharacter(blockPos, block.direction, collider);
		}
		if(block.block is MeshBlock) {
			return MeshCollision.GetContactMeshCharacter(block, blockPos, collider);
		}
		return BoxCollision.GetContactBoxCharacter(blockPos, collider);
	}
	
}