using UnityEngine;

// X/Y world roll for every playable world.
// IMPORTANT: the camera/player is NOT rolled. Instead the rendered voxel world rotates
// around the camera's current forward axis, through the camera position. This keeps the
// player at exactly the same place in front of the aimed block and keeps MouseLook unchanged.
public class SingularityViewAxisRoll : MonoBehaviour
{
    public float rollSpeed = 90f;
    public static Transform WorldRoot { get; private set; }

    public static bool Active {
        get { return WorldRoot != null; }
    }

    void LateUpdate()
    {
        if (Cursor.visible) return;

        if (WorldRoot == null) {
            GameObject root = GameObject.Find("InfiniteWorldRenderRoot");
            if (root != null) WorldRoot = root.transform;
        }
        if (WorldRoot == null) return;

        float direction = 0f;
        if (Input.GetKey(KeyCode.X) || Input.GetKey("x")) direction -= 1f;
        // Y opposite to X. Z remains only as physical-position fallback for QWERTZ input mappings.
        if (Input.GetKey(KeyCode.Y) || Input.GetKey("y") || Input.GetKey(KeyCode.Z)) direction += 1f;

        if (Mathf.Abs(direction) > 0.01f) {
            float angle = direction * rollSpeed * Time.deltaTime;
            // Rotate THE WORLD around the player's view axis. Camera/player rotation is untouched.
            WorldRoot.RotateAround(transform.position, transform.forward, angle);
        }
    }

    public static Vector3 ToLogicalPoint(Vector3 visualPoint)
    {
        return Active ? WorldRoot.InverseTransformPoint(visualPoint) : visualPoint;
    }

    public static Vector3 ToLogicalDirection(Vector3 visualDirection)
    {
        return Active ? WorldRoot.InverseTransformDirection(visualDirection) : visualDirection;
    }

    public static Ray ToLogicalRay(Ray visualRay)
    {
        if (!Active) return visualRay;
        return new Ray(ToLogicalPoint(visualRay.origin), ToLogicalDirection(visualRay.direction).normalized);
    }
}
