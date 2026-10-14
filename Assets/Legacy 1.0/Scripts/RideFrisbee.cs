using UnityEngine;

public class RideFrisbee : MonoBehaviour
{
	private GameObject arm;

	private GameObject gondel;

	private float arm_sin;

	private float arm_rot;

	private float gondel_rot;

	private void Awake()
	{
		arm = base.transform.Find("unterbau/plattform/staender/arm").gameObject;
		gondel = arm.transform.Find("gondel").gameObject;
		arm_sin = 0f;
		gondel_rot = 0f;
	}

	private void Start()
	{
		Update();
	}

	private void Update()
	{
		arm_rot = Mathf.Sin(arm_sin) * 90f;
		arm_sin = (arm_sin + Time.deltaTime * 1f) % 360f;
		arm.transform.localEulerAngles = new Vector3(0f, 0f, arm_rot);
		gondel_rot = (gondel_rot + Time.deltaTime * 80f) % 360f;
		gondel.transform.localEulerAngles = new Vector3(0f, gondel_rot, 0f);
	}
}
