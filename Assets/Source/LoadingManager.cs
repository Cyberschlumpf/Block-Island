using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingManager : MonoBehaviour {

	[SerializeField]
	private GameObject loadingContainer;

	private const float LoadTime = 3f;

	IEnumerator Start()
	{
		GameState.IsPause = true;
		yield return new WaitForSeconds(LoadTime * Time.timeScale);

		loadingContainer.SetActive(false);

		GameState.IsPause = false;
	}

}
