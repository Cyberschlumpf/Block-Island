using UnityEngine;
using System.Collections;

public class StairCollision {
	
	private static readonly Box box1 = Box.CenterSize( new Vector3(0.5f, 0.25f, 0.5f),  new Vector3(1, 0.5f, 1f)   );
	private static readonly Box box2 = Box.CenterSize( new Vector3(0.5f, 0.75f, 0.25f), new Vector3(1, 0.5f, 0.5f) );
	
	
	public static Contact GetContactStairCharacter(Vector3 blockPos, BlockDirection dir, CharacterCollider collider) {
		Vector3 pos = collider.pos;
		Vector3 bottom = pos+collider.bottom;
		float top = (pos+collider.top).y;
		
		Contact contact = GetContactStairCapsule(blockPos, dir, bottom, top);
		if(contact.sqrDistance < collider.radius*collider.radius) {
			contact.capsulePoint += contact.normal * collider.radius;
			return contact;
		}
		return null;
	}
	
	private static Contact GetContactStairCapsule(Vector3 blockPos, BlockDirection dir, Vector3 bottom, float top) {
		Vector3 boxMin = box1.min;
		Vector3 boxMax = box1.max;
		BoxCollision.TransformBox(ref boxMin, ref boxMax, blockPos, dir);
		Contact a = BoxCollision.GetContactBoxCapsule(boxMin, boxMax, bottom, top);
		
		boxMin = box2.min;
		boxMax = box2.max;
		BoxCollision.TransformBox(ref boxMin, ref boxMax, blockPos, dir);
		Contact b = BoxCollision.GetContactBoxCapsule(boxMin, boxMax, bottom, top);
		
		return Contact.Min(a, b); // самое глубокое проникновение в капсулу
	}
	
	
}
