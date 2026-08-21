Shader "Custom/URP/DarkFantasyLine"
{
    Properties
    {
        [HDR]
        _MainColor ("Dark Magic Color", Color) =
            (0.18, 0.015, 0.28, 1)

        [HDR]
        _GlowColor ("Blood Magic Color", Color) =
            (0.75, 0.015, 0.12, 1)

        [HDR]
        _CoreColor ("Core Color", Color) =
            (0.9, 0.08, 0.3, 1)

        _CoreWidth ("Core Width", Range(0.01, 1.0)) =
            0.28

        _GlowStrength ("Glow Strength", Range(0.1, 8.0)) =
            2.5

        _ScrollSpeed ("Magic Flow Speed", Float) =
            1.5

        _PatternScale ("Pattern Scale", Float) =
            12.0

        _PulseStrength ("Pulse Strength", Range(0.0, 5.0)) =
            2.0

        _EdgeSoftness ("Edge Softness", Range(0.1, 10.0)) =
            2.5

        _Darkness ("Darkness", Range(0.0, 1.0)) =
            0.35

        _FlickerSpeed ("Flicker Speed", Float) =
            5.0

        _FlickerStrength ("Flicker Strength", Range(0.0, 1.0)) =
            0.25
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }


        Blend SrcAlpha One
        ZWrite Off
        Cull Off


        Pass
        {
            Name "DarkFantasyLine"


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };


            CBUFFER_START(UnityPerMaterial)

            float4 _MainColor;
            float4 _GlowColor;
            float4 _CoreColor;

            float _CoreWidth;
            float _GlowStrength;
            float _ScrollSpeed;
            float _PatternScale;
            float _PulseStrength;
            float _EdgeSoftness;
            float _Darkness;
            float _FlickerSpeed;
            float _FlickerStrength;

            CBUFFER_END


            // ============================================================
            // VERTEX
            // ============================================================

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                output.positionHCS =
                    positionInputs.positionCS;

                output.uv =
                    input.uv;

                return output;
            }


            // ============================================================
            // RANDOM
            // ============================================================

            float Random(float value)
            {
                return frac(
                    sin(value * 127.1) *
                    43758.5453
                );
            }


            // ============================================================
            // FRAGMENT
            // ============================================================

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;


                // ========================================================
                // LINE DISTANCE
                // ========================================================

                float centerDistance =
                    abs(uv.y - 0.5) * 2.0;


                // ========================================================
                // DARK OUTER BODY
                // ========================================================

                float body =
                    1.0 -
                    smoothstep(
                        0.0,
                        1.0,
                        centerDistance
                    );


                body =
                    pow(
                        body,
                        _EdgeSoftness
                    );


                // ========================================================
                // SHARP MAGICAL CORE
                // ========================================================

                float core =
                    1.0 -
                    smoothstep(
                        0.0,
                        _CoreWidth,
                        centerDistance
                    );


                core =
                    pow(
                        core,
                        1.5
                    );


                // ========================================================
                // MOVING ENERGY
                // ========================================================

                float time =
                    _Time.y * _ScrollSpeed;


                float magicPosition =
                    uv.x *
                    _PatternScale +
                    time;


                // Main flowing wave.

                float wave =
                    sin(
                        magicPosition
                    );


                wave =
                    wave *
                    0.5 +
                    0.5;


                wave =
                    pow(
                        wave,
                        5.0
                    );


                // ========================================================
                // SECOND DARK WAVE
                // ========================================================

                float wave2 =
                    sin(
                        magicPosition * 2.7 -
                        time * 0.5 +
                        2.0
                    );


                wave2 =
                    wave2 *
                    0.5 +
                    0.5;


                wave2 =
                    pow(
                        wave2,
                        8.0
                    );


                // ========================================================
                // SHARP BLOOD PULSES
                // ========================================================

                float pulse =
                    sin(
                        magicPosition * 1.35
                    );


                pulse =
                    abs(pulse);


                pulse =
                    pow(
                        pulse,
                        12.0
                    );


                // ========================================================
                // SMALL MAGICAL FLICKER
                // ========================================================

                float flicker =
                    sin(
                        uv.x *
                        _PatternScale *
                        3.7 +
                        _Time.y *
                        _FlickerSpeed
                    );


                flicker =
                    flicker *
                    0.5 +
                    0.5;


                flicker *=
                    _FlickerStrength;


                // ========================================================
                // COLOR MIXING
                // ========================================================

                float3 darkColor =
                    _MainColor.rgb;


                float3 bloodColor =
                    _GlowColor.rgb;


                float3 coreColor =
                    _CoreColor.rgb;


                // Dark purple base.

                float3 finalColor =
                    darkColor;


                // Red magical waves.

                finalColor =
                    lerp(
                        finalColor,
                        bloodColor,
                        wave
                    );


                // Stronger red pulse.

                finalColor +=
                    bloodColor *
                    wave2 *
                    1.5;


                // Sharp blood flashes.

                finalColor +=
                    bloodColor *
                    pulse *
                    _PulseStrength;


                // White/pinkish magical core.

                finalColor =
                    lerp(
                        finalColor,
                        coreColor,
                        core
                    );


                // Small unstable flickering.

                finalColor +=
                    bloodColor *
                    flicker *
                    body;


                // ========================================================
                // DARKEN OUTER EDGES
                // ========================================================

                float darknessMask =
                    lerp(
                        1.0,
                        _Darkness,
                        centerDistance
                    );


                finalColor *=
                    darknessMask;


                // ========================================================
                // GLOW
                // ========================================================

                float glow =
                    body *
                    0.65;


                glow +=
                    core *
                    1.4;


                glow +=
                    wave *
                    0.35;


                glow +=
                    pulse *
                    _PulseStrength *
                    0.35;


                // ========================================================
                // FINAL BRIGHTNESS
                // ========================================================

                finalColor *=
                    glow *
                    _GlowStrength;


                // ========================================================
                // ALPHA
                // ========================================================

                float alpha =
                    body *
                    0.65;


                alpha +=
                    core *
                    0.35;


                alpha +=
                    pulse *
                    0.25;


                alpha =
                    saturate(
                        alpha
                    );


                // Slight magical flicker.

                alpha *=
                    0.9 +
                    flicker;


                return half4(
                    finalColor,
                    alpha
                );
            }


            ENDHLSL
        }
    }
}