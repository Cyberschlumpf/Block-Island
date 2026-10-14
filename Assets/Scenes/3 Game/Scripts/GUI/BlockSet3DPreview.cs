using UnityEngine;
using System.Collections.Generic;

// 0.9.9.7r - cached real-geometry thumbnails for BlockSet + project-owned unlit preview shader.
// Runtime voxel shaders also expect world/lightmap state; using them in the isolated preview camera
// produced the cyan thumbnails. Preview-only material clones keep the block's CURRENT texture/atlas
// while leaving the real world materials completely untouched.
public static class BlockSet3DPreview {
    static GameObject root; static Camera cam; static Light key,fill; static MeshFilter filter; static MeshRenderer renderer;
    static Mesh mesh; static Material emptyMat; static readonly Dictionary<int,RenderTexture> cache=new Dictionary<int,RenderTexture>();
    static readonly List<Material> previewMats=new List<Material>();
    static readonly Dictionary<Material,Material> previewMatCache=new Dictionary<Material,Material>();
    static void Ensure(){
        if(root!=null)return;
        root=new GameObject("BlockSet 3D Preview");root.hideFlags=HideFlags.HideAndDontSave;root.layer=31;
        filter=root.AddComponent<MeshFilter>();renderer=root.AddComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
        GameObject c=new GameObject("BlockSet Preview Camera");c.hideFlags=HideFlags.HideAndDontSave;c.layer=31;cam=c.AddComponent<Camera>();cam.enabled=false;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.035f,.04f,1);cam.fieldOfView=30;cam.nearClipPlane=.05f;cam.farClipPlane=20;
        GameObject k=new GameObject("BlockSet Preview Key");k.hideFlags=HideFlags.HideAndDontSave;k.layer=31;key=k.AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.05f;key.cullingMask=1<<31;k.transform.rotation=Quaternion.Euler(42,-38,0);
        GameObject f=new GameObject("BlockSet Preview Fill");f.hideFlags=HideFlags.HideAndDontSave;f.layer=31;fill=f.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.42f;fill.cullingMask=1<<31;f.transform.rotation=Quaternion.Euler(320,145,0);
        Shader sh=Shader.Find("Standard");if(sh==null)sh=Shader.Find("Unlit/Color");if(sh!=null){emptyMat=new Material(sh);emptyMat.color=new Color(.22f,.27f,.29f,1);emptyMat.hideFlags=HideFlags.HideAndDontSave;}
    }
    static Mesh EmptyCube(){ GameObject g=GameObject.CreatePrimitive(PrimitiveType.Cube);Mesh m=Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(g);m.hideFlags=HideFlags.HideAndDontSave;return m; }
    // 0.9.9.7r: one tiny project-owned preview shader.  This avoids depending on the
    // voxel world-lighting shaders OR Unity built-in preview shaders.  The block mesh keeps
    // its original UVs and material/submesh order; only lighting is removed for thumbnails.
    static int AnimationMode(Block b, Material src){
        string bn=b!=null?(b.name??"").ToLowerInvariant():"";
        string mn=src!=null?(src.name??"").ToLowerInvariant():"";
        string sn=(src!=null&&src.shader!=null)?src.shader.name.ToLowerInvariant():"";
        string all=bn+" "+mn+" "+sn;
        // 1.14: identify fluids by block type first. Names/material labels are not reliable
        // after BlockSet loading/importing and caused Water to be cached as a static preview.
        if(b is FluidBlock)return 1;
        if(all.Contains("water")||all.Contains("wasser"))return 1;
        // Cross/plant blocks in the set often share an atlas material whose shader name is not Grass.
        if(all.Contains("grass")||all.Contains("gras")||all.Contains("flower")||all.Contains("blume")||all.Contains("poppy")||all.Contains("tulip")||all.Contains("orchid")||all.Contains("dandelion")||all.Contains("fern")||all.Contains("reed")||all.Contains("bush")||all.Contains("plant")||all.Contains("flora"))return 2;
        if(b is CrossBlock)return 2;
        return 0;
    }
    static Material PreviewMaterial(Material src, Block b){
        if(src==null)return emptyMat;
        Texture tex=src.mainTexture;
        if(tex==null && src.HasProperty("_MainTex"))tex=src.GetTexture("_MainTex");
        Shader sh=Shader.Find("VoxelBox/PreviewUnlit");
        if(sh==null)return src;
        // Do not share the preview clone solely by source material: water/plants can use the same atlas
        // material as static blocks while requiring a different animation mode.
        Material m=new Material(sh);m.name="Preview - "+src.name;m.hideFlags=HideFlags.HideAndDontSave;
        if(tex!=null)m.SetTexture("_MainTex",tex);
        string sn=src.shader!=null?src.shader.name.ToLowerInvariant():"";
        float anim=AnimationMode(b,src);
        if(m.HasProperty("_AnimMode"))m.SetFloat("_AnimMode",anim);
        previewMats.Add(m);return m;
    }
    static Material[] PreviewMaterials(Material[] src, Block b){
        if(src==null||src.Length==0)return new Material[0];
        Material[] r=new Material[src.Length];for(int i=0;i<src.Length;i++)r[i]=PreviewMaterial(src[i],b);return r;
    }
    public static Texture Get(BlockSet set,Block b){
        if(set==null||b==null)return null;Ensure();int id=b.blockID;RenderTexture rt; bool hasRt=cache.TryGetValue(id,out rt)&&rt!=null;
        MeshBuilder mb=null;try{mb=b.Build();}catch(System.Exception e){Debug.LogWarning("BlockSet preview: "+e.Message);}Material[] mats=null;
        if(mb!=null&&mb.vertices.Count>0){
            mesh=mb.ToMesh(mesh);
            // 1.14: CPU-side water surface deformation. This deliberately does not depend on
            // shader animation support: the top vertices of the REAL 3D block mesh are moved
            // every repaint, so the water block visibly waves in the Game scene/hotbar.
            // Side/bottom vertices stay fixed, preserving the recognisable 3D cube preview.
            if(b is FluidBlock && mesh!=null) {
                Vector3[] vv=mesh.vertices;
                if(vv!=null && vv.Length>0) {
                    float maxY=vv[0].y; for(int vi=1;vi<vv.Length;vi++) if(vv[vi].y>maxY) maxY=vv[vi].y;
                    float t=Time.time;
                    for(int vi=0;vi<vv.Length;vi++) if(vv[vi].y>maxY-0.02f) {
                        Vector3 v=vv[vi];
                        v.y += Mathf.Sin(v.x*5.1f+t*2.4f)*0.055f + Mathf.Sin(v.z*4.3f+t*1.7f)*0.040f;
                        vv[vi]=v;
                    }
                    mesh.vertices=vv; mesh.RecalculateBounds();
                }
            }
            // Same two calls used by BlockDesignerGUI.Rebuild3DPreview().
            Material[] sourceMats=mb.GetMaterials(set.GetMaterials());
            bool animated=false;
            if(sourceMats!=null)for(int i=0;i<sourceMats.Length;i++){ if(AnimationMode(b,sourceMats[i])!=0){animated=true;break;} }
            if(hasRt&&!animated)return rt;
            mats=PreviewMaterials(sourceMats,b);
        }
        else return null;
        if(mesh==null)return null;filter.sharedMesh=mesh;renderer.sharedMaterials=mats;Bounds bo=mesh.bounds;root.transform.position=-bo.center;
        if(!hasRt){rt=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);rt.hideFlags=HideFlags.HideAndDontSave;rt.Create();cache[id]=rt;}cam.targetTexture=rt;
        Quaternion q=Quaternion.Euler(22,35,0);float radius=Mathf.Max(.55f,bo.extents.magnitude);Vector3 dir=q*new Vector3(0,0,-1);cam.transform.position=dir*(radius*3.25f);cam.transform.LookAt(Vector3.zero,Vector3.up);cam.Render();cam.targetTexture=null;return rt;
    }
    public static void Clear(){foreach(var p in cache)if(p.Value!=null){p.Value.Release();Object.Destroy(p.Value);}cache.Clear();foreach(Material m in previewMats)if(m!=null)Object.Destroy(m);previewMats.Clear();previewMatCache.Clear();}
}
