using UnityEngine;

public class RevealNightEarthTexture : MonoBehaviour
{
	private Transform tfLight;

	private void Start()
	{
		GameObject gameObject = GameObject.Find("RevealingLight");
		if ((bool)gameObject)
		{
			tfLight = gameObject.transform;
		}
	}

	private void Update()
	{
		if ((bool)tfLight)
		{
			GetComponent<Renderer>().material.SetVector("_LightPos", tfLight.position);
			GetComponent<Renderer>().material.SetVector("_LightDir", tfLight.forward);
		}
	}
}
