using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MontarJogoGRS1
{
    const string PastaCenas = "Assets/Scenes";
    const string CenaMenu = PastaCenas + "/MenuPrincipal.unity";
    const string CenaJogo = PastaCenas + "/Jogo.unity";

    [MenuItem("GRS 1/Montar Menu e Cena do Jogo")]
    public static void Montar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1) Menu: usa a SampleScene (com a capa) e renomeia para MenuPrincipal
        if (!System.IO.File.Exists(CenaMenu))
        {
            if (System.IO.File.Exists(PastaCenas + "/SampleScene.unity"))
            {
                string erro = AssetDatabase.RenameAsset(PastaCenas + "/SampleScene.unity", "MenuPrincipal");
                if (!string.IsNullOrEmpty(erro)) { Debug.LogError(erro); return; }
            }
        }
        var menu = EditorSceneManager.OpenScene(CenaMenu, OpenSceneMode.Single);
        MontarMenu();
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        // 2) Cena do jogo
        if (!System.IO.File.Exists(CenaJogo))
        {
            var jogo = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            MontarCenaJogo();
            EditorSceneManager.SaveScene(jogo, CenaJogo);
        }

        // 3) Lista de cenas do build: Menu (0) e Jogo (1)
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(CenaMenu, true),
            new EditorBuildSettingsScene(CenaJogo, true)
        };

        EditorSceneManager.OpenScene(CenaMenu, OpenSceneMode.Single);
        Debug.Log("GRS 1: menu e cena do jogo prontos! Aperte Play.");
    }

    static void MontarMenu()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Fundo: preenche a tela sem distorcer
        var fundo = canvas.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.GetComponent<Button>() == null);
        if (fundo != null)
        {
            fundo.name = "Fundo";
            var rt = fundo.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            if (fundo.sprite != null)
            {
                var fit = fundo.GetComponent<AspectRatioFitter>();
                if (fit == null) fit = fundo.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = fundo.sprite.rect.width / fundo.sprite.rect.height;
            }
            // Texto "New Text" padrao que ficou dentro da imagem: esconde
            foreach (var t in fundo.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.transform.parent == fundo.transform && (t.text == "New Text" || t.text == "")) t.gameObject.SetActive(false);
        }

        var mp = canvas.GetComponent<MenuPrincipal>();
        if (mp == null) mp = canvas.gameObject.AddComponent<MenuPrincipal>();

        var painel = canvas.transform.Find("Botoes");
        if (painel == null)
        {
            var p = new GameObject("Botoes", typeof(RectTransform), typeof(VerticalLayoutGroup));
            p.transform.SetParent(canvas.transform, false);
            var prt = p.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.18f, 0.25f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(380, 230);
            var lg = p.GetComponent<VerticalLayoutGroup>();
            lg.spacing = 24; lg.childAlignment = TextAnchor.MiddleCenter;
            lg.childControlWidth = lg.childControlHeight = true;
            lg.childForceExpandWidth = lg.childForceExpandHeight = true;
            painel = p.transform;

            var jogar = CriarBotao(painel, "BotaoJogar", "JOGAR");
            UnityEventTools.AddPersistentListener(jogar.onClick, mp.Jogar);
            var sair = CriarBotao(painel, "BotaoSair", "SAIR");
            UnityEventTools.AddPersistentListener(sair.onClick, mp.Sair);
        }
        painel.SetAsLastSibling();
        AjustarTitulo(canvas);

        // EventSystem compativel com o novo Input System
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        var antigo = es.GetComponent<StandaloneInputModule>();
        if (antigo != null) Object.DestroyImmediate(antigo);
        if (es.GetComponent<InputSystemUIInputModule>() == null) es.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    [MenuItem("GRS 1/Mostrar Titulo GRS 1 no Menu")]
    public static void AjustarTituloMenu()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("Abra a cena MenuPrincipal primeiro."); return; }
        AjustarTitulo(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Debug.Log("GRS 1: titulo ajustado.");
    }

    static void AjustarTitulo(Canvas canvas)
    {
        PlayerSettings.productName = "GRS 1";

        // Procura o texto "GRS 1" (estava dentro da imagem de fundo e ficava cortado)
        var titulo = canvas.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(t => t.text.Replace(" ", "").ToUpper().Contains("GRS"));
        if (titulo == null)
        {
            var go = new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI));
            titulo = go.GetComponent<TextMeshProUGUI>();
            titulo.text = "GRS 1";
        }
        titulo.name = "Titulo";
        titulo.gameObject.SetActive(true);
        titulo.transform.SetParent(canvas.transform, false);
        titulo.transform.localScale = Vector3.one;
        titulo.transform.localRotation = Quaternion.identity;

        var rt = titulo.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -60f);
        rt.sizeDelta = new Vector2(0f, 220f);

        titulo.fontSize = 180;
        titulo.fontStyle = FontStyles.Bold;
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.color = new Color(1f, 0.92f, 0.02f);
        titulo.raycastTarget = false;

        titulo.transform.SetAsLastSibling();
    }

    static Button CriarBotao(Transform pai, string nome, string texto)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(pai, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.08f, 0.05f, 0.12f, 0.85f);
        var btn = go.GetComponent<Button>();
        var cores = btn.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 0.55f, 0.75f);
        cores.pressedColor = new Color(0.9f, 0.35f, 0.55f);
        btn.colors = cores;

        var t = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        t.transform.SetParent(go.transform, false);
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = t.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 56;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return btn;
    }

    static void MontarCenaJogo()
    {
        var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "Chao";
        chao.transform.localScale = new Vector3(10, 1, 10);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.35f, 0.5f, 0.3f);
        AssetDatabase.CreateAsset(mat, "Assets/Scenes/ChaoMaterial.mat");
        chao.GetComponent<Renderer>().sharedMaterial = mat;

        // Alguns cubos para ter referencia de movimento
        var rnd = new System.Random(1);
        for (int i = 0; i < 12; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Caixa " + (i + 1);
            float s = 1f + (float)rnd.NextDouble() * 2f;
            c.transform.localScale = Vector3.one * s;
            c.transform.position = new Vector3(rnd.Next(-40, 40), s / 2f, rnd.Next(-40, 40));
        }

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.transform.position = new Vector3(0, 1.1f, 0);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;
        var ps = player.AddComponent<PlayerSimples>();
        if (Camera.main != null) ps.cameraAlvo = Camera.main.transform;
    }
}
