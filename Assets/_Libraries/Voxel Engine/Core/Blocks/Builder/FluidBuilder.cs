using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FluidBuilder {

	public static void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
		FluidBlock fluid = (FluidBlock) block.block;
        Face face = fluid.face;

        if(IsFrontVisible( pos, index, chunk )) {
            CubeBuilder.BuildFace( builder, 0, face, pos );
        }
        if(IsBackVisible( pos, index, chunk )) {
            CubeBuilder.BuildFace( builder, 1, face, pos );
        }

        if(IsRightVisible( pos, index, chunk )) {
            CubeBuilder.BuildFace( builder, 2, face, pos );
        }
        if(IsLeftVisible( pos, index, chunk )) {
            CubeBuilder.BuildFace( builder, 3, face, pos );
        }

        if(IsTopVisible( pos, index, chunk )) {
            CubeBuilder.BuildFace( builder, 4, face, pos );
        }
        if(IsBottomVisible( pos, index, chunk )) {
            CubeBuilder.BuildFace( builder, 5, face, pos );
        }

	}

    private static bool IsFrontVisible(LocalPosition pos, int index, Chunk chunk) {
        DataBlock block = chunk.GetFrontBlock(index);
        return !block.IsFluid() && block.IsAlpha();
    }

    private static bool IsBackVisible(LocalPosition pos, int index, Chunk chunk) {
        DataBlock block = chunk.GetBackBlock( index );
        return !block.IsFluid() && block.IsAlpha();
    }

    private static bool IsRightVisible(LocalPosition pos, int index, Chunk chunk) {
        DataBlock block = chunk.GetRightBlock( index );
        return !block.IsFluid() && block.IsAlpha();
    }

    private static bool IsLeftVisible(LocalPosition pos, int index, Chunk chunk) {
        DataBlock block = chunk.GetLeftBlock( index );
        return !block.IsFluid() && block.IsAlpha();
    }

    private static bool IsTopVisible(LocalPosition pos, int index, Chunk chunk) {
        DataBlock block = chunk.GetTopBlock( index );
        return !block.IsFluid();
    }

    private static bool IsBottomVisible(LocalPosition pos, int index, Chunk chunk) {
        DataBlock block = chunk.GetBottomBlock( index );
        return !block.IsFluid() && block.IsAlpha();
    }

	
	public static MeshBuilder Build(FluidBlock fluid) {
		MeshBuilder builder = new MeshBuilder();
		Face face = fluid.face;
		for(int i=0; i<6; i++) {
            CubeBuilder.BuildFace( builder, i, face, LocalPosition.zero );
		}
		return builder;
	}
	
}
