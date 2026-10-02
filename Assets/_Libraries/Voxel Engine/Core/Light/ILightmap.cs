using UnityEngine;
using System.Collections;

public interface ILightmap {

	bool SetMaxLight(int light, Vector3i pos);
	void SetLight(int light, Vector3i pos);
	int GetLight(Vector3i pos);
	int GetSourceLight(Vector3i pos);

}
