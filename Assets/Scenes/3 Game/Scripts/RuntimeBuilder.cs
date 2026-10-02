using UnityEngine;
using System.Collections;

public class RuntimeBuilder : MonoBehaviour {

    private readonly bool[,] chunks2d = new bool[ChunkGrid.X_SIZE, ChunkGrid.Z_SIZE];
    private readonly MeshBuilder builder = new MeshBuilder();

    public int maxDistance = 6;


    IEnumerator Start() {
        yield return null;
        while (Camera.main == null) yield return null;

        while( true ) {
            yield return StartCoroutine( BackgroundBuild() );
        }
    }


    private IEnumerator BackgroundBuild() {
        Vector3i center = Vector3i.Round(Camera.main.transform.position).Div(Chunk.SIZE);
        for (int d = 0; d <= maxDistance; d++) {
            for (int x = -d; x <= d; x++) {
                int dz = d - Mathf.Abs(x);
                int posX  = center.x + x;
                int posZ1 = center.z - dz;
                int posZ2 = center.z + dz;

                Vector3i newCenter = Vector3i.Round(Camera.main.transform.position).Div(Chunk.SIZE);
                if(center.x != newCenter.x || center.z != newCenter.z) {
                    yield break;
                }

                if (ChunkPosition.Check(posX, 0, posZ1) && !chunks2d[posX, posZ1]) {
                    chunks2d[posX, posZ1] = true;
                    yield return StartCoroutine(BackgroundBuildColumn(posX, posZ1));
                }

                if(ChunkPosition.Check( posX, 0, posZ2 ) && !chunks2d[posX, posZ2]) {
                    chunks2d[posX, posZ2] = true;
                    yield return StartCoroutine(BackgroundBuildColumn(posX, posZ2));
                }
            }
        }
    }

    private IEnumerator BackgroundBuildColumn(int x, int z) {
        Map map = Map.instance;
        for(int y = 0; y < ChunkGrid.Y_SIZE; y++) {
            Chunk chunk = map.grid.Get(x, y, z);
            if (chunk != null) {
                yield return StartCoroutine(BackgroundBuildChunk(chunk, map));
            }
        }
    }

    private IEnumerator BackgroundBuildChunk(Chunk chunk, Map map) {
        bool building = true;

        Tasks.InvokeASync(() => {
            ChunkBuilder.BuildChunk(builder, chunk, map);
            building = false;
        });

        while (building) yield return null;
        chunk.GetChunkRendererInstance().Build(builder);
    }

}
