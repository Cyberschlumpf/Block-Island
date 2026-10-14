using UnityEngine;

public class RideRiesenrad : MonoBehaviour
{
	private GameObject rad_main;

	private GameObject[] gondel;

	private float rad_rotation;

	private float[] gondel_rotation;

	private void Awake()
	{
		rad_main = base.transform.Find("plattform/staender/rad_main").gameObject;
		rad_rotation = 0f;
		gondel = new GameObject[24];
		gondel_rotation = new float[24];
		gondel[0] = rad_main.transform.Find("rad_arm_01/gondel_color1").gameObject;
		gondel[1] = rad_main.transform.Find("rad_arm_02/gondel_color1").gameObject;
		gondel[2] = rad_main.transform.Find("rad_arm_03/gondel_color1").gameObject;
		gondel[3] = rad_main.transform.Find("rad_arm_04/gondel_color1").gameObject;
		gondel[4] = rad_main.transform.Find("rad_arm_05/gondel_color1").gameObject;
		gondel[5] = rad_main.transform.Find("rad_arm_06/gondel_color1").gameObject;
		gondel[6] = rad_main.transform.Find("rad_arm_07/gondel_color1").gameObject;
		gondel[7] = rad_main.transform.Find("rad_arm_08/gondel_color1").gameObject;
		gondel[8] = rad_main.transform.Find("rad_arm_09/gondel_color1").gameObject;
		gondel[9] = rad_main.transform.Find("rad_arm_10/gondel_color1").gameObject;
		gondel[10] = rad_main.transform.Find("rad_arm_11/gondel_color1").gameObject;
		gondel[11] = rad_main.transform.Find("rad_arm_12/gondel_color1").gameObject;
		gondel[12] = rad_main.transform.Find("rad_arm_13/gondel_color1").gameObject;
		gondel[13] = rad_main.transform.Find("rad_arm_14/gondel_color1").gameObject;
		gondel[14] = rad_main.transform.Find("rad_arm_15/gondel_color1").gameObject;
		gondel[15] = rad_main.transform.Find("rad_arm_16/gondel_color1").gameObject;
		gondel[16] = rad_main.transform.Find("rad_arm_17/gondel_color1").gameObject;
		gondel[17] = rad_main.transform.Find("rad_arm_18/gondel_color1").gameObject;
		gondel[18] = rad_main.transform.Find("rad_arm_19/gondel_color1").gameObject;
		gondel[19] = rad_main.transform.Find("rad_arm_20/gondel_color1").gameObject;
		gondel[20] = rad_main.transform.Find("rad_arm_21/gondel_color1").gameObject;
		gondel[21] = rad_main.transform.Find("rad_arm_22/gondel_color1").gameObject;
		gondel[22] = rad_main.transform.Find("rad_arm_23/gondel_color1").gameObject;
		gondel[23] = rad_main.transform.Find("rad_arm_24/gondel_color1").gameObject;
		for (int i = 0; i < 24; i++)
		{
			gondel_rotation[i] = gondel[i].transform.localEulerAngles.z;
		}
	}

	private void Update()
	{
		rad_rotation = (rad_rotation + Time.deltaTime * 4f) % 360f;
		rad_main.transform.localEulerAngles = new Vector3(0f, 0f, rad_rotation);
		for (int i = 0; i < 24; i++)
		{
			gondel[i].transform.localEulerAngles = new Vector3(0f, 0f, gondel_rotation[i] - rad_rotation);
		}
	}
}
