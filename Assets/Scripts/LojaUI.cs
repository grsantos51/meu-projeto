using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Tela da loja: categorias, itens e pagamento SIMULADO (cartao de credito, PIX ou debito).
// Nenhum dado de cartao e salvo ou enviado; nenhuma cobranca real acontece.
public class LojaUI : MonoBehaviour
{
    public static LojaUI Instancia;
    public TMP_FontAsset fonte;          // texto geral (Bangers)
    public TMP_FontAsset fonteTitulo;    // titulo neon
    public Material materialTitulo;

    public bool Aberta { get; private set; }

    enum Forma { Credito, Pix, Debito }

    static readonly Color Roxo = new Color(0.55f, 0.15f, 1f);
    static readonly Color Rosa = new Color(1f, 0.2f, 0.75f);
    static readonly Color Ciano = new Color(0.1f, 0.9f, 1f);
    static readonly Color Verde = new Color(0.2f, 0.85f, 0.35f);
    static readonly Color VerdePix = new Color(0.2f, 0.74f, 0.66f);
    static readonly Color Fundo = new Color(0.05f, 0.03f, 0.1f, 0.97f);
    static readonly Color Cartao = new Color(0.11f, 0.07f, 0.19f, 1f);

    Canvas canvas;
    RectTransform janela, areaItens, areaCheckout, areaPagamento, areaFim, areaProvador;
    TextMeshProUGUI txtSaldo, txtRodape;
    readonly List<Button> abas = new List<Button>();
    CategoriaItem categoria = CategoriaItem.Roupa;
    ItemLoja itemAtual;
    List<ItemLoja> carrinho = new List<ItemLoja>();
    bool doProvador;

    // provador
    Camera camProva; RenderTexture rtProva; Light luzProva; RectTransform previaProva;
    float anguloProva;
    Transform playerT; Animator animPlayer;
    Forma formaAtual;
    Coroutine pixRotina;
    MonoBehaviour[] pausados;

    // campos do cartao
    TMP_InputField cNumero, cNome, cValidade, cCvv;
    TextMeshProUGUI cErro, prevNumero, prevNome, prevValidade;

    void Awake() { Instancia = this; }

    // ================= abrir / fechar =================
    public void Abrir()
    {
        if (Aberta) return;
        if (canvas == null) Construir();
        Aberta = true;
        canvas.gameObject.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        var p = GameObject.Find("Player");
        pausados = p ? p.GetComponents<MonoBehaviour>().Where(m => m.enabled && (m is PlayerSimples || m is EntrarCarro)).ToArray() : new MonoBehaviour[0];
        foreach (var m in pausados) m.enabled = false;
        playerT = p ? p.transform : null;
        animPlayer = p ? p.GetComponentInChildren<Animator>() : null;
        if (animPlayer)
        {
            animPlayer.updateMode = AnimatorUpdateMode.UnscaledTime;
            animPlayer.SetFloat("Speed", 0f); animPlayer.SetBool("Grounded", true);
        }
        MostrarItens(categoria);
    }

    public void Fechar()
    {
        if (!Aberta) return;
        PararPix();
        Inventario.LimparProva();           // o que nao comprou volta ao normal
        if (camProva) { camProva.enabled = false; luzProva.enabled = false; }
        if (animPlayer) animPlayer.updateMode = AnimatorUpdateMode.Normal;
        canvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        StartCoroutine(Religar());
    }

    IEnumerator Religar()
    {
        yield return null; // espera um frame para o ESC/E nao vazar para o jogo
        foreach (var m in pausados) if (m) m.enabled = true;
        Aberta = false;
    }

    void Update()
    {
        if (!Aberta || canvas == null || !canvas.gameObject.activeSelf) return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (areaPagamento.gameObject.activeSelf) AbrirCheckout(itemAtual);
            else if (areaCheckout.gameObject.activeSelf) VoltarDoCheckout();
            else if (areaProvador.gameObject.activeSelf) MostrarItens(categoria);
            else Fechar();
        }
        if (areaProvador.gameObject.activeSelf) AtualizarCameraProva();
        if (Dinheiro.Instancia) txtSaldo.text = "Seu dinheiro: <color=#7CFF7C>$ " + Dinheiro.Instancia.valor.ToString("N0") + "</color>";
    }

    // ================= construcao da tela =================
    void Construir()
    {
        if (EventSystem.current == null && FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        canvas = new GameObject("LojaCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var sc = canvas.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;

        var escuro = Img(canvas.transform, "Escuro", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.75f));
        var er = escuro.rectTransform; er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one; er.sizeDelta = Vector2.zero;

        // janela com borda neon
        var borda = Img(canvas.transform, "Borda", Vector2.zero, new Vector2(1510, 890), Rosa);
        borda.gameObject.AddComponent<Shadow>().effectColor = new Color(1f, 0.2f, 0.75f, 0.5f);
        janela = Img(borda.transform, "Janela", Vector2.zero, new Vector2(1500, 880), Fundo).rectTransform;

        var titulo = Txt(janela, "LOJA GRS", new Vector2(-430, 378), new Vector2(620, 110), 76, new Color(1f, 0.97f, 0.8f), TextAlignmentOptions.Left, fonteTitulo);
        if (materialTitulo) titulo.fontSharedMaterial = materialTitulo;
        txtSaldo = Txt(janela, "", new Vector2(330, 385), new Vector2(440, 60), 34, Color.white, TextAlignmentOptions.Right);
        Botao(janela, "PROVADOR", new Vector2(-10, 385), new Vector2(230, 58), Rosa, () => AbrirProvador(null), 34);
        Botao(janela, "X", new Vector2(690, 385), new Vector2(70, 70), new Color(0.35f, 0.08f, 0.15f), Fechar, 40);
        Txt(janela, "ESC", new Vector2(690, 335), new Vector2(80, 30), 20, new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Center);

        txtRodape = Txt(janela, "MODO TESTE: pagamentos simulados, nenhuma cobrança real é feita.", new Vector2(0, -418), new Vector2(1400, 36), 24, new Color(1f, 0.85f, 0.3f, 0.8f), TextAlignmentOptions.Center);

        areaItens = Area("Itens");
        areaCheckout = Area("Checkout");
        areaPagamento = Area("Pagamento");
        areaFim = Area("Fim");
        areaProvador = Area("Provador");

        string[] nomes = { "ROUPA DA ARISSA", "CARROS", "ARMAS E ITENS", "DINHEIRO DO JOGO" };
        for (int i = 0; i < 4; i++)
        {
            var cat = (CategoriaItem)i;
            abas.Add(Botao(areaItens, nomes[i], new Vector2(-525 + i * 350, 290), new Vector2(330, 64), Cartao, () => MostrarItens(cat), 30));
        }
        canvas.gameObject.SetActive(false);
    }

    RectTransform Area(string nome)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(janela, false);
        rt.sizeDelta = janela.sizeDelta;
        return rt;
    }

    void Mostrar(RectTransform qual)
    {
        foreach (var a in new[] { areaItens, areaCheckout, areaPagamento, areaFim, areaProvador }) a.gameObject.SetActive(a == qual);
        if (camProva) { camProva.enabled = qual == areaProvador; luzProva.enabled = camProva.enabled; }
    }

    static void Limpar(RectTransform area, int manter = 0)
    {
        for (int i = area.childCount - 1; i >= manter; i--) Destroy(area.GetChild(i).gameObject);
    }

    // ================= lista de itens =================
    void MostrarItens(CategoriaItem cat)
    {
        PararPix();
        categoria = cat;
        Mostrar(areaItens);
        Limpar(areaItens, 4); // mantem as 4 abas
        for (int i = 0; i < abas.Count; i++)
        {
            bool sel = i == (int)cat;
            abas[i].GetComponent<Image>().color = sel ? Roxo : Cartao;
            abas[i].GetComponentInChildren<TextMeshProUGUI>().color = sel ? Color.white : new Color(1, 1, 1, 0.6f);
        }

        var itens = Inventario.Catalogo.Where(it => it.categoria == cat).ToList();
        for (int i = 0; i < itens.Count; i++)
        {
            int col = i % 4, lin = i / 4;
            CartaoItem(itens[i], new Vector2(-525 + col * 350, 80 - lin * 320));
        }
    }

    void CartaoItem(ItemLoja it, Vector2 pos)
    {
        var c = Img(areaItens, it.id, pos, new Vector2(330, 300), Cartao).rectTransform;
        c.gameObject.AddComponent<Outline>().effectColor = new Color(it.cor.r, it.cor.g, it.cor.b, 0.5f);
        Icone(c, it, new Vector2(0, 85), 100);
        Txt(c, it.nome, new Vector2(0, 10), new Vector2(310, 44), 34, Color.white, TextAlignmentOptions.Center);
        var d = Txt(c, it.descricao, new Vector2(0, -32), new Vector2(300, 50), 21, new Color(1, 1, 1, 0.65f), TextAlignmentOptions.Center);
        d.textWrappingMode = TextWrappingModes.Normal;
        Txt(c, Inventario.Reais(it.preco), new Vector2(0, -78), new Vector2(300, 40), 34, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center);

        bool tem = !it.Consumivel && Inventario.Tem(it.id);
        bool vestivel = it.categoria == CategoriaItem.Roupa || it.categoria == CategoriaItem.Armas;
        if (!tem && vestivel)
        {
            Botao(c, "PROVAR", new Vector2(-73, -122), new Vector2(134, 46), Rosa, () => AbrirProvador(it), 26);
            Botao(c, "COMPRAR", new Vector2(73, -122), new Vector2(134, 46), Verde, () => Comprar(it), 26);
        }
        else if (!tem)
            Botao(c, "COMPRAR", new Vector2(0, -122), new Vector2(280, 46), Verde, () => Comprar(it), 28);
        else if (it.slot == null)
            Botao(c, "NA FRENTE DA LOJA", new Vector2(0, -122), new Vector2(280, 46), new Color(0.25f, 0.25f, 0.3f), null, 24);
        else
        {
            bool eq = Inventario.EstaEquipado(it);
            Botao(c, eq ? "EQUIPADO (tirar)" : "EQUIPAR", new Vector2(0, -122), new Vector2(280, 46), eq ? Roxo : new Color(0.2f, 0.45f, 0.9f),
                  () => { Inventario.AlternarEquipar(it); MostrarItens(categoria); }, 26);
        }
    }

    void Icone(RectTransform pai, ItemLoja it, Vector2 pos, float tam)
    {
        var fundo = Img(pai, "Icone", pos, new Vector2(tam, tam), new Color(it.cor.r * 0.35f, it.cor.g * 0.35f, it.cor.b * 0.35f, 1f));
        fundo.gameObject.AddComponent<Outline>().effectColor = it.cor;
        string s = it.categoria == CategoriaItem.Dinheiro ? "$" : it.nome.Substring(0, 1);
        if (it.id.StartsWith("pint_")) s = "P"; else if (it.id == "neon_baixo") s = "N"; else if (it.id == "carro_vip") s = "VIP";
        Color corLetra = it.cor.grayscale < 0.25f ? new Color(0.85f, 0.85f, 0.9f) : it.cor;
        Txt(fundo.rectTransform, s, Vector2.zero, new Vector2(tam, tam), s.Length > 1 ? tam * 0.38f : tam * 0.6f, corLetra, TextAlignmentOptions.Center);
    }

    // ================= provador =================
    void AbrirProvador(ItemLoja provarAgora)
    {
        PararPix();
        doProvador = true;
        if (provarAgora != null && !Inventario.Provando(provarAgora)) Inventario.Provar(provarAgora);
        if (camProva == null) CriarCameraProva();
        if (!areaProvador.gameObject.activeSelf && playerT)
        {
            // Arissa de frente para a avenida (onde fica a camera)
            playerT.rotation = Quaternion.LookRotation(Vector3.right);
            anguloProva = 0f;
        }
        Mostrar(areaProvador);
        Limpar(areaProvador);

        Botao(areaProvador, "< VOLTAR", new Vector2(-600, 310), new Vector2(180, 50), Cartao, () => MostrarItens(categoria), 26);
        Txt(areaProvador, "PROVADOR", new Vector2(300, 315), new Vector2(760, 60), 50, Rosa, TextAlignmentOptions.Center);

        // previa da Arissa
        var moldura = Img(areaProvador, "Moldura", new Vector2(-430, -25), new Vector2(436, 596), Rosa).rectTransform;
        var raw = new GameObject("Previa", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        raw.rectTransform.SetParent(moldura, false);
        raw.rectTransform.sizeDelta = new Vector2(420, 580);
        raw.texture = rtProva;
        previaProva = raw.rectTransform;
        Botao(areaProvador, "<", new Vector2(-560, -355), new Vector2(80, 44), Cartao, () => anguloProva -= 45f, 34);
        Txt(areaProvador, "GIRAR (ou arraste)", new Vector2(-430, -355), new Vector2(200, 40), 22, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Center);
        Botao(areaProvador, ">", new Vector2(-300, -355), new Vector2(80, 44), Cartao, () => anguloProva += 45f, 34);

        // lista de roupas e armas
        var itens = Inventario.Catalogo.Where(i => i.categoria == CategoriaItem.Roupa || i.categoria == CategoriaItem.Armas).ToList();
        float y = 245;
        foreach (var it in itens)
        {
            bool provando = Inventario.Provando(it);
            bool tem = Inventario.Tem(it.id);
            var linha = Img(areaProvador, it.id, new Vector2(300, y), new Vector2(760, 52), provando ? new Color(0.3f, 0.08f, 0.3f) : Cartao).rectTransform;
            if (provando) linha.gameObject.AddComponent<Outline>().effectColor = Rosa;
            Icone(linha, it, new Vector2(-350, 0), 40);
            Txt(linha, it.nome, new Vector2(-120, 0), new Vector2(400, 44), 28, Color.white, TextAlignmentOptions.Left);
            Txt(linha, tem ? "JÁ É SEU" : Inventario.Reais(it.preco), new Vector2(150, 0), new Vector2(160, 44), 28, tem ? Verde : new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center);
            Botao(linha, provando ? "TIRAR" : "PROVAR", new Vector2(305, 0), new Vector2(140, 42), provando ? Rosa : new Color(0.2f, 0.45f, 0.9f),
                  () => { Inventario.Provar(it); AbrirProvador(null); }, 26);
            y -= 58;
        }

        var c = Inventario.Carrinho;
        string resumo = c.Count == 0 ? "Prove os acessórios e veja como ficam na Arissa"
                      : $"Ficou bem? {c.Count} {(c.Count == 1 ? "item" : "itens")} para comprar: <color=#FFD633>{Inventario.Reais(c.Sum(i => i.preco))}</color>";
        Txt(areaProvador, resumo, new Vector2(300, -305), new Vector2(760, 40), 28, Color.white, TextAlignmentOptions.Center);
        Botao(areaProvador, "TIRAR TUDO", new Vector2(70, -362), new Vector2(240, 54), Cartao, () => { Inventario.LimparProva(); AbrirProvador(null); }, 26);
        var comprar = Botao(areaProvador, "COMPRAR O QUE FICOU", new Vector2(430, -362), new Vector2(460, 60), Verde, ComprarCarrinho, 32);
        comprar.interactable = c.Count > 0;
        if (c.Count == 0) comprar.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.27f);
    }

    void CriarCameraProva()
    {
        rtProva = new RenderTexture(504, 696, 24) { name = "Provador" };
        var go = new GameObject("CameraProvador");
        go.transform.SetParent(transform, false);
        camProva = go.AddComponent<Camera>();
        camProva.fieldOfView = 34f;
        camProva.nearClipPlane = 0.1f; camProva.farClipPlane = 120f;
        camProva.cullingMask = ~(1 << 5);
        camProva.targetTexture = rtProva;
        camProva.depth = -5;
        camProva.enabled = false;
        luzProva = go.AddComponent<Light>();
        luzProva.type = LightType.Point; luzProva.range = 7f; luzProva.intensity = 3f;
        luzProva.color = new Color(1f, 0.92f, 0.95f); luzProva.shadows = LightShadows.None;
        luzProva.enabled = false;
    }

    void AtualizarCameraProva()
    {
        if (camProva == null || playerT == null) return;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed && previaProva &&
            RectTransformUtility.RectangleContainsScreenPoint(previaProva, mouse.position.ReadValue(), null))
            anguloProva += mouse.delta.ReadValue().x * 0.6f;
        Vector3 alvo = playerT.position + Vector3.up * 1.0f;
        Vector3 dir = Quaternion.Euler(0, anguloProva, 0) * Vector3.right;
        camProva.transform.position = alvo + dir * 3.1f + Vector3.up * 0.25f;
        camProva.transform.LookAt(alvo);
    }

    // ================= escolha da forma de pagamento =================
    void Comprar(ItemLoja it)
    {
        doProvador = false;
        carrinho = new List<ItemLoja> { it };
        AbrirCheckout(it);
    }

    void ComprarCarrinho()
    {
        var c = Inventario.Carrinho;
        if (c.Count == 0) return;
        doProvador = true;
        carrinho = c;
        if (c.Count == 1) { AbrirCheckout(c[0]); return; }
        AbrirCheckout(new ItemLoja
        {
            id = "pacote", nome = c.Count + " ITENS", categoria = CategoriaItem.Roupa, cor = Rosa,
            preco = c.Sum(i => i.preco), descricao = string.Join(", ", c.Select(i => i.nome))
        });
    }

    void VoltarDoCheckout() { if (doProvador) AbrirProvador(null); else MostrarItens(categoria); }

    void AbrirCheckout(ItemLoja it)
    {
        PararPix();
        itemAtual = it;
        Mostrar(areaCheckout);
        Limpar(areaCheckout);
        Resumo(areaCheckout, it);
        Botao(areaCheckout, "< VOLTAR", new Vector2(-600, 290), new Vector2(200, 56), Cartao, VoltarDoCheckout, 28);

        Txt(areaCheckout, "ESCOLHA A FORMA DE PAGAMENTO", new Vector2(250, 230), new Vector2(800, 60), 42, Color.white, TextAlignmentOptions.Center);
        BotaoPagamento(new Vector2(250, 110), "CARTÃO DE CRÉDITO", "1x de " + Inventario.Reais(it.preco) + " sem juros", Roxo, () => AbrirCartao(Forma.Credito));
        BotaoPagamento(new Vector2(250, -20), "PIX", "Aprovação na hora com QR Code", VerdePix, AbrirPix);
        BotaoPagamento(new Vector2(250, -150), "CARTÃO DE DÉBITO", "Desconta direto da conta", new Color(0.15f, 0.5f, 0.95f), () => AbrirCartao(Forma.Debito));
    }

    void BotaoPagamento(Vector2 pos, string titulo, string sub, Color cor, UnityEngine.Events.UnityAction acao)
    {
        var b = Botao(areaCheckout, "", pos, new Vector2(620, 110), cor * 0.75f + new Color(0, 0, 0, 0.25f), acao, 10);
        Txt((RectTransform)b.transform, titulo, new Vector2(0, 16), new Vector2(600, 50), 44, Color.white, TextAlignmentOptions.Center);
        Txt((RectTransform)b.transform, sub, new Vector2(0, -28), new Vector2(600, 30), 22, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.Center);
    }

    void Resumo(RectTransform area, ItemLoja it)
    {
        var box = Img(area, "Resumo", new Vector2(-430, -40), new Vector2(480, 560), Cartao).rectTransform;
        Txt(box, "SEU PEDIDO", new Vector2(0, 240), new Vector2(440, 40), 30, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Center);
        Icone(box, it, new Vector2(0, 110), 160);
        Txt(box, it.nome, new Vector2(0, -20), new Vector2(440, 60), 46, Color.white, TextAlignmentOptions.Center);
        var d = Txt(box, it.descricao, new Vector2(0, -80), new Vector2(420, 60), 24, new Color(1, 1, 1, 0.65f), TextAlignmentOptions.Center);
        d.textWrappingMode = TextWrappingModes.Normal;
        Txt(box, "TOTAL", new Vector2(0, -150), new Vector2(440, 36), 28, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Center);
        Txt(box, Inventario.Reais(it.preco), new Vector2(0, -200), new Vector2(440, 70), 64, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center);
    }

    // ================= cartao de credito / debito =================
    void AbrirCartao(Forma forma)
    {
        formaAtual = forma;
        Mostrar(areaPagamento);
        Limpar(areaPagamento);
        Resumo(areaPagamento, itemAtual);
        Botao(areaPagamento, "< VOLTAR", new Vector2(-600, 290), new Vector2(200, 56), Cartao, () => AbrirCheckout(itemAtual), 28);
        bool credito = forma == Forma.Credito;
        Txt(areaPagamento, credito ? "CARTÃO DE CRÉDITO" : "CARTÃO DE DÉBITO", new Vector2(250, 300), new Vector2(800, 60), 46, Color.white, TextAlignmentOptions.Center);

        // desenho do cartao (mostra o que voce digita)
        Color corC = credito ? new Color(0.4f, 0.1f, 0.75f) : new Color(0.1f, 0.4f, 0.8f);
        var cc = Img(areaPagamento, "Cartao", new Vector2(60, 140), new Vector2(400, 240), corC).rectTransform;
        cc.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6, -6);
        Img(cc, "Faixa", new Vector2(0, -70), new Vector2(400, 40), new Color(1, 1, 1, 0.08f));
        Txt(cc, "GRS BANK", new Vector2(-90, 90), new Vector2(200, 40), 30, Color.white, TextAlignmentOptions.Left);
        Txt(cc, credito ? "CRÉDITO" : "DÉBITO", new Vector2(110, 90), new Vector2(160, 40), 24, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.Right);
        Img(cc, "Chip", new Vector2(-140, 30), new Vector2(54, 40), new Color(0.95f, 0.78f, 0.3f));
        prevNumero = Txt(cc, "", new Vector2(0, -20), new Vector2(370, 44), 34, Color.white, TextAlignmentOptions.Center);
        prevNome = Txt(cc, "", new Vector2(-50, -88), new Vector2(270, 34), 22, Color.white, TextAlignmentOptions.Left);
        prevValidade = Txt(cc, "", new Vector2(140, -88), new Vector2(100, 34), 22, Color.white, TextAlignmentOptions.Right);

        cNumero = Campo(areaPagamento, "Número do cartão", "0000 0000 0000 0000", new Vector2(510, 210), 400, TMP_InputField.ContentType.IntegerNumber, 16);
        cNome = Campo(areaPagamento, "Nome impresso no cartão", "NOME SOBRENOME", new Vector2(510, 120), 400, TMP_InputField.ContentType.Name, 26);
        cValidade = Campo(areaPagamento, "Validade", "MM/AA", new Vector2(405, 30), 190, TMP_InputField.ContentType.Standard, 5);
        cCvv = Campo(areaPagamento, "CVV", "123", new Vector2(615, 30), 190, TMP_InputField.ContentType.Pin, 4);
        foreach (var f in new[] { cNumero, cNome, cValidade, cCvv }) f.onValueChanged.AddListener(_ => AtualizarPrevia());
        AtualizarPrevia();

        Txt(areaPagamento, credito ? "Parcelas: 1x de " + Inventario.Reais(itemAtual.preco) + " sem juros" : "Valor debitado na hora",
            new Vector2(510, -35), new Vector2(400, 34), 24, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.Center);

        Botao(areaPagamento, "USAR CARTÃO DE TESTE", new Vector2(60, -80), new Vector2(400, 60), new Color(0.3f, 0.3f, 0.38f), PreencherTeste, 28);
        Botao(areaPagamento, "PAGAR " + Inventario.Reais(itemAtual.preco), new Vector2(510, -90), new Vector2(400, 80), Verde, ValidarCartao, 38);
        cErro = Txt(areaPagamento, "", new Vector2(285, -160), new Vector2(860, 40), 26, new Color(1f, 0.35f, 0.35f), TextAlignmentOptions.Center);
        Txt(areaPagamento, "Simulação: não digite um cartão de verdade. Nada é cobrado nem salvo.", new Vector2(285, -205), new Vector2(860, 34), 22, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Center);
        EventSystem.current?.SetSelectedGameObject(cNumero.gameObject);
    }

    void PreencherTeste()
    {
        cNumero.text = formaAtual == Forma.Credito ? "4111111111111111" : "5555555555554444";
        cNome.text = "ARISSA GRS";
        cValidade.text = "12/30";
        cCvv.text = "123";
    }

    void AtualizarPrevia()
    {
        string n = cNumero.text.PadRight(16, '*');
        prevNumero.text = n.Substring(0, 4) + " " + n.Substring(4, 4) + " " + n.Substring(8, 4) + " " + n.Substring(12, 4);
        prevNome.text = string.IsNullOrWhiteSpace(cNome.text) ? "SEU NOME" : cNome.text.ToUpper();
        prevValidade.text = string.IsNullOrEmpty(cValidade.text) ? "MM/AA" : cValidade.text;
    }

    void ValidarCartao()
    {
        string num = cNumero.text;
        string erro = null;
        if (num.Length < 13 || !Luhn(num)) erro = "Número do cartão inválido. Use o cartão de teste.";
        else if (cNome.text.Trim().Length < 3) erro = "Digite o nome impresso no cartão.";
        else if (!ValidadeOk(cValidade.text)) erro = "Validade inválida (use MM/AA, ex: 12/30).";
        else if (cCvv.text.Length < 3) erro = "CVV inválido.";
        if (erro != null) { cErro.text = erro; return; }
        string final = num.Substring(num.Length - 4);
        // apaga os dados digitados antes de seguir
        cNumero.text = cNome.text = cValidade.text = cCvv.text = "";
        StartCoroutine(Processar((formaAtual == Forma.Credito ? "Cartão de crédito" : "Cartão de débito") + " final " + final));
    }

    static bool Luhn(string n)
    {
        int soma = 0; bool dobra = false;
        for (int i = n.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(n[i])) return false;
            int d = n[i] - '0';
            if (dobra) { d *= 2; if (d > 9) d -= 9; }
            soma += d; dobra = !dobra;
        }
        return soma % 10 == 0;
    }

    static bool ValidadeOk(string v)
    {
        var p = v.Split('/');
        if (p.Length != 2 || !int.TryParse(p[0], out int mm) || !int.TryParse(p[1], out int aa)) return false;
        if (mm < 1 || mm > 12) return false;
        var hoje = System.DateTime.Now;
        int ano = 2000 + aa;
        return ano > hoje.Year || (ano == hoje.Year && mm >= hoje.Month);
    }

    // ================= PIX =================
    void AbrirPix()
    {
        formaAtual = Forma.Pix;
        Mostrar(areaPagamento);
        Limpar(areaPagamento);
        Resumo(areaPagamento, itemAtual);
        Botao(areaPagamento, "< VOLTAR", new Vector2(-600, 290), new Vector2(200, 56), Cartao, () => AbrirCheckout(itemAtual), 28);
        Txt(areaPagamento, "PAGUE COM PIX", new Vector2(250, 300), new Vector2(800, 60), 46, VerdePix, TextAlignmentOptions.Center);

        string txid = "GRS" + Random.Range(10000000, 99999999);
        string codigo = "00020126580014BR.GOV.BCB.PIX0136grs1-loja-simulada-" + txid.ToLower() + "52040000530398654"
                        + itemAtual.preco.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Length.ToString("00")
                        + itemAtual.preco.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "5802BR5908LOJA GRS6009SAO PAULO62070503***6304" + Random.Range(1000, 9999);

        var qrFundo = Img(areaPagamento, "QR", new Vector2(60, 100), new Vector2(320, 320), Color.white);
        var qr = new GameObject("Codigo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        qr.rectTransform.SetParent(qrFundo.transform, false);
        qr.rectTransform.sizeDelta = new Vector2(300, 300);
        qr.texture = GerarQR(txid.GetHashCode());

        var inst = Txt(areaPagamento, "1. Abra o app do seu banco\n2. Escolha pagar com PIX\n3. Escaneie o QR Code ou cole o código",
            new Vector2(510, 200), new Vector2(420, 120), 24, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Left);
        inst.textWrappingMode = TextWrappingModes.Normal;

        var caixa = Img(areaPagamento, "CopiaCola", new Vector2(510, 95), new Vector2(400, 56), new Color(0, 0, 0, 0.45f)).rectTransform;
        var tc = Txt(caixa, codigo, Vector2.zero, new Vector2(380, 50), 20, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Left);
        tc.overflowMode = TextOverflowModes.Ellipsis;
        TextMeshProUGUI copiado = null;
        Botao(areaPagamento, "COPIAR CÓDIGO PIX", new Vector2(510, 25), new Vector2(400, 56), new Color(0.3f, 0.3f, 0.38f), () =>
        {
            GUIUtility.systemCopyBuffer = codigo;
            copiado.text = "Código copiado!";
        }, 28);
        copiado = Txt(areaPagamento, "", new Vector2(510, -18), new Vector2(400, 30), 22, VerdePix, TextAlignmentOptions.Center);

        var timer = Txt(areaPagamento, "", new Vector2(510, -60), new Vector2(400, 40), 30, Color.white, TextAlignmentOptions.Center);
        var status = Txt(areaPagamento, "Aguardando pagamento...", new Vector2(510, -100), new Vector2(400, 36), 26, new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);
        Botao(areaPagamento, "SIMULAR PAGAMENTO", new Vector2(60, -120), new Vector2(320, 70), VerdePix, () =>
        {
            PararPix();
            StartCoroutine(Processar("PIX (" + txid + ")"));
        }, 32);
        Txt(areaPagamento, "Simulação: este QR Code não é real e não cobra nada.", new Vector2(285, -205), new Vector2(860, 34), 22, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Center);
        pixRotina = StartCoroutine(ContagemPix(timer, status));
    }

    IEnumerator ContagemPix(TextMeshProUGUI timer, TextMeshProUGUI status)
    {
        float fim = Time.unscaledTime + 300f;
        while (Time.unscaledTime < fim)
        {
            int s = Mathf.CeilToInt(fim - Time.unscaledTime);
            timer.text = $"Expira em {s / 60:00}:{s % 60:00}";
            status.alpha = 0.6f + Mathf.PingPong(Time.unscaledTime, 0.4f);
            yield return null;
        }
        timer.text = "QR Code expirado";
        status.text = "Volte e gere outro PIX";
    }

    void PararPix() { if (pixRotina != null) { StopCoroutine(pixRotina); pixRotina = null; } }

    static Texture2D GerarQR(int semente)
    {
        const int n = 29, borda = 2, t = n + borda * 2;
        var tex = new Texture2D(t, t, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var rnd = new System.Random(semente);
        for (int y = 0; y < t; y++) for (int x = 0; x < t; x++) tex.SetPixel(x, y, Color.white);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool preto;
                int fx = x < 8 ? x : (x >= n - 8 ? x - (n - 7) : -1);
                int fy = y < 8 ? y : (y >= n - 8 ? y - (n - 7) : -1);
                bool canto = (x < 8 && y < 8) || (x >= n - 8 && y < 8) || (x < 8 && y >= n - 8);
                if (canto)
                {
                    int lx = x < 8 ? x : x - (n - 7), ly = y < 8 ? y : y - (n - 7);
                    if (lx < 0 || ly < 0 || lx > 6 || ly > 6) preto = false; // separador branco
                    else preto = lx == 0 || lx == 6 || ly == 0 || ly == 6 || (lx >= 2 && lx <= 4 && ly >= 2 && ly <= 4);
                }
                else if (x == 6 || y == 6) preto = (x + y) % 2 == 0; // linhas de tempo
                else preto = rnd.NextDouble() < 0.5;
                if (preto) tex.SetPixel(x + borda, t - 1 - (y + borda), Color.black);
            }
        tex.Apply();
        return tex;
    }

    // ================= processando / aprovado =================
    IEnumerator Processar(string forma)
    {
        Mostrar(areaFim);
        Limpar(areaFim);
        var t = Txt(areaFim, "Processando pagamento", new Vector2(0, 40), new Vector2(1200, 80), 56, Color.white, TextAlignmentOptions.Center);
        var giro = Img(areaFim, "Giro", new Vector2(0, -80), new Vector2(70, 70), Ciano).rectTransform;
        float fim = Time.unscaledTime + 2f;
        while (Time.unscaledTime < fim)
        {
            giro.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 360f);
            t.text = "Processando pagamento" + new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
            yield return null;
        }

        foreach (var item in carrinho) Inventario.Entregar(item);
        Limpar(areaFim);
        string pedido = "GRS-" + Random.Range(10000, 99999);
        var ok = Img(areaFim, "Ok", new Vector2(0, 200), new Vector2(120, 120), Verde);
        Txt(ok.rectTransform, "OK", Vector2.zero, new Vector2(120, 120), 60, Color.white, TextAlignmentOptions.Center);
        Txt(areaFim, "PAGAMENTO APROVADO!", new Vector2(0, 80), new Vector2(1200, 90), 78, Verde, TextAlignmentOptions.Center);
        string entregue = carrinho.Count > 1 ? "Tudo já está equipado na Arissa!"
                         : itemAtual.Consumivel ? $"+$ {itemAtual.moedas:N0} na sua carteira"
                         : itemAtual.id == "carro_vip" ? "Seu carro está estacionado na frente da loja"
                         : itemAtual.slot != null ? "Já está equipado!" : "Item liberado";
        var det = Txt(areaFim, $"{itemAtual.nome}  -  {Inventario.Reais(itemAtual.preco)}\nPago com: {forma}\nPedido #{pedido}\n<color=#7CFF7C>{entregue}</color>",
            new Vector2(0, -50), new Vector2(1000, 170), 32, Color.white, TextAlignmentOptions.Center);
        det.textWrappingMode = TextWrappingModes.Normal;
        Txt(areaFim, "(simulação - nenhuma cobrança real)", new Vector2(0, -160), new Vector2(1000, 34), 22, new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Center);
        Botao(areaFim, "VOLTAR PARA A LOJA", new Vector2(-220, -250), new Vector2(400, 70), Roxo, () => { if (doProvador) AbrirProvador(null); else MostrarItens(categoria); }, 32);
        Botao(areaFim, "SAIR DA LOJA", new Vector2(220, -250), new Vector2(400, 70), Cartao, Fechar, 32);
    }

    // ================= helpers de UI =================
    Image Img(Transform pai, string nome, Vector2 pos, Vector2 tam, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        var im = go.GetComponent<Image>();
        im.color = cor;
        return im;
    }

    TextMeshProUGUI Txt(Transform pai, string texto, Vector2 pos, Vector2 tam, float tamFonte, Color cor, TextAlignmentOptions al, TMP_FontAsset f = null)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        var t = go.GetComponent<TextMeshProUGUI>();
        var ff = f != null ? f : fonte;
        if (ff != null) t.font = ff;
        t.text = texto; t.fontSize = tamFonte; t.color = cor; t.alignment = al;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    Button Botao(Transform pai, string texto, Vector2 pos, Vector2 tam, Color cor, UnityEngine.Events.UnityAction acao, float tamFonte)
    {
        var im = Img(pai, "Botao", pos, tam, cor);
        var b = im.gameObject.AddComponent<Button>();
        var cb = b.colors;
        cb.normalColor = Color.white; cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f); cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        cb.selectedColor = Color.white; cb.colorMultiplier = 1.3f; cb.fadeDuration = 0.08f;
        b.colors = cb;
        if (acao != null) b.onClick.AddListener(acao); else b.interactable = false;
        if (!string.IsNullOrEmpty(texto)) Txt(im.rectTransform, texto, Vector2.zero, tam, tamFonte, Color.white, TextAlignmentOptions.Center);
        return b;
    }

    TMP_InputField Campo(Transform pai, string rotulo, string dica, Vector2 pos, float largura, TMP_InputField.ContentType tipo, int limite)
    {
        Txt(pai, rotulo, pos + new Vector2(0, 42), new Vector2(largura, 30), 22, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Left);
        var go = new GameObject("Campo", typeof(RectTransform), typeof(Image));
        go.SetActive(false);
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(largura, 56);
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        go.AddComponent<Outline>().effectColor = new Color(0.55f, 0.15f, 1f, 0.8f);

        var area = new GameObject("Area", typeof(RectTransform), typeof(RectMask2D));
        var ar = (RectTransform)area.transform;
        ar.SetParent(rt, false);
        ar.anchorMin = Vector2.zero; ar.anchorMax = Vector2.one; ar.offsetMin = new Vector2(14, 6); ar.offsetMax = new Vector2(-14, -6);

        var ph = Txt(ar, dica, Vector2.zero, Vector2.zero, 28, new Color(1, 1, 1, 0.3f), TextAlignmentOptions.Left);
        var txt = Txt(ar, "", Vector2.zero, Vector2.zero, 28, Color.white, TextAlignmentOptions.Left);
        foreach (var t in new[] { ph, txt })
        {
            var r = t.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        }

        var inp = go.AddComponent<TMP_InputField>();
        inp.textViewport = ar;
        inp.textComponent = txt;
        inp.placeholder = ph;
        if (fonte) inp.fontAsset = fonte;
        inp.pointSize = 28;
        inp.contentType = tipo;
        inp.characterLimit = limite;
        inp.caretWidth = 3; inp.customCaretColor = true; inp.caretColor = Ciano;
        inp.selectionColor = new Color(0.55f, 0.15f, 1f, 0.5f);
        go.SetActive(true);
        return inp;
    }
}
