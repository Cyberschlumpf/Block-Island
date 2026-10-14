using UnityEngine;


public struct DataBlock {
	
	public int blockID;
	public BlockDirection direction;
	
	public Block block {
		set {
			if(value != null) {
				blockID = value.blockID + 1;
			} else {
				blockID = 0;
			}
		}
		get {
            if(blockID == 0) return null;
            int index = blockID - 1;
            if(BlockSet.blockArray == null || index < 0 || index >= BlockSet.blockArray.Length) return null;
            return BlockSet.blockArray[index];
		}
	}

    public DataBlock(Block block) : this( block, BlockDirection.FORWARD ) {
	}

    public DataBlock(Block block, BlockDirection direction) {
        if(block != null) {
            blockID = block.blockID + 1;
        } else {
            blockID = 0;
        }

        this.direction = direction;
    }
	
	public int GetGlow() {
		if(!IsEmpty()) return block.glow;
		return LightComputerUtils.MIN_LIGHT;
	}
	
	public bool IsAlpha() {
		return IsEmpty() || block.IsAlpha();
	}
	
	public int GetLightStep() {
		// пустые и GrassBlock блоки забирают 1 единицу света
		// остальные забирают 2 единицы света
		if( IsEmpty() ) return 1;
		return block.GetLightStep();
	}
	
	public bool IsSolid() {
		return !IsEmpty() && block.IsSolid();
	}
	
	public bool IsFluid() {
		return !IsEmpty() && block is FluidBlock;
	}
	
	public bool IsEmpty() {
        if(blockID == 0) return true;
        int index = blockID - 1;
        return BlockSet.blockArray == null || index < 0 || index >= BlockSet.blockArray.Length || BlockSet.blockArray[index] == null;
	}
	
	public override string ToString () {
		if( IsEmpty() ) return "Null";
		return block.name;
	}
	
}