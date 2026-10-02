using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ChunkBuilder {

    private static readonly MeshBuilder builder = new MeshBuilder();


    public static MeshBuilder BuildChunk(Chunk chunk, Map map) {
        BuildChunk( builder, chunk, map );
        return builder;
    }

    public static void BuildChunk(MeshBuilder builder, Chunk chunk, Map map) {
        builder.Clear();
        
        for(int z = 0, i = 0; z < Chunk.Z_SIZE; z++) {
            for(int y = 0; y < Chunk.Y_SIZE; y++) {
                for(int x = 0; x < Chunk.X_SIZE; x++, i++) {
                    DataBlock block = chunk.blocks[i];
                    if(!block.IsEmpty()) {
                        LocalPosition pos = new LocalPosition( x, y, z );
                        block.block.Build( builder, block, pos, i, chunk );
                    }
                }
            }
        }
    }

    public static Color32[] BuildLight(BlockTopology[] topology, IList<Vector3> vertices, ChunkPosition chunkPos, Map map) {
        Vector3i wPos = WorldPosition.ToWorldPosition( chunkPos, LocalPosition.zero );

        int index = 0;
        Color32[] colors = new Color32[vertices.Count];

        foreach(var block in topology) {
            Vector3i localPos = (Vector3i) block.pos;
            Vector3i worldPos = wPos + localPos;

            if(block.type == BlockTopologyType.Vertex) {
                Color32 color = LightComputerUtils.GetBlockLight( worldPos );
                for(int i = 0; i < block.count; i++) {
                    colors[index++] = color;
                }
            } else {
                CubeSide side = (CubeSide) block.type;

                Vector3 v1 = vertices[index + 0] - localPos;
                Vector3 v2 = vertices[index + 1] - localPos;
                Vector3 v3 = vertices[index + 2] - localPos;
                Vector3 v4 = vertices[index + 3] - localPos;

                BuildFaceLight( colors, index, side, v1, v2, v3, v4, worldPos, map );
                index += 4;
            }
        }

        return colors;
    }

    private static void BuildFaceLight(Color32[] colors, int index, CubeSide face, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3i worldPos, Map map) {
        Vector3i p1, p2, p3;
        Vector3i p4, p5, p6;
        Vector3i p7, p8, p9;

        if( face == CubeSide.Front || face == CubeSide.Back ) {
            //  +y
            //-x  +x
            //  -y 

            int tz = face == CubeSide.Front ? 1 : -1;

            p1 = worldPos + new Vector3i( -1, 1, tz );
            p2 = worldPos + new Vector3i( 0, 1, tz );
            p3 = worldPos + new Vector3i( 1, 1, tz );

            p4 = worldPos + new Vector3i( -1, 0, tz );
            p5 = worldPos + new Vector3i( 0, 0, tz );
            p6 = worldPos + new Vector3i( 1, 0, tz );

            p7 = worldPos + new Vector3i( -1, -1, tz );
            p8 = worldPos + new Vector3i( 0, -1, tz );
            p9 = worldPos + new Vector3i( 1, -1, tz );
        } else if(face == CubeSide.Right || face == CubeSide.Left) {
            //   +y
            // -z  +z
            //   -y

            int tx = face == CubeSide.Right ? 1 : -1;

            p1 = worldPos + new Vector3i( tx, 1, -1 );
            p2 = worldPos + new Vector3i( tx, 1, 0 );
            p3 = worldPos + new Vector3i( tx, 1, 1 );

            p4 = worldPos + new Vector3i( tx, 0, -1 );
            p5 = worldPos + new Vector3i( tx, 0, 0 );
            p6 = worldPos + new Vector3i( tx, 0, 1 );

            p7 = worldPos + new Vector3i( tx, -1, -1 );
            p8 = worldPos + new Vector3i( tx, -1, 0 );
            p9 = worldPos + new Vector3i( tx, -1, 1 );
        } else { // Top || Bottom
            //   +z
            // -x  +x
            //   -z

            int ty = face == CubeSide.Top ? 1 : -1;

            p1 = worldPos + new Vector3i( -1, ty, 1 );
            p2 = worldPos + new Vector3i( 0, ty, 1 );
            p3 = worldPos + new Vector3i( 1, ty, 1 );

            p4 = worldPos + new Vector3i( -1, ty, 0 );
            p5 = worldPos + new Vector3i( 0, ty, 0 );
            p6 = worldPos + new Vector3i( 1, ty, 0 );

            p7 = worldPos + new Vector3i( -1, ty, -1 );
            p8 = worldPos + new Vector3i( 0, ty, -1 );
            p9 = worldPos + new Vector3i( 1, ty, -1 );
        }

        //   2   
        // 4 5 6
        //   8

        bool a2 = map.GetBlock( p2 ).IsAlpha();
        bool a4 = map.GetBlock( p4 ).IsAlpha();
        bool a6 = map.GetBlock( p6 ).IsAlpha();
        bool a8 = map.GetBlock( p8 ).IsAlpha();

        Color32 c2 = LightComputerUtils.GetBlockLight( p2);
        Color32 c4 = LightComputerUtils.GetBlockLight( p4 );
        Color32 c5 = LightComputerUtils.GetBlockLight( p5 );
        Color32 c6 = LightComputerUtils.GetBlockLight( p6 );
        Color32 c8 = LightComputerUtils.GetBlockLight( p8 );

        Color32 a, b;
        Color32 c, d;

        if(a2 || a4) {
            Color32 c1 = LightComputerUtils.GetBlockLight( p1 );
            a = GetAverageLight( c1, c2, c4, c5 );
        } else {
            a = GetAverageLight( c2, c4, c5 );
        }

        if(a2 || a6) {
            Color32 c3 = LightComputerUtils.GetBlockLight( p3 );
            b = GetAverageLight( c3, c2, c5, c6 );
        } else {
            b = GetAverageLight( c2, c5, c6 );
        }

        if(a8 || a4) {
            Color32 c7 = LightComputerUtils.GetBlockLight( p7 );
            c = GetAverageLight( c7, c4, c5, c8 );
        } else {
            c = GetAverageLight( c4, c5, c8 );
        }

        if(a8 || a6) {
            Color32 c9 = LightComputerUtils.GetBlockLight( p9 );
            d = GetAverageLight( c9, c5, c6, c8 );
        } else {
            d = GetAverageLight( c5, c6, c8 );
        }

        colors[index++] = GetColor( face, v1, a, b, c, d );
        colors[index++] = GetColor( face, v2, a, b, c, d );
        colors[index++] = GetColor( face, v3, a, b, c, d );
        colors[index++] = GetColor( face, v4, a, b, c, d );
    }

    private static Color32 GetAverageLight(Color32 a, Color32 b, Color32 c, Color32 d) {
        int glow = (a.r + b.r + c.r + d.r) / 4;
        int sun = (a.g + b.g + c.g + d.g) / 4;

        Color32 color = default(Color32);
        color.r = (byte) glow;
        color.g = (byte) sun;
        return color;
    }
    private static Color32 GetAverageLight(Color32 a, Color32 b, Color32 c) {
        int glow = (a.r + b.r + c.r) / 3;
        int sun = (a.g + b.g + c.g) / 3;

        Color32 color = default( Color32 );
        color.r = (byte) glow;
        color.g = (byte) sun;
        return color;
    }


    private static Color32 GetColor(CubeSide face, Vector3 vertex, Color32 a, Color32 b, Color32 c, Color32 d) {
        Vector2 v = ProjectVertex( face, vertex );
        int sx = v.x >= 0.5f ? 1 : -1;
        int sy = v.y >= 0.5f ? 1 : -1;

        //a  +y  b
        //-x    +x
        //c  -y  d 

        if(sx < 0 && sy > 0) return a;
        if(sx > 0 && sy > 0) return b;
        if(sx < 0 && sy < 0) return c;
        if(sx > 0 && sy < 0) return d;

        return default(Color32);
    }

    private static Vector2 ProjectVertex(CubeSide face, Vector3 vertex) {
        if(face == CubeSide.Right || face == CubeSide.Left) return new Vector2( vertex.z, vertex.y );
        if(face == CubeSide.Top || face == CubeSide.Bottom) return new Vector2( vertex.x, vertex.z );
        return new Vector2( vertex.x, vertex.y );
    }

}

