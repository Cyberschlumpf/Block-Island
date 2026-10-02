using UnityEngine;
using System.Collections;

public class StairBlock : Block {
	
	public Face face;
	

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        StairBuilder.Build( builder, block, pos, index, chunk );
	}
	
	public override MeshBuilder Build() {
		return StairBuilder.Build(this);
	}

    public override Face GetPreviewFace() {
        return face;
    }
	
}
