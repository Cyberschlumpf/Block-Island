using UnityEngine;

public class AudioVelocityControl : MonoBehaviour
{
	private Vector3 lastpos = default(Vector3);

	public AudioSource[] a_sources;

	public AudioSource lift_sound;

	public float[] min_pitch;

	public float[] min_volume;

	public float[] pitch_per_velocity;

	private void Start()
	{
		lastpos = base.transform.position;
	}

	private void FixedUpdate()
	{
		float num = (base.transform.position - lastpos).magnitude / Time.deltaTime;
		lastpos = base.transform.position;
		for (int i = 0; i < a_sources.Length; i++)
		{
			a_sources[i].pitch = Mathf.Lerp(a_sources[i].pitch, min_pitch[i] + num * pitch_per_velocity[i], 0.04f);
			a_sources[i].volume = Mathf.Lerp(a_sources[i].volume, Mathf.Clamp01(num * pitch_per_velocity[i]) * 7f + min_volume[i], 0.04f);
		}
	}

	public void EnableLift()
	{
		lift_sound.enabled = true;
	}

	public void DisableLift()
	{
		lift_sound.enabled = false;
	}
}
