using UnityEngine;
using System.Collections.Generic;

public class CubeBlock : Block {
	
	public Face front, back, right, left, top, bottom;
	private bool alpha;
	
	
	public override void Init(BlockSet blockSet, int blockId) {
		base.Init(blockSet, blockId);

		if(front.material != null) {
			Shader shader = front.material.shader;
			alpha = shader.name.Contains("Alpha");
		}
	}

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        CubeBuilder.Build( builder, block, pos, index, chunk );
    }

    public override MeshBuilder Build() {
        return CubeBuilder.Build( this );
    }
	
	public static CubeSide TransformSide(CubeSide side, BlockDirection dir) {
		if(side == CubeSide.Top || side == CubeSide.Bottom) {
			return side;
		}
		
		//Front, Right, Back, Left
		//0      90     180   270
		
        //    front
        // left | right
        //    back

		int angle = 0;
		if(side == CubeSide.Right) angle = 90;
		if(side == CubeSide.Back)  angle = 180;
		if(side == CubeSide.Left)  angle = 270;
		
		if(dir == BlockDirection.RIGHT) angle += 90;
		if(dir == BlockDirection.BACKWARD) angle += 180;
		if(dir == BlockDirection.LEFT) angle += 270;
		
		angle %= 360;
		
		if(angle == 0) return CubeSide.Front;
		if(angle == 90) return CubeSide.Right;
		if(angle == 180) return CubeSide.Back;
		if(angle == 270) return CubeSide.Left;
		
		return CubeSide.Front;
	}


    public override Face GetPreviewFace() {
        return front;
    }
	
	public override bool IsAlpha() {
		return alpha;
	}
	
}