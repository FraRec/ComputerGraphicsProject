using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class OceanManager : MonoBehaviour {
    // Singleton
    public static OceanManager OceanManagerStatic;

    [Header("Ocean Properties:")]
    [SerializeField]
    private int _Resolution = 256;
    [SerializeField]
    private OceanSettings _OceanSettings;

    [Header("Compute Shaders:")]
    [SerializeField]
    private ComputeShader h0Shader;
    [SerializeField]
    private ComputeShader htShader;
    [SerializeField]
    private ComputeShader butterflyShader;
    [SerializeField]
    private ComputeShader fftShader;
    [SerializeField]
    private ComputeShader finalPassShader;
    [SerializeField]
    private ComputeShader normalMapShader;

    [Header("Noise Textures:")]
    [SerializeField]
    private List<Texture2D> _Noises;

    [Header("Material:")]
    [SerializeField]
    private Material _OceanMaterial;

    [Header("DEBUG:")]
    public RenderTexture _h0;
    public RenderTexture _hkt_dy;
    public RenderTexture _hkt_dx;
    public RenderTexture _hkt_dz;
    public RenderTexture _butterfly;
    public RenderTexture _pingpong0;
    public RenderTexture _pingpong1;
    public RenderTexture _displacements;
    public RenderTexture _normals;

    private OceanInstance ocean;
    private Camera mainCamera;

    private void Awake() {
        // Singleton Logic
        if (OceanManagerStatic == null) OceanManagerStatic = this;
        else                            Destroy(this.gameObject);

        // Initialize the Oceans
        ocean = new OceanInstance(_Resolution, this, h0Shader, htShader, butterflyShader, fftShader, finalPassShader, normalMapShader);
        ocean.Calculate_h0(_OceanSettings, _Noises);
        
        // --- DEBUG ---
        _h0        = ocean._h0;
        _butterfly = ocean._butterfly;
        
        // Create Ocean Plane
        mainCamera = Camera.main;
    }

    private void Update() {
        if (ocean != null) {
            ocean.Calculate_ht(_OceanSettings);
            
            // FFT
            ocean.Calculate_FFT(ocean._hkt_dy, ocean._buffer_dy);
            ocean.Calculate_FFT(ocean._hkt_dx, ocean._buffer_dx);
            ocean.Calculate_FFT(ocean._hkt_dz, ocean._buffer_dz);
            
            // FinalPass
            ocean.Calculate_FinalPass(ocean._buffer_dy, ocean._displacements_dy);
            ocean.Calculate_FinalPass(ocean._buffer_dx, ocean._displacements_dx);
            ocean.Calculate_FinalPass(ocean._buffer_dz, ocean._displacements_dz);
            
            // Displacements
            ocean.Merge_Displacements(ocean._displacements_dy, ocean._displacements_dx, ocean._displacements_dz, ocean._displacements);
            
            //Normals
            ocean.Calculate_Normals(ocean._displacements, ocean._normals);
            
            
            // Initialize Material
            _OceanMaterial.SetTexture("_Displacements", ocean._displacements);
            _OceanMaterial.SetTexture("_Normals", _normals);

            // --- DEBUG ---
            _hkt_dy = ocean._hkt_dy;
            _hkt_dx = ocean._hkt_dx;
            _hkt_dz = ocean._hkt_dz;
            _pingpong0 = ocean._pingpong0;
            _pingpong1 = ocean._pingpong1;
            _displacements = ocean._displacements;
            _normals = ocean._normals;
        }
    }

    // --- CALCULATE NOISE TEXTURE ---------------------------------------------------------------------------------------
    //    Texture2D GenerateNoiseTexture(int size, bool saveIntoAssetFile) {
    //        Texture2D noise = new Texture2D(size, size, TextureFormat.RGFloat, false, true);
    //        noise.filterMode = FilterMode.Point;
    //        for (int i = 0; i < size; i++) {
    //            for (int j = 0; j < size; j++) {
    //                noise.SetPixel(i, j, new Vector4(NormalRandom(), NormalRandom()));
    //            }
    //        }
    //        noise.Apply();
    //
    //#if UNITY_EDITOR
    //        if (saveIntoAssetFile) {
    //            string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString();
    //            string path = "Assets/Resources/GaussianNoiseTextures/";
    //            AssetDatabase.CreateAsset(noise, path + filename + ".asset");
    //            Debug.Log("Texture \"" + filename + "\" was created at path \"" + path + "\".");
    //        }
    //#endif
    //        return noise;
    //    }
    //
    //    Texture2D GetNoiseTexture(int size) {
    //        string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString();
    //        Texture2D noise = Resources.Load<Texture2D>("GaussianNoiseTextures/" + filename);
    //        return noise ? noise : GenerateNoiseTexture(size, true);
    //    }
    //
    //    float NormalRandom() {
    //        return Mathf.Cos(2 * Mathf.PI * Random.value) * Mathf.Sqrt(-2 * Mathf.Log(Random.value));
    //    }

    // --- CREATE NOISE TEXTURE ----------------------------------------------------------------------------------------
    public Texture2D GetNoiseTexture(int size, int index) {
        string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString() + "_" + index;
        Texture2D noise = Resources.Load<Texture2D>("GaussianNoiseTextures/" + filename);
        return noise ? noise : GenerateNoiseTexture(size, index, true);
    }

    public Texture2D GenerateNoiseTexture(int size, int index, bool saveIntoAssetFile) {
        Texture2D noise = new Texture2D(size, size, TextureFormat.RFloat, false, true);
        noise.filterMode = FilterMode.Point;
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                float normalRandom = NormalRandom();
                noise.SetPixel(i, j, new Vector4(1, 1, 1, 1) * normalRandom);
            }
        }
        noise.Apply();

        #if UNITY_EDITOR
        if (saveIntoAssetFile) {
            string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString() + "_" + index;
            string path = "Assets/Resources/GaussianNoiseTextures/";
            AssetDatabase.CreateAsset(noise, path + filename + ".asset");
            Debug.Log("Texture \"" + filename + "\" was created at path \"" + path + "\".");
        }
        #endif

        return noise;
    }

    float NormalRandom() {
        return Mathf.Cos(2 * Mathf.PI * Random.value) * Mathf.Sqrt(-2 * Mathf.Log(Random.value));
    }

    // --- CREATE RENDER TEXTURE ---------------------------------------------------------------------------------------
    public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format = RenderTextureFormat.RGFloat, FilterMode filterMode = FilterMode.Point, bool useMips = false) {
        RenderTexture rt = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
        rt.useMipMap = useMips;
        rt.autoGenerateMips = false;
        rt.anisoLevel = 6;
        rt.filterMode = filterMode;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    public static RenderTexture CreateRenderTexture(int width, int height, RenderTextureFormat format = RenderTextureFormat.RGFloat, FilterMode filterMode = FilterMode.Point, bool useMips = false) {
        RenderTexture rt = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
        rt.useMipMap = useMips;
        rt.autoGenerateMips = false;
        rt.anisoLevel = 6;
        rt.filterMode = filterMode;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }
}
