using UnityEngine;

public class RideOctopus : MonoBehaviour
{
	private GameObject dreharm;

	private GameObject[] arm;

	private GameObject[] kreuz;

	private float dreharm_rot;

	private float arm_rot;

	private float kreuz_rot;

	private float arm_sin;

	private void Awake()
	{
		dreharm = base.transform.Find("dreharm").gameObject;
		arm = new GameObject[4];
		arm[0] = dreharm.transform.Find("arm1").gameObject;
		arm[1] = dreharm.transform.Find("arm2").gameObject;
		arm[2] = dreharm.transform.Find("arm3").gameObject;
		arm[3] = dreharm.transform.Find("arm4").gameObject;
		kreuz = new GameObject[4];
		kreuz[0] = arm[0].transform.Find("kreuzcenter").gameObject;
		kreuz[1] = arm[1].transform.Find("kreuzcenter").gameObject;
		kreuz[2] = arm[2].transform.Find("kreuzcenter").gameObject;
		kreuz[3] = arm[3].transform.Find("kreuzcenter").gameObject;
		dreharm_rot = 0f;
		arm_rot = 0f;
		kreuz_rot = 0f;
		arm_sin = 0f;
	}

	private void Start()
	{
		Update();
	}

	private void Update()
	{
		dreharm_rot = (dreharm_rot + Time.deltaTime * 65f) % 360f;
		dreharm.transform.localEulerAngles = new Vector3(0f, dreharm_rot, 0f);
		arm_rot = Mathf.Sin(arm_sin) * 11f + 11f;
		arm_sin = (arm_sin + Time.deltaTime * 2f) % 360f;
		arm[0].transform.localEulerAngles = new Vector3(0f, 0f, arm_rot);
		arm[1].transform.localEulerAngles = new Vector3(0f, 90f, 22f - arm_rot);
		arm[2].transform.localEulerAngles = new Vector3(0f, 180f, arm_rot);
		arm[3].transform.localEulerAngles = new Vector3(0f, 270f, 22f - arm_rot);
		kreuz_rot = (dreharm_rot - Time.deltaTime * 180f) % 360f;
		kreuz[0].transform.localEulerAngles = new Vector3(0f, kreuz_rot, 0f);
		kreuz[1].transform.localEulerAngles = new Vector3(0f, kreuz_rot, 0f);
		kreuz[2].transform.localEulerAngles = new Vector3(0f, kreuz_rot, 0f);
		kreuz[3].transform.localEulerAngles = new Vector3(0f, kreuz_rot, 0f);
	}
}
