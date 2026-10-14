using UnityEngine;
using System.Collections.Generic;
using System.IO;
using SevenZip.Compression.LZMA;
using System;

public static class MapSaver {

    private const int VERSION = 2;

    public static byte[] Export(BlockSet blockSet, Map map) {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream)) {
            Write( writer, blockSet, map );

            stream.Position = 0;
            return SevenZipHelper.Compress( stream );
        }
    }


    private static void Write(BinaryWriter writer, BlockSet blockSet, Map map) {
        writer.Write( (Int32) VERSION );
        WriteBlockSet( writer, blockSet );
        WriteMap( writer, map );
    }

    private static void WriteBlockSet(BinaryWriter writer, BlockSet blockSet) {
        var blocks = blockSet.GetBlocks();

        writer.Write( blockSet.name );
        writer.Write( (Int32) blocks.Count );
        foreach(Block block in blocks) {
            writer.Write( block.name );
        }
    }

    private static void WriteMap(BinaryWriter writer, Map map) {
        List<Chunk> list = new List<Chunk>();
        foreach(var chunk in map.grid.chunks) {
            if (chunk != null && !IsChunkEmpty(chunk)) {
                list.Add( chunk );
            }
        }

        writer.Write( (Int32) list.Count );
        foreach(var chunk in list) {
            WriteChunk(writer, chunk);
        }
    }

    private static bool IsChunkEmpty(Chunk chunk) {
        foreach(var block in chunk.blocks) {
            if(!block.IsEmpty()) return false;
        }
        return true;
    }

    private static void WriteChunk(BinaryWriter writer, Chunk chunk) {
        ChunkPosition pos = chunk.position;
        writer.Write( (Int32) pos.x );
        writer.Write( (Int32) pos.y );
        writer.Write( (Int32) pos.z );

        for (int x = 0; x < Chunk.X_SIZE; x++) {
            for (int y = 0; y < Chunk.Y_SIZE; y++) {
                for (int z = 0; z < Chunk.Z_SIZE; z++) {
                    DataBlock block = chunk.GetBlock(x, y, z);
                    WriteBlock( writer, block );
                }
            }
        }
    }

    private static void WriteBlock(BinaryWriter writer, DataBlock block) {
        writer.Write( (Int32) block.blockID );
        writer.Write( (byte) block.direction );
    }

}
