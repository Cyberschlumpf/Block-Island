using System.IO;
using UnityEditor;
using UnityEngine;

// Renders every Block Island 1.0 slot (block mesh or figure prefab) into one overview image.
// Menu: Block Island > 1.0 Blockübersicht rendern, or batchmode -executeMethod BlockIsland1ContactSheet.Run
public static class BlockIsland1ContactSheet {
    const int Cell = 128, Cols = 20;

    [MenuItem("Block Island/1.0 Blockübersicht rendern")]
    public static void Run() {
        GameObject setGo = Object.Instantiate(Resources.Load<GameObject>("BlockSet"));
        BlockSet set = setGo.GetComponent<BlockSet>();
        if(BlockSet.instance != set) set.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
        Block[] slots = BlockSet.blockIsland1Slots;

        GameObject camGo = new GameObject("SheetCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(.18f, .2f, .24f, 1f);
        cam.fieldOfView = 30f;
        GameObject lightGo = new GameObject("SheetLight");
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientLight = new Color(.55f, .55f, .55f);

        int rows = (slots.Length + Cols - 1) / Cols;
        Texture2D sheet = new Texture2D(Cols * Cell, rows * Cell, TextureFormat.RGB24, false);
        RenderTexture rt = new RenderTexture(Cell, Cell, 24);
        cam.targetTexture = rt;
        Texture2D cellTex = new Texture2D(Cell, Cell, TextureFormat.RGB24, false);

        for(int i = 0; i < slots.Length; i++) {
            GameObject subject = Build(slots[i]);
            if(subject != null) {
                subject.transform.position = new Vector3(0f, 0f, 0f);
                Bounds b = new Bounds(subject.transform.position, Vector3.zero); bool any = false;
                foreach(Renderer r in subject.GetComponentsInChildren<Renderer>()) { if(!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if(!any) b = new Bounds(subject.transform.position, Vector3.one);
                float size = Mathf.Max(b.extents.magnitude, .5f);
                cam.transform.position = b.center + new Vector3(1f, .8f, -1.2f).normalized * size / Mathf.Sin(cam.fieldOfView * .5f * Mathf.Deg2Rad);
                cam.transform.LookAt(b.center);
                cam.nearClipPlane = .01f; cam.farClipPlane = size * 20f + 10f;
            }
            cam.Render();
            RenderTexture.active = rt;
            cellTex.ReadPixels(new Rect(0, 0, Cell, Cell), 0, 0); cellTex.Apply();
            RenderTexture.active = null;
            int cx = i % Cols, cy = rows - 1 - i / Cols;
            sheet.SetPixels(cx * Cell, cy * Cell, Cell, Cell, cellTex.GetPixels());
            if(subject != null) Object.DestroyImmediate(subject);
        }
        sheet.Apply();
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Migration 1.0 nach 2.0/blockuebersicht_1.0.png"));
        File.WriteAllBytes(path, sheet.EncodeToPNG());
        Object.DestroyImmediate(camGo); Object.DestroyImmediate(lightGo); Object.DestroyImmediate(setGo);
        Debug.Log("BLOCK ISLAND 1.0 contact sheet: " + path);
    }

    static GameObject Build(Block block) {
        if(block == null) return null;
        GameObjectBlock gob = block as GameObjectBlock;
        if(gob != null) return gob.gameObject != null ? (GameObject)Object.Instantiate(gob.gameObject) : null;
        MeshBuilder mb = null;
        try { mb = block.Build(); } catch { }
        if(mb == null) return null;
        GameObject go = new GameObject(block.name, typeof(MeshFilter), typeof(MeshRenderer));
        Mesh mesh = mb.ToMesh(null);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterials = mb.GetMaterials(BlockSet.instance.GetMaterials());
        return go;
    }
}
