using UnityEditor;
using UnityEngine;

// Copies the weather content used by BlockIslandWeather into Resources/Weather:
// rain and snow particles from the "Time of Day & Weather System" asset, plus the 1.0 rainbow texture and sounds
// (copied there by hand). Menu: Block Island/Wetter einrichten.  Batch: -executeMethod BlockIslandWeatherSetup.Setup
public static class BlockIslandWeatherSetup {
    public const string Folder = "Assets/Resources/Weather";
    const string Source = "Assets/TimeOfDay&WeatherSystem/Prefabs/";

    [MenuItem("Block Island/Wetter einrichten")]
    public static void Setup() {
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Weather");
        foreach(string n in new[] { "RainParticle", "SnowParticle" }) {
            string dst = Folder + "/" + n + ".prefab";
            AssetDatabase.DeleteAsset(dst);
            if(!AssetDatabase.CopyAsset(Source + n + ".prefab", dst)) { Debug.LogError("WEATHER SETUP: cannot copy " + n); continue; }
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(dst);
            foreach(ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true)) {
                var m = ps.main; var sh = ps.shape; var em = ps.emission; var r = ps.GetComponent<ParticleSystemRenderer>();
                Debug.Log("WEATHER SETUP: " + n + "/" + ps.name + " pos " + ps.transform.localPosition + " max " + m.maxParticles + " rate " + em.rateOverTimeMultiplier
                    + " life " + m.startLifetimeMultiplier + " speed " + m.startSpeedMultiplier + " size " + m.startSizeMultiplier + " gravity " + m.gravityModifierMultiplier
                    + " space " + m.simulationSpace + " shape " + sh.shapeType + " scale " + sh.scale + " radius " + sh.radius + " mat " + (r != null && r.sharedMaterial != null ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "-")
                    + " renderMode " + (r != null ? r.renderMode.ToString() : "-"));
            }
        }
        // Particle materials (the asset's own ones use the opaque Standard shader) and the 1.0 rainbow.
        Texture2D dot = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        Material("Rain", "Legacy Shaders/Particles/Alpha Blended", dot, new Color(.66f, .74f, .86f, .5f));
        Material("Snow", "Legacy Shaders/Particles/Alpha Blended", dot, new Color(.95f, .95f, .98f, .85f));
        var tex = Folder + "/Rainbow.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(tex);
        if(imp != null) { imp.wrapMode = TextureWrapMode.Clamp; imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
        // Sprites/Default has no fog: the rainbow stands far away and Unity fog would turn it into a white band.
        Material("Rainbow", "Sprites/Default", AssetDatabase.LoadAssetAtPath<Texture2D>(tex), Color.white);
        AssetDatabase.SaveAssets();
    }

    static void Material(string name, string shader, Texture tex, Color tint) {
        string path = Folder + "/" + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        Shader sh = Shader.Find(shader);
        if(sh == null) { Debug.LogError("WEATHER SETUP: shader missing " + shader); return; }
        var m = new Material(sh) { mainTexture = tex };
        if(m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint); else m.color = tint;
        AssetDatabase.CreateAsset(m, path);
    }
}
