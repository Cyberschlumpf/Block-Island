using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections.Generic;

// One placed copy of a prefab block (or the importer preview). Applies the block's settings:
// size, animation (own Animator controller, a chosen clip, or none; speed; loop) and stands the
// model on the block floor (lowest rendered point = floor + height). Settings changes reach every
// copy through ApplyAll.
public class PrefabBlockInstance : MonoBehaviour {
    public static readonly List<PrefabBlockInstance> All = new List<PrefabBlockInstance>();

    public PrefabBlockLibrary.Entry entry;
    public float floorY;          // world Y the model stands on
    Vector3 baseScale;
    bool initialised;
    int snapFrames;

    PlayableGraph graph; AnimationClipPlayable clipPlayable; bool hasGraph; AnimationClip playing;

    public void Init(PrefabBlockLibrary.Entry e, float floor, bool preview) {
        entry = e; floorY = floor;
        if(!initialised) { baseScale = transform.localScale; initialised = true; }
        foreach(SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>(true)) s.updateWhenOffscreen = true;   // bounds follow the pose
        Apply();
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
    void OnDestroy() { StopGraph(); }

    public static void ApplyAll(PrefabBlockLibrary.Entry e) { foreach(PrefabBlockInstance p in All.ToArray()) if(p != null && p.entry == e) p.Apply(); }

    public void Apply() {
        if(entry == null) return;
        transform.localScale = baseScale * Mathf.Max(.01f, entry.scale);
        ApplyAnimation();
        snapFrames = 2;   // measure after the animation has posed the model
    }

    void LateUpdate() {
        if(snapFrames > 0 && --snapFrames == 0) Snap();
        if(hasGraph && playing != null && playing.length > 0f) {
            double t = clipPlayable.GetTime();
            if(entry.loop) { if(t > playing.length) clipPlayable.SetTime(t % playing.length); }
            else if(t >= playing.length) { clipPlayable.SetTime(playing.length); clipPlayable.SetSpeed(0); }
        }
    }

    void Snap() {
        float low = BlockIsland1Placement.LowestPoint(gameObject);
        if(float.IsNaN(low)) return;
        Vector3 p = transform.position; p.y += floorY + entry.height - low; transform.position = p;
    }

    void ApplyAnimation() {
        StopGraph();
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        Animation[] legacy = GetComponentsInChildren<Animation>(true);
        string mode = entry.animation ?? PrefabBlockLibrary.StandardAnimation;
        float speed = Mathf.Max(0f, entry.speed);

        if(mode == PrefabBlockLibrary.NoAnimation) {
            foreach(Animator a in animators) a.enabled = false;
            foreach(Animation an in legacy) an.Stop();
            return;
        }
        if(mode == PrefabBlockLibrary.StandardAnimation) {
            foreach(Animator a in animators) { a.enabled = true; a.speed = speed; }
            foreach(Animation an in legacy) {
                foreach(AnimationState s in an) { s.speed = speed; s.wrapMode = entry.loop ? WrapMode.Loop : WrapMode.ClampForever; }
                if(an.clip != null) an.Play();
            }
            return;
        }
        // A chosen clip: legacy Animation plays it by name, an Animator gets a playable graph with only that clip.
        foreach(Animation an in legacy) {
            AnimationState s = an[mode];
            if(s == null) continue;
            s.speed = speed; s.wrapMode = entry.loop ? WrapMode.Loop : WrapMode.ClampForever;
            an.Play(mode);
            return;
        }
        foreach(Animator a in animators) {
            if(a.runtimeAnimatorController == null) continue;
            foreach(AnimationClip c in a.runtimeAnimatorController.animationClips) {
                if(c == null || c.name != mode) continue;
                a.enabled = true;
                graph = PlayableGraph.Create(name + " animation");
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var output = AnimationPlayableOutput.Create(graph, "Prefab", a);
                clipPlayable = AnimationClipPlayable.Create(graph, c);
                clipPlayable.SetSpeed(speed);
                output.SetSourcePlayable(clipPlayable);
                graph.Play();
                hasGraph = true; playing = c;
                return;
            }
        }
        Debug.LogWarning("PREFAB BLOCKS: animation '" + mode + "' not found on " + name);
    }

    void StopGraph() {
        if(hasGraph && graph.IsValid()) graph.Destroy();
        hasGraph = false; playing = null;
    }
}
