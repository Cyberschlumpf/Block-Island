using UnityEngine;
using System.Collections;

public class GroundBlock : Block {

    private static GroundBlock dirt;
	
	public Face side, top, bottom, wings;

    public override void Init(BlockSet blockSet, int blockId) {
        base.Init(blockSet, blockId);


        if (dirt == null) dirt = blockSet.FindBlock<GroundBlock>(Blocks.dirt);
    }

    public override void OnNeighborBlockChanged(Vector3i pos, Vector3i neighborPos) {
        if (pos + Vector3i.up == neighborPos && name.Equals(Blocks.grass, System.StringComparison.CurrentCultureIgnoreCase)) {
            Map map = Map.instance;

            Block neighborBlock = map.GetBlock(neighborPos).block;
            if (neighborBlock is GroundBlock || (neighborBlock is CubeBlock && !neighborBlock.IsAlpha())) {
                map.SetBlock( pos, dirt );
            }
        }
    }

	
	public Face GetFace(CubeSide face) {
        switch(face) {
            case CubeSide.Front: return side;
            case CubeSide.Back: return side;

            case CubeSide.Right: return side;
            case CubeSide.Left: return side;

            case CubeSide.Top: return top;
            case CubeSide.Bottom: return bottom;
        }
		return null;
	}


    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        GroundBuilder.Build( builder, block, pos, index, chunk );
	}
	
	public override MeshBuilder Build() {
		return GroundBuilder.Build( this );
	}

    public override Face GetPreviewFace() {
        return side;
    }
	
	public override bool IsAlpha() {
		return false;
	}
	
}
