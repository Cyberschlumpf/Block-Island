using UnityEngine;

// Stage 6.12.8 - reconstructed Block Island special geometry.
public class BlockIslandLegacySpecialBlock : Block {
    public enum LegacyKind { GlassPanel, IronMesh, Ladder }
    public LegacyKind kind;
    public Face face;

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        // Infinite World has neighbour-aware implementations for connected panels.
        BuildPreview(builder, pos, block.direction);
    }

    public override MeshBuilder Build() {
        MeshBuilder b = new MeshBuilder();
        BuildPreview(b, LocalPosition.zero, BlockDirection.FORWARD);
        return b;
    }

    void BuildPreview(MeshBuilder b, LocalPosition p, BlockDirection dir) {
        if(face == null) return;
        if(kind == LegacyKind.GlassPanel) {
            b.AddBox(Box.CenterSize(new Vector3(.5f,.5f,.5f),new Vector3(.16f,1f,.16f)),face.rect,p,face.materialID);
            b.AddBox(Box.CenterSize(new Vector3(.5f,.5f,.5f),new Vector3(1f,.92f,.08f)),face.rect,p,face.materialID);
        } else if(kind == LegacyKind.IronMesh) {
            b.AddBox(Box.CenterSize(new Vector3(.5f,.5f,.5f),new Vector3(.08f,1f,.08f)),face.rect,p,face.materialID);
            b.AddBox(Box.CenterSize(new Vector3(.5f,.5f,.5f),new Vector3(1f,.92f,.035f)),face.rect,p,face.materialID);
        } else {
            AddLadder(b,p,face,dir);
        }
    }

    public static void AddLadder(MeshBuilder b, LocalPosition p, Face f, BlockDirection dir) {
        // Build facing FORWARD, then rely on MeshBuilder preview only for inventory. Infinite renderer
        // builds the same boxes with direction-aware coordinates.
        b.AddBox(Box.CenterSize(new Vector3(.20f,.5f,.92f),new Vector3(.10f,1f,.08f)),f.rect,p,f.materialID);
        b.AddBox(Box.CenterSize(new Vector3(.80f,.5f,.92f),new Vector3(.10f,1f,.08f)),f.rect,p,f.materialID);
        float[] ys={.16f,.38f,.62f,.84f};
        foreach(float y in ys) b.AddBox(Box.CenterSize(new Vector3(.5f,y,.90f),new Vector3(.68f,.08f,.10f)),f.rect,p,f.materialID);
    }

    public override Face GetPreviewFace() { return face; }
    public override bool IsSolid() { return false; }
    public override bool IsAlpha() { return true; }
    public override int GetLightStep() { return 1; }
}
