using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TerrainChunk {
    public Vector2 Position    { get; }
    public GameObject Chunk    { get; }

    public TerrainChunk(Vector2 position, GameObject chunk) {
        this.Position = position;
        this.Chunk = chunk;
    }
}

public class QuadTreeManager : MonoBehaviour {

    [Header("References:")]
    [SerializeField]
    private GameObject _ObjToFollow;
    [SerializeField]
    private Material _TerrainChunkMaterial;
    [SerializeField]
    private Material _TerrainChunkMaterialLight;

    [Header("Properties:")]
    [SerializeField]
    private float minNodeSize = 500;
    [SerializeField]
    private float rootQuadSize = 2000;
    [SerializeField]
    private int _GridSize = 100;

    [Header("Debug:")]
    [SerializeField]
    private bool _IsGizmosActive;

    private QuadTree _QuadTree;
    private TerrainChunkCreator _TCC;
    private Dictionary<string, TerrainChunk> _TerrainChunks;
    private Dictionary<int, int> gridSizeDividingFactor;
    private Dictionary<int, Material> chunkMaterials;

    private void Awake() {
        gridSizeDividingFactor = new Dictionary<int, int>();
        gridSizeDividingFactor[1] = Mathf.RoundToInt(_GridSize);
        gridSizeDividingFactor[2] = Mathf.RoundToInt(1.0f * gridSizeDividingFactor[1]);
        gridSizeDividingFactor[4] = Mathf.RoundToInt(0.5f * gridSizeDividingFactor[2]);
        gridSizeDividingFactor[8] = Mathf.RoundToInt(0.25f * gridSizeDividingFactor[4]);
        gridSizeDividingFactor[16] = Mathf.RoundToInt(0.25f * gridSizeDividingFactor[8]);
        gridSizeDividingFactor[32] = Mathf.RoundToInt(0.25f * gridSizeDividingFactor[16]);
        gridSizeDividingFactor[64] = Mathf.RoundToInt(0.25f * gridSizeDividingFactor[32]);

        chunkMaterials = new Dictionary<int, Material>();
        chunkMaterials[1] = _TerrainChunkMaterial;
        chunkMaterials[2] = _TerrainChunkMaterialLight;
        chunkMaterials[4] = _TerrainChunkMaterialLight;
        chunkMaterials[8] = _TerrainChunkMaterialLight;
        chunkMaterials[16] = _TerrainChunkMaterialLight;
        chunkMaterials[32] = _TerrainChunkMaterialLight;
        chunkMaterials[64] = _TerrainChunkMaterialLight;


        _TCC = new TerrainChunkCreator(this);

        _TerrainChunks = new Dictionary<string, TerrainChunk>();
        Vector2 max = new Vector2(this.transform.position.x + rootQuadSize / 2f, this.transform.position.y + rootQuadSize / 2f);
        Vector2 min = new Vector2(this.transform.position.x - rootQuadSize / 2f, this.transform.position.y - rootQuadSize / 2f);
        this._QuadTree = new QuadTree(min, max, minNodeSize);
    }

    private void Update() {
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks() {
        // Update QuadTree
        this._QuadTree.Insert(_ObjToFollow.transform.position, this.transform.position.y);
        List<Quad> children = this._QuadTree.GetChildren();

        float minChunkSize = rootQuadSize;
        foreach(Quad c in children) {
            if (minChunkSize > c.Size) minChunkSize = c.Size;
        }
        float minChunkL = minChunkSize * Mathf.Sqrt(2) * 0.5f;

        // Define Dictionary based on the last QuadTree Update
        Dictionary<string, Quad> newTerrainChunks = new Dictionary<string, Quad>();
        foreach(Quad c in children) {
            string k = KeyGenerator(c);
            newTerrainChunks[k] = c;
        }

        // Define difference (in terms of Chunks) between old QuadTree and current QuadTree
        Dictionary<string, Quad> terrainChunksToAdd = new Dictionary<string, Quad>();
        Dictionary<string, TerrainChunk> terrainChunksToRemove = new Dictionary<string, TerrainChunk>();
        TerrainChunkDictDifference(newTerrainChunks, this._TerrainChunks, ref terrainChunksToAdd, ref terrainChunksToRemove);

        // Removing Terrain Chunks
        foreach (string k in terrainChunksToRemove.Keys) {
            TerrainChunk terrainChunk = this._TerrainChunks[k];
            this._TerrainChunks.Remove(k);
            Destroy(terrainChunk.Chunk.gameObject);
        }

        // Adding Terrain Chunks
        foreach (string k in terrainChunksToAdd.Keys) {
            float xp = terrainChunksToAdd[k].Center.x;
            float zp = terrainChunksToAdd[k].Center.y;

            float currentChunkL = terrainChunksToAdd[k].Size * Mathf.Sqrt(2) * 0.5f;
            int chunkDepth = Mathf.RoundToInt(currentChunkL / (minChunkL));
            //Debug.Log("chunkDepth = " + chunkDepth);
            chunkDepth = Mathf.Clamp(chunkDepth, 1, 64);

            Vector2 offset = new Vector2(xp, zp);
            TerrainChunk terrainChunk = new TerrainChunk(
                offset, 
                _TCC.CreateTerrainChunk(offset, terrainChunksToAdd[k].Size, gridSizeDividingFactor[chunkDepth], chunkMaterials[chunkDepth])
                //_TCC.CreateTerrainChunk(offset, terrainChunksToAdd[k].Size, _GridSize)
            );

            this._TerrainChunks[k] = terrainChunk;
        }
    }

    private string KeyGenerator(Quad node) {
        return (node.Center.x).ToString() + "-" + (node.Center.y).ToString();
    }

    private void TerrainChunkDictDifference(Dictionary<string, Quad> newTerrainChunks, Dictionary<string, TerrainChunk> terrainChunks, ref Dictionary<string, Quad> terrainChunksToAdd, ref Dictionary<string, TerrainChunk> terrainChunksToRemove) {
        foreach(string k in newTerrainChunks.Keys) {
            if (!terrainChunks.ContainsKey(k)) {
                terrainChunksToAdd[k] = newTerrainChunks[k];
            }
        }

        foreach(string k in terrainChunks.Keys) {
            if (!newTerrainChunks.ContainsKey(k)) {
                terrainChunksToRemove[k] = terrainChunks[k];
            }
        }
    }

    private void OnDrawGizmos() {
        if (_QuadTree == null) return;

        if (_IsGizmosActive) {
            List<Quad> leaf = _QuadTree.GetChildren();

            Gizmos.color = Color.green;
            foreach (Quad quad in leaf) {
                //Debug.Log("Quad Size = " + (quad.Size * Mathf.Sqrt(2) / 2));

                Vector3 v1 = new Vector3(quad.Bounds.MIN.x, this.transform.position.y, quad.Bounds.MIN.y);
                Vector3 v2 = new Vector3(quad.Bounds.MIN.x, this.transform.position.y, quad.Bounds.MAX.y);
                Vector3 v3 = new Vector3(quad.Bounds.MAX.x, this.transform.position.y, quad.Bounds.MAX.y);
                Vector3 v4 = new Vector3(quad.Bounds.MAX.x, this.transform.position.y, quad.Bounds.MIN.y);

                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v4);
                Gizmos.DrawLine(v4, v1);
            }
        }
    }

}
