using UnityEngine;
using System.Collections;

public class BoxCollision {

	public static Contact GetContactBoxCharacter(Vector3 blockPos, CharacterCollider collider) {
		Vector3 pos = collider.pos;
		Vector3 bottom = pos+collider.bottom;
		float top = (pos+collider.top).y;
		
		Contact contact = GetContactBoxCapsule(blockPos, bottom, top);
		if(contact.sqrDistance < collider.radius*collider.radius) {
			contact.capsulePoint += contact.normal * collider.radius;
			return contact;
		}
		return null;
	}
	
	private static Contact GetContactBoxCapsule(Vector3 blockPos, Vector3 bottom, float top) {
		Vector3 boxMin = blockPos;
		Vector3 boxMax = blockPos + Vector3.one;
		return GetContactBoxCapsule(boxMin, boxMax, bottom, top);
	}
	
	public static Contact GetContactBoxCapsule(Vector3 boxMin, Vector3 boxMax, Vector3 bottom, float top) {
		//           capsule
		//  block   |------|
		// -------  . point|
		// |     |  |      |
		// |     |  |      |
		// -------  |      |
		//          |------|
		Vector3 point = bottom;
		point.y = Mathf.Clamp(boxMax.y, bottom.y, top); // upper y coord of block
		return GetContactBoxPoint(boxMin, boxMax, point);
	}
	
	private static Contact GetContactBoxPoint(Vector3 boxMin, Vector3 boxMax, Vector3 point) {
		Vector3 closest = point;
		for(int i=0; i<3; i++) {
			if (closest[i] > boxMax[i]) closest[i] = boxMax[i];
        	if (closest[i] < boxMin[i]) closest[i] = boxMin[i];
		}
		return new Contact(closest, point);
	}

	public static void TransformBox(ref Vector3 boxMin, ref Vector3 boxMax, Vector3 blockPos, BlockDirection dir) {
        boxMin = BlockDirectionUtils.TransformBlockVertex( boxMin, dir ) + blockPos;
        boxMax = BlockDirectionUtils.TransformBlockVertex( boxMax, dir ) + blockPos;
		
		if(boxMin.x > boxMax.x) {
			float tmp = boxMin.x;
			boxMin.x = boxMax.x;
			boxMax.x = tmp;
		}
		if(boxMin.z > boxMax.z) {
			float tmp = boxMin.z;
			boxMin.z = boxMax.z;
			boxMax.z = tmp;
		}
	}
	
}
