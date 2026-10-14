using UnityEngine;
using System.Collections;

public static class RectUtils {
	

	public static Rect ResizeAroundCenter(Rect rect, int dx, int dy) {
		Vector2 center = rect.center;
        rect.size += new Vector2(dx, dy);
		rect.center = center;
		return rect;
	}
	
	public static Rect Mul(Rect rect, Matrix4x4 matrix) {
		Vector3 pos = matrix.MultiplyPoint( rect.position );
		Vector3 size = matrix.MultiplyVector( rect.size );
		return new Rect( pos.x, pos.y, size.x, size.y );
	}

    public static Rect Scale(Rect rect, Vector2 scale) {
        rect.min = Vector2.Scale(rect.min, scale);
        rect.max = Vector2.Scale(rect.max, scale);
        return rect;
    }

}
