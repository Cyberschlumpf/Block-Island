using UnityEngine;

// Stage 6.13.4 - reusable fantasy architectural shapes for Tanviir/imported worlds.
// Geometry stays inside one voxel so it can be streamed by InfiniteChunkMeshRenderer's
// generic Block.Build() fallback without introducing GameObjects per block.
public class TanviirArchitecturalBlock : Block {
    public enum Shape { Column, Arch, WindowFrame, GateFrame, RoofRidge, Buttress, BeamCross, PillarCap, Slab, WallPanel, Bench, Planter, LanternFrame, Table, Chair, FlowerBox, FountainBasin, LampPost, PergolaPost, BridgeRail }
    public Shape shape;
    public Face face;

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        BuildShape(builder,pos);
    }
    public override MeshBuilder Build() { MeshBuilder b=new MeshBuilder(); BuildShape(b,LocalPosition.zero); return b; }
    public override Face GetPreviewFace() { return face; }
    public override bool IsSolid() { return false; }
    public override bool IsAlpha() { return true; }

    void BoxAt(MeshBuilder b,LocalPosition p,Vector3 c,Vector3 s) {
        b.AddBox(Box.CenterSize(c,s),face.rect,p,face.materialID);
    }
    void BuildShape(MeshBuilder b,LocalPosition p) {
        if(face==null) return;
        switch(shape) {
            case Shape.Column:
                BoxAt(b,p,new Vector3(.5f,.10f,.5f),new Vector3(.72f,.20f,.72f));
                BoxAt(b,p,new Vector3(.5f,.50f,.5f),new Vector3(.42f,.68f,.42f));
                BoxAt(b,p,new Vector3(.5f,.90f,.5f),new Vector3(.68f,.20f,.68f)); break;
            case Shape.Arch:
                BoxAt(b,p,new Vector3(.16f,.45f,.5f),new Vector3(.24f,.90f,.30f));
                BoxAt(b,p,new Vector3(.84f,.45f,.5f),new Vector3(.24f,.90f,.30f));
                BoxAt(b,p,new Vector3(.28f,.86f,.5f),new Vector3(.20f,.28f,.30f));
                BoxAt(b,p,new Vector3(.72f,.86f,.5f),new Vector3(.20f,.28f,.30f));
                BoxAt(b,p,new Vector3(.50f,.94f,.5f),new Vector3(.28f,.12f,.30f)); break;
            case Shape.WindowFrame:
                BoxAt(b,p,new Vector3(.12f,.5f,.5f),new Vector3(.16f,1f,.18f));
                BoxAt(b,p,new Vector3(.88f,.5f,.5f),new Vector3(.16f,1f,.18f));
                BoxAt(b,p,new Vector3(.5f,.10f,.5f),new Vector3(.68f,.16f,.18f));
                BoxAt(b,p,new Vector3(.5f,.90f,.5f),new Vector3(.68f,.16f,.18f)); break;
            case Shape.GateFrame:
                BoxAt(b,p,new Vector3(.13f,.5f,.5f),new Vector3(.26f,1f,.34f));
                BoxAt(b,p,new Vector3(.87f,.5f,.5f),new Vector3(.26f,1f,.34f));
                BoxAt(b,p,new Vector3(.5f,.88f,.5f),new Vector3(.50f,.24f,.34f)); break;
            case Shape.RoofRidge:
                BoxAt(b,p,new Vector3(.5f,.20f,.5f),new Vector3(1f,.40f,1f));
                BoxAt(b,p,new Vector3(.5f,.53f,.5f),new Vector3(.72f,.28f,.72f));
                BoxAt(b,p,new Vector3(.5f,.76f,.5f),new Vector3(.42f,.20f,.42f));
                BoxAt(b,p,new Vector3(.5f,.91f,.5f),new Vector3(.18f,.10f,.18f)); break;
            case Shape.Buttress:
                BoxAt(b,p,new Vector3(.5f,.18f,.72f),new Vector3(.80f,.36f,.56f));
                BoxAt(b,p,new Vector3(.5f,.48f,.64f),new Vector3(.62f,.30f,.44f));
                BoxAt(b,p,new Vector3(.5f,.73f,.58f),new Vector3(.44f,.26f,.34f));
                BoxAt(b,p,new Vector3(.5f,.91f,.54f),new Vector3(.30f,.14f,.26f)); break;
            case Shape.BeamCross:
                BoxAt(b,p,new Vector3(.5f,.5f,.5f),new Vector3(.18f,1f,.18f));
                BoxAt(b,p,new Vector3(.5f,.5f,.5f),new Vector3(1f,.18f,.18f));
                BoxAt(b,p,new Vector3(.5f,.5f,.5f),new Vector3(.18f,.18f,1f)); break;
            case Shape.PillarCap:
                BoxAt(b,p,new Vector3(.5f,.18f,.5f),new Vector3(.50f,.36f,.50f));
                BoxAt(b,p,new Vector3(.5f,.50f,.5f),new Vector3(.68f,.28f,.68f));
                BoxAt(b,p,new Vector3(.5f,.78f,.5f),new Vector3(.86f,.28f,.86f)); break;
            // Block Island creative shapes, inspired by the useful construction ideas in Cube Life.
            // They are native Block Island/Tanviir blocks and use no Cube Life gameplay code/assets.
            case Shape.Slab:
                BoxAt(b,p,new Vector3(.5f,.25f,.5f),new Vector3(1f,.50f,1f)); break;
            case Shape.WallPanel:
                BoxAt(b,p,new Vector3(.5f,.5f,.5f),new Vector3(1f,1f,.16f)); break;
            case Shape.Bench:
                BoxAt(b,p,new Vector3(.5f,.48f,.5f),new Vector3(.92f,.18f,.62f));
                BoxAt(b,p,new Vector3(.18f,.22f,.5f),new Vector3(.14f,.44f,.48f));
                BoxAt(b,p,new Vector3(.82f,.22f,.5f),new Vector3(.14f,.44f,.48f));
                BoxAt(b,p,new Vector3(.5f,.76f,.82f),new Vector3(.92f,.42f,.14f)); break;
            case Shape.Planter:
                BoxAt(b,p,new Vector3(.5f,.10f,.5f),new Vector3(.90f,.20f,.90f));
                BoxAt(b,p,new Vector3(.10f,.35f,.5f),new Vector3(.20f,.50f,.90f));
                BoxAt(b,p,new Vector3(.90f,.35f,.5f),new Vector3(.20f,.50f,.90f));
                BoxAt(b,p,new Vector3(.5f,.35f,.10f),new Vector3(.60f,.50f,.20f));
                BoxAt(b,p,new Vector3(.5f,.35f,.90f),new Vector3(.60f,.50f,.20f)); break;
            case Shape.LanternFrame:
                BoxAt(b,p,new Vector3(.5f,.12f,.5f),new Vector3(.52f,.16f,.52f));
                BoxAt(b,p,new Vector3(.5f,.52f,.5f),new Vector3(.34f,.64f,.34f));
                BoxAt(b,p,new Vector3(.5f,.88f,.5f),new Vector3(.52f,.16f,.52f)); break;
            // Block Island 0.2 - additional peaceful creative-world furniture/architecture.
            case Shape.Table:
                BoxAt(b,p,new Vector3(.5f,.68f,.5f),new Vector3(.92f,.14f,.92f));
                BoxAt(b,p,new Vector3(.18f,.34f,.18f),new Vector3(.14f,.68f,.14f));
                BoxAt(b,p,new Vector3(.82f,.34f,.18f),new Vector3(.14f,.68f,.14f));
                BoxAt(b,p,new Vector3(.18f,.34f,.82f),new Vector3(.14f,.68f,.14f));
                BoxAt(b,p,new Vector3(.82f,.34f,.82f),new Vector3(.14f,.68f,.14f)); break;
            case Shape.Chair:
                BoxAt(b,p,new Vector3(.5f,.48f,.5f),new Vector3(.72f,.14f,.72f));
                BoxAt(b,p,new Vector3(.24f,.24f,.24f),new Vector3(.12f,.48f,.12f));
                BoxAt(b,p,new Vector3(.76f,.24f,.24f),new Vector3(.12f,.48f,.12f));
                BoxAt(b,p,new Vector3(.24f,.48f,.82f),new Vector3(.12f,.96f,.12f));
                BoxAt(b,p,new Vector3(.76f,.48f,.82f),new Vector3(.12f,.96f,.12f));
                BoxAt(b,p,new Vector3(.5f,.82f,.82f),new Vector3(.52f,.12f,.12f)); break;
            case Shape.FlowerBox:
                BoxAt(b,p,new Vector3(.5f,.08f,.5f),new Vector3(.94f,.16f,.48f));
                BoxAt(b,p,new Vector3(.08f,.28f,.5f),new Vector3(.16f,.40f,.48f));
                BoxAt(b,p,new Vector3(.92f,.28f,.5f),new Vector3(.16f,.40f,.48f));
                BoxAt(b,p,new Vector3(.5f,.28f,.30f),new Vector3(.68f,.40f,.10f));
                BoxAt(b,p,new Vector3(.5f,.28f,.70f),new Vector3(.68f,.40f,.10f)); break;
            case Shape.FountainBasin:
                BoxAt(b,p,new Vector3(.5f,.08f,.5f),new Vector3(1f,.16f,1f));
                BoxAt(b,p,new Vector3(.08f,.24f,.5f),new Vector3(.16f,.32f,1f));
                BoxAt(b,p,new Vector3(.92f,.24f,.5f),new Vector3(.16f,.32f,1f));
                BoxAt(b,p,new Vector3(.5f,.24f,.08f),new Vector3(.68f,.32f,.16f));
                BoxAt(b,p,new Vector3(.5f,.24f,.92f),new Vector3(.68f,.32f,.16f));
                BoxAt(b,p,new Vector3(.5f,.48f,.5f),new Vector3(.16f,.80f,.16f)); break;
            case Shape.LampPost:
                BoxAt(b,p,new Vector3(.5f,.08f,.5f),new Vector3(.52f,.16f,.52f));
                BoxAt(b,p,new Vector3(.5f,.48f,.5f),new Vector3(.14f,.80f,.14f));
                BoxAt(b,p,new Vector3(.5f,.82f,.5f),new Vector3(.42f,.12f,.42f));
                BoxAt(b,p,new Vector3(.5f,.94f,.5f),new Vector3(.26f,.18f,.26f)); break;
            case Shape.PergolaPost:
                BoxAt(b,p,new Vector3(.5f,.5f,.5f),new Vector3(.18f,1f,.18f));
                BoxAt(b,p,new Vector3(.5f,.91f,.5f),new Vector3(1f,.14f,.18f));
                BoxAt(b,p,new Vector3(.5f,.91f,.5f),new Vector3(.18f,.14f,1f)); break;
            case Shape.BridgeRail:
                BoxAt(b,p,new Vector3(.5f,.10f,.5f),new Vector3(1f,.16f,.20f));
                BoxAt(b,p,new Vector3(.08f,.45f,.5f),new Vector3(.14f,.70f,.18f));
                BoxAt(b,p,new Vector3(.50f,.45f,.5f),new Vector3(.14f,.70f,.18f));
                BoxAt(b,p,new Vector3(.92f,.45f,.5f),new Vector3(.14f,.70f,.18f));
                BoxAt(b,p,new Vector3(.5f,.76f,.5f),new Vector3(1f,.14f,.20f)); break;
        }
    }
}
