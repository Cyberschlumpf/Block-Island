using UnityEngine;

public class CloudRotation : MonoBehaviour
{
	public float planetSpeedRotation = 3f;

	private void Start()
	{
	}

	private void Update()
	{
		base.transform.Rotate(Vector3.up * Time.deltaTime * planetSpeedRotation);
	}
}
