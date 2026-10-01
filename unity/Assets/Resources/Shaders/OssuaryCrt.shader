// OssuaryCrt — subtle CRT overlay drawn in front of the terminal text.
//
// A single fullscreen quad, unlit, no scene texture. Everything is arithmetic on
// the interpolated 0..1 quad uv, so the effect is resolution independent and
// costs one blended quad. It sits on top of text, so it only ever *tints and
// darkens* by a small amount: the overlay alpha is clamped well below 1 so the
// glyphs underneath stay fully legible.
//
// Property contract
//   _Scanline      depth of the horizontal scanline darkening
//   _ScanlineCount number of scanline bands across the quad height
//   _Vignette      corner darkening strength
//   _Aberration    chromatic fringing amount, in pixels at the screen edge
//   _Flicker       amplitude of the slow brightness wobble
//   _Tint          CRT phosphor colour the overlay blends toward
//   _Time          seconds; Unity drives the built-in _Time uniform every frame
//                  (declared here only so the slot shows up on the material)

Shader "Ossuary/Crt"
{
    Properties
    {
        _Scanline ("Scanline Strength", Range(0, 0.5)) = 0.10
        _ScanlineCount ("Scanline Count", Range(50, 400)) = 180
        _Vignette ("Vignette", Range(0, 1)) = 0.30
        _Aberration ("Chromatic Aberration", Range(0, 3)) = 0.6
        _Flicker ("Flicker", Range(0, 0.2)) = 0.03
        _Tint ("Tint", Color) = (0.85, 0.92, 1.0, 1)
        _Time ("Time", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float _Scanline;
            float _ScanlineCount;
            float _Vignette;
            float _Aberration;
            float _Flicker;
            float4 _Tint;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // The renderer hands us the built-in quad scaled so it spans
                // -1..1 in object space and is centred on the origin. Mapping
                // straight off object position rather than screen position keeps
                // the pattern locked to the quad instead of sliding when the
                // window is resized, and the +0.5 puts the origin at bottom-left
                // to match texture/v coordinate orientation.
                o.uv = v.vertex.xy * 0.5 + 0.5;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;

                // --- scanlines -------------------------------------------------
                // Smoothstep-based duty cycle: a lit band covering roughly the
                // first 55% of each period, softened at both edges. The soft
                // edges matter — a hard square wave at these counts turns into
                // visible moire once the quad is minified.
                float t = frac(i.uv.y * _ScanlineCount);
                float duty = smoothstep(0.0, 0.15, t) * (1.0 - smoothstep(0.55, 0.70, t));
                float lum = 1.0 - _Scanline * (1.0 - duty);

                // --- chromatic aberration -------------------------------------
                // There is no scene texture to resample, so the fringing is
                // faked on the colour weights instead: red and blue are pushed
                // apart radially, which is the same thing a real per-channel UV
                // offset does at the edge of the screen. _ScreenParams.xy is
                // 1/screen size, so _Aberration (in pixels) converts to uv.
                float radial = saturate(length(c) * 1.41421356);
                float ab = _Aberration * _ScreenParams.x * radial;
                float wR = 1.0 + ab;
                float wB = 1.0 - ab;
                float wG = 1.0;
                // Renormalise so the tint never brightens or darkens overall.
                float wSum = max(wR + wG + wB, 1e-4);

                // --- vignette ---------------------------------------------------
                float vig = 1.0 - _Vignette * smoothstep(0.25, 0.75, length(c) * 1.41421356);

                // --- flicker ----------------------------------------------------
                float flick = 1.0 - _Flicker * (0.5 + 0.5 * sin(_Time * 7.3) * sin(_Time * 2.1));

                lum = saturate(lum * vig * flick);

                // Blending is SrcAlpha OneMinusSrcAlpha, so darkening happens by
                // emitting a dim colour with coverage (1 - lum). Halving that
                // coverage caps the overlay alpha at 0.5, which is what keeps the
                // glyphs underneath readable at every setting.
                float3 rgb = _Tint.rgb * float3(wR, wG, wB) / wSum * lum;
                float  a   = saturate(1.0 - lum) * 0.5;
                return float4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}