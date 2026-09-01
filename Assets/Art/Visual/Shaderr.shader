Shader "Custom/URP/Shaderr"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color ("Sprite Color", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Range(0,0.1)) = 0.02
        _OutlineGlow ("Outline Glow", Range(0,5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpriteOutline"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;

            float4 _Color;
            float4 _OutlineColor;

            float _OutlineThickness;
            float _OutlineGlow;

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv =
                    TRANSFORM_TEX(input.uv, _MainTex);

                output.color =
                    input.color * _Color;

                return output;
            }

            float GetAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                ).a;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float centerAlpha =
                    GetAlpha(uv);

                // If this pixel belongs to the sprite,
                // don't draw an outline over it.
                if (centerAlpha > 0.01)
                {
                    return half4(0, 0, 0, 0);
                }

                // Sprite texture size.
                float2 texelSize =
                    1.0 / float2(
                        _ScreenParams.x,
                        _ScreenParams.y
                    );

                float thickness =
                    _OutlineThickness;

                // Sample around the pixel.
                float alpha = 0;

                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(thickness, 0)
                    )
                );

                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(-thickness, 0)
                    )
                );

                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(0, thickness)
                    )
                );

                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(0, -thickness)
                    )
                );

                // Diagonal samples.
                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(thickness, thickness)
                    )
                );

                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(-thickness, thickness)
                    )
                );

                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(thickness, -thickness)
                    )
                );

                alpha = max(
                    alpha,
                    GetAlpha(
                        uv + float2(-thickness, -thickness)
                    )
                );

                float outlineAlpha =
                    saturate(alpha * _OutlineGlow);

                return half4(
                    _OutlineColor.rgb,
                    _OutlineColor.a * outlineAlpha
                );
            }

            ENDHLSL
        }
    }
}