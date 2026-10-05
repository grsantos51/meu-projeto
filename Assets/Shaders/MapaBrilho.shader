// Imagem do mapa (vista de cima) clareada para enxergar a cidade a noite.
// Compativel com Mask (stencil) do UI.
Shader "GRS1/MapaBrilho"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
        _Color ("Cor", Color) = (1,1,1,1)
        _Brilho ("Brilho", Float) = 3.0
        _Azul ("Tom azulado", Float) = 0.25
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _MainTex; fixed4 _Color; float _Brilho; float _Azul;
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                c = 1.0 - exp(-c * _Brilho);                 // clareia os escuros sem estourar o neon
                float l = dot(c, float3(0.3, 0.59, 0.11));
                c = lerp(c, l * float3(0.75, 0.9, 1.1), _Azul); // leve tom de mapa tatico
                return fixed4(c * i.color.rgb, i.color.a);
            }
            ENDCG
        }
    }
}
