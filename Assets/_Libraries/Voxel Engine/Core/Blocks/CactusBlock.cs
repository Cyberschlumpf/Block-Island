using UnityEngine;
using System.Collections;

public class CactusBlock : Block {
	
	public Face side, top, bottom;


    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        CactusBuilder.Build( builder, block, pos, index, chunk );
    }

    public override MeshBuilder Build() {
        return CactusBuilder.Build( this );
    }
	
	public override Face GetPreviewFace() {
		return side;
	}
	
	public Face GetFace(CubeSide face) {
		switch (face) {
			case CubeSide.Front: return side;
			case CubeSide.Back: return side;
			
			case CubeSide.Right: return side;
			case CubeSide.Left: return side;
			
			case CubeSide.Top: return top;
			case CubeSide.Bottom: return bottom;
		}
		return null;
	}
	
}
