using UnityEngine;

public class ConstantRotate : MonoBehaviour
{
	public Vector3 rotateSpeed;

	public bool RandomRotationSpeed;

	public bool RandomRotationDirection;

	public Vector3 RotationSpeedMin = Vector3.zero;

	public Vector3 RotationSpeedMax = Vector3.zero;

	public bool RandomStartOrientation;

	public Vector3 StartOrientationMin = Vector3.zero;

	public Vector3 StartOrientationMax = Vector3.zero;

	private void Start()
	{
		if (RandomStartOrientation)
		{
			float x = Random.Range(StartOrientationMin.x, StartOrientationMax.x);
			float y = Random.Range(StartOrientationMin.y, StartOrientationMax.y);
			float z = Random.Range(StartOrientationMin.z, StartOrientationMax.z);
			base.transform.localRotation = Quaternion.Euler(x, y, z);
		}
		if (RandomRotationSpeed)
		{
			float x2 = Random.Range(RotationSpeedMin.x, RotationSpeedMin.x);
			float y2 = Random.Range(RotationSpeedMin.y, RotationSpeedMin.y);
			float z2 = Random.Range(RotationSpeedMin.z, RotationSpeedMin.z);
			rotateSpeed = new Vector3(x2, y2, z2);
		}
		if (RandomRotationDirection && Random.value > 0.5f)
		{
			rotateSpeed = -rotateSpeed;
		}
	}

	private void Update()
	{
		base.transform.localRotation = Quaternion.Euler(base.transform.localRotation.eulerAngles + rotateSpeed * Time.deltaTime);
	}
}
