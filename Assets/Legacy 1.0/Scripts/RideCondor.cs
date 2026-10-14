using UnityEngine;

public class RideCondor : MonoBehaviour
{
	private GameObject hubwerk;

	private GameObject arme;

	private GameObject[] ring;

	private float hubwerk_ypos;

	private float arm_rot;

	private float ring_rot;

	private int hubwerk_mode;

	private float hubwerk_timer;

	private void Awake()
	{
		hubwerk = base.transform.Find("plattform_typ_02a/mast/hubwerk").gameObject;
		arme = hubwerk.transform.Find("arm_halter").gameObject;
		ring = new GameObject[4];
		ring[0] = arme.transform.Find("arm1/ring").gameObject;
		ring[1] = arme.transform.Find("arm2/ring").gameObject;
		ring[2] = arme.transform.Find("arm3/ring").gameObject;
		ring[3] = arme.transform.Find("arm4/ring").gameObject;
		hubwerk_ypos = 3.45f;
		arm_rot = 0f;
		ring_rot = 0f;
		hubwerk_mode = 0;
		hubwerk_timer = 0f;
	}

	private void Start()
	{
	}

	private void Update()
	{
		if (hubwerk_mode == 0)
		{
			hubwerk_timer += Time.deltaTime * 1f;
			if (hubwerk_timer >= 5f)
			{
				hubwerk_mode = 1;
			}
		}
		else if (hubwerk_mode == 1)
		{
			hubwerk_ypos += Time.deltaTime * 2f;
			if (hubwerk_ypos >= 37.6f)
			{
				hubwerk_timer = 0f;
				hubwerk_mode = 2;
			}
		}
		else if (hubwerk_mode == 2)
		{
			hubwerk_timer += Time.deltaTime * 1f;
			if (hubwerk_timer >= 5f)
			{
				hubwerk_mode = 3;
			}
		}
		else
		{
			hubwerk_ypos -= Time.deltaTime * 2f;
			if (hubwerk_ypos <= 3.45f)
			{
				hubwerk_timer = 0f;
				hubwerk_mode = 0;
			}
		}
		hubwerk.transform.localPosition = new Vector3(0f, hubwerk_ypos, -1.758529f);
		arm_rot = (arm_rot + Time.deltaTime * 50f) % 360f;
		arme.transform.localEulerAngles = new Vector3(0f, arm_rot, 0f);
		ring_rot = (ring_rot - Time.deltaTime * 120f) % 360f;
		for (int i = 0; i < 4; i++)
		{
			ring[i].transform.localEulerAngles = new Vector3(0f, ring_rot, 0f);
		}
	}
}
