using UnityEngine;

public class RideBreakdancer : MonoBehaviour
{
	private GameObject teller;

	private GameObject[] kreuze;

	private GameObject[] gondeln;

	private float teller_rotation;

	private float kreuz_rotation;

	private float[] gondel_start_rot;

	private float[] gondel_rotation;

	private float[] gondel_sin1;

	private float[] gondel_sin2;

	private float[] gondel_sin3;

	private float[] gondel_add1;

	private float[] gondel_add2;

	private float[] gondel_add3;

	private void Awake()
	{
		teller = base.transform.Find("plattform_typ_01a/teller_null/teller").gameObject;
		kreuze = new GameObject[4];
		gondeln = new GameObject[16];
		kreuze[0] = teller.transform.Find("kreuz_01").gameObject;
		kreuze[1] = teller.transform.Find("kreuz_02").gameObject;
		kreuze[2] = teller.transform.Find("kreuz_03").gameObject;
		kreuze[3] = teller.transform.Find("kreuz_04").gameObject;
		gondeln[0] = kreuze[0].transform.Find("gondel1").gameObject;
		gondeln[1] = kreuze[0].transform.Find("gondel2").gameObject;
		gondeln[2] = kreuze[0].transform.Find("gondel3").gameObject;
		gondeln[3] = kreuze[0].transform.Find("gondel4").gameObject;
		gondeln[4] = kreuze[1].transform.Find("gondel1").gameObject;
		gondeln[5] = kreuze[1].transform.Find("gondel2").gameObject;
		gondeln[6] = kreuze[1].transform.Find("gondel3").gameObject;
		gondeln[7] = kreuze[1].transform.Find("gondel4").gameObject;
		gondeln[8] = kreuze[2].transform.Find("gondel1").gameObject;
		gondeln[9] = kreuze[2].transform.Find("gondel2").gameObject;
		gondeln[10] = kreuze[2].transform.Find("gondel3").gameObject;
		gondeln[11] = kreuze[2].transform.Find("gondel4").gameObject;
		gondeln[12] = kreuze[3].transform.Find("gondel1").gameObject;
		gondeln[13] = kreuze[3].transform.Find("gondel2").gameObject;
		gondeln[14] = kreuze[3].transform.Find("gondel3").gameObject;
		gondeln[15] = kreuze[3].transform.Find("gondel4").gameObject;
		teller_rotation = 0f;
		kreuz_rotation = 0f;
		gondel_rotation = new float[16];
		gondel_start_rot = new float[16];
		gondel_sin1 = new float[16];
		gondel_sin2 = new float[16];
		gondel_sin3 = new float[16];
		gondel_add1 = new float[16];
		gondel_add2 = new float[16];
		gondel_add3 = new float[16];
		for (int i = 0; i < 16; i++)
		{
			gondel_start_rot[i] = gondeln[i].transform.localEulerAngles.y;
			gondel_rotation[i] = 0f;
			gondel_sin1[i] = Random.Range(0f, 360f);
			gondel_sin2[i] = Random.Range(0f, 360f);
			gondel_sin3[i] = Random.Range(0f, 360f);
			gondel_add1[i] = Random.Range(0.2f, 3f);
			gondel_add2[i] = Random.Range(0.2f, 3f);
			gondel_add3[i] = Random.Range(0.2f, 3f);
		}
	}

	private void Start()
	{
		Update();
	}

	private void Update()
	{
		teller_rotation = (teller_rotation + Time.deltaTime * 60f) % 360f;
		teller.transform.localEulerAngles = new Vector3(0f, teller_rotation, 0f);
		kreuz_rotation = (kreuz_rotation - Time.deltaTime * 210f) % 360f;
		for (int i = 0; i < 4; i++)
		{
			kreuze[i].transform.localEulerAngles = new Vector3(0f, kreuz_rotation, 0f);
		}
		for (int i = 0; i < 16; i++)
		{
			float num = Mathf.Sin(gondel_sin1[i]) * 360f;
			float num2 = Mathf.Sin(gondel_sin2[i]) * 210f;
			float num3 = Mathf.Sin(gondel_sin3[i]) * 450f;
			num = (num + num2 + num3) / 3.5f;
			gondeln[i].transform.localEulerAngles = new Vector3(0f, num + gondel_start_rot[i], 0f);
			gondel_sin1[i] += gondel_add1[i] * Time.deltaTime % 360f;
			gondel_sin2[i] -= gondel_add2[i] * Time.deltaTime % 360f;
			gondel_sin3[i] += gondel_add3[i] * Time.deltaTime % 360f;
		}
	}
}
