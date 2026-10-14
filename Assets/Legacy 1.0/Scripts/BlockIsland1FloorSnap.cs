using System.Collections.Generic;
using UnityEngine;

// Measures a freshly placed 1.0 figure while its animation runs, then puts its feet on the floor.
// The typical (median) contact point over the measuring window is used, so a figure's normal pose
// stands on the floor even if it briefly kneels or lifts a foot.
// Objects whose lowest point moves a lot (rising skeletons, hovering obelisks, laser beams) are
// animated on purpose and keep their original 1.0 set point, like the rides.
public class BlockIsland1FloorSnap : MonoBehaviour {
    const float MeasureSeconds = 2f;
    const float MaxSpread = 1f;

    public GameObject prefab;
    float started = -1f, nextSample;
    readonly List<float> samples = new List<float>();
    readonly List<KeyValuePair<Animator, AnimatorCullingMode>> culling = new List<KeyValuePair<Animator, AnimatorCullingMode>>();
    readonly List<SkinnedMeshRenderer> skinned = new List<SkinnedMeshRenderer>();

    void Start() {
        // Off-screen animators would otherwise keep the bind pose and be measured wrongly.
        foreach(Animator a in GetComponentsInChildren<Animator>()) {
            culling.Add(new KeyValuePair<Animator, AnimatorCullingMode>(a, a.cullingMode));
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        // Exact per-frame bounds of the animated pose while measuring.
        foreach(SkinnedMeshRenderer r in GetComponentsInChildren<SkinnedMeshRenderer>())
            if(!r.updateWhenOffscreen) { r.updateWhenOffscreen = true; skinned.Add(r); }
    }

    void LateUpdate() {
        if(started < 0f) { started = Time.time; return; }   // first frame: animation not applied yet
        bool last = Time.time - started >= MeasureSeconds;
        if(Time.time < nextSample && !last) return;           // sample every 0.1 s, not every frame
        nextSample = Time.time + .1f;
        float low = BlockIsland1Placement.LowestPoint(gameObject);
        if(!float.IsNaN(low)) samples.Add(low - transform.position.y);
        if(!last) return;

        samples.Sort();
        bool keep = samples.Count == 0 || samples[samples.Count - 1] - samples[0] > MaxSpread;
        BlockIsland1Placement.Remember(prefab, keep ? float.NaN : samples[samples.Count / 2]);
        // Position every copy already placed (this one included), honouring a manual height.
        BlockIsland1Figure.ApplyAll(prefab);
        foreach(var kv in culling) if(kv.Key != null) kv.Key.cullingMode = kv.Value;
        foreach(SkinnedMeshRenderer r in skinned) if(r != null) r.updateWhenOffscreen = false;
        Destroy(this);
    }
}
