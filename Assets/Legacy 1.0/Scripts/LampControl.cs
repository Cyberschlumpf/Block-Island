using UnityEngine;

public class LampControl : MonoBehaviour
{
	public bool fireOn = true;

	public bool coneOn = true;

	public bool conePartOn = true;

	public GameObject fire;

	public GameObject cone;

	public GameObject coneParticles;

	private void Start()
	{
		if (fire == null)
		{
			fire = base.gameObject.transform.Find("Fire").gameObject;
		}
	}

	private void Awake()
	{
		if (fire == null)
		{
			fire = base.gameObject.transform.Find("Fire").gameObject;
		}
	}

	private void Update()
	{
		if (fireOn)
		{
			TurnOn(fire);
		}
	}

	private void TurnOn(GameObject go)
	{
		go.SetActive(true);
	}
}
