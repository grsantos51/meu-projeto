using System.Collections.Generic;
using UnityEngine;

// Aplica a pintura e o neon comprados na loja no carro que a Arissa dirigir.
public class PinturaCarro : MonoBehaviour
{
    public static Material materialCinza;   // AFRC_Mat_Col4 (definido pela Loja)

    readonly Dictionary<Renderer, Material[]> originais = new Dictionary<Renderer, Material[]>();
    GameObject neon;
    bool pintadoAgora;

    public static void Aplicar(CarroSimples carro)
    {
        var p = carro.GetComponent<PinturaCarro>();
        if (p == null) p = carro.gameObject.AddComponent<PinturaCarro>();
        p.Atualizar();
    }

    void OnEnable() { Inventario.Mudou += Atualizar; }
    void OnDisable() { Inventario.Mudou -= Atualizar; }

    public void Atualizar()
    {
        var pintura = Inventario.ItemEquipado("pintura");
        Pintar(pintura != null ? pintura.cor : (Color?)null);
        Neon(Inventario.EstaEquipado(Inventario.Item("neon_baixo")));
    }

    void Pintar(Color? cor)
    {
        if (originais.Count == 0)
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer) && r.gameObject != neon && !r.name.StartsWith("Neon"))
                    originais[r] = r.sharedMaterials;

        if (cor == null)
        {
            if (!pintadoAgora) return;
            foreach (var kv in originais) if (kv.Key) kv.Key.sharedMaterials = kv.Value;
            pintadoAgora = false;
            return;
        }

        foreach (var kv in originais)
        {
            if (!kv.Key) continue;
            var mats = (Material[])kv.Value.Clone();
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null || m.name.Contains("Emission") || m.name.Contains("Env")) continue;
                // carro ARCADE: troca a textura colorida pela cinza e tinge; outros: so tinge
                var baseM = (m.name.StartsWith("AFRC_Mat_Col") && materialCinza) ? materialCinza : m;
                var nova = new Material(baseM) { name = baseM.name + " (pintura)" };
                Color c = cor.Value;
                if (baseM == materialCinza) c *= 1.35f; // a textura cinza e escura
                nova.SetColor("_BaseColor", c);
                if (nova.HasProperty("_Smoothness")) nova.SetFloat("_Smoothness", 0.75f);
                mats[i] = nova;
            }
            kv.Key.sharedMaterials = mats;
        }
        pintadoAgora = true;
    }

    void Neon(bool ligado)
    {
        if (!ligado) { if (neon) neon.SetActive(false); return; }
        if (neon == null)
        {
            neon = new GameObject("NeonBaixo");
            neon.transform.SetParent(transform, false);
            neon.transform.localPosition = new Vector3(0, 0.25f, 0);
            var luz = neon.AddComponent<Light>();
            luz.type = LightType.Point; luz.color = new Color(0.1f, 1f, 1f);
            luz.range = 6f; luz.intensity = 6f; luz.shadows = LightShadows.None;
            // faixa brilhando no chao
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "NeonChao"; Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(neon.transform, false);
            q.transform.localPosition = new Vector3(0, -0.2f, 0);
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            q.transform.localScale = new Vector3(2.2f, 4.4f, 1f);
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0);
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetColor("_BaseColor", new Color(0.1f, 1f, 1f, 0.35f));
            m.SetTexture("_BaseMap", Brilho());
            q.GetComponent<Renderer>().sharedMaterial = m;
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        neon.SetActive(true);
    }

    void Update()
    {
        if (neon && neon.activeSelf)
        {
            var l = neon.GetComponent<Light>();
            l.intensity = 5f + Mathf.Sin(Time.time * 4f) * 1.2f;
        }
    }

    static Texture2D brilhoTex;
    static Texture2D Brilho()
    {
        if (brilhoTex) return brilhoTex;
        const int n = 64;
        brilhoTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy) * 0.9f));
                brilhoTex.SetPixel(x, y, new Color(1, 1, 1, a * a * 1.5f));
            }
        brilhoTex.Apply();
        return brilhoTex;
    }
}
