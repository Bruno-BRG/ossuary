// OssuaryTerminal — unlit glyph renderer for the text-mode roguelike.
//
// One quad per character cell, all cells batched into a single MeshRenderer.
// The atlas stores white RGB with coverage in alpha (see GlyphAtlas), and the
// atlas texture is created linear, so this shader does a straight multiply with
// no sRGB<->linear conversion of its own.
//
// Property contract
//   _MainTex     glyph atlas, sample the ALPHA channel only
//   _Boost       multiplies vertex-colour intensity (HDR values allowed)
//   _GlowExpand  shrinks UVs toward the cell centre to fake a bloom halo
//   _SrcBlend    BlendFunc enum value for the source factor
//   _DstBlend    BlendFunc enum value for the destination factor
//   _ZWrite      0 or 1, as a float because it has to be material-driven
//
// One shader serves both passes. Material setup:
//
//   main pass : _SrcBlend 5, _DstBlend 10, _ZWrite 1  (default values)
//   glow pass : _SrcBlend 5, _DstBlend 1,  _ZWrite 0
//
// BlendFunc enum values used above: Zero 0, One 1, DstColor 2, SrcColor 3,
// OneMinusDstColor 4, SrcAlpha 5, OneMinusSrcColor 6, DstAlpha 7,
// OneMinusDstAlpha 8, SrcAlphaSaturate 9, OneMinusSrcAlpha 10.
// So the defaults _SrcBlend 5 / _DstBlend 10 give SrcAlpha OneMinusSrcAlpha,
// which is the opaque-looking main pass. For the additive glow pass set
// _SrcBlend 5 / _DstBlend 1 and leave _ZWrite 0.

Shader "Ossuary/Terminal"
{
    Properties
    {
        _MainTex ("Glyph Atlas", 2D) = "white" {}
        _Mode ("Mode (0 = glyph, 1 = solid)", Float) = 0
        _Boost ("Boost", Range(0, 4)) = 1.0
        _GlowExpand ("Glow Expand", Range(0, 0.5)) = 0.0
        _SrcBlend ("Src Blend", Float) = 5     // SrcAlpha
        _DstBlend ("Dst Blend", Float) = 10    // OneMinusSrcAlpha
        _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
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
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            // Must be a sampler2D, not a float4: the D3D11 shader compiler rejects
            // tex2D(float4, float2). A plain texture property declared in
            // Properties{} gets bound as a sampler automatically.
            sampler2D _MainTex;
            float  _Mode;
            float  _Boost;
            float  _GlowExpand;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Glow pass trick: pulling the sample point toward the middle of
                // the cell shrinks the glyph, so the discarded outer ring of the
                // atlas cell contributes a soft halo at reduced coverage.
                float2 uv = 0.5 + (i.uv - 0.5) * (1.0 - _GlowExpand * 2.0);

                // Coverage. Unity's dynamic font texture is an Alpha8 surface, but sampling it
                // can deliver the shape in EITHER alpha or the colour channels
                // depending on the platform, and the difference is decisive: read the
                // wrong one and every glyph renders as a solid rectangle.
                // max(alpha, luminance) resolves both, because the channel that does
                // NOT carry the shape is uniformly 1 in that case, never partially lit.
                float solid = step(0.5, _Mode);
                float4 s = tex2D(_MainTex, uv);
                float cov = max(s.a, max(s.r, max(s.g, s.b)));
                float a = lerp(cov, 1.0, solid) * i.color.a;
                float3 rgb = i.color.rgb * _Boost;
                return float4(rgb, a);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}