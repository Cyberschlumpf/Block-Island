using UnityEngine;
using System.Collections;

public static class BlockDirectionUtils {

    public static Vector3 TransformBlockVertex(Vector3 vertex, BlockDirection dir) {
        vertex.x -= 0.5f;
        vertex.y -= 0.5f;
        vertex.z -= 0.5f;

        if(dir == BlockDirection.BACKWARD) {
            vertex.x = -vertex.x;
            vertex.z = -vertex.z;
        } else if(dir == BlockDirection.LEFT) {
            float x = vertex.x;
            float z = vertex.z;
            vertex.x = -z;
            vertex.z = x;
        } else if(dir == BlockDirection.RIGHT) {
            float x = vertex.x;
            float z = vertex.z;
            vertex.x = z;
            vertex.z = -x;
        }

        vertex.x += 0.5f;
        vertex.y += 0.5f;
        vertex.z += 0.5f;
        return vertex;
    }

    public static Vector3 TransformVector(Vector3 vector, BlockDirection dir) {
        if(dir == BlockDirection.BACKWARD) {
            vector.x = -vector.x;
            vector.z = -vector.z;
        } else if(dir == BlockDirection.LEFT) {
            float x = vector.x;
            float z = vector.z;
            vector.x = -z;
            vector.z = x;
        } else if(dir == BlockDirection.RIGHT) {
            float x = vector.x;
            float z = vector.z;
            vector.x = z;
            vector.z = -x;
        }
        return vector;
    }

    public static Vector3i TransformVector(Vector3i vector, BlockDirection dir) {
        if(dir == BlockDirection.BACKWARD) {
            vector.x = -vector.x;
            vector.z = -vector.z;
        } else if(dir == BlockDirection.LEFT) {
            int x = vector.x;
            int z = vector.z;
            vector.x = -z;
            vector.z = x;
        } else if(dir == BlockDirection.RIGHT) {
            int x = vector.x;
            int z = vector.z;
            vector.x = z;
            vector.z = -x;
        }
        return vector;
    }

    public static CubeSide TransformCubeSide(CubeSide side, BlockDirection dir) {
        if(side == CubeSide.Top || side == CubeSide.Bottom) {
            return side;
        }

        //Front, Right, Back, Left
        //0      90     180   270

        //    front
        // left | right
        //    back

        int angle = 0;
        if(side == CubeSide.Right) angle = 90;
        if(side == CubeSide.Back) angle = 180;
        if(side == CubeSide.Left) angle = 270;

        if(dir == BlockDirection.RIGHT) angle += 90;
        if(dir == BlockDirection.BACKWARD) angle += 180;
        if(dir == BlockDirection.LEFT) angle += 270;

        angle %= 360;

        if(angle == 0) return CubeSide.Front;
        if(angle == 90) return CubeSide.Right;
        if(angle == 180) return CubeSide.Back;
        if(angle == 270) return CubeSide.Left;

        return CubeSide.Front;
    }

}
