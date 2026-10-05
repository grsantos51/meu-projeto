using System.Collections.Generic;
using UnityEngine;

// Fica no Player: monta na Arissa os acessorios comprados/equipados na loja
// e da os bonus das armas e itens.
public class Acessorios : MonoBehaviour
{
    public static Acessorios Instancia;

    // bonus usados por PlayerSimples e Procurado
    public static float AlcanceSoco => Inventario.EstaEquipado(Inventario.Item("taco")) ? 1.8f : 1f;
    public static float EsperaSoco => Inventario.EstaEquipado(Inventario.Item("soco_ingles")) ? 0.35f : 0.7f;
    public static float MultPrisao => Inventario.EstaEquipado(Inventario.Item("colete")) ? 2f : 1f;

    // ajuste fino da posicao do taco na mao (testado na Arissa)
    public Vector3 tacoRotacao = new Vector3(0f, 0f, 90f);

    Animator anim;
    readonly Dictionary<string, GameObject> visuais = new Dictionary<string, GameObject>();
    static Material matBase;

    void Awake() { Instancia = this; }

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        Inventario.Mudou += Atualizar;
        Atualizar();
    }

    void OnDestroy() { Inventario.Mudou -= Atualizar; }

    public void Atualizar()
    {
        if (anim == null || !anim.isHuman) return;
        foreach (var it in Inventario.Catalogo)
        {
            if (it.categoria != CategoriaItem.Roupa && it.categoria != CategoriaItem.Armas) continue;
            bool quer = Inventario.Visivel(it); // inclui o que esta sendo provado na loja
            visuais.TryGetValue(it.id, out var go);
            if (quer && go == null) { go = Criar(it); if (go) visuais[it.id] = go; }
            if (go) go.SetActive(quer);
        }
    }

    // ---------- construcao dos acessorios (formas simples) ----------
    GameObject Criar(ItemLoja it)
    {
        var cabeca = anim.GetBoneTransform(HumanBodyBones.Head);
        var pescoco = anim.GetBoneTransform(HumanBodyBones.Neck) ?? cabeca;
        var peito = anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? anim.GetBoneTransform(HumanBodyBones.Chest);
        var mao = anim.GetBoneTransform(HumanBodyBones.RightHand);
        if (cabeca == null || peito == null || mao == null) return null;

        // escala pela altura da personagem (formas feitas para ~1,7 m)
        float k = Mathf.Max(0.3f, (cabeca.position.y - transform.position.y) / 1.5f);
        Vector3 f = transform.forward, u = Vector3.up, r = transform.right;
        var raiz = new GameObject("Acessorio_" + it.id);
        raiz.transform.SetPositionAndRotation(cabeca.position, transform.rotation);
        Transform osso = cabeca;

        switch (it.id)
        {
            case "oculos":
            {
                var m = Mat(new Color(0.03f, 0.03f, 0.04f), 0.95f, 0.6f);
                Vector3 c = u * 0.085f + f * 0.095f;
                Peca(raiz, PrimitiveType.Cube, c + r * 0.034f, new Vector3(0.055f, 0.034f, 0.012f), m, k);
                Peca(raiz, PrimitiveType.Cube, c - r * 0.034f, new Vector3(0.055f, 0.034f, 0.012f), m, k);
                Peca(raiz, PrimitiveType.Cube, c + u * 0.01f, new Vector3(0.03f, 0.007f, 0.008f), m, k);
                Peca(raiz, PrimitiveType.Cube, c + r * 0.066f - f * 0.05f, new Vector3(0.006f, 0.008f, 0.1f), m, k);
                Peca(raiz, PrimitiveType.Cube, c - r * 0.066f - f * 0.05f, new Vector3(0.006f, 0.008f, 0.1f), m, k);
                break;
            }
            case "bone":
            {
                var m = Mat(it.cor, 0.4f, 0f, it.cor * 1.2f);
                Peca(raiz, PrimitiveType.Sphere, u * 0.15f - f * 0.005f, new Vector3(0.2f, 0.13f, 0.215f), m, k);
                Peca(raiz, PrimitiveType.Cube, u * 0.13f + f * 0.13f, new Vector3(0.17f, 0.012f, 0.11f), m, k);
                break;
            }
            case "chapeu":
            {
                var m = Mat(it.cor, 0.2f, 0f);
                var fita = Mat(new Color(0.15f, 0.1f, 0.08f), 0.3f, 0f);
                Peca(raiz, PrimitiveType.Cylinder, u * 0.2f, new Vector3(0.19f, 0.055f, 0.2f), m, k);
                Peca(raiz, PrimitiveType.Cylinder, u * 0.165f, new Vector3(0.195f, 0.015f, 0.205f), fita, k);
                Peca(raiz, PrimitiveType.Cylinder, u * 0.15f, new Vector3(0.36f, 0.005f, 0.37f), m, k);
                break;
            }
            case "corrente":
            {
                osso = pescoco;
                raiz.transform.position = pescoco.position;
                var m = Mat(it.cor, 0.85f, 1f, it.cor * 0.25f);
                for (int i = 0; i < 18; i++)
                {
                    float a = i / 18f * Mathf.PI * 2f, cos = Mathf.Cos(a);
                    Vector3 p = r * Mathf.Sin(a) * 0.07f + f * cos * 0.075f + u * (-0.01f - 0.07f * Mathf.Max(0f, cos));
                    Peca(raiz, PrimitiveType.Sphere, p, Vector3.one * 0.022f, m, k);
                }
                Peca(raiz, PrimitiveType.Cube, f * 0.085f - u * 0.1f, new Vector3(0.035f, 0.045f, 0.01f), m, k);
                break;
            }
            case "mochila":
            {
                osso = peito;
                raiz.transform.position = peito.position;
                var m = Mat(it.cor, 0.3f, 0f);
                var esc = Mat(it.cor * 0.5f, 0.3f, 0f);
                Peca(raiz, PrimitiveType.Cube, -f * 0.17f - u * 0.06f, new Vector3(0.3f, 0.38f, 0.14f), m, k);
                Peca(raiz, PrimitiveType.Cube, -f * 0.25f - u * 0.12f, new Vector3(0.22f, 0.16f, 0.04f), esc, k);
                Peca(raiz, PrimitiveType.Cube, r * 0.1f - f * 0.03f + u * 0.06f, new Vector3(0.04f, 0.3f, 0.2f), esc, k);
                Peca(raiz, PrimitiveType.Cube, -r * 0.1f - f * 0.03f + u * 0.06f, new Vector3(0.04f, 0.3f, 0.2f), esc, k);
                break;
            }
            case "colete":
            {
                osso = peito;
                raiz.transform.position = peito.position;
                var m = Mat(it.cor, 0.25f, 0f);
                var faixa = Mat(new Color(0.9f, 0.9f, 0.2f), 0.3f, 0f, new Color(0.5f, 0.5f, 0.1f));
                Peca(raiz, PrimitiveType.Cube, -u * 0.08f + f * 0.005f, new Vector3(0.36f, 0.4f, 0.27f), m, k);
                Peca(raiz, PrimitiveType.Cube, -u * 0.12f + f * 0.142f, new Vector3(0.3f, 0.04f, 0.005f), faixa, k);
                break;
            }
            case "taco":
            {
                osso = mao;
                raiz.transform.SetPositionAndRotation(mao.position, mao.rotation * Quaternion.Euler(tacoRotacao));
                var m = Mat(it.cor, 0.5f, 0f);
                var cabo = Mat(new Color(0.1f, 0.1f, 0.1f), 0.2f, 0f);
                // eixo do taco = "up" local da raiz
                Peca(raiz, PrimitiveType.Cylinder, raiz.transform.up * 0.05f, new Vector3(0.03f, 0.1f, 0.03f), cabo, k);
                Peca(raiz, PrimitiveType.Cylinder, raiz.transform.up * 0.42f, new Vector3(0.05f, 0.28f, 0.05f), m, k);
                Peca(raiz, PrimitiveType.Capsule, raiz.transform.up * 0.62f, new Vector3(0.065f, 0.14f, 0.065f), m, k);
                break;
            }
            case "soco_ingles":
            {
                osso = mao;
                raiz.transform.SetPositionAndRotation(mao.position, mao.rotation);
                var m = Mat(it.cor, 0.95f, 1f, new Color(0.1f, 0.1f, 0.12f));
                Peca(raiz, PrimitiveType.Cube, mao.up * 0.09f * k + mao.forward * 0.015f * k, new Vector3(0.09f, 0.03f, 0.04f), m, 1f, mao.rotation);
                break;
            }
        }
        raiz.transform.SetParent(osso, true);
        return raiz;
    }

    static void Peca(GameObject raiz, PrimitiveType tipo, Vector3 offset, Vector3 tamanho, Material m, float k, Quaternion? rot = null)
    {
        var p = GameObject.CreatePrimitive(tipo);
        Destroy(p.GetComponent<Collider>());
        p.transform.SetParent(raiz.transform, false);
        p.transform.position = raiz.transform.position + offset * k;
        p.transform.rotation = rot ?? raiz.transform.rotation;
        // cilindro/capsula do Unity tem 2 m de altura: tamanho.y vira meia altura
        p.transform.localScale = tamanho * k;
        var rd = p.GetComponent<Renderer>();
        rd.sharedMaterial = m;
        rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    static Material Mat(Color cor, float liso, float metal, Color? emissao = null)
    {
        if (matBase == null) matBase = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var m = new Material(matBase);
        m.SetColor("_BaseColor", cor);
        m.SetFloat("_Smoothness", liso);
        m.SetFloat("_Metallic", metal);
        if (emissao.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emissao.Value); }
        return m;
    }
}
