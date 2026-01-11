Shader "Allium/Sprites/VortexSwirl"
{
    Properties
    {
        // SpriteRenderer fournit la texture via le sprite (PerRendererData)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Strength ("Swirl Strength", Float) = 4.0
        _Speed ("Swirl Speed", Float) = 1.0
        _RadiusPower ("Radius Power", Float) = 1.5

        // Soft edge pour cacher le carré du sprite
        _EdgeRadius ("Edge Radius", Float) = 0.48
        _EdgeFade ("Edge Fade", Float) = 0.15
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

        Cull Off
        Lighting Off
        ZWrite Off

        // Blend en premultiplied alpha (va bien avec c.rgb *= c.a)
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float4 _Center;
            float _Strength;
            float _Speed;
            float _RadiusPower;

            float _EdgeRadius;
            float _EdgeFade;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // --- Swirl (polar) ---
                float2 p = uv - _Center.xy;
                float r = length(p);
                float a = atan2(p.y, p.x);

                float r2 = pow(max(r, 1e-5), _RadiusPower);
                a += r2 * _Strength + _Time.y * _Speed;

                float2 uv2 = float2(cos(a), sin(a)) * r + _Center.xy;

                fixed4 c = tex2D(_MainTex, uv2) * i.color;

                // --- Soft circular edge mask (hides the sprite square) ---
                // Using original uv (stable) rather than uv2 (warped) gives a clean circle.
                float dist = length(uv - _Center.xy);
                float mask = 1.0 - smoothstep(_EdgeRadius - _EdgeFade, _EdgeRadius, dist);

                c.a *= mask;

                // Premultiply RGB to reduce edge fringes with premult blend
                c.rgb *= c.a;

                return c;
            }
            ENDCG
        }
    }
}
