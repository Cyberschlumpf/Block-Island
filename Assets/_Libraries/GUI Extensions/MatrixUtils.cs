using UnityEngine;
using System.Collections;

public class MatrixUtils {

	public static Matrix4x4 Create(float posX, float posY, float scaleX, float scaleY) {
		return Matrix4x4.TRS( new Vector3(posX, posY), Quaternion.identity, new Vector3(scaleX, scaleY, 1) );
	}

}
