using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class CommonLightmap {

    public readonly BlockLight[][] chunks = new BlockLight[ChunkGrid.ARRAY_SIZE][];


    /*public bool SetMaxLight(int light, int chunkIndex, int localIndex) {
        if(light <= LightComputerUtils.MIN_LIGHT) return false;

        var chunk = chunks[chunkIndex];
        int oldLight = LightComputerUtils.MIN_LIGHT;
        if(chunk != null) {
            oldLight = chunk[localIndex];
        }

        if(light > oldLight) {
            if(chunk == null) chunk = GetChunkInstance( chunkIndex );
            chunk[localIndex] = (byte) light;
            return true;
        }
        return false;
    }



    public void SetLight(int light, int chunkIndex, int localIndex) {
        byte[] chunk = GetChunkInstance( chunkIndex );
        chunk[localIndex] = (byte) light;
    }



    public int GetLight(int chunkIndex, int localIndex) {
        var chunk = chunks[chunkIndex];
        if(chunk != null) return chunk[localIndex];
        return LightComputerUtils.MIN_LIGHT;
    }


    public byte[] GetChunkInstance(int chunkIndex) {
        var chunk = chunks[chunkIndex];
        if(chunk == null) {
            chunk = new byte[Chunk.ARRAY_SIZE];
            chunks[chunkIndex] = chunk;
        }
        return chunk;
    }*/




    public bool SetMaxLight(int light, int chunkIndex, int localIndex) {
        if(light <= LightComputerUtils.MIN_LIGHT) return false;

        var chunk = chunks[chunkIndex];
        int oldLight = LightComputerUtils.MIN_LIGHT;
        if(chunk != null) {
            oldLight = chunk[localIndex >> 1].Get( localIndex & 1 );
        }

        if(light > oldLight) {
            if(chunk == null) chunk = GetChunkInstance( chunkIndex );
            chunk[localIndex >> 1].Set( localIndex & 1, light );
            return true;
        }
        return false;
    }



    public void SetLight(int light, int chunkIndex, int localIndex) {
        BlockLight[] chunk = GetChunkInstance( chunkIndex );
        chunk[localIndex >> 1].Set( localIndex & 1, light );
    }



    public int GetLight(int chunkIndex, int localIndex) {
        var chunk = chunks[chunkIndex];
        if(chunk != null) {
            return chunk[localIndex >> 1].Get( localIndex & 1 );
        }
        return LightComputerUtils.MIN_LIGHT;
    }


    private BlockLight[] GetChunkInstance(int chunkIndex) {
        var chunk = chunks[chunkIndex];
        if(chunk == null) {
            chunk = new BlockLight[Chunk.X_SIZE * Chunk.Y_SIZE * Chunk.Z_SIZE / 2];
            chunks[chunkIndex] = chunk;
        }
        return chunk;
    }

}



public struct BlockLight {

    private byte data;

    public void Set(int index, int value) { 
        // #11110000
        if(index == 0) {
            data &= 0xF0; // reset first 4 bits
            data = (byte) (data | value); // set first 4 bits
        } else {
            data &= 0x0F; // reset second 4 bits
            value <<= 4;
            data = (byte) (data | value); // set second 4 bits
        }
    }

    public int Get(int index) {
        // #11110000
        if(index == 0) {
            return data & 0x0F; // return first 4 bits
        } else {
            return data >> 4; // return second 4 bits
        }
    }

}
