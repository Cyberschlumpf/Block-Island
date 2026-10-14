using UnityEngine;

public class AnimSpeedController : MonoBehaviour
{
	public float speed;

	private void Start()
	{
	}

	private void Update()
	{
		GetComponent<Animation>()["1_center_loop"].speed = speed;
	}
}
