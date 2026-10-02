public enum BlockTopologyType : byte {
    FrontFace = 0,
    BackFace = 1,
    RightFace = 2,
    LeftFace = 3,
    TopFace = 4,
    BottomFace = 5,
    Vertex = 6
}

public struct BlockTopology {

    public LocalPosition pos;
    public BlockTopologyType type;
    public ushort count;

    public BlockTopology(LocalPosition pos, BlockTopologyType type, int count) {
        this.pos = pos;
        this.type = type;
        this.count = (ushort) count;
    }

    public BlockTopology(LocalPosition pos, BlockTopologyType type) {
        this.pos = pos;
        this.type = type;
        this.count = 0;
    }

}