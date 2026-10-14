using UnityEngine;
using System.Collections;

public class FenceBlock : Block {
	
	public static readonly Box box   = Box.CenterSize( new Vector3( 0.5f, 0.5f,  0.5f ), new Vector3( 0.2f, 1, 0.2f ) );

    public static readonly Box xBox1 = Box.CenterSize( new Vector3( 0.5f, 0.75f, 0.5f ), new Vector3( 1, 0.2f, 0.1f ) );
    public static readonly Box xBox2 = Box.CenterSize( new Vector3( 0.5f, 0.3f, 0.5f ), new Vector3( 1, 0.2f, 0.1f ) );

    public static readonly Box zBox1 = Box.CenterSize( new Vector3( 0.5f, 0.75f, 0.5f ), new Vector3( 0.1f, 0.2f, 1 ) );
    public static readonly Box zBox2 = Box.CenterSize( new Vector3( 0.5f, 0.3f, 0.5f ), new Vector3( 0.1f, 0.2f, 1 ) );
	
	public Face face;
	

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
		int materialID = face.materialID;
		
		Box box = FenceBlock.box;
		Box xBox1 = FenceBlock.xBox1;
		Box xBox2 = FenceBlock.xBox2;
		Box zBox1 = FenceBlock.zBox1;
		Box zBox2 = FenceBlock.zBox2;

        bool z1 = IsBackSolid( index, chunk );
        bool z2 = IsFrontSolid( index, chunk );

        bool x1 = IsLeftSolid( index, chunk );
        bool x2 = IsRightSolid( index, chunk );
		
		if( !x1 ) {
			xBox1.min.x = 0.5f;
            xBox2.min.x = 0.5f;
		}
		if( !x2 ) {
            xBox1.max.x = 0.5f;
            xBox2.max.x = 0.5f;
		}
		if( !z1 ) {
            zBox1.min.z = 0.5f;
            zBox2.min.z = 0.5f;
		}
		if( !z2 ) {
            zBox1.max.z = 0.5f;
            zBox2.max.z = 0.5f;
		}

        builder.AddBox( box, face.rect, pos, materialID );
		if(x1 || x2) {
            builder.AddBox( xBox1, face.rect, pos, materialID );
            builder.AddBox( xBox2, face.rect, pos, materialID );
		}
		if(z1 || z2) {
            builder.AddBox( zBox1, face.rect, pos, materialID );
            builder.AddBox( zBox2, face.rect, pos, materialID );
		}
	}

    public static bool IsFrontSolid(int index, Chunk chunk) {
        Block block = chunk.GetFrontBlock( index ).block;
        return block is FenceBlock || block is GroundBlock || block is CubeBlock;
    }

    public static bool IsBackSolid(int index, Chunk chunk) {
        Block block = chunk.GetBackBlock( index ).block;
        return block is FenceBlock || block is GroundBlock || block is CubeBlock;
    }

    public static bool IsRightSolid(int index, Chunk chunk) {
        Block block = chunk.GetRightBlock( index ).block;
        return block is FenceBlock || block is GroundBlock || block is CubeBlock;
    }

    public static bool IsLeftSolid(int index, Chunk chunk) {
        Block block = chunk.GetLeftBlock( index ).block;
        return block is FenceBlock || block is GroundBlock || block is CubeBlock;
    }


	
	public override MeshBuilder Build() {
		MeshBuilder builder = new MeshBuilder();
		
		builder.AddBox( box,   face.rect, LocalPosition.zero, face.materialID );
        builder.AddBox( xBox1, face.rect, LocalPosition.zero, face.materialID );
        builder.AddBox( xBox2, face.rect, LocalPosition.zero, face.materialID );
        builder.AddBox( zBox1, face.rect, LocalPosition.zero, face.materialID );
        builder.AddBox( zBox2, face.rect, LocalPosition.zero, face.materialID );
		
		return builder;
	}

    public override Face GetPreviewFace() {
        return face;
    }
	
}
