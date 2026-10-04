using UnityEngine;
using System.Collections.Generic;
using System;

public static class MapRayIntersection {
	
	
	public static bool Raycast(Map map, Ray ray, float distance, out Vector3i pos, out Vector3i prevPos) {
        // Singularitaet may visually roll the world while the camera stays upright.
        // Convert the camera ray back into the voxel world's logical coordinates.
        ray = SingularityViewAxisRoll.ToLogicalRay(ray);
		Vector3 start = ray.origin;
		Vector3 dir = ray.direction;
		pos = prevPos = Vector3i.Floor( start );


        Vector3i step = Sign( dir );

		Vector3i nearPlane;
		nearPlane.x = dir.x >= 0 ? pos.x+1 : pos.x;
		nearPlane.y = dir.y >= 0 ? pos.y+1 : pos.y;
		nearPlane.z = dir.z >= 0 ? pos.z+1 : pos.z;
		
		Vector3 max = Div( nearPlane-start, dir );
		Vector3 delta = new Vector3(1f/Mathf.Abs(dir.x), 1f/Mathf.Abs(dir.y), 1f/Mathf.Abs(dir.z));

		while( true ) {
			float dis = Mathf.Min( max.x, max.y, max.z );
			if( dis > distance ) break;
			prevPos = pos;
			
			if(dis == max.x) {
				max.x += delta.x;
        		pos.x += step.x;
			} else if(dis == max.y) {
				max.y += delta.y;
        		pos.y += step.y;
			} else {
				max.z += delta.z;
        		pos.z += step.z;
			}
			
			DataBlock block = map.GetBlock(pos);
			if( !block.IsFluid() && !block.IsEmpty() ) return true;
		}
		
		return false;
	}
	
	
	
	
	public static bool Raycast(Map map, Ray ray, ref float distance) {
        // Same visual-world -> logical-voxel conversion for precise block intersection.
        ray = SingularityViewAxisRoll.ToLogicalRay(ray);
		Vector3 start = ray.origin;
		Vector3 dir = ray.direction;
		Vector3i pos = Vector3i.Floor( start );

        Vector3i step = Sign( dir );

		Vector3i nearPlane;
		nearPlane.x = dir.x >= 0 ? pos.x+1 : pos.x;
		nearPlane.y = dir.y >= 0 ? pos.y+1 : pos.y;
		nearPlane.z = dir.z >= 0 ? pos.z+1 : pos.z;
		
		Vector3 max = Div( nearPlane-start, dir );
		Vector3 delta = new Vector3(1f/Mathf.Abs(dir.x), 1f/Mathf.Abs(dir.y), 1f/Mathf.Abs(dir.z));
		
		while( true ) {
			float dis = Mathf.Min( max.x, max.y, max.z );
			if( dis > distance ) break;

			if(max.x == dis) {
				max.x += delta.x;
        		pos.x += step.x;
			} else if(max.y == dis) {
				max.y += delta.y;
        		pos.y += step.y;
			} else {
				max.z += delta.z;
        		pos.z += step.z;
			}

            dis = BlockIntersection.BlockRayIntersection( map.GetBlock( pos ), pos, ray, dis );
            if(dis > 0) {
                distance = dis;
                return true;
            }
		}
		
		return false;
	}

	
	private static Vector3 Div(Vector3 a, Vector3 b) {
		return new Vector3(a.x/b.x, a.y/b.y, a.z/b.z);
	}

    private static Vector3i Sign(Vector3 v) {
        Vector3i sign;
        sign.x = v.x > 0 ? 1 : -1;
        sign.y = v.y > 0 ? 1 : -1;
        sign.z = v.z > 0 ? 1 : -1;
        return sign;
    }

	
}
