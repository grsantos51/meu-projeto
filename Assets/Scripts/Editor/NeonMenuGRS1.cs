using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class NeonMenuGRS1
{
    [MenuItem("GRS 1/Menu Neon")]
    public static void AplicarNeon()
    {
        // Neonderthaw: letra cursiva de letreiro neon (titulo e botoes)
        var fTitulo = CriarFonte("Assets/Fonts/Neonderthaw-Regular.ttf", "Assets/Fonts/Neonderthaw SDF.asset", "Neonderthaw SDF");
        // botoes: letra gotica (Fraktur), como o usuario pediu
        var fBotao = CriarFonte("Assets/Fonts/UnifrakturCook-Bold.ttf", "Assets/Fonts/UnifrakturCook SDF.asset", "UnifrakturCook SDF");
        if (fTitulo == null || fBotao == null) return;

        var rosa = new Color(1f, 0.15f, 0.75f);
        var ciano = new Color(0.1f, 0.9f, 1f);
        var amarelo = new Color(1f, 0.85f, 0.1f);
        var mTitulo = CriarMaterialNeon(fTitulo, "Assets/Fonts/Neon Titulo.mat", amarelo, 0.12f, 0.9f);
        mTitulo.SetColor("_OutlineColor", Color.black);
        mTitulo.SetFloat("_OutlineWidth", 0.2f);
        mTitulo.SetFloat("_OutlineSoftness", 0f);
        mTitulo.SetFloat("_FaceDilate", 0.25f);
        mTitulo.SetFloat("_GlowOffset", 0.45f);
        mTitulo.SetFloat("_GlowInner", 0.05f);
        mTitulo.SetFloat("_GlowOuter", 0.9f);
        EditorUtility.SetDirty(mTitulo);
        var mBotao = CriarMaterialNeon(fBotao, "Assets/Fonts/Neon Botao Gotico.mat", ciano, 0.1f, 0.7f);
        // camadas: letra branca + contorno preto + brilho azul por fora
        mBotao.SetColor("_OutlineColor", Color.black);
        mBotao.SetFloat("_OutlineWidth", 0.22f);
        mBotao.SetFloat("_OutlineSoftness", 0f);
        mBotao.SetFloat("_FaceDilate", 0.2f);
        mBotao.SetFloat("_GlowOffset", 0.45f);
        mBotao.SetFloat("_GlowInner", 0.05f);
        mBotao.SetFloat("_GlowOuter", 0.85f);
        mBotao.SetFloat("_GlowPower", 1f);
        mBotao.SetColor("_GlowColor", new Color(0.1f, 0.6f, 1f, 1f));
        mBotao.SetColor("_UnderlayColor", new Color(0.1f, 0.5f, 1f, 0.55f));
        EditorUtility.SetDirty(mBotao);

        var cena = EditorSceneManager.OpenScene("Assets/Scenes/MenuPrincipal.unity", OpenSceneMode.Single);
        var canvas = Object.FindAnyObjectByType<Canvas>();
        var ct = canvas.transform;

        // Titulo
        var titulo = ct.Find("Titulo").GetComponent<TextMeshProUGUI>();
        var antigo = titulo.GetComponent<TituloAnimado>();
        if (antigo != null) Object.DestroyImmediate(antigo);
        titulo.font = fTitulo;
        titulo.fontSharedMaterial = mTitulo;
        titulo.enableVertexGradient = false;
        titulo.color = new Color(1f, 0.97f, 0.8f);
        titulo.fontSize = 185;
        titulo.characterSpacing = 2;
        titulo.enableAutoSizing = false;
        titulo.textWrappingMode = TextWrappingModes.NoWrap;
        titulo.overflowMode = TextOverflowModes.Overflow;
        titulo.rectTransform.localRotation = Quaternion.Euler(0, 0, -3f);
        titulo.rectTransform.localScale = Vector3.one;
        var na = titulo.GetComponent<NeonAnimado>();
        if (na == null) na = titulo.gameObject.AddComponent<NeonAnimado>();
        na.contornoPreto = true;
        na.neonA = amarelo;                          // neon amarelo
        na.neonB = new Color(1f, 0.6f, 0.05f);        // pulsa pro laranja-dourado
        EditorUtility.SetDirty(na);

        // Botoes
        var painel = ct.Find("Botoes");
        foreach (Transform b in painel)
        {
            var img = b.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, 0f); // sem fundo escuro (continua clicavel)
            var txt = b.GetComponentInChildren<TextMeshProUGUI>(true);
            txt.font = fBotao;
            txt.fontSharedMaterial = mBotao;
            txt.fontSize = 96;
            txt.characterSpacing = 4;
            txt.text = txt.text.ToLower(); // "jogar", "sair" em letra gotica
            txt.enableAutoSizing = false;
            txt.textWrappingMode = TextWrappingModes.NoWrap;
            txt.overflowMode = TextOverflowModes.Overflow;
            var ba = b.GetComponent<BotaoAnimado>();
            if (ba != null) { ba.corNormal = Color.white; ba.corHover = new Color(1f, 0.92f, 0.4f); }
            txt.color = Color.white;
        }

        // Moldura neon na capa
        var capa = ct.Find("CapaArea/Fundo");
        if (capa != null)
        {
            var ol = capa.GetComponent<Outline>();
            if (ol == null) ol = capa.gameObject.AddComponent<Outline>();
            ol.effectColor = rosa;
            ol.effectDistance = new Vector2(5, -5);
            var sh = capa.GetComponents<Shadow>().FirstOrDefault(x => !(x is Outline));
            if (sh == null) sh = capa.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(ciano.r, ciano.g, ciano.b, 0.8f);
            sh.effectDistance = new Vector2(-9, 9);
        }

        // Fundo um pouco mais escuro para o neon aparecer
        var amb = ct.Find("FundoAmbiente");
        if (amb != null) amb.GetComponent<Image>().color = new Color(0.18f, 0.1f, 0.25f);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log("GRS 1: menu neon aplicado. Aperte Play para ver o letreiro acender.");
    }

    static TMP_FontAsset CriarFonte(string ttfPath, string assetPath, string nome)
    {
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fa != null) return fa;
        AssetDatabase.ImportAsset(ttfPath);
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (ttf == null) { Debug.LogError("Fonte nao encontrada: " + ttfPath); return null; }
        fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 18, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        fa.name = nome;
        AssetDatabase.CreateAsset(fa, assetPath);
        fa.atlasTexture.name = nome + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        fa.material.name = nome + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !?.,:-");
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static Material CriarMaterialNeon(TMP_FontAsset fa, string caminho, Color neon, float contorno, float glowOuter)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        bool novo = mat == null;
        if (novo) mat = new Material(fa.material);
        var sh = Shader.Find("TextMeshPro/Distance Field");
        if (sh != null) mat.shader = sh;
        mat.SetTexture("_MainTex", fa.atlasTexture);
        mat.SetFloat("_TextureWidth", fa.atlasWidth);
        mat.SetFloat("_TextureHeight", fa.atlasHeight);
        mat.SetFloat("_GradientScale", fa.atlasPadding + 1);
        mat.SetFloat("_FaceDilate", 0.15f);
        mat.SetFloat("_OutlineWidth", contorno);
        mat.SetColor("_OutlineColor", neon);
        mat.SetFloat("_OutlineSoftness", 0.1f);
        mat.EnableKeyword("GLOW_ON");
        mat.SetColor("_GlowColor", new Color(neon.r, neon.g, neon.b, 0.8f));
        mat.SetFloat("_GlowOffset", 0.1f);
        mat.SetFloat("_GlowInner", 0.15f);
        mat.SetFloat("_GlowOuter", glowOuter);
        mat.SetFloat("_GlowPower", 0.8f);
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color(neon.r, neon.g, neon.b, 0.5f));
        mat.SetFloat("_UnderlayOffsetX", 0f);
        mat.SetFloat("_UnderlayOffsetY", 0f);
        mat.SetFloat("_UnderlayDilate", 0.6f);
        mat.SetFloat("_UnderlaySoftness", 1f);
        if (novo) AssetDatabase.CreateAsset(mat, caminho);
        EditorUtility.SetDirty(mat);
        return mat;
    }
}
