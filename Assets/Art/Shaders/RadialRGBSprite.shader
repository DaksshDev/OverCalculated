Shader "Custom/RadialRGBSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Radial RGB Effect)]
        _Speed ("Rotation Speed", Float) = 1.0
        _Intensity ("Effect Intensity", Range(0, 2)) = 1.0
        _RadialScale ("Radial Scale", Range(0.1, 5)) = 1.0
        _ColorSeparation ("Color Separation", Range(0, 0.5)) = 0.1
        _CenterX ("Center X", Range(0, 1)) = 0.5
        _CenterY ("Center Y", Range(0, 1)) = 0.5
        
        [Header(Glow Effect)]
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.0
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowFalloff ("Glow Falloff", Range(0.1, 5)) = 1.0
        
        [Header(Sprite Rendering)]
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RadialRGBSprite"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Speed;
                float _Intensity;
                float _RadialScale;
                float _ColorSeparation;
                float _CenterX;
                float _CenterY;
                float _GlowIntensity;
                float4 _GlowColor;
                float _GlowFalloff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample the original sprite texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Calculate center offset
                float2 center = float2(_CenterX, _CenterY);
                float2 delta = input.uv - center;
                
                // Calculate angle and distance from center
                float angle = atan2(delta.y, delta.x);
                float dist = length(delta) * _RadialScale;
                
                // Animate the angle based on time
                float animatedAngle = angle + _Time.y * _Speed;
                
                // Create radial gradient channels with offset angles
                float r = sin(animatedAngle + 0.0) * 0.5 + 0.5;
                float g = sin(animatedAngle + 2.094) * 0.5 + 0.5; // 120 degrees
                float b = sin(animatedAngle + 4.189) * 0.5 + 0.5; // 240 degrees
                
                // Add chromatic aberration style separation
                float2 uvR = input.uv + normalize(delta) * _ColorSeparation * r;
                float2 uvG = input.uv + normalize(delta) * _ColorSeparation * g;
                float2 uvB = input.uv + normalize(delta) * _ColorSeparation * b;
                
                // Sample with separated UVs for chromatic effect
                half4 texR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvR);
                half4 texG = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvG);
                half4 texB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvB);
                
                // Combine chromatic channels
                half4 chromaticColor = half4(texR.r, texG.g, texB.b, texColor.a);
                
                // Create RGB gradient
                float3 rgbGradient = float3(r, g, b);
                
                // Mix gradient with sprite based on distance and intensity
                float mixFactor = saturate(dist * _Intensity);
                half3 finalColor = lerp(chromaticColor.rgb, chromaticColor.rgb * rgbGradient, mixFactor);
                
                // Calculate glow
                float glowFactor = pow(texColor.a, _GlowFalloff) * _GlowIntensity;
                float3 glow = _GlowColor.rgb * glowFactor * rgbGradient;
                
                // Add glow to final color
                finalColor += glow;
                
                // Apply tint and vertex color
                finalColor *= input.color.rgb;
                
                // Final output with original alpha
                half4 output = half4(finalColor, chromaticColor.a * input.color.a);
                
                #ifdef UNITY_UI_ALPHACLIP
                clip(output.a - 0.001);
                #endif
                
                return output;
            }
            ENDHLSL
        }
    }
    
    FallBack "Sprites/Default"
}