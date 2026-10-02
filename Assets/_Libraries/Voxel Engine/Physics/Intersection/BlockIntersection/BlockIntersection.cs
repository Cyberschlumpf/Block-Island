using UnityEngine;
using System.Collections;

public class BlockIntersection {

	public static float BlockRayIntersection(DataBlock block, Vector3i pos, Ray ray, float distance) {
		if(!block.IsSolid()) return -1;

        if(block.block is StairBlock) return StairIntersection.StairRayIntersection( pos, block.direction, ray );
        if(block.block is MeshBlock) return MeshIntersection.MeshRayIntersection( block, pos, ray );
        if(block.block is FenceBlock) return FenceIntersection.FenceRayIntersection( block, pos, ray );
        return distance;
	}

}
