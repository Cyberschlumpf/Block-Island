using System;
using System.IO;


namespace SevenZip.Compression.LZMA {
    public static class SevenZipHelper {

        private static readonly CoderPropID[] propertyIDs = {
					CoderPropID.DictionarySize, // [0, 29], default: 23 (8MB)
					CoderPropID.PosStateBits,   // [0, 4], default: 2
					CoderPropID.LitContextBits, // [0, 8], default: 3
					CoderPropID.LitPosBits,     // [0, 4], default: 0
					CoderPropID.Algorithm,
					CoderPropID.NumFastBytes,   // [5, 273], default: 128  // чем меньше, тем быстрее
					CoderPropID.MatchFinder,    // bt2, bt4
					CoderPropID.EndMarker       // true - длина вх. данных неизвестна, false - inStream должен поддерживать св-во Length
				};

        private static readonly object[] properties = {
					(Int32) (23 * 1024 * 1024), // DictionarySize
					(Int32) 2,  // PosStateBits
					(Int32) 3,  // LitContextBits
					(Int32) 0,  // LitPosBits
					(Int32) 2,  // Algorithm
					(Int32) 5,  // NumFastBytes
					"bt4",      // MatchFinder
					false       // EndMarker
				};




        public static byte[] Compress(byte[] inputBytes) {
            using (MemoryStream input = new MemoryStream(inputBytes))
            using (MemoryStream output = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(output)) {
                Compress(input, writer);
                return output.ToArray();
            }
        }

        public static byte[] Compress(MemoryStream input) {
            using(MemoryStream output = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter( output )) {
                Compress( input, writer );
                return output.ToArray();
            }
        }

        private static void Compress(MemoryStream input, BinaryWriter writer) {
            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(propertyIDs, properties);
            

            encoder.WriteCoderProperties(writer.BaseStream);
            writer.Write((Int64)input.Length);
            encoder.Code(input, writer.BaseStream, -1, -1, null);
        }



        public static byte[] Decompress(byte[] inputBytes) {
            using (MemoryStream input = new MemoryStream(inputBytes))
            using (BinaryReader reader = new BinaryReader(input))
            using (MemoryStream output = new MemoryStream()) {
                Decompress(reader, output);
                return output.ToArray();
            }
        }

        private static void Decompress(BinaryReader reader, MemoryStream output) {
            Decoder decoder = new Decoder();
            decoder.SetDecoderProperties(reader.ReadBytes(5));
            long size = reader.ReadInt64();
            decoder.Code(reader.BaseStream, output, -1, size, null);
        }

    }
}
