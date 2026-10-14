using UnityEngine;
using System.Collections;

public class FenceIntersection {

    public static float FenceRayIntersection(DataBlock block, Vector3i pos, Ray ray) {
        Box box = FenceBlock.box;
        Box xBox1 = FenceBlock.xBox1;
        Box xBox2 = FenceBlock.xBox2;
        Box zBox1 = FenceBlock.zBox1;
        Box zBox2 = FenceBlock.zBox2;

        ChunkPosition chunkPos = WorldPosition.ToChunkPosition( pos );
        LocalPosition localPos = WorldPosition.ToLocalPosition( pos );
        int localIndex = localPos.ToIndex();
        Chunk chunk = Map.instance.grid.Get( chunkPos );

        bool z1 = FenceBlock.IsBackSolid( localIndex, chunk );
        bool z2 = FenceBlock.IsFrontSolid( localIndex, chunk );

        bool x1 = FenceBlock.IsLeftSolid( localIndex, chunk );
        bool x2 = FenceBlock.IsRightSolid( localIndex, chunk );

        if(!x1) {
            xBox1.min.x = 0.5f;
            xBox2.min.x = 0.5f;
        }
        if(!x2) {
            xBox1.max.x = 0.5f;
            xBox2.max.x = 0.5f;
        }
        if(!z1) {
            zBox1.min.z = 0.5f;
            zBox2.min.z = 0.5f;
        }
        if(!z2) {
            zBox1.max.z = 0.5f;
            zBox2.max.z = 0.5f;
        }

        box.Add( pos );
        xBox1.Add( pos );
        xBox2.Add( pos );
        zBox1.Add( pos );
        zBox2.Add( pos );

        float dis = BoxIntersection.BoxRayIntersection( box, ray );

        if(x1 || x2) {
            float d1 = BoxIntersection.BoxRayIntersection( xBox1, ray );
            float d2 = BoxIntersection.BoxRayIntersection( xBox2, ray );
            dis = MinDistance( dis, d1 );
            dis = MinDistance( dis, d2 );
        }
        if(z1 || z2) {
            float d1 = BoxIntersection.BoxRayIntersection( zBox1, ray );
            float d2 = BoxIntersection.BoxRayIntersection( zBox2, ray );
            dis = MinDistance( dis, d1 );
            dis = MinDistance( dis, d2 );
        }

        return dis;
    }

    private static float MinDistance(float d1, float d2) {
        if(d1 < 0) return d2;
        if(d2 < 0) return d1;
        return Mathf.Min(d1, d2);
    }


}
