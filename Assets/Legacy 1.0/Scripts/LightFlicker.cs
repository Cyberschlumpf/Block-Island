using UnityEngine;

public class LightFlicker : MonoBehaviour
{
	public bool isFlickering = true;

	public float lowRange = 1.75f;

	public float highRange = 2.25f;

	public float flickerRate = 0.15f;

	public float startFlickeringIn;

	private void Start()
	{
		if (isFlickering)
		{
			TurnOn();
		}
	}

	private void Awake()
	{
		if (isFlickering)
		{
			TurnOn();
		}
	}

	private void FlickerLight()
	{
		float value = Random.value;
		float num = highRange - lowRange;
		if (0f > lowRange || lowRange > 8f || 0f > highRange || highRange > 8f)
		{
			Debug.Log("lowRange and highRange cannot be negetive or higher than 8. lowRange: " + lowRange + " highRange: " + highRange);
		}
		else
		{
			float intensity = num * value + lowRange;
			base.gameObject.GetComponent<Light>().intensity = intensity;
		}
	}

	private void TurnOff()
	{
		CancelInvoke("FlickerLight");
	}

	private void TurnOn()
	{
		InvokeRepeating("FlickerLight", startFlickeringIn, flickerRate);
	}
}
