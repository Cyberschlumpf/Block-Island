using UnityEngine;

// Unity 6 replacement for the Unity 4.x legacy fire particle renderer used by the old BlockSet.
// Attached only to streamed GameObjectBlock fire instances; preserves the original prefab/light.
public sealed class InfiniteFireVisual : MonoBehaviour {
    static Material flameMaterial;

    public static void Ensure(GameObject root) {
        if(root==null || root.GetComponent<InfiniteFireVisual>()!=null) return;
        root.AddComponent<InfiniteFireVisual>().CreateFlames();
    }

    void CreateFlames() {
        // If a modern ParticleSystem already exists, do not duplicate it.
        if(GetComponentInChildren<ParticleSystem>(true)!=null) return;
        CreateLayer("FlameOuter", new Color(1f,.28f,.02f,.90f), .18f, .48f, 18f, .45f);
        CreateLayer("FlameInner", new Color(1f,.86f,.18f,.95f), .11f, .32f, 14f, .34f);
    }

    void CreateLayer(string layerName, Color color, float startSize, float lifetime, float rate, float speed) {
        GameObject go=new GameObject(layerName);
        go.transform.SetParent(transform,false);
        go.transform.localPosition=new Vector3(0f,-.22f,0f);
        ParticleSystem ps=go.AddComponent<ParticleSystem>();
        var main=ps.main;
        main.loop=true; main.playOnAwake=true; main.startLifetime=lifetime; main.startSpeed=speed;
        main.startSize=startSize; main.startColor=color; main.simulationSpace=ParticleSystemSimulationSpace.Local;
        main.maxParticles=64;
        var emission=ps.emission; emission.rateOverTime=rate;
        var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=12f; shape.radius=.12f;
        var size=ps.sizeOverLifetime; size.enabled=true;
        AnimationCurve curve=new AnimationCurve(new Keyframe(0f,.45f),new Keyframe(.25f,1f),new Keyframe(1f,.05f));
        size.size=new ParticleSystem.MinMaxCurve(1f,curve);
        var col=ps.colorOverLifetime; col.enabled=true;
        Gradient g=new Gradient();
        g.SetKeys(new[]{new GradientColorKey(color,0f),new GradientColorKey(color,.65f),new GradientColorKey(new Color(.35f,.03f,0f),1f)},
                  new[]{new GradientAlphaKey(0f,0f),new GradientAlphaKey(color.a,.12f),new GradientAlphaKey(.75f,.7f),new GradientAlphaKey(0f,1f)});
        col.color=new ParticleSystem.MinMaxGradient(g);
        var noise=ps.noise; noise.enabled=true; noise.strength=.16f; noise.frequency=.9f; noise.scrollSpeed=.25f;
        ParticleSystemRenderer r=go.GetComponent<ParticleSystemRenderer>();
        r.renderMode=ParticleSystemRenderMode.Billboard;
        r.material=GetFlameMaterial();
        ps.Play(true);
    }

    static Material GetFlameMaterial() {
        if(flameMaterial!=null) return flameMaterial;
        Shader shader=Shader.Find("Particles/Standard Unlit");
        if(shader==null) shader=Shader.Find("Legacy Shaders/Particles/Additive");
        if(shader==null) shader=Shader.Find("Sprites/Default");
        flameMaterial=new Material(shader); flameMaterial.name="Infinite Fire Runtime Material";
        if(flameMaterial.HasProperty("_Color")) flameMaterial.SetColor("_Color",Color.white);
        return flameMaterial;
    }
}
