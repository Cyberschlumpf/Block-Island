using UnityEngine;
using System.Collections;

public static class MapCharacterCollision {
	
	
	public static void Collision(Map map, CharacterCollider character) {
		for(int i=0; i<3; i++) {
			Vector3i blockPos = Vector3i.zero;
			Contact contact = GetClosestContact(map, character, ref blockPos);
			if(contact == null) continue;
			
            Vector3 normal = contact.normal;
            if(!RelativityGravityController.Active){ float footY=character.pos.y; float deepY=contact.blockPoint.y-footY; if(deepY<=0.55f) contact.capsulePoint=GetFloorY(character,contact.blockPoint); }
			
			character.pos += contact.delta;
			character.OnCollision(contact.capsulePoint, normal, blockPos);
		}
	}
	
	private static Contact GetClosestContact(Map map, CharacterCollider character, ref Vector3i blockPos) {
        Vector3 a=character.pos+character.bottom,b=character.pos+character.top; float r=character.radius;
        int x1=Mathf.FloorToInt(Mathf.Min(a.x,b.x)-r), y1=Mathf.FloorToInt(Mathf.Min(a.y,b.y)-r), z1=Mathf.FloorToInt(Mathf.Min(a.z,b.z)-r);
        int x2=Mathf.CeilToInt(Mathf.Max(a.x,b.x)+r), y2=Mathf.CeilToInt(Mathf.Max(a.y,b.y)+r), z2=Mathf.CeilToInt(Mathf.Max(a.z,b.z)+r);
		
		Contact contact = null;
		for(int x=x1; x<=x2; x++) {
			for(int y=y1; y<=y2; y++) {
				for(int z=z1; z<=z2; z++) {
					Vector3i pos = new Vector3i(x, y, z);
					Contact newContact = BlockCollision.GetContactBlockCharacter(map.GetBlock(pos), pos, character);
					if(newContact != null && newContact.delta.magnitude > float.Epsilon) {
						if(contact == null) {
							contact = newContact;
						} else {
							contact = Contact.Max( contact, newContact );
						}
						blockPos = pos;
					}
				}
			}
		}
		
		return contact;
	}
	
	private static Vector3 GetFloorY(CharacterCollider character, Vector3 point) {
		Vector3 pos = character.pos;
		Vector3 center = pos + character.bottom;
		float rad = character.radius;
		float dx = center.x-point.x;
		float dz = center.z-point.z;
		float sqrDis = dx*dx + dz*dz;
		float y = Mathf.Sqrt( rad*rad - sqrDis );
		return new Vector3(point.x, center.y-y, point.z);
	}
	
}

