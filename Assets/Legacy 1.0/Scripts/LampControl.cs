using UnityEngine;

// Block Island 1.0 street lantern. In 2.0 the flame burns only when it is dark (dusk, night, rain, thunderstorm,
// snowstorm) and its brightness follows the level chosen with Enter (Aus, Schwach, Mittel, Hell, Sehr hell).
public class LampControl : MonoBehaviour
{
	public bool fireOn = true;

	public bool coneOn = true;

	public bool conePartOn = true;

	public GameObject fire;

	public GameObject cone;

	public GameObject coneParticles;

	Light flame; LightFlicker flicker; float baseRange; float strength = 1f; float nextCheck; Transform blockRoot;

	private void Awake()
	{
		if (fire == null) fire = base.gameObject.transform.Find("Fire").gameObject;
		flame = fire != null ? fire.GetComponentInChildren<Light>(true) : null;
		flicker = fire != null ? fire.GetComponentInChildren<LightFlicker>(true) : null;
		if (flame != null) baseRange = flame.range;
	}

	private void Update()
	{
		if (fire == null) return;
		if (Time.unscaledTime >= nextCheck)
		{
			nextCheck = Time.unscaledTime + .5f;
			strength = Strength();
		}
		bool on = fireOn && strength > 0f && FunctionBlocks.IsDark;
		if (fire.activeSelf != on) fire.SetActive(on);
		if (on && flame != null) { flame.range = baseRange * Mathf.Max(.6f, strength * 1.6f); if (flicker != null) flicker.scale = strength; }
	}

	// The level is stored for the lantern's block; the model may sit a little below its cell, so look around it.
	float Strength()
	{
		if (blockRoot == null) { blockRoot = transform; while (blockRoot.parent != null && blockRoot.parent.name != "InfiniteSpecialBlocks") blockRoot = blockRoot.parent; }
		Vector3 p = blockRoot.position;
		int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
		for (int dy = 1; dy >= -1; dy--)
		{
			Vector3i c = new Vector3i(x, y + dy, z);
			if (Map.instance != null && FunctionBlocks.IsStreetLantern(Map.instance.GetBlock(c).block)) return FunctionBlocks.LampStrength(c);
		}
		return 1f;
	}
}
