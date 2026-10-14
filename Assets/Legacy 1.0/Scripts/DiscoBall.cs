using UnityEngine;

public class DiscoBall : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		base.transform.Rotate(Vector3.forward * Time.deltaTime * 15f);
	}
}
