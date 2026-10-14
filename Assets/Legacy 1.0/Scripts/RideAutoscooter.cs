using UnityEngine;

public class RideAutoscooter : MonoBehaviour
{
	private GameObject wagen;

	private float wagen_rot;

	private void Awake()
	{
		wagen = base.transform.Find("wagen_center").gameObject;
		wagen_rot = 0f;
	}

	private void Start()
	{
	}

	private void Update()
	{
		wagen_rot = (wagen_rot + Time.deltaTime * 25f) % 360f;
		wagen.transform.localEulerAngles = new Vector3(0f, wagen_rot, 0f);
	}
}
