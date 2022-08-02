Shader "palewhite/ParallaxSkybox" {
    Properties {
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("CullMode", Int) = 2
        [Enum(Off, 0, On, 1)] _ZWriteMode("ZWriteMode", Float) = 1
        [KeywordEnum(ADD,MULTIPLY,OVERLAY,REPLACE)] _ENVMIX("Panorama Image MixMode", float) = 0
        [Space(5)]
        _EnvImgNear("Panorama Near Image", 2D) = "gray" {}
        _EnvScaleNear("Panorama Near Image Scale", Range(0, 1)) = 1
        _EnvRotationNear("Panorama Near Image Rotation", Range(0, 1)) = 0
        _EnvScrollNear("Panorama Near Image Scroll", Float) = 0
        _EnvOffsetNear("Panorama Near Image Offset", Range(-1, 1)) = 0
        [Space(5)]
        _EnvImgMid("Panorama Mid Image", 2D) = "gray" {}
        _EnvScaleMid("Panorama Mid Image Scale", Range(0, 1)) = 1
        _EnvRotationMid("Panorama Mid Image Rotation", Range(0, 1)) = 0
        _EnvScrollMid("Panorama Mid Image Scroll", Float) = 0
        _EnvOffsetMid("Panorama Mid Image Offset", Range(-1, 1)) = 0
        [Space(5)]
        _EnvImgFar("Panorama Far Image", 2D) = "gray" {}
        _EnvScaleFar("Panorama Far Image Scale", Range(0, 1)) = 1
        _EnvRotationFar("Panorama Far Image Rotation", Range(0, 1)) = 0
        _EnvScrollFar("Panorama Far Image Scroll", Float) = 0
        _EnvOffsetFar("Panorama Far Image Offset", Range(-1, 1)) = 0
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        Pass {
            LOD 200
            Cull[_CullMode]
            ZWrite[_ZWriteMode]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal vulkan
            #pragma target 3.0
            #pragma shader_feature_local _ENVMIX_ADD _ENVMIX_MULTIPLY _ENVMIX_OVERLAY _ENVMIX_REPLACE

            uniform sampler2D _EnvImgNear;
            uniform float4 _EnvImgNear_ST;
            uniform float _EnvScaleNear;
            uniform float _EnvRotationNear;
            uniform float _EnvScrollNear;
            uniform float _EnvOffsetNear;
            uniform sampler2D _EnvImgMid;
            uniform float4 _EnvImgMid_ST;
            uniform float _EnvScaleMid;
            uniform float _EnvRotationMid;
            uniform float _EnvScrollMid;
            uniform float _EnvOffsetMid;
            uniform sampler2D _EnvImgFar;
            uniform float4 _EnvImgFar_ST;
            uniform float _EnvScaleFar;
            uniform float _EnvRotationFar;
            uniform float _EnvScrollFar;
            uniform float _EnvOffsetFar;

            struct VertexInput {
            float4 vertex : POSITION;
        };

            struct VertexOutput {
            float4 pos : SV_POSITION;
            float4 posWorld : TEXCOORD1;
        };

            inline float overlay(float base, float over) {
            base = base < 0.5 ? 2.0 * base * over : 1.0 - 2.0 * (1.0 - base) * (1.0 - over);
            return base;
        }

            inline float2 spherecoods(float3 viewDir) {
            viewDir = normalize(viewDir);
            float vertangle = asin(viewDir.y) * -1;
            float latitude = (vertangle + UNITY_HALF_PI) / UNITY_PI;
            float horizonangle = viewDir.z >= 0 ? atan(viewDir.x / viewDir.z) : atan(viewDir.x / viewDir.z) + UNITY_PI;
            float longitude = (horizonangle + UNITY_HALF_PI) / UNITY_PI * 0.5;
            return float2(longitude, latitude);
        }

            VertexOutput vert(VertexInput v) {
            VertexOutput o = (VertexOutput)0;
            o.posWorld = mul(unity_ObjectToWorld, v.vertex);
            o.pos = UnityObjectToClipPos(v.vertex);
            return o;
        }

            float4 frag(VertexOutput i) : SV_TARGET {
            float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
            float3 sphereDirection = viewDirection;

            float3 sphereDirectionNear = mul((float3x3)UNITY_MATRIX_V, sphereDirection);
            float3 sphereDirectionMid = mul((float3x3)UNITY_MATRIX_V, sphereDirection);
            float3 sphereDirectionFar = mul((float3x3)UNITY_MATRIX_V, sphereDirection);
            sphereDirectionNear += float3(0, 0, _EnvOffsetNear);
            sphereDirectionMid += float3(0, 0, _EnvOffsetMid);
            sphereDirectionFar += float3(0, 0, _EnvOffsetFar);
            sphereDirectionNear = mul(sphereDirectionNear, (float3x3)UNITY_MATRIX_V);
            sphereDirectionMid = mul(sphereDirectionMid, (float3x3)UNITY_MATRIX_V);
            sphereDirectionFar = mul(sphereDirectionFar, (float3x3)UNITY_MATRIX_V);

            float2 envUVNear = spherecoods(sphereDirectionNear);
            float2 envUVMid = spherecoods(sphereDirectionMid);
            float2 envUVFar = spherecoods(sphereDirectionFar);
            envUVNear.x = frac(envUVNear.x + _EnvRotationNear + (_Time.x * 1.2 * _EnvScrollNear));
            envUVMid.x = frac(envUVMid.x + _EnvRotationMid + (_Time.x * 1.2 * _EnvScrollMid));
            envUVFar.x = frac(envUVFar.x + _EnvRotationFar + (_Time.x * 1.2 * _EnvScrollFar));

            float4 _EnvImgNear_var = tex2D(_EnvImgNear, TRANSFORM_TEX(envUVNear, _EnvImgNear));
            float4 _EnvImgMid_var = tex2D(_EnvImgMid, TRANSFORM_TEX(envUVMid, _EnvImgMid));
            float4 _EnvImgFar_var = tex2D(_EnvImgFar, TRANSFORM_TEX(envUVFar, _EnvImgFar));

            #ifdef _ENVMIX_ADD
                float3 envColor = 0;
                envColor += lerp(0, _EnvImgFar_var.rgb, _EnvScaleFar * _EnvImgFar_var.a);
                envColor += lerp(0, _EnvImgMid_var.rgb, _EnvScaleMid * _EnvImgMid_var.a);
                envColor += lerp(0, _EnvImgNear_var.rgb, _EnvScaleNear * _EnvImgNear_var.a);
            #elif _ENVMIX_MULTIPLY
                float3 envColor = 1;
                envColor *= lerp(1, _EnvImgFar_var.rgb, _EnvScaleFar * _EnvImgFar_var.a);
                envColor *= lerp(1, _EnvImgMid_var.rgb, _EnvScaleMid * _EnvImgMid_var.a);
                envColor *= lerp(1, _EnvImgNear_var.rgb, _EnvScaleNear * _EnvImgNear_var.a);
            #elif _ENVMIX_OVERLAY
                float3 envColor = 0.5;
                envColor.r = overlay(envColor.r, lerp(0.5, _EnvImgFar_var.r, _EnvScaleFar * _EnvImgFar_var.a));
                envColor.g = overlay(envColor.g, lerp(0.5, _EnvImgFar_var.g, _EnvScaleFar * _EnvImgFar_var.a));
                envColor.b = overlay(envColor.b, lerp(0.5, _EnvImgFar_var.b, _EnvScaleFar * _EnvImgFar_var.a));
                envColor.r = overlay(envColor.r, lerp(0.5, _EnvImgMid_var.r, _EnvScaleMid * _EnvImgMid_var.a));
                envColor.g = overlay(envColor.g, lerp(0.5, _EnvImgMid_var.g, _EnvScaleMid * _EnvImgMid_var.a));
                envColor.b = overlay(envColor.b, lerp(0.5, _EnvImgMid_var.b, _EnvScaleMid * _EnvImgMid_var.a));
                envColor.r = overlay(envColor.r, lerp(0.5, _EnvImgNear_var.r, _EnvScaleNear * _EnvImgNear_var.a));
                envColor.g = overlay(envColor.g, lerp(0.5, _EnvImgNear_var.g, _EnvScaleNear * _EnvImgNear_var.a));
                envColor.b = overlay(envColor.b, lerp(0.5, _EnvImgNear_var.b, _EnvScaleNear * _EnvImgNear_var.a));
            #elif _ENVMIX_REPLACE
                float3 envColor = 0.5;
                envColor = lerp(envColor, _EnvImgFar_var.rgb, _EnvScaleFar * _EnvImgFar_var.a);
                envColor = lerp(envColor, _EnvImgMid_var.rgb, _EnvScaleMid * _EnvImgMid_var.a);
                envColor = lerp(envColor, _EnvImgNear_var.rgb, _EnvScaleNear * _EnvImgNear_var.a);
            #endif
                envColor = saturate(envColor);

            return float4(envColor, 1);
        }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
}
