Shader "Custom/OceanShader"
{
    Properties {
        _Color("Color", Color) = (1, 1, 1, 1)
        _MaxGloss("Max Gloss", Range(0, 1)) = 0
        _LOD_scale("LOD_scale", Range(0, 10)) = 7.13
        _LengthScale("LengthScale", Range(0, 200)) = 40.0

        [Header(Sub Surface Scattering)]
        _SSSColor("SSS Color", Color) = (1, 1, 1, 1)
        _SSSStrength("SSS Strength", Range(0, 1)) = 0.2
        _SSSScale("SSS Scale", Range(0.1,50)) = 4.0
        _SSSBase("SSS Base", Range(-5,1)) = 0
        
        [Header(Roughness)]
        _Roughness("Distant Roughness", Range(0, 1)) = 0
        _RoughnessScale("Roughness Scale", Range(0, 0.01)) = 0.1

        [Header(Textures)]
        _Displacements("Displacements", 2D) = "black" {}
        _Normals("Normals", 2D)             = "bump" {}
        _ReflectionMap("ReflectionMap", 2D) = "white" {}
        [Cubemap]
        [NoScaleOffset] _CubeMap("Cubemap", Cube) = "grey" {}
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline"  }
        LOD 100

        Pass {
            Name "ForwardLitURP"    // For Debugging
            Tags{ "LightMode" = "UniversalForward"}	//Pass specific tags

            //Blend SrcAlpha OneMinusSrcAlpha
            //ZWrite On
            Cull Off

            HLSLPROGRAM
            #define _SPECULAR_COLOR
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOW_CASCADE
            #pragma multi_compile_fragment _ _SHADOW_SOFT
            #pragma multi_compile_fog
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes {
                float3 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Interpolators {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float3 viewVector   : TEXCOORD3;
                float4 lodScales    : TEXCOORD4;
                float4 screenPos    : TEXCOORD5;
                float2 uv           : TEXCOORD6;
                float2 worldUV      : TEXCOORD0;
            };

            float _SSSBase;
            float _SSSScale;
            float _LOD_scale;
            float _LengthScale;

            // Texture Variables
            sampler2D _Displacements;

            Interpolators Vertex(Attributes input) {
                Interpolators output = (Interpolators)0;

                float3 worldPos = TransformObjectToWorld(input.positionOS);
                float4 worldUV = float4(worldPos.xz, 0, 0);
                output.worldUV = worldUV.xy;

                output.viewVector = GetCameraPositionWS() - worldPos;
                float viewDst = length(output.viewVector);

                float lod_c = min(_LOD_scale * _LengthScale / viewDst, 1); // 7.13 * 40 / viewDist

                float largeWaveBias = 0;


                // Displacements
                float3 displacement = (float3)0;
                //displacement.y =  tex2Dlod(_Displacements, float4(input.uv, 0, 0)).y;
                //displacement.x = -tex2Dlod(_Displacements, float4(input.uv, 0, 0)).x;
                //displacement.z = -tex2Dlod(_Displacements, float4(input.uv, 0, 0)).z;
                displacement = tex2Dlod(_Displacements, worldUV / _LengthScale) * lod_c;
                displacement.x *= -1;
                displacement.z *= -1;

                
                float3 displacedPosOS = input.positionOS.xyz + displacement;
                VertexPositionInputs posnInputs = GetVertexPositionInputs(displacedPosOS);
                output.positionCS = posnInputs.positionCS;
                output.positionWS = posnInputs.positionWS;
                output.screenPos  = ComputeScreenPos(posnInputs.positionCS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                
                output.uv = input.uv;
                output.lodScales = float4(0, 0, lod_c, max(displacement.y - largeWaveBias * 0.8f - _SSSBase, 0) / _SSSScale);

                return output;
            }

            // Float Variables
            float _SSSStrength;
            float _Roughness, _RoughnessScale, _MaxGloss;
            float4 _Color, _SSSColor, _ColorDark;
            TEXTURECUBE(_CubeMap); SAMPLER(sampler_CubeMap);
            float4 _CubeMap_ST;

            // Texture Variables
            sampler2D _Normals;
            sampler2D _ReflectionMap;

            float pow5(float f) {
                return f * f * f * f * f;
            }

            float4 Fragment(Interpolators input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_TARGET{
                // --- INPUT DATA ------------------------------------------------------------------------------------
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS      = input.positionWS;
                lightingInput.viewDirectionWS = normalize(input.viewVector);
                lightingInput.shadowCoord     = TransformWorldToShadowCoord(input.positionWS);
                lightingInput.positionCS      = input.positionCS;

                // Normals
                float3 worldNormal = normalize(tex2Dlod(_Normals, float4(input.worldUV, 0, 0) / _LengthScale).xzy);
                //float3 worldNormal = tex2Dlod(_Normals, float4(input.uv, 0, 0)) * 1.5f;
                //worldNormal = normalize(worldNormal);
                //worldNormal = UnpackNormal(float4(worldNormal, 1));
                //worldNormal = TransformObjectToWorldNormal(worldNormal);
                lightingInput.normalWS = worldNormal; //?

                // Fresnel
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float fresnel = dot(worldNormal, viewDir);
                fresnel = saturate(1 - fresnel);
                fresnel = pow5(fresnel);

                // --- SURFACE DATA ----------------------------------------------------------------------------------
                SurfaceData surfaceInput = (SurfaceData)0;
                surfaceInput.albedo = 0;
                float distanceGloss = lerp(1 - _Roughness, _MaxGloss, 1 / (1 + length(input.viewVector) * _RoughnessScale));
                surfaceInput.smoothness = distanceGloss;
                surfaceInput.metallic = 0;
                surfaceInput.alpha = 1;
                surfaceInput.specular = 1;
                 
                // Color
                float3 H = normalize(-worldNormal + _MainLightPosition.xyz);
                float ViewDotH = pow5(saturate(dot(viewDir, -H))) * 30 * _SSSStrength;
                float3 color = lerp(_Color.xyz, saturate(_Color.xyz + _SSSColor.rgb * ViewDotH * input.lodScales.w).xyz, input.lodScales.z);
                float3 finalEmission = color * (1 - fresnel);

                // Reflection
                float2 screenUVs = input.screenPos.xy / input.screenPos.w;
                float3 refractedUVs = float3(screenUVs, 0) + worldNormal * 0.015f;
                
                float3 sceneReflection = tex2D(_ReflectionMap, refractedUVs);
                float4 _SampleCubemap_Out = SAMPLE_TEXTURECUBE_LOD(_CubeMap, sampler_CubeMap, reflect(-viewDir, worldNormal), 0);   // Skybox Cubemap for Reflection

                //float3 frontColor = lerp(finalEmission, sceneReflection.xyz * 0.25f, fresnel);
                float3 frontColor = lerp(finalEmission, _SampleCubemap_Out.xyz * 0.15f, saturate(fresnel - ViewDotH * 0.07f));

                //Output
                surfaceInput.emission = frontColor;

                return UniversalFragmentPBR(lightingInput, surfaceInput);
                //return float4(color, 1);
                //return float4(1, 1, 1, 1) * saturate(fresnel - ViewDotH);
            }

            ENDHLSL
        }

        /*
            struct SurfaceData
            {
                half3 albedo;
                half3 specular;
                half  metallic;
                half  smoothness;
                half3 normalTS;
                half3 emission;
                half  occlusion;
                half  alpha;
                half  clearCoatMask;
                half  clearCoatSmoothness;
            };

            struct InputData
            {
                float3  positionWS;
                float4  positionCS;
                float3  normalWS;
                half3   viewDirectionWS;
                float4  shadowCoord;
            }
        */

        /*
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _Displacement;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
        */
    }
    FallBack "Diffuse"
}
