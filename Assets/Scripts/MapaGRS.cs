using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Minimapa redondo (cidade vista de cima, de verdade) + mapa grande na tecla M,
// com nomes dos bairros, ruas e lugares (Banco, Loja...). Clique no mapa grande marca um destino (GPS).
public class MapaGRS : MonoBehaviour
{
    public static MapaGRS Instancia;
    public TMP_FontAsset fonte;
    public Material materialMapa;   // GRS1/MapaBrilho

    public float zoomMini = 42f;    // metros do centro ate a borda do minimapa
    const float RaioMini = 150f;    // pixels
    static readonly Vector2 CentroMapa = new Vector2(0f, 20f);
    const float TamMapa = 88f;      // metade do lado do mapa grande, em metros
    const float LadoMapa = 940f;    // pixels

    public bool Aberto { get; private set; }

    // ---------- nomes ----------
    struct Nome { public string texto; public Vector2 pos; public float rot; public float tam; public Color cor; public bool rua; }
    struct Lugar { public string nome, simbolo; public Vector2 pos; public Color cor; public bool destaque; }

    static readonly Nome[] Nomes =
    {
        new Nome { texto = "CENTRO",          pos = new Vector2(0, 0),     tam = 9, cor = new Color(1f, 0.85f, 0.3f) },
        new Nome { texto = "JARDINS",         pos = new Vector2(0, 40),    tam = 7, cor = Color.white },
        new Nome { texto = "MERCADÃO",        pos = new Vector2(0, -40),   tam = 7, cor = Color.white },
        new Nome { texto = "VILA RICA",       pos = new Vector2(-40, 0),   tam = 7, cor = Color.white },
        new Nome { texto = "DISTRITO NEON",   pos = new Vector2(40, 0),    tam = 7, cor = new Color(1f, 0.5f, 0.9f) },
        new Nome { texto = "ZONA INDUSTRIAL", pos = new Vector2(-40, -40), tam = 6, cor = new Color(1f, 0.7f, 0.4f) },
        new Nome { texto = "DOCAS",           pos = new Vector2(40, -40),  tam = 7, cor = new Color(1f, 0.7f, 0.4f) },
        new Nome { texto = "FÁBRICAS",        pos = new Vector2(-40, 40),  tam = 7, cor = new Color(1f, 0.7f, 0.4f) },
        new Nome { texto = "GALPÕES",         pos = new Vector2(40, 40),   tam = 7, cor = new Color(1f, 0.7f, 0.4f) },
        new Nome { texto = "PRAIA GRS",       pos = new Vector2(0, 80),    tam = 10, cor = new Color(1f, 0.9f, 0.55f) },
        new Nome { texto = "MAR",             pos = new Vector2(0, 102),   tam = 10, cor = new Color(0.5f, 0.85f, 1f) },
        // ruas norte-sul
        new Nome { texto = "RUA DO PORTO",      pos = new Vector2(-60, -40), rot = 90, tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
        new Nome { texto = "AV. CENTRAL",       pos = new Vector2(-20, -40), rot = 90, tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
        new Nome { texto = "AV. NEON",          pos = new Vector2(20, -40),  rot = 90, tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
        new Nome { texto = "RUA DAS FÁBRICAS",  pos = new Vector2(60, -40),  rot = 90, tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
        // ruas leste-oeste
        new Nome { texto = "RUA SUL",            pos = new Vector2(-40, -60), tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
        new Nome { texto = "AV. BRASIL",         pos = new Vector2(-40, -20), tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
        new Nome { texto = "AV. DAS PALMEIRAS",  pos = new Vector2(-40, 20),  tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
        new Nome { texto = "AV. BEIRA-MAR",      pos = new Vector2(-40, 60),  tam = 3.2f, cor = new Color(0.85f, 0.95f, 1f), rua = true },
    };

    List<Lugar> lugares;

    // ---------- estado ----------
    Transform player;
    Camera camMini, camGrande;
    RenderTexture rtMini, rtGrande;
    Canvas canvas;
    RectTransform mini, miniCamada, grande, grandeCamada, setaMini, setaGrande, destinoMini, destinoGrande;
    TextMeshProUGUI txtDestino, txtLocal;
    readonly List<(RectTransform rt, Vector2 pos)> itensMini = new List<(RectTransform, Vector2)>();
    readonly List<RectTransform> policiaMini = new List<RectTransform>(), policiaGrande = new List<RectTransform>();
    RectTransform vipMini, vipGrande;
    GameObject carroVip;
    Vector2? destino;
    MonoBehaviour[] pausados;
    Sprite circulo, seta;

    void Awake() { Instancia = this; }

    // sem neblina nas cameras do mapa (vista de cima ficaria toda roxa)
    bool fogSalvo;
    void OnEnable() { RenderPipelineManager.beginCameraRendering += Antes; RenderPipelineManager.endCameraRendering += Depois; }
    void OnDisable() { RenderPipelineManager.beginCameraRendering -= Antes; RenderPipelineManager.endCameraRendering -= Depois; }
    void Antes(ScriptableRenderContext ctx, Camera c) { if (c == camMini || c == camGrande) { fogSalvo = RenderSettings.fog; RenderSettings.fog = false; } }
    void Depois(ScriptableRenderContext ctx, Camera c) { if (c == camMini || c == camGrande) RenderSettings.fog = fogSalvo; }

    void Start()
    {
        var p = GameObject.Find("Player"); if (p) player = p.transform;
        var banco = GameObject.Find("Banco");
        var loja = GameObject.Find("Loja");
        var vipT = GameObject.Find("Loja") ? loja.GetComponent<Loja>() : null;
        carroVip = vipT ? vipT.carroVip : null;
        lugares = new List<Lugar>
        {
            new Lugar { nome = "BANCO", simbolo = "$", pos = banco ? XZ(banco.transform.position) : new Vector2(-16, 0), cor = new Color(0.25f, 0.95f, 0.35f), destaque = true },
            new Lugar { nome = "LOJA",  simbolo = "L", pos = loja ? XZ(loja.transform.position) : new Vector2(16, 0),   cor = new Color(1f, 0.25f, 0.85f), destaque = true },
            new Lugar { nome = "INÍCIO", simbolo = "I", pos = p ? XZ(p.transform.position) : Vector2.zero,              cor = new Color(0.35f, 0.7f, 1f) },
        };
        circulo = CriarCirculo(128);
        seta = CriarSeta(64);
        CriarCameras();
        Construir();
    }

    static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

    // ================= cameras de cima =================
    void CriarCameras()
    {
        camMini = NovaCamera("CameraMinimapa", 384, out rtMini);
        camMini.orthographicSize = zoomMini;
        camGrande = NovaCamera("CameraMapa", 1024, out rtGrande);
        camGrande.orthographicSize = TamMapa;
        camGrande.transform.position = new Vector3(CentroMapa.x, 120f, CentroMapa.y);
        camGrande.enabled = false;
    }

    Camera NovaCamera(string nome, int res, out RenderTexture rt)
    {
        rt = new RenderTexture(res, res, 24) { name = nome };
        var go = new GameObject(nome);
        go.transform.SetParent(transform, false);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // norte (+Z) para cima
        var c = go.AddComponent<Camera>();
        c.orthographic = true;
        c.nearClipPlane = 1f; c.farClipPlane = 400f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
        c.cullingMask = ~(1 << 5); // tudo menos UI
        c.targetTexture = rt;
        c.depth = -10;
        var d = c.GetUniversalAdditionalCameraData();
        d.renderShadows = false;
        d.renderPostProcessing = false;
        d.antialiasing = AntialiasingMode.None;
        d.requiresDepthOption = CameraOverrideOption.Off;
        d.requiresColorOption = CameraOverrideOption.Off;
        return c;
    }

    // ================= tela =================
    void Construir()
    {
        canvas = new GameObject("MapaCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var sc = canvas.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;

        // ----- MINIMAPA (canto inferior esquerdo) -----
        mini = Ret(canvas.transform, "Minimapa", new Vector2(40 + RaioMini + 10, 40 + RaioMini + 10), new Vector2(RaioMini * 2 + 20, RaioMini * 2 + 20));
        mini.anchorMin = mini.anchorMax = Vector2.zero;
        var anel = Img(mini, "Anel", Vector2.zero, new Vector2(RaioMini * 2 + 20, RaioMini * 2 + 20), new Color(0.02f, 0.02f, 0.03f), circulo);
        anel.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.9f, 1f, 0.6f);
        var mascara = Img(mini, "Mascara", Vector2.zero, new Vector2(RaioMini * 2, RaioMini * 2), Color.white, circulo);
        mascara.useSpriteMesh = true; // malha redonda: a mascara corta em circulo
        mascara.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var vista = new GameObject("Vista", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        vista.rectTransform.SetParent(mascara.transform, false);
        vista.rectTransform.sizeDelta = new Vector2(RaioMini * 2, RaioMini * 2);
        vista.texture = rtMini;
        if (materialMapa) vista.material = materialMapa;
        vista.raycastTarget = false;
        miniCamada = Ret(mascara.transform, "Nomes", Vector2.zero, Vector2.zero);

        float escMini = RaioMini / zoomMini; // pixels por metro
        foreach (var n in Nomes)
        {
            if (n.texto == "MAR") continue;
            var t = Txt(miniCamada, n.texto, Vector2.zero, new Vector2(400, 60), Mathf.Max(13f, n.tam * escMini * (n.rua ? 1.1f : 0.7f)), n.cor * new Color(1, 1, 1, n.rua ? 0.85f : 0.9f));
            t.rectTransform.localRotation = Quaternion.Euler(0, 0, n.rot);
            itensMini.Add((t.rectTransform, n.pos));
        }
        foreach (var l in lugares)
        {
            var ic = Icone(miniCamada, l, l.destaque ? 34 : 24, true);
            itensMini.Add((ic, l.pos));
        }
        destinoMini = Pino(miniCamada, 26);
        vipMini = IconeVip(miniCamada, 24);
        setaMini = Img(mini, "Voce", Vector2.zero, new Vector2(30, 30), Color.white, seta).rectTransform;
        setaMini.gameObject.AddComponent<Outline>().effectColor = Color.black;
        Txt(mini, "N", new Vector2(0, RaioMini + 4), new Vector2(40, 30), 24, new Color(1f, 0.35f, 0.35f));
        txtLocal = Txt(mini, "", new Vector2(0, -RaioMini - 30), new Vector2(420, 34), 24, Color.white);
        txtDestino = Txt(mini, "", new Vector2(0, RaioMini + 34), new Vector2(420, 30), 22, new Color(1f, 0.85f, 0.2f));
        Txt(mini, "M = MAPA", new Vector2(RaioMini - 20, -RaioMini + 6), new Vector2(140, 26), 18, new Color(1, 1, 1, 0.6f));

        // ----- MAPA GRANDE (tecla M) -----
        grande = Ret(canvas.transform, "MapaGrande", Vector2.zero, Vector2.zero);
        grande.anchorMin = Vector2.zero; grande.anchorMax = Vector2.one; grande.sizeDelta = Vector2.zero;
        var fundo = Img(grande, "Fundo", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.88f), null);
        fundo.rectTransform.anchorMin = Vector2.zero; fundo.rectTransform.anchorMax = Vector2.one; fundo.rectTransform.sizeDelta = Vector2.zero;

        var moldura = Img(grande, "Moldura", new Vector2(-230, 0), new Vector2(LadoMapa + 12, LadoMapa + 12), new Color(0.1f, 0.9f, 1f, 0.7f), null);
        var vistaG = new GameObject("Vista", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        vistaG.rectTransform.SetParent(moldura.transform, false);
        vistaG.rectTransform.sizeDelta = new Vector2(LadoMapa, LadoMapa);
        vistaG.texture = rtGrande;
        if (materialMapa) vistaG.material = materialMapa;
        vistaG.raycastTarget = true;
        grandeCamada = Ret(vistaG.transform, "Nomes", Vector2.zero, new Vector2(LadoMapa, LadoMapa));
        float escG = LadoMapa / (TamMapa * 2f);
        foreach (var n in Nomes)
        {
            var t = Txt(grandeCamada, n.texto, ParaGrande(n.pos), new Vector2(600, 80), n.tam * escG * (n.rua ? 1.15f : 0.75f), n.cor);
            t.rectTransform.localRotation = Quaternion.Euler(0, 0, n.rot);
        }
        foreach (var l in lugares)
        {
            var ic = Icone(grandeCamada, l, l.destaque ? 46 : 32, true);
            ic.anchoredPosition = ParaGrande(l.pos);
        }
        destinoGrande = Pino(grandeCamada, 34);
        vipGrande = IconeVip(grandeCamada, 32);
        setaGrande = Img(grandeCamada, "Voce", Vector2.zero, new Vector2(40, 40), Color.white, seta).rectTransform;
        setaGrande.gameObject.AddComponent<Outline>().effectColor = Color.black;

        // legenda
        var leg = Img(grande, "Legenda", new Vector2(600, 0), new Vector2(520, LadoMapa + 12), new Color(0.05f, 0.04f, 0.1f, 0.95f), null).rectTransform;
        leg.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.2f, 0.75f, 0.7f);
        Txt(leg, "MAPA DE GRS", new Vector2(0, 410), new Vector2(480, 80), 64, new Color(1f, 0.85f, 0.2f));
        float y = 320;
        LinhaLegenda(leg, ref y, new Lugar { simbolo = "$", nome = "BANCO  (assalte com R)", cor = lugares[0].cor });
        LinhaLegenda(leg, ref y, new Lugar { simbolo = "L", nome = "LOJA  (entre com E)", cor = lugares[1].cor });
        LinhaLegenda(leg, ref y, new Lugar { simbolo = "I", nome = "PONTO DE INÍCIO", cor = lugares[2].cor });
        LinhaLegenda(leg, ref y, new Lugar { simbolo = "V", nome = "CARRO EXCLUSIVO", cor = new Color(1f, 0.8f, 0.15f) });
        var lp = Img(leg, "Pol", new Vector2(-190, y), new Vector2(26, 26), new Color(1f, 0.15f, 0.15f), circulo);
        Txt(leg, "POLÍCIA", new Vector2(40, y), new Vector2(380, 40), 30, Color.white, TextAlignmentOptions.Left);
        y -= 60;
        var ls = Img(leg, "Seta", new Vector2(-190, y), new Vector2(34, 34), Color.white, seta);
        Txt(leg, "VOCÊ", new Vector2(40, y), new Vector2(380, 40), 30, Color.white, TextAlignmentOptions.Left);
        y -= 60;
        var pl = Pino(leg, 30); pl.anchoredPosition = new Vector2(-190, y); pl.gameObject.SetActive(true);
        Txt(leg, "SEU DESTINO (GPS)", new Vector2(40, y), new Vector2(380, 40), 30, Color.white, TextAlignmentOptions.Left);
        y -= 90;
        var ajuda = Txt(leg, "Clique no mapa: marcar destino\nBotão direito: apagar destino\nM ou ESC: fechar", new Vector2(0, y - 40), new Vector2(460, 140), 28, new Color(1, 1, 1, 0.75f));
        ajuda.textWrappingMode = TextWrappingModes.Normal;

        grande.gameObject.SetActive(false);
    }

    void LinhaLegenda(RectTransform leg, ref float y, Lugar l)
    {
        var ic = Icone(leg, l, 44, false);
        ic.anchoredPosition = new Vector2(-190, y);
        Txt(leg, l.nome, new Vector2(40, y), new Vector2(380, 40), 30, Color.white, TextAlignmentOptions.Left);
        y -= 66;
    }

    Vector2 ParaGrande(Vector2 mundo) => (mundo - CentroMapa) * (LadoMapa / (TamMapa * 2f));

    // ================= atualizacao =================
    void Update()
    {
        if (player == null || canvas == null) return;
        var kb = Keyboard.current;
        bool lojaAberta = LojaUI.Instancia && LojaUI.Instancia.Aberta;
        if (kb != null && !lojaAberta)
        {
            if (kb.mKey.wasPressedThisFrame) { if (Aberto) Fechar(); else Abrir(); }
            else if (Aberto && kb.escapeKey.wasPressedThisFrame) Fechar();
        }
        mini.gameObject.SetActive(!Aberto && !lojaAberta);

        Vector3 pp = player.position;
        var carro = player.GetComponentInParent<CarroSimples>();
        float yaw = carro ? carro.transform.eulerAngles.y : player.eulerAngles.y;

        // minimapa segue a Arissa
        camMini.transform.position = new Vector3(pp.x, pp.y + 90f, pp.z);
        float esc = RaioMini / zoomMini;
        Vector2 c = XZ(pp);
        foreach (var (rt, pos) in itensMini)
        {
            Vector2 d = (pos - c) * esc;
            rt.anchoredPosition = d;
            rt.gameObject.SetActive(d.magnitude < RaioMini + 60);
        }
        // lugares importantes ficam presos na borda quando estao longe
        for (int i = itensMini.Count - lugares.Count; i < itensMini.Count; i++)
        {
            var (rt, pos) = itensMini[i];
            Vector2 d = (pos - c) * esc;
            if (d.magnitude > RaioMini - 18) d = d.normalized * (RaioMini - 18);
            rt.anchoredPosition = d;
            rt.gameObject.SetActive(true);
        }
        setaMini.localRotation = Quaternion.Euler(0, 0, -yaw);
        setaGrande.anchoredPosition = ParaGrande(c);
        setaGrande.localRotation = Quaternion.Euler(0, 0, -yaw);

        // carro exclusivo
        bool vip = carroVip && carroVip.activeInHierarchy;
        Posicionar(vipMini, vipGrande, vip, vip ? XZ(carroVip.transform.position) : Vector2.zero, c, esc, true);

        // destino (GPS)
        if (destino.HasValue && Vector2.Distance(destino.Value, c) < 8f) { destino = null; if (Dinheiro.Instancia) Dinheiro.Instancia.Aviso("Voce chegou ao destino!", 2f); }
        Posicionar(destinoMini, destinoGrande, destino.HasValue, destino ?? Vector2.zero, c, esc, true);
        txtDestino.text = destino.HasValue ? $"DESTINO: {Vector2.Distance(destino.Value, c):0} m" : "";

        // viaturas
        AtualizarPolicia(c, esc);

        txtLocal.text = NomeDoLugar(c);

        if (Aberto) CliqueNoMapa();
    }

    void Posicionar(RectTransform m, RectTransform g, bool ativo, Vector2 pos, Vector2 c, float esc, bool prenderBorda)
    {
        m.gameObject.SetActive(ativo); g.gameObject.SetActive(ativo);
        if (!ativo) return;
        Vector2 d = (pos - c) * esc;
        if (prenderBorda && d.magnitude > RaioMini - 16) d = d.normalized * (RaioMini - 16);
        m.anchoredPosition = d;
        g.anchoredPosition = ParaGrande(pos);
    }

    float proximaBusca; PoliciaCarro[] viaturas = new PoliciaCarro[0];
    void AtualizarPolicia(Vector2 c, float esc)
    {
        if (Time.unscaledTime > proximaBusca)
        {
            proximaBusca = Time.unscaledTime + 0.5f;
            viaturas = FindObjectsByType<PoliciaCarro>(FindObjectsSortMode.None).Where(v => v.enabled && v.gameObject.activeInHierarchy).ToArray();
        }
        while (policiaMini.Count < viaturas.Length)
        {
            policiaMini.Add(Img(miniCamada, "Policia", Vector2.zero, new Vector2(16, 16), Color.red, circulo).rectTransform);
            policiaGrande.Add(Img(grandeCamada, "Policia", Vector2.zero, new Vector2(20, 20), Color.red, circulo).rectTransform);
        }
        Color pisca = Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.5f ? new Color(1f, 0.15f, 0.15f) : new Color(0.2f, 0.4f, 1f);
        for (int i = 0; i < policiaMini.Count; i++)
        {
            bool ativo = i < viaturas.Length && viaturas[i];
            if (ativo)
            {
                Posicionar(policiaMini[i], policiaGrande[i], true, XZ(viaturas[i].transform.position), c, esc, false);
                policiaMini[i].gameObject.SetActive(policiaMini[i].anchoredPosition.magnitude < RaioMini);
                policiaMini[i].GetComponent<Image>().color = pisca;
                policiaGrande[i].GetComponent<Image>().color = pisca;
            }
            else { policiaMini[i].gameObject.SetActive(false); policiaGrande[i].gameObject.SetActive(false); }
        }
    }

    string NomeDoLugar(Vector2 p)
    {
        if (p.y > 64f) return "PRAIA GRS";
        foreach (var l in lugares) if (l.destaque && Vector2.Distance(l.pos, p) < 6f) return l.nome;
        // na rua?
        foreach (var n in Nomes.Where(n => n.rua))
        {
            bool ns = n.rot > 45f;
            float linha = ns ? n.pos.x : n.pos.y;
            if (Mathf.Abs((ns ? p.x : p.y) - linha) < 5f) return n.texto;
        }
        int bx = Mathf.RoundToInt(p.x / 40f), bz = Mathf.RoundToInt(p.y / 40f);
        var bairro = Nomes.FirstOrDefault(n => !n.rua && n.texto != "MAR" && n.texto != "PRAIA GRS" && Mathf.RoundToInt(n.pos.x / 40f) == bx && Mathf.RoundToInt(n.pos.y / 40f) == bz);
        return bairro.texto ?? "";
    }

    // ================= mapa grande =================
    void Abrir()
    {
        Aberto = true;
        camGrande.enabled = true;
        grande.gameObject.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        pausados = player.GetComponents<MonoBehaviour>().Where(m => m.enabled && (m is PlayerSimples || m is EntrarCarro)).ToArray();
        foreach (var m in pausados) m.enabled = false;
    }

    void Fechar()
    {
        camGrande.enabled = false;
        grande.gameObject.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        StartCoroutine(Religar());
    }

    IEnumerator Religar()
    {
        yield return null; // o ESC nao vaza para o jogo (voltaria ao menu)
        foreach (var m in pausados) if (m) m.enabled = true;
        Aberto = false;
    }

    void CliqueNoMapa()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (mouse.rightButton.wasPressedThisFrame) { destino = null; return; }
        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(grandeCamada, mouse.position.ReadValue(), null, out Vector2 local)) return;
        if (Mathf.Abs(local.x) > LadoMapa / 2 || Mathf.Abs(local.y) > LadoMapa / 2) return;
        destino = CentroMapa + local / (LadoMapa / (TamMapa * 2f));
    }

    // ================= helpers =================
    RectTransform Icone(Transform pai, Lugar l, float tam, bool comNome)
    {
        var bola = Img(pai, "Icone " + l.nome, Vector2.zero, new Vector2(tam, tam), l.cor, circulo);
        bola.gameObject.AddComponent<Outline>().effectColor = Color.black;
        var borda = Img(bola.transform, "Borda", Vector2.zero, new Vector2(tam * 0.82f, tam * 0.82f), new Color(0.05f, 0.05f, 0.08f), circulo);
        Txt(borda.transform, l.simbolo, new Vector2(0, -1), new Vector2(tam, tam), tam * 0.62f, l.cor);
        if (comNome)
        {
            var t = Txt(bola.transform, l.nome, new Vector2(0, -tam * 0.5f - 12), new Vector2(240, 30), Mathf.Max(18, tam * 0.55f), l.cor);
            t.fontStyle = FontStyles.Bold;
        }
        return bola.rectTransform;
    }

    RectTransform IconeVip(Transform pai, float tam)
    {
        var r = Icone(pai, new Lugar { nome = "", simbolo = "V", cor = new Color(1f, 0.8f, 0.15f) }, tam, false);
        r.gameObject.SetActive(false);
        return r;
    }

    RectTransform Pino(Transform pai, float tam)
    {
        var p = Img(pai, "Destino", Vector2.zero, new Vector2(tam, tam), new Color(1f, 0.85f, 0.1f), circulo);
        p.gameObject.AddComponent<Outline>().effectColor = Color.black;
        Img(p.transform, "Miolo", Vector2.zero, new Vector2(tam * 0.4f, tam * 0.4f), Color.black, circulo);
        p.gameObject.SetActive(false);
        return p.rectTransform;
    }

    static RectTransform Ret(Transform pai, string nome, Vector2 pos, Vector2 tam)
    {
        var rt = (RectTransform)new GameObject(nome, typeof(RectTransform)).transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        return rt;
    }

    static Image Img(Transform pai, string nome, Vector2 pos, Vector2 tam, Color cor, Sprite sp)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        var im = go.GetComponent<Image>();
        im.sprite = sp; im.color = cor; im.raycastTarget = false;
        return im;
    }

    TextMeshProUGUI Txt(Transform pai, string texto, Vector2 pos, Vector2 tam, float tamFonte, Color cor, TextAlignmentOptions al = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (fonte != null) t.font = fonte;
        t.text = texto; t.fontSize = tamFonte; t.color = cor; t.alignment = al;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.outlineWidth = 0.28f; t.outlineColor = new Color32(0, 0, 0, 255); // contorno preto para ler em cima do mapa
        return t;
    }

    static Sprite CriarCirculo(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static Sprite CriarSeta(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 a = new Vector2(n * 0.5f, n * 0.95f), b = new Vector2(n * 0.12f, n * 0.08f), c = new Vector2(n * 0.5f, n * 0.3f), d = new Vector2(n * 0.88f, n * 0.08f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                bool dentro = Tri(p, a, b, c) || Tri(p, a, c, d);
                tex.SetPixel(x, y, dentro ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static bool Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        float s2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
        float s3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
        return (s1 >= 0 && s2 >= 0 && s3 >= 0) || (s1 <= 0 && s2 <= 0 && s3 <= 0);
    }
}
