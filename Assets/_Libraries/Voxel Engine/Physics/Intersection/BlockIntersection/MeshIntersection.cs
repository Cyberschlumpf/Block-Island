using UnityEngine;
using System.Collections;

public class MeshIntersection {

	public static float MeshRayIntersection(DataBlock block, Vector3i pos, Ray ray) {
		MeshBlock mesh = (MeshBlock) block.block;
		return Intersection(mesh.vertices, mesh.indices, pos, block.direction, ray);
	}
	
	private static float Intersection(Vector3[] vertices, int[] indices, Vector3i pos, BlockDirection dir, Ray ray) {
		float minDistance = float.MaxValue;
		for(int i=0; i<indices.Length; ) {
			int i1 = indices[i++];
			int i2 = indices[i++];
			int i3 = indices[i++];

            Vector3 a = BlockDirectionUtils.TransformBlockVertex( vertices[i1], dir ) + pos;
            Vector3 b = BlockDirectionUtils.TransformBlockVertex( vertices[i2], dir ) + pos;
            Vector3 c = BlockDirectionUtils.TransformBlockVertex( vertices[i3], dir ) + pos;
			
			float dis = TriangleRayIntersection(a, b, c, ray);
			if(dis > 0) minDistance = Mathf.Min(minDistance, dis);
		}

		if(minDistance != float.MaxValue) return minDistance;
		return -1;
	}
	
	private static float TriangleRayIntersection(Vector3 a, Vector3 b, Vector3 c, Ray ray) {
		Vector3 side1 = b - a;
		Vector3 side2 = c - a;
		Vector3 normal = Vector3.Cross(side1, side2).normalized;

		
		float dis = PlaneRayIntersection(a, normal, ray);
		if(dis > 0) {
			Vector3 point = ray.GetPoint(dis);
			if( IsPointOnTriangle(a, b, c, normal, point) ) return dis;
		}
		return -1;
	}
	
	private static float PlaneRayIntersection(Vector3 point, Vector3 normal, Ray ray) {
		float d = Vector3.Dot(point, normal);
		float a = Vector3.Dot(ray.direction, normal);
		float b = d - Vector3.Dot(ray.origin, normal);
		if( Mathf.Abs(a) < float.Epsilon ) return -1;
		return b / a;
	}
	
	private static bool IsPointOnTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector3 point) {
		float nx = Mathf.Abs(normal.x);
		float ny = Mathf.Abs(normal.y);
		float nz = Mathf.Abs(normal.z);
		
		if(  nx >= ny && nx >= nz ) {
			return IsPointOnTriangle( point.z, point.y, a.z, a.y, b.z, b.y, c.z, c.y );
		}
		if(  ny >= nx && ny >= nz ) {
			return IsPointOnTriangle( point.x, point.z, a.x, a.z, b.x, b.z, c.x, c.z );
		}
		return IsPointOnTriangle( point.x, point.y, a.x, a.y, b.x, b.y, c.x, c.y );
	}
	
	private static bool IsPointOnTriangle(float px, float py, float x1, float y1, float x2, float y2, float x3, float y3) {
		bool s1 = (x2-x1)*(py-y1) - (px-x1)*(y2-y1) <= 0;
		bool s2 = (x3-x2)*(py-y2) - (px-x2)*(y3-y2) <= 0;
		bool s3 = (x1-x3)*(py-y3) - (px-x3)*(y1-y3) <= 0;
		return s1 == s2 && s2 == s3;
	}

}
