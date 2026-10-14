using UnityEngine;

public class RideTopspin : MonoBehaviour
{
	private GameObject arme;

	private GameObject gondel;

	private float arm_rot;

	private float gondel_sin;

	private int arm_mode;

	private float arm_timer;

	private bool arm_direction;

	private void Awake()
	{
		arme = base.transform.Find("rotationsarm_achse").gameObject;
		gondel = arme.transform.Find("gondel").gameObject;
		arm_direction = true;
		arm_mode = 0;
		arm_timer = 0f;
		arm_rot = 0f;
		gondel_sin = 0f;
	}

	private void Start()
	{
		Update();
	}

	private void Update()
	{
		if (arm_mode == 0)
		{
			arm_timer -= Time.deltaTime * 1f;
			if (arm_timer < 0f)
			{
				arm_timer = Random.Range(16f, 32f);
				arm_mode = 1;
				arm_direction = !arm_direction;
			}
		}
		else
		{
			arm_timer -= Time.deltaTime * 1f;
			if (arm_timer < 0f)
			{
				arm_timer = Random.Range(2f, 5f);
				arm_mode = 0;
			}
			else if (arm_direction)
			{
				arm_rot += Time.deltaTime * 60f;
			}
			else
			{
				arm_rot -= Time.deltaTime * 60f;
			}
		}
		arme.transform.localEulerAngles = new Vector3(arm_rot, 0f, 0f);
		float num = Mathf.Sin(gondel_sin) * 45f;
		gondel.transform.localEulerAngles = new Vector3(0f - arm_rot + num, 0f, 0f);
		gondel_sin += 5f * Time.deltaTime % 360f;
	}
}
