using System.Collections.Generic;
using UnityEngine;

[HelpURL("http://www.illusionloop.com/docs/animatedsteelcoaster.html")]
[SelectionBase]
public class CoasterControllerSWLXR : MonoBehaviour
{
	public bool info = true;

	[Range(1f, 4f)]
	[HideInInspector]
	public int trains = 2;

	[HideInInspector]
	[Range(0.25f, 2f)]
	public float animationSpeed = 1f;

	[Tooltip("Deactivated copy of a train. Used to instantiate all trains but the first one.")]
	public GameObject[] trainPrefab;

	[Tooltip("The train that is activated in editor. Use this train to add camera etc.")]
	public GameObject[] firstTrain;

	private List<Animation> cartAnimations;

	private List<int> trainIndices;

	public float[] startDelay = new float[4] { 0f, 48.5f, 24.25f, 17.25f };

	private void Start()
	{
		if (!Check())
		{
			return;
		}
		cartAnimations = new List<Animation>();
		trainIndices = new List<int>();
		for (int i = 0; i < trainPrefab.Length; i++)
		{
			Animation component = trainPrefab[i].GetComponent<Animation>();
			foreach (AnimationState item in component)
			{
				item.speed = animationSpeed;
			}
		}
		for (int j = 0; j < firstTrain.Length; j++)
		{
			Animation component2 = firstTrain[j].GetComponent<Animation>();
			cartAnimations.Add(component2);
			trainIndices.Add(0);
			foreach (AnimationState item2 in component2)
			{
				item2.speed = animationSpeed;
			}
		}
		for (int k = 0; k < trains - 1; k++)
		{
			for (int l = 0; l < trainPrefab.Length; l++)
			{
				GameObject gameObject = Object.Instantiate(trainPrefab[l], trainPrefab[l].transform.position, trainPrefab[l].transform.rotation);
				gameObject.SetActive(true);
				gameObject.GetComponent<AnimationDelay>().delay = startDelay[trains - 1] * (float)(k + 1);
				gameObject.transform.parent = base.transform;
				Animation component3 = gameObject.GetComponent<Animation>();
				cartAnimations.Add(component3);
				trainIndices.Add(k + 1);
				foreach (AnimationState item3 in component3)
				{
					item3.speed = animationSpeed;
				}
			}
		}
		base.enabled = false;
	}

	public void Reset()
	{
		if (cartAnimations == null)
		{
			return;
		}
		foreach (Animation cartAnimation in cartAnimations)
		{
			bool flag = false;
			GameObject[] array = firstTrain;
			foreach (GameObject gameObject in array)
			{
				if (gameObject == cartAnimation.gameObject)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				Object.Destroy(cartAnimation.gameObject);
			}
		}
		Start();
		Rewind();
	}

	public void Rewind()
	{
		if (cartAnimations == null)
		{
			return;
		}
		foreach (Animation cartAnimation in cartAnimations)
		{
			AnimationDelay component = cartAnimation.GetComponent<AnimationDelay>();
			if (!(component != null))
			{
				continue;
			}
			foreach (AnimationState item in cartAnimation)
			{
				item.time = component.delay;
			}
		}
	}

	public void FastForward(float time, bool normalizedTime)
	{
		if (cartAnimations == null)
		{
			return;
		}
		foreach (Animation cartAnimation in cartAnimations)
		{
			if (normalizedTime)
			{
				AnimationDelay component = cartAnimation.GetComponent<AnimationDelay>();
				if (!(component != null))
				{
					continue;
				}
				foreach (AnimationState item in cartAnimation)
				{
					item.normalizedTime = time + component.delay;
				}
				continue;
			}
			AnimationDelay component2 = cartAnimation.GetComponent<AnimationDelay>();
			if (!(component2 != null))
			{
				continue;
			}
			foreach (AnimationState item2 in cartAnimation)
			{
				item2.time = time + component2.delay;
			}
		}
	}

	public void SetSpeed(float speed)
	{
		if (cartAnimations == null)
		{
			return;
		}
		foreach (Animation cartAnimation in cartAnimations)
		{
			foreach (AnimationState item in cartAnimation)
			{
				item.speed = speed;
			}
		}
	}

	public bool Check()
	{
		if (trainPrefab == null)
		{
			Debug.Log("Unable to execute.\n Train prefabs are missing. Try to reload prefab, reassign script or assign train prefabs manually.");
			return false;
		}
		for (int i = 0; i < trainPrefab.Length; i++)
		{
			if (trainPrefab[i] == null)
			{
				Debug.Log("Unable to execute.\n Train prefab cart nr: " + i + " is missing. please assign a prefab.");
				return false;
			}
			if (trainPrefab[i].GetComponent<Animation>() == null || trainPrefab[i].GetComponent<AnimationDelay>() == null)
			{
				Debug.Log("Unable to execute.\n Animation or animation delay component is missing on prefab cart " + i + ". A prefab cart must use these components.");
				return false;
			}
		}
		return true;
	}

	public bool Check(out string message)
	{
		if (trainPrefab == null)
		{
			message = "Unable to execute.\n Train prefabs are missing. Try to reload prefab, reassign script or assign train prefabs manually.";
			return false;
		}
		for (int i = 0; i < trainPrefab.Length; i++)
		{
			if (trainPrefab[i] == null)
			{
				message = "train prefab cart nr: " + i + " is missing. please assign a prefab.";
				return false;
			}
			if (trainPrefab[i].GetComponent<Animation>() == null || trainPrefab[i].GetComponent<AnimationDelay>() == null)
			{
				message = "Animation or animation delay component is missing on prefab cart " + i + ". A prefab cart must use these components.";
				return false;
			}
		}
		message = string.Empty;
		return true;
	}
}
