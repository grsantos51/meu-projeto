// Shader da cidade GRS 1: cores da Kenney + textura de detalhe "triplanar"
// (concreto/asfalto sem precisar de UV) + janelas que acendem a noite.
Shader "GRS1/CidadeKenney"
{
    Properties
    {
        _BaseMap ("Colormap", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1,1,1,1)
        _DetailTex ("Detalhe (ruido)", 2D) = "gray" {}
        _DetailScale ("Escala do detalhe", Float) = 0.35
        _DetailStrength ("Forca do detalhe", Range(0,0.6)) = 0.22
        _Smoothness ("Brilho", Range(0,1)) = 0.2
        _JanelaCor ("Cor das janelas", Color) = (1,0.78,0.45,1)
        _JanelaIntensidade ("Janelas acesas (0 = dia)", Range(0,6)) = 0
        _JanelaChance ("Quantas janelas acesas", Range(0,1)) = 0.6
        // usados pelos passes copiados do Lit (sombra/profundidade)
        [HideInInspector] _Cutoff ("Cutoff", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _DetailScale;
                half _DetailStrength;
                half _Smoothness;
                half4 _JanelaCor;
                half _JanelaIntensidade;
                half _JanelaChance;
                half _Cutoff;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailTex); SAMPLER(sampler_DetailTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogCoord = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                float3 nw = normalize(i.normalWS);

                // detalhe triplanar (nao depende do UV da Kenney)
                float3 w = abs(nw); w /= (w.x + w.y + w.z + 1e-4);
                float3 P = i.positionWS * _DetailScale;
                half dx = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, P.zy).r;
                half dy = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, P.xz).r;
                half dz = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, P.xy).r;
                half d = dx * w.x + dy * w.y + dz * w.z;
                half3 albedo = base.rgb * (1.0h + (d - 0.5h) * 2.0h * _DetailStrength);

                // janelas: pixels "vidro azul" da paleta, em paredes (nao no teto)
                half3 c = base.rgb;
                half vidro = step(0.12h, c.b - c.r) * step(c.r, c.g) * step(0.45h, c.g) * step(0.6h, c.b);
                float3 cel = floor(i.positionWS / float3(2.3, 3.0, 2.3));
                float h = frac(sin(dot(cel, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                half acesa = step(1.0 - _JanelaChance, h);
                half3 tom = lerp(_JanelaCor.rgb, half3(0.65h, 0.85h, 1.0h), step(0.82, h));
                half3 emissao = vidro * acesa * tom * _JanelaIntensidade * saturate(1.0h - w.y * 1.5h);

                InputData id = (InputData)0;
                id.positionWS = i.positionWS;
                id.positionCS = i.positionCS;
                id.normalWS = nw;
                id.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                id.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                id.fogCoord = i.fogCoord;
                id.bakedGI = SampleSH(nw);
                id.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                id.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.alpha = 1;
                s.metallic = 0;
                s.smoothness = saturate(_Smoothness + vidro * 0.55h);
                s.occlusion = 1;
                s.emission = emissao;
                s.normalTS = half3(0, 0, 1);

                half4 cor = UniversalFragmentPBR(id, s);
                cor.rgb = MixFog(cor.rgb, i.fogCoord);
                return cor;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }
    FallBack "Universal Render Pipeline/Lit"
}
