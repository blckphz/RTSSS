Shader "Custom/ElectricLightning"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.2, 0.8, 1, 1)

        _GlowColor ("Glow Color", Color) = (0.1, 0.6, 1, 1)
        _GlowStrength ("Glow Strength", Range(0, 10)) = 3

        _NoiseScale ("Noise Scale", Float) = 4
        _NoiseSpeed ("Noise Speed", Float) = 3
        _Distortion ("Distortion", Range(0, 2)) = 0.4

        _CoreWidth ("Core Width", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float4 _Color;
            float4 _GlowColor;

            float _GlowStrength;
            float _NoiseScale;
            float _NoiseSpeed;
            float _Distortion;
            float _CoreWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y * _NoiseSpeed;

                float2 uv = i.uv;

                // Animate noise along the lightning.
                uv.x += time;

                // Distort vertically.
                float noise =
                    tex2D(
                        _MainTex,
                        uv * _NoiseScale
                    ).r;

                float distortion =
                    (noise - 0.5) *
                    _Distortion;

                uv.y += distortion;

                // Create bright electrical core.
                float distanceFromCenter =
                    abs(uv.y - 0.5) * 2.0;

                float core =
                    1.0 -
                    smoothstep(
                        _CoreWidth,
                        1.0,
                        distanceFromCenter
                    );

                // Flicker.
                float flicker =
                    0.75 +
                    sin(
                        _Time.y * 25.0 +
                        uv.x * 15.0
                    ) * 0.25;

                float intensity =
                    core *
                    flicker;

                float3 finalColor =
                    lerp(
                        _GlowColor.rgb,
                        _Color.rgb,
                        core
                    );

                finalColor *=
                    _GlowStrength *
                    intensity;

                return fixed4(
                    finalColor,
                    intensity
                );
            }

            ENDCG
        }
    }
}