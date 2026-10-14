using UnityEngine;

public class AnimationDelay : MonoBehaviour
{
	public float delay = 30f;

	private void Start()
	{
	}

	private void Update()
	{
		GetComponent<Animation>()[GetComponent<Animation>().clip.name].time = delay;
		base.enabled = false;
	}
}
