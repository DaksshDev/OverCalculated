Shader "UI/MathematicalCardShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Mathematical Colors)]
        _PrimaryColor ("Primary Color (Base)", Color) = (0.1, 0.15, 0.25, 1.0)
        _SecondaryColor ("Secondary Color (Accents)", Color) = (0.2, 0.6, 0.9, 1.0)
        _TertiaryColor ("Tertiary Color (Highlights)", Color) = (0.8, 0.9, 1.0, 1.0)
        _GridColor ("Grid/Pattern Color", Color) = (0.15, 0.3, 0.5, 1.0)
        
        [Header(Fractal Settings)]
        _FractalComplexity ("Fractal Iterations", Range(1, 8)) = 4
        _FractalScale ("Fractal Scale", Range(0.5, 10)) = 2.5
        _FractalSpeed ("Animation Speed", Range(0, 5)) = 0.8
        _FractalDistortion ("Distortion", Range(0, 2)) = 0.5
        
        [Header(Mathematical Patterns)]
        [KeywordEnum(Mandelbrot, Julia, Fibonacci, Voronoi, Grid)] _PatternType ("Pattern Type", Float) = 0
        _GridDensity ("Grid Density", Range(1, 50)) = 12.0
        _PatternMix ("Pattern Mix", Range(0, 1)) = 0.7
        
        [Header(Dithering)]
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.15
        _DitherScale ("Dither Scale", Range(1, 20)) = 8.0
        [Toggle] _OrderedDither ("Use Ordered Dither", Float) = 1
        
        [Header(Visual Effects)]
        _Contrast ("Contrast", Range(0.1, 5)) = 1.8
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _EdgeGlow ("Edge Glow", Range(0, 1)) = 0.3
        _NoiseAmount ("Noise Detail", Range(0, 1)) = 0.2
        
        [Header(Animation)]
        [Toggle] _Animate ("Animate Pattern", Float) = 1
        _RotationSpeed ("Rotation Speed", Range(-5, 5)) = 0.5
        
        // UI Material properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "MathematicalCard"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_local _PATTERNTYPE_MANDELBROT _PATTERNTYPE_JULIA _PATTERNTYPE_FIBONACCI _PATTERNTYPE_VORONOI _PATTERNTYPE_GRID

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _PrimaryColor;
                float4 _SecondaryColor;
                float4 _TertiaryColor;
                float4 _GridColor;
                float _FractalComplexity;
                float _FractalScale;
                float _FractalSpeed;
                float _FractalDistortion;
                float _PatternType;
                float _GridDensity;
                float _PatternMix;
                float _DitherStrength;
                float _DitherScale;
                float _OrderedDither;
                float _Contrast;
                float _Brightness;
                float _EdgeGlow;
                float _NoiseAmount;
                float _Animate;
                float _RotationSpeed;
            CBUFFER_END

            #define PI 3.14159265359
            #define PHI 1.618033988749895

            // Hash function for pseudo-random numbers
            float hash(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac(p.x * p.y);
            }

            // 2D Rotation matrix
            float2x2 rot2D(float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2x2(c, -s, s, c);
            }

            // Ordered dithering matrix (Bayer 4x4)
            float orderedDither(float2 screenPos)
            {
                int x = int(fmod(screenPos.x, 4.0));
                int y = int(fmod(screenPos.y, 4.0));
                
                const float bayerMatrix[16] = {
                    0.0/16.0, 8.0/16.0, 2.0/16.0, 10.0/16.0,
                    12.0/16.0, 4.0/16.0, 14.0/16.0, 6.0/16.0,
                    3.0/16.0, 11.0/16.0, 1.0/16.0, 9.0/16.0,
                    15.0/16.0, 7.0/16.0, 13.0/16.0, 5.0/16.0
                };
                
                return bayerMatrix[y * 4 + x];
            }

            // Random dithering
            float randomDither(float2 screenPos)
            {
                return hash(screenPos);
            }

            // Mandelbrot fractal
            float mandelbrot(float2 c, float time)
            {
                float2 z = float2(0.0, 0.0);
                float iterations = 0.0;
                int maxIter = int(_FractalComplexity * 8.0);
                
                [loop]
                for(int i = 0; i < 64; i++)
                {
                    if(i >= maxIter || dot(z, z) > 4.0) break;
                    z = float2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;
                    iterations += 1.0;
                }
                
                return iterations / float(maxIter);
            }

            // Julia set fractal
            float julia(float2 z, float time)
            {
                float2 c = float2(cos(time * 0.3) * 0.7, sin(time * 0.5) * 0.3);
                float iterations = 0.0;
                int maxIter = int(_FractalComplexity * 8.0);
                
                [loop]
                for(int i = 0; i < 64; i++)
                {
                    if(i >= maxIter || dot(z, z) > 4.0) break;
                    z = float2(z.x * z.x - z.y * z.y, 2.0 * z.x * z.y) + c;
                    iterations += 1.0;
                }
                
                return iterations / float(maxIter);
            }

            // Fibonacci spiral pattern
            float fibonacci(float2 p, float time)
            {
                float angle = atan2(p.y, p.x);
                float radius = length(p);
                
                float spiral = fmod(angle + log(radius + 0.1) * PHI + time * 0.5, PI * 2.0);
                float pattern = abs(sin(spiral * 5.0 + radius * 3.0));
                
                return pattern;
            }

            // Voronoi cells
            float voronoi(float2 p, float time)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float minDist = 1.0;
                
                for(int y = -1; y <= 1; y++)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float h = hash(i + neighbor);
                        float2 offset = float2(
                            frac(h * 43758.5453 + sin(time * 0.2)),
                            frac(h * 23421.631 + cos(time * 0.3))
                        );
                        float2 diff = neighbor + offset - f;
                        float dist = length(diff);
                        minDist = min(minDist, dist);
                    }
                }
                
                return minDist;
            }

            // Mathematical grid
            float mathGrid(float2 p, float time)
            {
                float2 grid = frac(p * _GridDensity);
                float lines = min(
                    smoothstep(0.0, 0.05, grid.x) * smoothstep(0.0, 0.05, 1.0 - grid.x),
                    smoothstep(0.0, 0.05, grid.y) * smoothstep(0.0, 0.05, 1.0 - grid.y)
                );
                
                // Add intersection points
                float points = smoothstep(0.1, 0.0, length(grid - 0.5));
                
                return 1.0 - lines + points * 0.5;
            }

            // Fractal noise
            float fractalNoise(float2 p, float time)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                int complexity = int(_FractalComplexity);
                
                [loop]
                for(int i = 0; i < 8; i++)
                {
                    if(i >= complexity) break;
                    value += amplitude * (hash(p * frequency + time * 0.1) * 2.0 - 1.0);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value * 0.5 + 0.5;
            }

            // Calculate pattern (separated for edge detection)
            float calculatePattern(float2 uv, float time)
            {
                float pattern = 0.0;
                
                #if defined(_PATTERNTYPE_MANDELBROT)
                    pattern = mandelbrot(uv * 0.5, time);
                #elif defined(_PATTERNTYPE_JULIA)
                    pattern = julia(uv, time);
                #elif defined(_PATTERNTYPE_FIBONACCI)
                    pattern = fibonacci(uv, time);
                #elif defined(_PATTERNTYPE_VORONOI)
                    pattern = voronoi(uv, time);
                #elif defined(_PATTERNTYPE_GRID)
                    pattern = mathGrid(uv, time);
                #else
                    pattern = mandelbrot(uv * 0.5, time);
                #endif
                
                return pattern;
            }

            v2f vert(appdata v)
            {
                v2f o;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInput.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.screenPos = ComputeScreenPos(o.vertex);
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float time = _Animate > 0.5 ? _Time.y * _FractalSpeed : 0.0;
                
                // Calculate UV coordinates
                float2 uv = (i.uv - 0.5) * _FractalScale;
                
                // Apply rotation
                if(_Animate > 0.5)
                {
                    uv = mul(rot2D(time * _RotationSpeed * 0.2), uv);
                }
                
                // Add distortion
                float2 distortion = float2(
                    sin(uv.y * 3.0 + time) * _FractalDistortion,
                    cos(uv.x * 3.0 + time) * _FractalDistortion
                ) * 0.1;
                uv += distortion;
                
                // Calculate main pattern
                float pattern = calculatePattern(uv, time);
                
                // Add fractal noise detail
                float noise = fractalNoise(uv * 2.0, time);
                pattern = lerp(pattern, noise, _NoiseAmount);
                
                // Apply contrast
                pattern = pow(saturate(pattern), _Contrast);
                
                // Edge detection for glow
                float2 e = float2(0.01, 0.0);
                float edge = length(float2(
                    pattern - calculatePattern(uv + e.xy, time),
                    pattern - calculatePattern(uv + e.yx, time)
                ));
                edge = saturate(edge * 10.0) * _EdgeGlow;
                
                // Apply dithering
                float2 screenPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                float dither = _OrderedDither > 0.5 ? 
                    orderedDither(screenPos * _DitherScale) : 
                    randomDither(screenPos * _DitherScale);
                
                pattern += (dither - 0.5) * _DitherStrength;
                pattern = saturate(pattern);
                
                // Multi-level color mapping
                half4 finalColor;
                
                if(pattern < 0.33)
                {
                    float t = pattern / 0.33;
                    finalColor = lerp(_PrimaryColor, _GridColor, t);
                }
                else if(pattern < 0.66)
                {
                    float t = (pattern - 0.33) / 0.33;
                    finalColor = lerp(_GridColor, _SecondaryColor, t);
                }
                else
                {
                    float t = (pattern - 0.66) / 0.34;
                    finalColor = lerp(_SecondaryColor, _TertiaryColor, t);
                }
                
                // Add edge glow
                finalColor.rgb += _TertiaryColor.rgb * edge;
                
                // Apply brightness
                finalColor.rgb *= _Brightness;
                
                // Apply vertex color
                finalColor *= i.color;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}