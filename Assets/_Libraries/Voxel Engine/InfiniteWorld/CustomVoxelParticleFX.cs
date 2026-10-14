using UnityEngine;
// Block Island 0.9.9.5d: lightweight visual-only particle layer for Custom Voxel blocks.
public sealed class CustomVoxelParticleFX : MonoBehaviour {
    ParticleSystem ps; Material mat;
    public void Setup(CustomVoxelBlock b, int seed) {
        if(b==null || b.particleFx==CustomVoxelBlock.ParticleFx.None) return;
        Random.InitState(seed);
        GameObject g=new GameObject("Custom FX - "+b.particleFx); g.transform.SetParent(transform,false); g.transform.localPosition=Vector3.zero;
        ps=g.AddComponent<ParticleSystem>(); var main=ps.main; var emission=ps.emission; var shape=ps.shape;
        main.loop=true; main.playOnAwake=true; main.simulationSpace=ParticleSystemSimulationSpace.Local; main.maxParticles=180;
        main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.5f); main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.45f); main.startSize=new ParticleSystem.MinMaxCurve(.025f,.07f); main.startColor=Color.white;
        emission.rateOverTime=18f*Mathf.Clamp(b.particleStrength,.2f,3f); shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=.22f;
        Color c=Color.white; bool gravity=false;
        switch(b.particleFx) {
            case CustomVoxelBlock.ParticleFx.Fire: c=new Color(1f,.32f,.03f,1); main.startLifetime=new ParticleSystem.MinMaxCurve(.3f,.75f); main.startSpeed=new ParticleSystem.MinMaxCurve(.25f,.7f); emission.rateOverTime=35f; break;
            case CustomVoxelBlock.ParticleFx.FireSmall:c=new Color(1f,.45f,.08f,1);main.startSize=new ParticleSystem.MinMaxCurve(.015f,.04f);emission.rateOverTime=22f;break;
            case CustomVoxelBlock.ParticleFx.FireLarge:c=new Color(1f,.22f,.02f,1);main.startSize=new ParticleSystem.MinMaxCurve(.05f,.12f);main.startSpeed=new ParticleSystem.MinMaxCurve(.35f,.9f);emission.rateOverTime=48f;break;
            case CustomVoxelBlock.ParticleFx.Smoke:c=new Color(.28f,.28f,.28f,.7f);main.startSize=new ParticleSystem.MinMaxCurve(.07f,.18f);main.startLifetime=new ParticleSystem.MinMaxCurve(1.2f,2.4f);break;
            case CustomVoxelBlock.ParticleFx.GreenSmoke:c=new Color(.15f,.75f,.22f,.65f);main.startSize=new ParticleSystem.MinMaxCurve(.07f,.18f);main.startLifetime=new ParticleSystem.MinMaxCurve(1.2f,2.4f);break;
            case CustomVoxelBlock.ParticleFx.BlueSmoke:c=new Color(.15f,.42f,1f,.65f);main.startSize=new ParticleSystem.MinMaxCurve(.07f,.18f);main.startLifetime=new ParticleSystem.MinMaxCurve(1.2f,2.4f);break;
            case CustomVoxelBlock.ParticleFx.PurpleSmoke:c=new Color(.65f,.2f,.9f,.65f);main.startSize=new ParticleSystem.MinMaxCurve(.07f,.18f);main.startLifetime=new ParticleSystem.MinMaxCurve(1.2f,2.4f);break;
            case CustomVoxelBlock.ParticleFx.Steam:c=new Color(.9f,.95f,1f,.5f);main.startSize=new ParticleSystem.MinMaxCurve(.06f,.14f);main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.5f);break;
            case CustomVoxelBlock.ParticleFx.Fountain:c=new Color(.25f,.65f,1f,.9f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.2f,2.2f);main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.3f);gravity=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=8;break;
            case CustomVoxelBlock.ParticleFx.DrippingWater:c=new Color(.3f,.7f,1f,.9f);main.startSpeed=new ParticleSystem.MinMaxCurve(.05f,.15f);gravity=true;emission.rateOverTime=8f;break;
            case CustomVoxelBlock.ParticleFx.WaterMist:c=new Color(.65f,.85f,1f,.35f);main.startSize=new ParticleSystem.MinMaxCurve(.05f,.12f);main.startSpeed=new ParticleSystem.MinMaxCurve(.1f,.3f);break;
            case CustomVoxelBlock.ParticleFx.Bubbles:c=new Color(.55f,.85f,1f,.7f);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.35f);main.startSize=new ParticleSystem.MinMaxCurve(.025f,.065f);break;
            case CustomVoxelBlock.ParticleFx.Embers:c=new Color(1f,.35f,.02f,1);main.startSize=new ParticleSystem.MinMaxCurve(.008f,.025f);main.startSpeed=new ParticleSystem.MinMaxCurve(.25f,.7f);emission.rateOverTime=16f;break;
            case CustomVoxelBlock.ParticleFx.Sparkles:c=Color.white;main.startSize=new ParticleSystem.MinMaxCurve(.01f,.035f);emission.rateOverTime=24f;break;
            case CustomVoxelBlock.ParticleFx.GoldenSparkles:c=new Color(1f,.75f,.15f,1);main.startSize=new ParticleSystem.MinMaxCurve(.01f,.035f);emission.rateOverTime=28f;break;
            case CustomVoxelBlock.ParticleFx.GlowingDust:c=new Color(1f,.85f,.35f,.9f);main.startSize=new ParticleSystem.MinMaxCurve(.015f,.045f);main.startSpeed=new ParticleSystem.MinMaxCurve(.03f,.12f);main.startLifetime=new ParticleSystem.MinMaxCurve(1.4f,2.8f);break;
            case CustomVoxelBlock.ParticleFx.Magic:c=new Color(.75f,.2f,1f,1);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.5f);break;
            case CustomVoxelBlock.ParticleFx.BlueMagic:c=new Color(.15f,.45f,1f,1);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.5f);break;
            case CustomVoxelBlock.ParticleFx.PurpleMagic:c=new Color(.7f,.15f,1f,1);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.5f);break;
            case CustomVoxelBlock.ParticleFx.MagicSwirl:c=new Color(.35f,1f,.85f,1);shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.3f;main.startSpeed=.2f;break;
            case CustomVoxelBlock.ParticleFx.MagicBurst:c=new Color(1f,.25f,.85f,1);main.startSpeed=new ParticleSystem.MinMaxCurve(.4f,1.2f);emission.rateOverTime=8f;break;
            case CustomVoxelBlock.ParticleFx.SnowDust:c=Color.white;main.startSpeed=new ParticleSystem.MinMaxCurve(.02f,.12f);main.startLifetime=new ParticleSystem.MinMaxCurve(1.5f,3f);gravity=true;break;
            case CustomVoxelBlock.ParticleFx.Ash:c=new Color(.25f,.22f,.2f,.75f);main.startSize=new ParticleSystem.MinMaxCurve(.01f,.04f);main.startLifetime=new ParticleSystem.MinMaxCurve(1.5f,3f);break;
            case CustomVoxelBlock.ParticleFx.Leaves:c=new Color(.3f,.75f,.18f,1);main.startSize=new ParticleSystem.MinMaxCurve(.025f,.06f);main.startSpeed=new ParticleSystem.MinMaxCurve(.15f,.45f);break;
            case CustomVoxelBlock.ParticleFx.ElectricSparks:c=new Color(.45f,.75f,1f,1);main.startLifetime=new ParticleSystem.MinMaxCurve(.08f,.25f);main.startSpeed=new ParticleSystem.MinMaxCurve(.8f,1.8f);main.startSize=new ParticleSystem.MinMaxCurve(.008f,.025f);emission.rateOverTime=20f;break;
            case CustomVoxelBlock.ParticleFx.EnergyPulse:c=new Color(.2f,1f,.8f,.9f);main.startSize=new ParticleSystem.MinMaxCurve(.03f,.09f);main.startSpeed=new ParticleSystem.MinMaxCurve(.25f,.7f);shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.3f;break;
        }
        main.startColor=c; if(gravity) main.gravityModifier=.75f;
        var rend=ps.GetComponent<ParticleSystemRenderer>(); Shader sh=Shader.Find("Particles/Standard Unlit"); if(sh==null)sh=Shader.Find("Sprites/Default"); if(sh!=null){mat=new Material(sh);mat.color=c;rend.material=mat;}
        ps.Play();
    }
}
