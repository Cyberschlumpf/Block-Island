using UnityEngine;
using System.Collections;

public class MeshCollision {
	
	public static Contact GetContactMeshCharacter(DataBlock block, Vector3 blockPos, CharacterCollider collider) {
		Vector3 pos = collider.pos;
		Vector3 bottom = pos+collider.bottom;
		float top = (pos+collider.top).y;
		
		Contact contact = GetContactMeshCharacter(block, blockPos, bottom, top);
		if(contact.sqrDistance < collider.radius*collider.radius) {
			contact.capsulePoint += contact.normal * collider.radius;
			return contact;
		}
		return null;
	}
	
	private static Contact GetContactMeshCharacter(DataBlock block, Vector3 blockPos, Vector3 bottom, float top) {
		MeshBlock meshBlock = (MeshBlock)block.block;
		BlockDirection dir = block.direction;
		
		Vector3 boxMin = meshBlock.min;
		Vector3 boxMax = meshBlock.max;
		BoxCollision.TransformBox(ref boxMin, ref boxMax, blockPos, dir);
		return BoxCollision.GetContactBoxCapsule(boxMin, boxMax, bottom, top);
	}
	
}
