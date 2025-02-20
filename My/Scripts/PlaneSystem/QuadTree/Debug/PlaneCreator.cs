using UnityEngine;
using UnityEditor;


public class PlaneCreator : MonoBehaviour {
    [HideInInspector]
    public Transform origin;

    [Header("Plane Properties")]
    [SerializeField]
    public int gridSize;
    [SerializeField]
    public float cellSize;
    [SerializeField]
    public Transform center;

    private MeshFilter mf;
    private MeshRenderer mr;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    private int effectiveGridSize;

    private void Awake() {
        origin = this.GetComponent<Transform>();
        mf = this.GetComponent<MeshFilter>();
        mr = this.GetComponent<MeshRenderer>();

        effectiveGridSize = gridSize + 1;
        CreateMesh();
        
        mf.mesh.RecalculateBounds();
        mf.mesh.RecalculateNormals();
    }

    private void Update() {
        //Vector3 offset;
        //if (center != null)
        //    offset = center.transform.position;
        //else
        //    offset = Vector3.zero;
        //float mid = -(gridSize * cellSize) / 2;
        //this.transform.position = new Vector3(mid + offset.x, 0, mid + offset.z);
    }

    private void CreateMesh() {
        CreateVertices();
        CreateTriangles();
        CreateUVs(); 

        mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mf.mesh.vertices = vertices;
        mf.mesh.triangles = triangles;
        mf.mesh.uv = uvs;
    }

    private void CreateVertices() {
        vertices = new Vector3[effectiveGridSize * effectiveGridSize];
        Vector3 newPos;
        int i, j;
        for (j = 0; j < effectiveGridSize; j++)
            for(i = 0; i < effectiveGridSize; i++) {
                newPos = new Vector3(cellSize * i, 0, cellSize * j);
                vertices[j * effectiveGridSize + i] = newPos;
            }
    }

    private void CreateTriangles() {
        triangles = new int[3 * 2 * (effectiveGridSize * effectiveGridSize - (effectiveGridSize * 2) + 1)];
        int triangleIndexCounter = 0;
        int vertex;
        for(vertex = 0; vertex < (effectiveGridSize * effectiveGridSize - effectiveGridSize); vertex++) {
            if (vertex % effectiveGridSize != (effectiveGridSize - 1)) {
                //Triangolo Up
                int A = vertex;
                int B = A + effectiveGridSize;
                int C = B + 1;
                triangles[triangleIndexCounter]     = A;
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
    }

    private void CreateUVs() {
        uvs = new Vector2[effectiveGridSize * effectiveGridSize];

        int index = 0;
        foreach (Vector3 vertex in vertices) {
            uvs[index] = new Vector2(vertex.x / (gridSize * cellSize), vertex.z / (gridSize * cellSize));
            index++;
        }
    }
}
