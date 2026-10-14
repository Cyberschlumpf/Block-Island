using UnityEngine;

// Tanviir texture pack shapes that are not full cubes: slabs, thin panels (doors, ladders,
// wall signs, vines), floor layers (carpet, snow, pressure plates, redstone), torches, cake, bed.
// The mesh is built in the FORWARD orientation; the infinite renderer rotates it by the voxel's
// direction (MergeBuiltBlock). Panels sit on the back (-z) side: FORWARD = north wall,
// BACKWARD = south wall, LEFT = east wall, RIGHT = west wall.
// UVs are projected from the box extents (BoxBuilder.ComputeTexCoord), so thin shapes show
// the matching part of the texture instead of a squashed copy.
public class TanviirPackBlock : Block {
    public enum Kind { SlabBottom, SlabTop, Panel, Carpet, SnowLayer, Flat, Trapdoor, Torch, Cake, Bed, Repeater }
    public Kind kind;
    public Face top, side, bottom;

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        BuildShape(builder, pos);
    }
    public override MeshBuilder Build() { MeshBuilder b = new MeshBuilder(); BuildShape(b, LocalPosition.zero); return b; }
    public override Face GetPreviewFace() { return kind == Kind.Panel || kind == Kind.Torch ? side : top; }
    public override bool IsSolid() { return kind == Kind.SlabBottom || kind == Kind.SlabTop || kind == Kind.Cake || kind == Kind.Bed; }
    public override bool IsAlpha() { return true; }
    public override int GetLightStep() { return 1; }

    void BuildShape(MeshBuilder b, LocalPosition p) {
        if(top == null || side == null) return;
        Face bot = bottom ?? top;
        const float px = 1f / 16f;
        switch(kind) {
            case Kind.SlabBottom: AddBox(b, p, new Vector3(0, 0, 0), new Vector3(1, .5f, 1), bot); break;
            case Kind.SlabTop:    AddBox(b, p, new Vector3(0, .5f, 0), new Vector3(1, 1, 1), bot); break;
            case Kind.Panel:      AddBox(b, p, new Vector3(0, 0, 0), new Vector3(1, 1, 3 * px), bot); break;
            case Kind.Carpet:     AddBox(b, p, new Vector3(0, 0, 0), new Vector3(1, px, 1), bot); break;
            case Kind.SnowLayer:  AddBox(b, p, new Vector3(0, 0, 0), new Vector3(1, 2 * px, 1), bot); break;
            case Kind.Flat:       AddFace(b, p, new Vector3(0, 0, 0), new Vector3(1, px * .25f, 1), 4, top); break;
            case Kind.Trapdoor:   AddBox(b, p, new Vector3(0, 0, 0), new Vector3(1, 3 * px, 1), bot); break;
            case Kind.Torch:      AddBox(b, p, new Vector3(7 * px, 0, 7 * px), new Vector3(9 * px, 10 * px, 9 * px), bot); break;
            case Kind.Cake:       AddBox(b, p, new Vector3(px, 0, px), new Vector3(15 * px, 8 * px, 15 * px), bot); break;
            case Kind.Bed:        AddBox(b, p, new Vector3(0, 0, 0), new Vector3(1, 9 * px, 1), bot); break;
            case Kind.Repeater:   AddBox(b, p, new Vector3(0, 0, 0), new Vector3(1, 2 * px, 1), bot); break;
        }
    }

    void AddBox(MeshBuilder b, LocalPosition p, Vector3 min, Vector3 max, Face bot) {
        for(int i = 0; i < 6; i++) AddFace(b, p, min, max, i, i == 4 ? top : i == 5 ? bot : side);
    }

    // Same vertex order, normals and UV projection as BoxBuilder.AddFace, but with one face per side.
    static void AddFace(MeshBuilder b, LocalPosition p, Vector3 min, Vector3 max, int iSide, Face face) {
        if(face == null) return;
        b.AddFaceIndices(face.materialID);
        Vector3[] vts = BoxBuilder.vertices[iSide];
        CubeSide side = BoxBuilder.sides[iSide];
        foreach(Vector3 v in vts) {
            Vector3 ver = new Vector3(Mathf.Lerp(min.x, max.x, Mathf.Clamp01(v.x)), Mathf.Lerp(min.y, max.y, Mathf.Clamp01(v.y)), Mathf.Lerp(min.z, max.z, Mathf.Clamp01(v.z)));
            b.AddVertex(p + ver);
            b.AddTexCoord(BoxBuilder.ComputeTexCoord(ver, side), face.rect);
        }
        b.AddFaceNormal(BoxBuilder.directions[iSide]);
        b.topology.Add(new BlockTopology(p, BlockTopologyType.Vertex, vts.Length));
    }
}
