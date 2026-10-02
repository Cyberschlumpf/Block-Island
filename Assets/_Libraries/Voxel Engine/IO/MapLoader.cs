using UnityEngine;
using System.Collections.Generic;
using System.IO;
using SevenZip.Compression.LZMA;

public static class MapLoader {

    private const int VERSION = 1;

    public static void Import(byte[] file) {
        byte[] decompressedFile = SevenZipHelper.Decompress(file);
        using (MemoryStream stream = new MemoryStream(decompressedFile))
        using (BinaryReader reader = new BinaryReader(stream)) {
            Read(reader);
        }
    }

    private static void Read(BinaryReader reader) {
        int version = reader.ReadInt32();
        if(version == 1 || version == 2) {
            int[] newIndices = ReadBlockSet( reader );
            ReadMap( reader, newIndices, version );
        }
    }

    private static int[] ReadBlockSet(BinaryReader reader) {
        string name = reader.ReadString();
        int count = reader.ReadInt32();

        BlockSet resource = Resources.Load<BlockSet>( name );
        BlockSet blockSet = (BlockSet) Object.Instantiate( resource );
        blockSet.name = resource.name;

        List<Block> blocks = new List<Block>( blockSet.GetBlocks() );

        int[] newIndices = new int[count];
        for(int index = 0; index < count; index++) {
            string blockName = reader.ReadString();
            Block block = FindBlock(blocks, blockName);

            newIndices[index] = block.blockID;
            //Debug.Log( block.name+"  "+block.blockID );
        }

        return newIndices;
    }

    private static Block FindBlock(List<Block> blocks, string name) {
        for(int i = 0; i < blocks.Count; i++) {
            if(blocks[i].name == name) {
                Block block = blocks[i];
                blocks.RemoveAt(i);
                return block;
            }
        }
        return null;
    }


    private static Map ReadMap(BinaryReader reader, int[] newIndices, int version) {
        Map map = Map.Create();

        int count = reader.ReadInt32();
        for(int i = 0; i < count; i++) {
            ReadChunk( reader, map, newIndices, version );
        }

        return map;
    }

    private static void ReadChunk(BinaryReader reader, Map map, int[] newIndices, int version) {
        int px = reader.ReadInt32();
        int py = reader.ReadInt32();
        int pz = reader.ReadInt32();
        ChunkPosition pos = new ChunkPosition(px, py, pz);
        Chunk chunk = map.grid.GetChunkInstance(pos);

        for(int x = 0; x < Chunk.X_SIZE; x++) {
            for(int y = 0; y < Chunk.Y_SIZE; y++) {
                for(int z = 0; z < Chunk.Z_SIZE; z++) {
                    DataBlock block = ReadBlock(reader, newIndices, version);
                    chunk.SetBlock( x, y, z, block );
                }
            }
        }
    }

    private static DataBlock ReadBlock(BinaryReader reader, int[] newIndices, int version) {
        int id = version == 1 ? (int)reader.ReadByte() : reader.ReadInt32();
        byte dir = reader.ReadByte();

        int oldIndex = id - 1;
        int newId = 0;
        if(oldIndex >= 0 && oldIndex < newIndices.Length) newId = newIndices[oldIndex] + 1;

        DataBlock block = new DataBlock();
        block.blockID = newId;
        block.direction = (BlockDirection) dir;
        return block;
    }


}
