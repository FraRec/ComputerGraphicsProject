using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkCreator {
    private int _GridSize;
    private float _Y;
    private QuadTreeManager quadTreeManager;

    public TerrainChunkCreator(QuadTreeManager quadTreeManager) {
        this._Y = quadTreeManager.transform.position.y;
        this.quadTreeManager = quadTreeManager;
    }

    public GameObject CreateTerrainChunk(Vector2 offset, float chunkSize, int gridSize, Material chunkMaterial) {
        GameObject chunk = new GameObject();
        chunk.transform.position = new Vector3(offset.x, _Y, offset.y);
        chunk.transform.parent = quadTreeManager.transform;
        chunk.layer = 20;
        chunk.name = "Chunk_Size" + chunkSize + "_OffsetX" + offset.x + "_OffsetY" + offset.y;

        MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
        mr.material = chunkMaterial;

        MeshFilter mf = chunk.AddComponent<MeshFilter>();
        mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        this._GridSize = gridSize;
        mf.mesh.vertices = CreateVertices(gridSize + 1, chunkSize);
        mf.mesh.triangles = CreateTriangles(gridSize + 1);
        mf.mesh.uv = CreateUVs(gridSize + 1, chunkSize, mf.mesh.vertices);

        mf.mesh.RecalculateBounds();
        mf.mesh.RecalculateNormals();

        return chunk;
    }

    private Vector3[] CreateVertices(int effectiveGridSize, float chunkSize) {
        int effectiveGridSizeExtra = effectiveGridSize + 2;

        //Debug.Log("N. Vertices = " + (effectiveGridSizeExtra * effectiveGridSizeExtra));

        Vector3[] vertices = new Vector3[effectiveGridSizeExtra * effectiveGridSizeExtra];
        Vector3 newPos;
        float chunkSide = (chunkSize * Mathf.Sqrt(2f) / 2f);
        float cellSize = chunkSide / _GridSize;

        int i, j;
        for (j = 0; j < effectiveGridSizeExtra; j++) {
            for (i = 0; i < effectiveGridSizeExtra; i++) {
                newPos = new Vector3(cellSize * i - chunkSide / 2f - cellSize, 0, cellSize * j - chunkSide / 2f - cellSize);
                vertices[j * effectiveGridSizeExtra + i] = newPos;
            }
        }

        return vertices;
    }

    private int[] CreateTriangles(int effectiveGridSize) {
        int effectiveGridSizeExtra = effectiveGridSize + 2;

        //Debug.Log("N. Triangles = " + ((_GridSize + 2) * (_GridSize + 2) * 2 * 3));

        int[] triangles = new int[((_GridSize + 2) * (_GridSize + 2) * 2 * 3)];
        int triangleIndexCounter = 0;

        int row, column = 0;
        int vertex;
        for (vertex = 0; vertex < (effectiveGridSizeExtra * effectiveGridSizeExtra - effectiveGridSizeExtra); vertex++) {
            if (vertex % effectiveGridSizeExtra != (effectiveGridSizeExtra - 1)) {    // Skip Last Vertex per Row

                row = Mathf.FloorToInt(vertex / effectiveGridSizeExtra);
                column = vertex % effectiveGridSizeExtra;
                //Debug.Log("Vertex_" + vertex + " - Row: " + row + " | Column: " + column);

                int A = vertex;
                int B = A + effectiveGridSizeExtra;
                int C = B + 1;
                triangles[triangleIndexCounter] = A;
                triangles[triangleIndexCounter + 1] = B;
                triangles[triangleIndexCounter + 2] = C;


                A = vertex;
                B = A + effectiveGridSizeExtra + 1;
                C = A + 1;
                triangles[triangleIndexCounter + 3] = A;
                triangles[triangleIndexCounter + 4] = B;
                triangles[triangleIndexCounter + 5] = C;

                triangleIndexCounter += 6;
            }
        }

        return triangles;
    }

    private Vector2[] CreateUVs(int effectiveGridSize, float chunkSize, Vector3[] vertices) {
        int effectiveGridSizeExtra = effectiveGridSize + 2;

        Vector2[] uvs = new Vector2[effectiveGridSizeExtra * effectiveGridSizeExtra];
        float chunkSide = (chunkSize * Mathf.Sqrt(2f) / 2f);
        float cellSize = chunkSide / _GridSize;

        int index = 0;
        foreach (Vector3 vertex in vertices) {
            uvs[index] = new Vector2(vertex.x / (_GridSize * cellSize), vertex.z / (_GridSize * cellSize));
            index++;
        }

        return uvs;
    }



    // === OLD ===
    /* CreateTerrainChunk
     * 
    private GameObject CreateTerrainChunk(Vector2 offset, float size) {
        GameObject chunk = new GameObject();
        chunk.transform.position = new Vector3(offset.x, this.transform.position.y, offset.y);

        MeshRenderer mr = chunk.AddComponent<MeshRenderer>();
        mr.material = _TerrainChunkMaterial;

        MeshFilter mf = chunk.AddComponent<MeshFilter>();
        mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mf.mesh.vertices = CreateVertices(_GridSize + 1, size);
        mf.mesh.triangles = CreateTriangles(_GridSize + 1);
        mf.mesh.uv = CreateUVs(_GridSize + 1, size, mf.mesh.vertices);

        mf.mesh.RecalculateBounds();
        mf.mesh.RecalculateNormals();

        return chunk;
    }
    */

    /* CreateVertices
     * 
    private Vector3[] CreateVertices(int effectiveGridSize, float chunkSize) {
        
        //Vector3[] vertices = new Vector3[effectiveGridSize * effectiveGridSize];
        //float chunkSide = (chunkSize * Mathf.Sqrt(2f) / 2f);
        //float cellSize = chunkSide / _GridSize;
        //
        //Vector3 newPos;
        //int i, j;
        //for (j = 0; j < effectiveGridSize; j++)
        //    for (i = 0; i < effectiveGridSize; i++) {
        //        newPos = new Vector3(cellSize * i - chunkSide / 2f, this.transform.position.y, cellSize * j - chunkSide / 2f);               
        //        vertices[j * effectiveGridSize + i] = newPos;
        //    }
        

        Debug.Log("N. Vertices = " + ((effectiveGridSize * effectiveGridSize) + effectiveGridSize * 2));

        Vector3[] vertices = new Vector3[effectiveGridSize * effectiveGridSize + effectiveGridSize * 2];
        Vector3 newPos;
        float chunkSide = (chunkSize * Mathf.Sqrt(2f) / 2f);
        float cellSize = chunkSide / _GridSize;

        int i, j;
        for (j = 0; j < effectiveGridSize + 2; j++) {
            for (i = 0; i < effectiveGridSize; i++) {
                newPos = new Vector3(cellSize * i, this.transform.position.y, cellSize * j);
                if (j == 0) {
                    newPos = new Vector3(cellSize * i + cellSize / 2f, this.transform.position.y, cellSize * (j + 1));
                } else if (j == effectiveGridSize + 1) {
                    newPos = new Vector3(cellSize * i - cellSize / 2f, this.transform.position.y, cellSize * (j - 1));
                }

                newPos -= new Vector3(chunkSide / 2f, 0, chunkSide / 2f + cellSize);
                vertices[j * effectiveGridSize + i] = newPos;
            }
        }

        return vertices;
    }
    */

    /* CreateTriangles
     * 
    private int[] CreateTriangles(int effectiveGridSize) {
        
        //int[] triangles = new int[3 * 2 * (effectiveGridSize * effectiveGridSize - (effectiveGridSize * 2) + 1)];
        //int triangleIndexCounter = 0;
        //int vertex;
        //for (vertex = 0; vertex < (effectiveGridSize * effectiveGridSize - effectiveGridSize); vertex++) {
        //    if (vertex % effectiveGridSize != (effectiveGridSize - 1)) {
        //        //Triangolo Up
        //        int A = vertex;
        //        int B = A + effectiveGridSize;
        //        int C = B + 1;
        //        triangles[triangleIndexCounter] = A;
        //        triangles[triangleIndexCounter + 1] = B;
        //        triangles[triangleIndexCounter + 2] = C;
        //
        //        //Triangolo Down
        //        B += 1;
        //        C = A + 1;
        //        triangles[triangleIndexCounter + 3] = A;
        //        triangles[triangleIndexCounter + 4] = B;
        //        triangles[triangleIndexCounter + 5] = C;
        //
        //        triangleIndexCounter += 6;
        //    }
        //}
        
        Debug.Log("N. Triangles = " + ((_GridSize * _GridSize + _GridSize * 2) * 2 * 3));

        int[] triangles = new int[((_GridSize * _GridSize + _GridSize * 2) * 2 * 3)];
        int triangleIndexCounter = 0;
        int vertex;
        for (vertex = 0; vertex < (effectiveGridSize * effectiveGridSize + effectiveGridSize); vertex++) {
            if (vertex % effectiveGridSize != (effectiveGridSize - 1)) {    // Skip Last Vertex per Row

                if (vertex < (effectiveGridSize * effectiveGridSize)) {
                    //Triangolo Up
                    int A = vertex;
                    int B = A + effectiveGridSize;
                    int C = B + 1;
                    triangles[triangleIndexCounter] = A;
                    triangles[triangleIndexCounter + 1] = B;
                    triangles[triangleIndexCounter + 2] = C;
                }

                if (vertex >= effectiveGridSize) {
                    //Triangolo Down
                    int A = vertex;
                    int B = A + effectiveGridSize + 1;
                    int C = A + 1;
                    triangles[triangleIndexCounter + 3] = A;
                    triangles[triangleIndexCounter + 4] = B;
                    triangles[triangleIndexCounter + 5] = C;
                }

                triangleIndexCounter += 6;
            }
        }

        return triangles;
    }
    */

    /* CreateUVs
     * 
    private Vector2[] CreateUVs(int effectiveGridSize, float chunkSize, Vector3[] vertices) {
        //Vector2[] uvs = new Vector2[effectiveGridSize * effectiveGridSize];
        //float cellSize = chunkSize / effectiveGridSize;
        //
        //int index = 0;
        //foreach (Vector3 vertex in vertices) {
        //    uvs[index] = new Vector2(vertex.x / (_GridSize * cellSize), vertex.z / (_GridSize * cellSize));
        //    index++;
        //}
        

        Vector2[] uvs = new Vector2[effectiveGridSize * effectiveGridSize + effectiveGridSize * 2];
        float cellSize = chunkSize / effectiveGridSize;

        int index = 0;
        foreach (Vector3 vertex in vertices) {
            uvs[index] = new Vector2(vertex.x / (_GridSize * cellSize), vertex.z / (_GridSize * cellSize));
            if (index <= effectiveGridSize)
                uvs[index] = Vector2.zero;
            else if (index >= (effectiveGridSize * effectiveGridSize))
                uvs[index] = Vector2.one;

            index++;
        }

        return uvs;
    }
    */
}
