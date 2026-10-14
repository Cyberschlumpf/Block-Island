using UnityEngine;
using System.Collections;

public class StairIntersection {

    private static readonly Box box1 = Box.CenterSize( new Vector3( 0.5f, 0.25f, 0.5f  ), new Vector3( 1, 0.5f, 1f   ) );
    private static readonly Box box2 = Box.CenterSize( new Vector3( 0.5f, 0.75f, 0.25f ), new Vector3( 1, 0.5f, 0.5f ) );

	public static float StairRayIntersection(Vector3i pos, BlockDirection dir, Ray ray) {
		Vector3 boxMin = box1.min;
		Vector3 boxMax = box1.max;
		BoxCollision.TransformBox(ref boxMin, ref boxMax, pos, dir);
		float dis1 = BoxIntersection.BoxRayIntersection(boxMin, boxMax, ray);
		
		boxMin = box2.min;
		boxMax = box2.max;
		BoxCollision.TransformBox(ref boxMin, ref boxMax, pos, dir);
		float dis2 = BoxIntersection.BoxRayIntersection(boxMin, boxMax, ray);

		if(dis1 < 0) return dis2;
		if(dis2 < 0) return dis1;
		return Mathf.Min(dis1, dis2);
	}

}
