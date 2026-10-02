using UnityEngine;
using System.Collections;

public class BoxIntersection {

	public static float BoxRayIntersection(Vector3 blockPos, Ray ray) {
		return BoxRayIntersection( blockPos, blockPos+Vector3.one, ray );
	}

    public static float BoxRayIntersection(Box box, Ray ray) {
        return BoxRayIntersection(box.min, box.max, ray);
    }
	
	public static float BoxRayIntersection(Vector3 boxMin, Vector3 boxMax, Ray ray) {
		Vector3 origin = ray.origin;
		Vector3 dir = ray.direction;
		Vector3 invDir = new Vector3(1f/dir.x, 1f/dir.y, 1f/dir.z);
		Vector3 tMin = Vector3.Scale(boxMin - origin, invDir);
		Vector3 tMax = Vector3.Scale(boxMax - origin, invDir);
		
		MinMax(ref tMin.x, ref tMax.x);
		MinMax(ref tMin.y, ref tMax.y);
		MinMax(ref tMin.z, ref tMax.z);
		
		float near = Mathf.Max(tMin.x, tMin.y, tMin.z);
		float far  = Mathf.Min(tMax.x, tMax.y, tMax.z);
		
		
		if (near > far) return -1;
		if (near > 0.0f) return near;
		if (far > 0.0f)  return far;
		return -1;
	}
	
	private static bool MinMax(ref float min, ref float max) {
		if(min > max) {
			float tmp = min;
			min = max;
			max = tmp;
			return true;
		}
		return false;
	}

}
