using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneModCreator : MonoBehaviour {

    [Header("References:")]
    [SerializeField]
    private Material _DefaultPlaneMaterial;

    [Header("Plane Properties")]
    [SerializeField]
    public int _GridSize;
    [SerializeField]
    public float _CellSize;

    private MeshFilter mf;
    private MeshRenderer mr;

    private void Awake() {
        CreatePlane();
    }

    private void CreatePlane() {
        mr = this.gameObject.AddComponent<MeshRenderer>();
        mr.material = _DefaultPlaneMaterial;

        mf = this.gameObject.AddComponent<MeshFilter>();
        mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mf.mesh.vertices = CreateVertices(_GridSize + 1);
        mf.mesh.triangles = CreateTriangles(_GridSize + 1);
        //mf.mesh.uv = CreateUVs(_GridSize + 1, mf.mesh.vertices);
        
        mf.mesh.RecalculateBounds();
        mf.mesh.RecalculateNormals();
    }

    private Vector3[] CreateVertices(int effectiveGridSize) {
        int effectiveGridSizeExtra = effectiveGridSize + 2;

        Debug.Log("N. Vertices = " + (effectiveGridSizeExtra * effectiveGridSizeExtra));

        Vector3[] vertices = new Vector3[effectiveGridSizeExtra * effectiveGridSizeExtra];
        Vector3 newPos;

        int i, j;
        for (j = 0; j < effectiveGridSizeExtra; j++) {
            for (i = 0; i < effectiveGridSizeExtra; i++) {
                newPos = new Vector3(_CellSize * i, 0, _CellSize * j);
                //if (j == 0) {
                //    newPos = new Vector3(_CellSize * i, 0, _CellSize * j);
                //} else if(j == effectiveGridSize + 1) {
                //    newPos = new Vector3(_CellSize * i, 0, _CellSize * j);
                //} else if()
                vertices[j * effectiveGridSizeExtra + i] = newPos;
            }
        }

        return vertices;
    }

    private int[] CreateTriangles(int effectiveGridSize) {
        int effectiveGridSizeExtra = effectiveGridSize + 2;

        Debug.Log("N. Triangles = " + ((_GridSize + 2) * (_GridSize + 2) * 2 * 3));

        int[] triangles = new int[((_GridSize + 2) * (_GridSize + 2) * 2 * 3)];
        int triangleIndexCounter = 0;


        int row, column = 0;
        int vertex;
        for (vertex = 0; vertex < (effectiveGridSizeExtra * effectiveGridSizeExtra - effectiveGridSizeExtra); vertex++) {
            if (vertex % effectiveGridSizeExtra != (effectiveGridSizeExtra - 1)) {    // Skip Last Vertex per Row

                row = Mathf.FloorToInt(vertex / effectiveGridSizeExtra);
                column = vertex % effectiveGridSizeExtra;
                Debug.Log("Vertex_" + vertex + " - Row: " + row + " | Column: " + column);

                //Triangolo Up
                if (row != (effectiveGridSizeExtra - 2) && column != 0){
                    if (!(row == 0 && column == (effectiveGridSizeExtra - 2))) {
                        int A = vertex;
                        int B = A + effectiveGridSizeExtra;
                        int C = B + 1;
                        triangles[triangleIndexCounter] = A;
                        triangles[triangleIndexCounter + 1] = B;
                        triangles[triangleIndexCounter + 2] = C;
                    }
                }

                //Triangolo Down
                if (row != 0 && column != (effectiveGridSizeExtra - 2)) {
                    if (!(row == (effectiveGridSizeExtra - 2) && column == 0)) {
                        int A = vertex;
                        int B = A + effectiveGridSizeExtra + 1;
                        int C = A + 1;
                        triangles[triangleIndexCounter + 3] = A;
                        triangles[triangleIndexCounter + 4] = B;
                        triangles[triangleIndexCounter + 5] = C;
                    }
                }

                triangleIndexCounter += 6;
            }
        }

        /*
        //int[] triangles = new int[3 * 2 * (effectiveGridSize * effectiveGridSize - (effectiveGridSize * 2) + 1)];
        //int[] triangles = new int[3 * 2 * (effectiveGridSize * effectiveGridSize - (effectiveGridSize * 2) + 1) + effectiveGridSize * 3];
        int triangleIndexCounter = 0;
        int vertex;
        for (vertex = 0; vertex < (effectiveGridSize * effectiveGridSize - effectiveGridSize); vertex++) {

            if(vertex < effectiveGridSize) {
                int A = vertex;
                int B = A + effectiveGridSize;
                int C = B + 1;

                triangles[triangleIndexCounter] = A;
                triangles[triangleIndexCounter] = B;
                triangles[triangleIndexCounter] = C;

            } else if (vertex % effectiveGridSize != (effectiveGridSize - 1)) {
                //Triangolo Up
                int A = vertex;
                int B = A + effectiveGridSize;
                int C = B + 1;
                triangles[triangleIndexCounter] = A;
                triangles[triangleIndexCounter + 1] = B;
                triangles[triangleIndexCounter + 2] = C;

                //Triangolo Down
                B += 1;
                C = A + 1;
                triangles[triangleIndexCounter + 3] = A;
                triangles[triangleIndexCounter + 4] = B;
                triangles[triangleIndexCounter + 5] = C;

                triangleIndexCounter += 6;
            }
        }
        */

        return triangles;
    }

    private Vector2[] CreateUVs(int effectiveGridSize, Vector3[] vertices) {
        int effectiveGridSizeExtra = effectiveGridSize + 2;

        Vector2[] uvs = new Vector2[effectiveGridSizeExtra * effectiveGridSizeExtra];

        int index = 0;
        foreach (Vector3 vertex in vertices) {
            uvs[index] = new Vector2(vertex.x / (_GridSize * _CellSize), vertex.z / (_GridSize * _CellSize));
            index++;
        }

        return uvs;
    }

    private void OnDrawGizmosSelected() {
        if (mf == null) return;

        Gizmos.color = Color.green;
        foreach(Vector3 v in mf.mesh.vertices) {
            if(v != null) Gizmos.DrawWireSphere(v, 0.05f);
        }

        Gizmos.color = Color.red;
        for(int i = 0; i < mf.mesh.triangles.Length; i += 3) {
            int A = mf.mesh.triangles[i];
            int B = mf.mesh.triangles[i + 1];
            int C = mf.mesh.triangles[i + 2];
            Vector3 v1 = mf.mesh.vertices[A];
            Vector3 v2 = mf.mesh.vertices[B];
            Vector3 v3 = mf.mesh.vertices[C];

            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v3);
            Gizmos.DrawLine(v3, v1);
        }
    }
}
