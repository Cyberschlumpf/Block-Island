using UnityEngine;
using UnityEditor;
using System.Text;

// One-time setup for the portal assets (menu Block Island/Portale einrichten, or batch -executeMethod PortalAssetsSetup.Run):
// - Hovl Studio "Portal Effects" ships Shader Graph materials (URP/HDRP only). They are switched to the built-in stand-in
//   shader "Block Island/Hovl Particles"; Noise63bd goes to Hovl's own built-in BlendDistort shader.
// - Creates Resources/Portal/PortalStars.mat (soft additive star for the transition).
// - Logs the "Portal space" effect's size so the portal can scale it.
public static class PortalAssetsSetup {
    [MenuItem("Block Island/Portale einrichten")]
    public static void Run() {
        Shader stand = Shader.Find("Block Island/Hovl Particles");
        Shader blendDistort = Shader.Find("Hovl/Particles/BlendDistort");
        if(stand == null) { Debug.LogError("PORTAL SETUP: shader Block Island/Hovl Particles missing"); return; }
        int changed = 0;
        foreach(string g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Hovl Studio" })) {
            string path = AssetDatabase.GUIDToAssetPath(g);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m == null) continue;
            string sn = m.shader != null ? m.shader.name : "";
            bool broken = m.shader == null || sn.StartsWith("Shader Graphs/") || sn == "Hidden/InternalErrorShader" || !m.shader.isSupported || sn == "Hovl/Particles/BlendDistort";
            if(!broken) continue;
            Texture noise = m.HasProperty("_Noise") ? m.GetTexture("_Noise") : null;
            m.shader = stand;
            // FlareFX only has a noise texture: use it as the main texture too.
            if(m.shader == stand && m.GetTexture("_MainTex") == null && noise != null) m.SetTexture("_MainTex", noise);
            EditorUtility.SetDirty(m); changed++;
        }
        Debug.Log("PORTAL SETUP: " + changed + " Hovl materials switched to built-in shaders");

        const string starsPath = "Assets/Resources/Portal/PortalStars.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(starsPath) == null) {
            Shader add = Shader.Find("Legacy Shaders/Particles/Additive");
            var stars = new Material(add) { name = "PortalStars" };
            stars.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Hovl Studio/HSFiles/Textures/Glow1.png");
            stars.SetColor("_TintColor", new Color(.5f, .5f, .5f, .5f));
            AssetDatabase.CreateAsset(stars, starsPath);
            Debug.Log("PORTAL SETUP: created " + starsPath);
        }
        AssetDatabase.SaveAssets();

        var space = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Portal/Portal space.prefab");
        if(space != null) {
            var sb = new StringBuilder("PORTAL SETUP: Portal space parts:");
            foreach(Renderer r in space.GetComponentsInChildren<Renderer>(true))
                sb.Append("\n  " + r.GetType().Name + " '" + r.name + "' pos " + r.transform.position + " rot " + r.transform.eulerAngles + " scale " + r.transform.lossyScale
                    + " mats " + string.Join(",", System.Array.ConvertAll(r.sharedMaterials, x => x != null ? x.name + "[" + x.shader.name + "]" : "null")));
            foreach(ParticleSystem p in space.GetComponentsInChildren<ParticleSystem>(true))
                sb.Append("\n  PS '" + p.name + "' shape " + p.shape.shapeType + " radius " + p.shape.radius + " scale " + p.shape.scale + " startSize " + p.main.startSize.constantMax + " space " + p.main.simulationSpace + " scaling " + p.main.scalingMode);
            foreach(Light l in space.GetComponentsInChildren<Light>(true)) sb.Append("\n  Light '" + l.name + "' range " + l.range + " intensity " + l.intensity);
            foreach(MonoBehaviour mb in space.GetComponentsInChildren<MonoBehaviour>(true)) sb.Append("\n  Script " + (mb != null ? mb.GetType().Name : "missing"));
            Debug.Log(sb.ToString());
        }
    }
}
