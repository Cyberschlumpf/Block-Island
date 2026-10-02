using UnityEngine;

// Stage 6.13.1 - compact low-poly sphere that occupies one voxel cell.
public class SphereBlock : Block {
    public Face face;
    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) { BuildSphere(builder,pos,block.direction); }
    public override MeshBuilder Build() { MeshBuilder b=new MeshBuilder(); BuildSphere(b,LocalPosition.zero,BlockDirection.FORWARD); return b; }
    public override Face GetPreviewFace() { return face; }
    public override bool IsAlpha() { return true; }

    void BuildSphere(MeshBuilder mb, LocalPosition pos, BlockDirection dir) {
        const int slices=12, rings=6;
        Vector3 center=new Vector3(.5f,.5f,.5f); float radius=.48f;
        Vector3[] v=new Vector3[(rings+1)*(slices+1)];
        Vector3[] n=new Vector3[v.Length]; Vector2[] uv=new Vector2[v.Length];
        int k=0;
        for(int r=0;r<=rings;r++) {
            float vr=r/(float)rings; float phi=Mathf.PI*vr;
            for(int s=0;s<=slices;s++) {
                float u=s/(float)slices; float th=Mathf.PI*2f*u;
                Vector3 normal=new Vector3(Mathf.Sin(phi)*Mathf.Cos(th),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(th));
                v[k]=center+normal*radius; n[k]=normal; uv[k]=new Vector2(face.rect.x+u*face.rect.width,face.rect.y+(1f-vr)*face.rect.height); k++;
            }
        }
        int[] tris=new int[rings*slices*6]; int ti=0;
        for(int r=0;r<rings;r++) for(int s=0;s<slices;s++) {
            int a=r*(slices+1)+s,b=a+1,c=a+(slices+1),d=c+1;
            tris[ti++]=a;tris[ti++]=c;tris[ti++]=b; tris[ti++]=b;tris[ti++]=c;tris[ti++]=d;
        }
        mb.AddIndices(tris,face.materialID); mb.AddVertices(v,pos,dir); mb.AddNormals(n,dir); mb.AddTexCoords(uv,new Rect(0,0,1,1));
        mb.topology.Add(new BlockTopology(pos,BlockTopologyType.Vertex,v.Length));
    }
}
