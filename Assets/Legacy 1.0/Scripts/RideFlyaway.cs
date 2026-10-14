using UnityEngine;

public class RideFlyaway : MonoBehaviour
{
	private GameObject arm;

	private GameObject teller;

	private GameObject[] gondeln;

	private float arm_rot;

	private float teller_rot;

	private float gondel_rot;

	private int arm_mode;

	private float arm_timer;

	private void Awake()
	{
		arm = base.transform.Find("plattform/arm").gameObject;
		teller = arm.transform.Find("teller").gameObject;
		gondeln = new GameObject[18];
		gondeln[0] = teller.transform.Find("teller_arm_01/gondel").gameObject;
		gondeln[1] = teller.transform.Find("teller_arm_02/gondel").gameObject;
		gondeln[2] = teller.transform.Find("teller_arm_03/gondel").gameObject;
		gondeln[3] = teller.transform.Find("teller_arm_04/gondel").gameObject;
		gondeln[4] = teller.transform.Find("teller_arm_05/gondel").gameObject;
		gondeln[5] = teller.transform.Find("teller_arm_06/gondel").gameObject;
		gondeln[6] = teller.transform.Find("teller_arm_07/gondel").gameObject;
		gondeln[7] = teller.transform.Find("teller_arm_08/gondel").gameObject;
		gondeln[8] = teller.transform.Find("teller_arm_09/gondel").gameObject;
		gondeln[9] = teller.transform.Find("teller_arm_10/gondel").gameObject;
		gondeln[10] = teller.transform.Find("teller_arm_11/gondel").gameObject;
		gondeln[11] = teller.transform.Find("teller_arm_12/gondel").gameObject;
		gondeln[12] = teller.transform.Find("teller_arm_13/gondel").gameObject;
		gondeln[13] = teller.transform.Find("teller_arm_14/gondel").gameObject;
		gondeln[14] = teller.transform.Find("teller_arm_15/gondel").gameObject;
		gondeln[15] = teller.transform.Find("teller_arm_16/gondel").gameObject;
		gondeln[16] = teller.transform.Find("teller_arm_17/gondel").gameObject;
		gondeln[17] = teller.transform.Find("teller_arm_18/gondel").gameObject;
		arm_rot = 0f;
		teller_rot = 0f;
		gondel_rot = 0f;
		arm_mode = 0;
		arm_timer = 0f;
	}

	private void Start()
	{
	}

	private void Update()
	{
		if (arm_mode == 0)
		{
			arm_timer += Time.deltaTime * 1f;
			if (arm_timer >= 5f)
			{
				arm_mode = 1;
			}
		}
		else if (arm_mode == 1)
		{
			arm_rot += Time.deltaTime * 6f;
			if (arm_rot >= 75f)
			{
				arm_timer = 0f;
				arm_mode = 2;
			}
		}
		else if (arm_mode == 2)
		{
			arm_timer += Time.deltaTime * 1f;
			if (arm_timer >= 5f)
			{
				arm_mode = 3;
			}
		}
		else
		{
			arm_rot -= Time.deltaTime * 6f;
			if (arm_rot <= 0.01f)
			{
				arm_timer = 0f;
				arm_mode = 0;
			}
		}
		arm.transform.localEulerAngles = new Vector3(arm_rot, 0f, 0f);
		teller_rot = (teller_rot + Time.deltaTime * 100f) % 360f;
		teller.transform.localEulerAngles = new Vector3(0f, teller_rot, 0f);
		gondel_rot = 0f - arm_rot;
		for (int i = 0; i < 18; i++)
		{
			gondeln[i].transform.localEulerAngles = new Vector3(gondel_rot, 10f, 0f);
		}
	}
}
