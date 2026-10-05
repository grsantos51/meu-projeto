using UnityEngine;

// Fica no Player: deixa o mar "funcional".
// - no raso a Arissa anda mais devagar (agua na perna)
// - no fundo ela nada: flutua com a cabeca para fora e deita para nadar quando anda
// - respingos na agua e limites para nao nadar ate o horizonte
public class Natacao : MonoBehaviour
{
    public static Natacao Instancia;
    public float nivelAgua = -0.35f;        // altura da superficie do mar
    public float profundidadeNado = 1.3f;   // a partir daqui ela nada
    public float limiteMar = 175f;          // ate onde da para nadar (z)

    public bool Nadando { get; private set; }
    public bool NaAgua { get; private set; }
    public float Mult => Nadando ? 0.55f : (NaAgua ? 0.7f : 1f);
    public float AlturaNado => nivelAgua - 1.3f; // pes; a cabeca fica fora d'agua

    CharacterController cc;
    Transform modelo;
    Vector3 posBase; Quaternion rotBase;
    float inclinacao;
    Vector3 ultimaPos;
    float velHoriz;
    ParticleSystem respingo;
    bool avisou;

    void Awake() { Instancia = this; }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        var anim = GetComponentInChildren<Animator>();
        if (anim) { modelo = anim.transform; posBase = modelo.localPosition; rotBase = modelo.localRotation; }
        ultimaPos = transform.position;

        // a parede invisivel do raso continua segurando os CARROS, mas a Arissa passa
        var praia = GameObject.Find("Praia");
        if (praia && cc)
            foreach (var col in praia.GetComponentsInChildren<BoxCollider>())
                if (col.name == "ParedeInvisivel" && col.bounds.center.z > 95f && col.bounds.size.x > 50f)
                    Physics.IgnoreCollision(cc, col);

        // novos limites no mar (fundo e laterais)
        Limite(new Vector3(0, 2f, limiteMar), new Vector3(200, 12, 1));
        Limite(new Vector3(-85f, 2f, (95f + limiteMar) / 2f), new Vector3(1, 12, limiteMar - 95f));
        Limite(new Vector3(85f, 2f, (95f + limiteMar) / 2f), new Vector3(1, 12, limiteMar - 95f));

        CriarRespingo();
    }

    void Limite(Vector3 pos, Vector3 tam)
    {
        var g = new GameObject("LimiteMar", typeof(BoxCollider));
        g.transform.position = pos;
        g.GetComponent<BoxCollider>().size = tam;
    }

    void Update()
    {
        Vector3 d = transform.position - ultimaPos; d.y = 0;
        velHoriz = Mathf.Lerp(velHoriz, d.magnitude / Mathf.Max(Time.deltaTime, 0.0001f), 10f * Time.deltaTime);
        ultimaPos = transform.position;

        if (cc == null || !cc.enabled) { NaAgua = Nadando = false; return; } // dentro do carro

        // profundidade = superficie - chao embaixo dela
        float chao = -100f;
        if (Physics.Raycast(transform.position + Vector3.up * 1.2f, Vector3.down, out RaycastHit h, 8f, ~0, QueryTriggerInteraction.Ignore))
            chao = h.point.y;
        float prof = nivelAgua - chao;
        bool noMar = transform.position.z > 85f;

        NaAgua = noMar && prof > 0.12f && transform.position.y < nivelAgua + 0.2f;
        if (!Nadando && noMar && prof > profundidadeNado) Nadando = true;
        else if (Nadando && (!noMar || prof < profundidadeNado - 0.2f)) Nadando = false;

        if (Nadando && !avisou && Dinheiro.Instancia) { Dinheiro.Instancia.Aviso("Nadando! (use as setas para nadar)", 2.5f); avisou = true; }
        if (!NaAgua) avisou = false;

        // respingos quando se mexe na agua
        if (NaAgua && velHoriz > 0.6f && respingo)
        {
            respingo.transform.position = new Vector3(transform.position.x, nivelAgua + 0.03f, transform.position.z);
            respingo.Emit(Mathf.CeilToInt(velHoriz * 8f * Time.deltaTime + (Random.value < 0.5f ? 1 : 0)));
        }
    }

    void LateUpdate()
    {
        if (modelo == null) return;
        // deita para nadar quando esta se movendo no fundo
        float alvo = Nadando && velHoriz > 0.4f ? 1f : 0f;
        inclinacao = Mathf.MoveTowards(inclinacao, alvo, 2.5f * Time.deltaTime);
        if (inclinacao <= 0.001f && !Nadando) { modelo.localPosition = posBase; modelo.localRotation = rotBase; return; }
        float s = Mathf.SmoothStep(0f, 1f, inclinacao);
        modelo.localRotation = rotBase * Quaternion.Euler(75f * s, 0f, 0f);
        modelo.localPosition = posBase + new Vector3(0f, 1.15f * s, -0.7f * s);
    }

    void CriarRespingo()
    {
        var go = new GameObject("Respingo");
        respingo = go.AddComponent<ParticleSystem>();
        respingo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = respingo.main;
        main.loop = false; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new Color(0.85f, 0.95f, 1f, 0.8f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        var em = respingo.emission; em.rateOverTime = 0f;
        var sh = respingo.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.35f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        var col = respingo.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        var sh2 = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh2)
        {
            var m = new Material(sh2);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetTexture("_BaseMap", Gota());
            rend.sharedMaterial = m;
        }
    }

    static Texture2D Gota()
    {
        const int n = 32;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * 1.5f));
            }
        t.Apply();
        return t;
    }
}
