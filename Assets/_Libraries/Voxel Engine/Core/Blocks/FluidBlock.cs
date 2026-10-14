using UnityEngine;
using System.Collections;

public class FluidBlock : Block {
	
	public Face face;
	

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        FluidBuilder.Build( builder, block, pos, index, chunk );
	}
	
	public override MeshBuilder Build() {
		return FluidBuilder.Build(this);
	}

    public override Face GetPreviewFace() {
        return face;
    }
	
	public override bool IsSolid() {
		return false;
	}
	
}
