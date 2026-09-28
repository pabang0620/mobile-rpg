// Sprite shader for glowing skill VFX (built-in render pipeline).
// Premultiplied output with a partially additive blend: _Additive = 0 is a normal
// alpha blend, 1 is fully additive. Glow art stays bright over light ground tiles
// instead of turning muddy, while SpriteRenderer.color alpha still fades it out.
// Loaded at runtime via Resources.Load<Shader>("Shaders/SpriteGlow") so it is always
// included in player builds.
Shader "Sapphire/SpriteGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Additive ("Additive Amount", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            half _Additive;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
                c.rgb *= c.a;
                c.a *= 1.0 - _Additive;
                return c;
            }
            ENDCG
        }
    }
}
