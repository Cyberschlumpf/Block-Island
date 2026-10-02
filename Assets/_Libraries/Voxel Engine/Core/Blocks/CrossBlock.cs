using UnityEngine;
using System.Collections;

public class CrossBlock : Block {
	
	public Face face;
	internal bool doubleSided = false;
	
	
	public override void Init(BlockSet blockSet, int blockId) {
		base.Init(blockSet, blockId);
		
		if(face.material != null) {
			Shader shader = face.material.shader;
			doubleSided = !shader.name.EndsWith("cull_off", System.StringComparison.OrdinalIgnoreCase);
		}
	}

    public override void Build(MeshBuilder builder, DataBlock block, LocalPosition pos, int index, Chunk chunk) {
        CrossBuilder.Build( builder, block, pos, chunk );
    }

    public override MeshBuilder Build() {
        return CrossBuilder.Build( this );
    }
	
	public override bool CanCreateBlock(Vector3i pos) {
		Block block = Map.instance.GetBlock(pos-Vector3i.up).block;
		return block is CubeBlock || block is GroundBlock;
	}
	
	public override void OnNeighborBlockChanged (Vector3i pos, Vector3i neighborPos) {
		if(pos-Vector3i.up == neighborPos) { // удален нижний блок
			Map map = Map.instance;
            GlowLightmap glowmap = GlowLightmap.instance;

			DataBlock neighborBlock = map.GetBlock( neighborPos );
			if( neighborBlock.IsEmpty() ) {
                int glow = map.GetBlock( pos ).GetGlow();

                map.SetBlock( pos, null );
				map.MarkBlockAsDirty( pos );

                if (glow > LightComputerUtils.MIN_LIGHT) {
                    GlowLightComputer.RemoveBlock( glowmap, map, pos );
                }
			}
		}
	}
	
	public override Face GetPreviewFace() {
		return face;
	}
	
	public override bool IsSolid() {
		return false;
	}
	
	public override int GetLightStep() {
		return 1;
	}
	
}