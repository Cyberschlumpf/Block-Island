using UnityEngine;
using System.Collections;

public class BoxCollision {

	public static Contact GetContactBoxCharacter(Vector3 blockPos, CharacterCollider collider) {
		Vector3 pos = collider.pos;
		Vector3 bottom = pos+collider.bottom;
		Contact contact;
        if(RelativityGravityController.Active){ Vector3 topPoint=pos+collider.top; contact=GetContactBoxSegment(blockPos, bottom, topPoint); }
        else { float top=(pos+collider.top).y; contact=GetContactBoxCapsule(blockPos,bottom,top); }
		if(contact.sqrDistance < collider.radius*collider.radius) {
			contact.capsulePoint += contact.normal * collider.radius;
			return contact;
		}
		return null;
	}
	
    private static Contact GetContactBoxSegment(Vector3 blockPos, Vector3 a, Vector3 b) {
        Vector3 mn=blockPos, mx=blockPos+Vector3.one, ab=b-a; float t=0.5f;
        // Convex distance in one dimension; ternary search is stable for the tiny voxel capsule segment.
        for(int i=0;i<12;i++){ float t1=t-0.25f/(1<<i), t2=t+0.25f/(1<<i); t1=Mathf.Clamp01(t1);t2=Mathf.Clamp01(t2);
            Vector3 p1=a+ab*t1,p2=a+ab*t2; Vector3 q1=new Vector3(Mathf.Clamp(p1.x,mn.x,mx.x),Mathf.Clamp(p1.y,mn.y,mx.y),Mathf.Clamp(p1.z,mn.z,mx.z)); Vector3 q2=new Vector3(Mathf.Clamp(p2.x,mn.x,mx.x),Mathf.Clamp(p2.y,mn.y,mx.y),Mathf.Clamp(p2.z,mn.z,mx.z));
            if((p1-q1).sqrMagnitude<(p2-q2).sqrMagnitude)t=t1;else t=t2; }
        Vector3 p=a+ab*t; return GetContactBoxPoint(mn,mx,p);
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
