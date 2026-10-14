using UnityEngine;

public class ConstantRotateAxis : MonoBehaviour
{
	public Vector3 axis;

	public float speed;

	private void Update()
	{
		if (axis.magnitude > 0f)
		{
			base.transform.localRotation *= Quaternion.AngleAxis(speed * Time.deltaTime, axis.normalized);
		}
	}
}
