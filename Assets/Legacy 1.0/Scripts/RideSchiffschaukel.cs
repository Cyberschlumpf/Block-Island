using UnityEngine;

public class RideSchiffschaukel : MonoBehaviour
{
	private GameObject gondelarm;

	private float gondelarm_sin;

	private void Awake()
	{
		gondelarm = base.transform.Find("plattform/staender/gondel_arm").gameObject;
		gondelarm_sin = 0f;
	}

	private void Start()
	{
	}

	private void Update()
	{
		float z = Mathf.Sin(gondelarm_sin) * 80f;
		gondelarm_sin = (gondelarm_sin + Time.deltaTime * 1f) % 360f;
		gondelarm.transform.localEulerAngles = new Vector3(0f, 0f, z);
	}
}
