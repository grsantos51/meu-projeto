using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class EstilizarMenuGRS1
{
    const string PastaFontes = "Assets/Fonts";
    const string FonteTTF = PastaFontes + "/Bangers-Regular.ttf";
    const string FonteTMP = PastaFontes + "/Bangers SDF.asset";
    const string MatTitulo = PastaFontes + "/Bangers Titulo.mat";
    const string MatBotao = PastaFontes + "/Bangers Botao.mat";

    [MenuItem("GRS 1/Estilizar Menu (fonte + animacao)")]
    public static void Estilizar()
    {
        var fonte = CriarFonte();
        if (fonte == null) return;
        var matTitulo = CriarMaterial(fonte, MatTitulo, 0.28f, new Color32(60, 10, 40, 255), true);
        var matBotao = CriarMaterial(fonte, MatBotao, 0.2f, new Color32(20, 5, 20, 255), true);

        var cena = EditorSceneManager.OpenScene("Assets/Scenes/MenuPrincipal.unity", OpenSceneMode.Single);
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("Canvas nao encontrado"); return; }
        var ct = canvas.transform;

        // --- Fundo: copia escurecida da capa preenchendo a tela ---
        var capaT = ct.Find("Fundo") != null ? ct.Find("Fundo") : ct.Find("CapaArea/Fundo");
        var capa = capaT != null ? capaT.GetComponent<Image>() : null;
        if (capa == null) { Debug.LogError("Imagem 'Fundo' nao encontrada"); return; }

        var ambiente = ct.Find("FundoAmbiente");
        if (ambiente == null)
        {
            var go = new GameObject("FundoAmbiente", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            go.transform.SetParent(ct, false);
            ambiente = go.transform;
        }
        var ambImg = ambiente.GetComponent<Image>();
        ambImg.sprite = capa.sprite;
        ambImg.color = new Color(0.32f, 0.22f, 0.35f, 1f);
        ambImg.raycastTarget = false;
        Esticar((RectTransform)ambiente);
        var ambFit = ambiente.GetComponent<AspectRatioFitter>();
        ambFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ambFit.aspectRatio = capa.sprite.rect.width / capa.sprite.rect.height;
        ambiente.SetSiblingIndex(0);

        // Degrade escuro da esquerda para dar leitura ao titulo/botoes
        var sombra = ct.Find("SombraEsquerda");
        if (sombra == null)
        {
            var go = new GameObject("SombraEsquerda", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(ct, false);
            sombra = go.transform;
        }
        var sImg = sombra.GetComponent<Image>();
        sImg.sprite = CriarDegradeHorizontal();
        if (sImg.sprite == null) sImg.color = new Color(0.05f, 0.01f, 0.08f, 0.5f);
        else sImg.color = Color.white;
        sImg.raycastTarget = false;
        Esticar((RectTransform)sombra);
        sombra.SetSiblingIndex(1);

        // --- Capa inteira do lado direito, sem cortar ---
        var area = ct.Find("CapaArea") as RectTransform;
        if (area == null)
        {
            area = new GameObject("CapaArea", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(ct, false);
        }
        area.anchorMin = new Vector2(0.5f, 0.04f);
        area.anchorMax = new Vector2(0.97f, 0.96f);
        area.offsetMin = area.offsetMax = Vector2.zero;
        area.localScale = Vector3.one;
        capa.transform.SetParent(area, false);
        var crt = capa.rectTransform;
        var fit = capa.GetComponent<AspectRatioFitter>();
        if (fit == null) fit = capa.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = capa.sprite.rect.width / capa.sprite.rect.height;
        capa.color = Color.white;
        capa.raycastTarget = false;
        var borda = capa.GetComponent<Outline>();
        if (borda == null) borda = capa.gameObject.AddComponent<Outline>();
        borda.effectColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        borda.effectDistance = new Vector2(6, -6);
        area.SetSiblingIndex(2);

        // --- Titulo ---
        var titulo = ct.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(t => t.text.Replace(" ", "").ToUpper().Contains("GRS"));
        if (titulo == null)
        {
            var go = new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI));
            titulo = go.GetComponent<TextMeshProUGUI>();
        }
        titulo.name = "Titulo";
        titulo.gameObject.SetActive(true);
        titulo.transform.SetParent(ct, false);
        var trt = titulo.rectTransform;
        trt.anchorMin = new Vector2(0.03f, 0.5f);
        trt.anchorMax = new Vector2(0.47f, 0.95f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        trt.localScale = Vector3.one;
        trt.localRotation = Quaternion.Euler(0, 0, -4f);
        titulo.text = "GRS 1";
        titulo.font = fonte;
        titulo.fontSharedMaterial = matTitulo;
        titulo.fontStyle = FontStyles.Normal;
        titulo.fontSize = 230;
        titulo.enableAutoSizing = false;
        titulo.textWrappingMode = TextWrappingModes.NoWrap;
        titulo.characterSpacing = 6;
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.color = Color.white;
        titulo.enableVertexGradient = true;
        titulo.colorGradient = new VertexGradient(new Color(1f, 0.95f, 0.3f), new Color(1f, 0.95f, 0.3f),
                                                  new Color(1f, 0.45f, 0.1f), new Color(1f, 0.45f, 0.1f));
        titulo.raycastTarget = false;
        if (titulo.GetComponent<TituloAnimado>() == null) titulo.gameObject.AddComponent<TituloAnimado>();

        // --- Botoes ---
        var painel = ct.Find("Botoes") as RectTransform;
        if (painel != null)
        {
            painel.anchorMin = new Vector2(0.11f, 0.12f);
            painel.anchorMax = new Vector2(0.39f, 0.44f);
            painel.offsetMin = painel.offsetMax = Vector2.zero;
            var lg = painel.GetComponent<VerticalLayoutGroup>();
            if (lg != null) { lg.spacing = 30; lg.padding = new RectOffset(20, 20, 10, 10); }

            foreach (Transform b in painel)
            {
                var img = b.GetComponent<Image>();
                if (img != null) { img.color = new Color(0.06f, 0.02f, 0.08f, 0.8f); img.sprite = null; }
                var ol = b.GetComponent<Outline>();
                if (ol != null) Object.DestroyImmediate(ol);

                var txt = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    txt.font = fonte;
                    txt.fontSharedMaterial = matBotao;
                    txt.fontStyle = FontStyles.Normal;
                    txt.fontSize = 84;
                    txt.characterSpacing = 8;
                    txt.color = Color.white;
                }
                if (b.GetComponent<BotaoAnimado>() == null) b.gameObject.AddComponent<BotaoAnimado>();
            }
            painel.SetAsLastSibling();
        }
        titulo.transform.SetSiblingIndex(painel != null ? painel.GetSiblingIndex() : ct.childCount - 1);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log("GRS 1: menu estilizado (fonte Bangers + animacoes).");
    }

    static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
    }

    static TMP_FontAsset CriarFonte()
    {
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FonteTMP);
        if (existente != null) return existente;

        AssetDatabase.ImportAsset(FonteTTF);
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(FonteTTF);
        if (ttf == null) { Debug.LogError("Fonte nao encontrada em " + FonteTTF); return null; }

        var fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = "Bangers SDF";
        AssetDatabase.CreateAsset(fa, FonteTMP);
        fa.atlasTexture.name = "Bangers SDF Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        fa.material.name = "Bangers SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !?.,:-ÁÃÂÇÉÊÍÓÕÔÚ");
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static Material CriarMaterial(TMP_FontAsset fa, string caminho, float contorno, Color corContorno, bool sombra)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        bool novo = mat == null;
        if (novo) mat = new Material(fa.material);
        mat.shader = fa.material.shader;
        mat.SetTexture("_MainTex", fa.atlasTexture);
        mat.SetFloat("_OutlineWidth", contorno);
        mat.SetColor("_OutlineColor", corContorno);
        mat.SetFloat("_FaceDilate", 0.1f);
        if (sombra)
        {
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.75f));
            mat.SetFloat("_UnderlayOffsetX", 0.6f);
            mat.SetFloat("_UnderlayOffsetY", -0.6f);
            mat.SetFloat("_UnderlaySoftness", 0.25f);
        }
        if (novo) AssetDatabase.CreateAsset(mat, caminho);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Sprite CriarDegradeHorizontal()
    {
        const string caminho = "Assets/Fonts/DegradeSombra.png";
        var s = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().FirstOrDefault();
        if (s != null) return s;
        var tex = new Texture2D(256, 4, TextureFormat.RGBA32, false);
        for (int x = 0; x < 256; x++)
        {
            float a = Mathf.Lerp(0.85f, 0f, Mathf.SmoothStep(0.25f, 1f, x / 255f));
            for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(0.05f, 0.01f, 0.08f, a));
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(caminho, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(caminho);
        var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().FirstOrDefault();
    }
}
