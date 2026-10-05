using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Nivel de procurado (estrelas), viaturas perseguindo e captura ("PRESA!").
public class Procurado : MonoBehaviour
{
    public static Procurado Instancia;
    public int estrelas;
    public int maxEstrelas = 5;
    public GameObject modeloViatura;      // viatura modelo (desativada) criada pelo editor
    public float tempoParaPrender = 2.5f;
    public float reacaoPolicia = 3f;      // segundos ate os policiais a pe reagirem
    float crimeEm;
    Vector3 ultimaPos;
    float imuneAte;

    Transform player;
    Vector3 inicioPlayer; Quaternion inicioRot;
    float semPoliciaDesde, ultimaPerda, captura;
    readonly List<PoliciaCarro> viaturas = new List<PoliciaCarro>();
    Image[] icones;
    float pisca;

    void Awake() { Instancia = this; }

    void Start()
    {
        var p = GameObject.Find("Player");
        if (p) { player = p.transform; inicioPlayer = player.position; inicioRot = player.rotation; }
        CriarHUD();
    }

    public void Crime(int quanto, string motivo = null)
    {
        int antes = estrelas;
        estrelas = Mathf.Clamp(estrelas + quanto, 0, maxEstrelas);
        semPoliciaDesde = Time.time; ultimaPerda = Time.time;
        if (antes == 0) crimeEm = Time.time;
        if (estrelas > antes && Dinheiro.Instancia) Dinheiro.Instancia.Aviso("POLICIA A CAMINHO! " + new string('*', estrelas), 2.5f);
        pisca = 1.5f;
    }

    void Update()
    {
        if (player == null) return;
        AtualizarHUD();
        if (estrelas == 0) { DispensarViaturas(); captura = 0; return; }

        // viaturas: 1 por estrela (max 4)
        viaturas.RemoveAll(v => v == null || !v.enabled);
        int quer = Mathf.Min(estrelas, 4);
        if (viaturas.Count < quer && modeloViatura) ChamarViatura();

        // policiais a pe por perto entram na perseguicao
        if (Time.time - crimeEm > reacaoPolicia)
            foreach (var ped in Pedestre.Todos)
                if (ped.policial && !ped.morto && !ped.perseguindo && Vector3.Distance(ped.transform.position, PosicaoAlvo()) < 35f)
                    ped.Perseguir(true);

        // alguma policia perto? (senao as estrelas vao baixando)
        float maisPerto = float.MaxValue; string quem = "";
        foreach (var v in viaturas) { float dv = Vector3.Distance(v.transform.position, PosicaoAlvo()); if (dv < maisPerto) { maisPerto = dv; quem = v.name; } }
        foreach (var ped in Pedestre.Todos) if (ped.perseguindo && !ped.morto) { float dp = Vector3.Distance(ped.transform.position, PosicaoAlvo()); if (dp < maisPerto) { maisPerto = dp; quem = ped.name; } }
        if (maisPerto < 35f) semPoliciaDesde = Time.time;
        if (Time.time - semPoliciaDesde > 12f && Time.time - ultimaPerda > 8f)
        {
            estrelas--; ultimaPerda = Time.time;
            if (Dinheiro.Instancia) Dinheiro.Instancia.Aviso(estrelas == 0 ? "Voce despistou a policia!" : "A policia esta perdendo seu rastro...", 2.5f);
        }

        // captura: policia colada em voce (a pe) ou carro parado cercado
        var carro = player.GetComponentInParent<CarroSimples>();
        bool dirigindo = carro && carro.dirigindo;
        float limite = dirigindo ? 5f : 1.8f;
        // a pe: so prende se estiver parada/andando devagar (correndo voce escapa)
        Vector3 mov = player.position - ultimaPos; ultimaPos = player.position;
        float velPlayer = mov.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        bool podePrender = dirigindo ? carro.GetComponent<Rigidbody>().linearVelocity.magnitude < 3f : velPlayer < 3f;
        if (podePrender && maisPerto < limite && Time.time > imuneAte)
        {
            captura += Time.deltaTime;
            if (Dinheiro.Instancia) Dinheiro.Instancia.Aviso("SENDO PRESA! Fuja! " + Mathf.CeilToInt(tempoParaPrender * Acessorios.MultPrisao - captura), 0.3f);
            if (captura >= tempoParaPrender * Acessorios.MultPrisao) { Debug.Log($"GRS 1: presa por {quem} a {maisPerto:F1} m"); Prender(); }
        }
        else captura = Mathf.Max(0, captura - Time.deltaTime * 2f);
    }

    public Vector3 PosicaoAlvo() => player ? player.position : Vector3.zero;

    void ChamarViatura()
    {
        // aparece numa rua longe da Arissa e vem atras dela
        for (int t = 0; t < 12; t++)
        {
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(70f, 100f);
            Vector3 p = PosicaoAlvo() + new Vector3(r.x, 0, r.y);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                var g = Instantiate(modeloViatura, hit.position, Quaternion.LookRotation(PosicaoAlvo() - hit.position));
                g.name = "Viatura";
                g.SetActive(true);
                var pc = g.AddComponent<PoliciaCarro>();
                pc.velocidade = 13f + estrelas * 2f;
                viaturas.Add(pc);
                return;
            }
        }
    }

    void DispensarViaturas()
    {
        foreach (var v in viaturas) if (v) v.IrEmbora();
        viaturas.Clear();
        foreach (var ped in Pedestre.Todos) if (ped.perseguindo) ped.Perseguir(false);
    }

    void Prender()
    {
        captura = 0;
        imuneAte = Time.time + 10f;
        int multa = 0;
        if (Dinheiro.Instancia)
        {
            multa = Mathf.RoundToInt(Dinheiro.Instancia.valor * 0.3f);
            Dinheiro.Instancia.Adicionar(-multa);
            Dinheiro.Instancia.Aviso($"PRESA!  Multa de ${multa:N0}", 4f);
        }
        estrelas = 0;
        DispensarViaturas();
        foreach (var v in FindObjectsByType<PoliciaCarro>(FindObjectsSortMode.None)) Destroy(v.gameObject);

        // tira do carro e volta pro ponto inicial (a "delegacia")
        var ec = player.GetComponent<EntrarCarro>();
        if (ec) ec.SairDoCarro();
        var cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        player.SetPositionAndRotation(inicioPlayer, inicioRot);
        if (cc) cc.enabled = true;
    }

    // ---------- HUD de estrelas ----------
    void CriarHUD()
    {
        var canvas = new GameObject("HUD_Procurado", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 11;
        var sc = canvas.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        canvas.transform.SetParent(transform, false);
        var spr = SpriteEstrela();
        icones = new Image[maxEstrelas];
        for (int i = 0; i < maxEstrelas; i++)
        {
            var go = new GameObject("Estrela", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(46, 46);
            rt.anchoredPosition = new Vector2(-40 - (maxEstrelas - 1 - i) * 52, -170);
            icones[i] = go.GetComponent<Image>(); icones[i].sprite = spr; icones[i].raycastTarget = false;
        }
    }

    void AtualizarHUD()
    {
        if (icones == null) return;
        pisca = Mathf.Max(0, pisca - Time.deltaTime);
        bool on = pisca <= 0 || Mathf.Repeat(Time.time * 6f, 1f) > 0.4f;
        for (int i = 0; i < icones.Length; i++)
        {
            bool cheia = i < estrelas;
            icones[i].color = cheia ? (on ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.85f, 0.2f, 0.25f)) : new Color(1, 1, 1, estrelas > 0 ? 0.25f : 0f);
        }
    }

    static Sprite SpriteEstrela()
    {
        int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var pts = new Vector2[10];
        for (int k = 0; k < 10; k++)
        {
            float ang = Mathf.PI / 2 + k * Mathf.PI / 5; float r = (k % 2 == 0) ? 30f : 13f;
            pts[k] = new Vector2(32 + Mathf.Cos(ang) * r, 32 + Mathf.Sin(ang) * r);
        }
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            tex.SetPixel(x, y, Dentro(new Vector2(x + 0.5f, y + 0.5f), pts) ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static bool Dentro(Vector2 p, Vector2[] poly)
    {
        bool c = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
        return c;
    }
}
