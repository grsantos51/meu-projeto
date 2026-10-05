using TMPro;
using UnityEngine;

// Efeito de letreiro neon: acende letra por letra piscando,
// depois fica com brilho pulsando, cor mudando e "falhas" aleatorias.
[RequireComponent(typeof(TextMeshProUGUI))]
public class NeonAnimado : MonoBehaviour
{
    public Color neonA = new Color(1f, 0.15f, 0.75f);   // rosa
    public Color neonB = new Color(0.1f, 0.9f, 1f);     // ciano
    public float velocidadeCor = 0.35f;
    public float tempoPorLetra = 0.18f;
    [Range(0f, 1f)] public float chanceFalha = 0.012f;
    public bool trocarCor = true;
    public bool contornoPreto = false; // letra branca + contorno preto + brilho colorido

    TextMeshProUGUI tmp;
    Material mat;
    float t0, falhaAte, proximaFalhaLetra;
    int letraFalhando = -1;

    static readonly int GlowColor = Shader.PropertyToID("_GlowColor");
    static readonly int GlowPower = Shader.PropertyToID("_GlowPower");
    static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
    static readonly int UnderlayColor = Shader.PropertyToID("_UnderlayColor");

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        mat = tmp.fontMaterial; // instancia so deste texto
    }

    void OnEnable() { t0 = Time.unscaledTime; }

    void Update()
    {
        float t = Time.unscaledTime - t0;
        int total = tmp.textInfo.characterCount > 0 ? tmp.textInfo.characterCount : tmp.text.Length;

        // 1) Acendendo letra por letra (com piscadas)
        float acender = t / tempoPorLetra;
        int visiveis = Mathf.Clamp(Mathf.FloorToInt(acender), 0, total);
        bool piscaEntrada = visiveis < total && Mathf.Repeat(acender, 1f) > 0.55f && Random.value > 0.5f;
        tmp.maxVisibleCharacters = piscaEntrada ? Mathf.Max(0, visiveis - 1) : visiveis;

        // 2) Cor neon trocando devagar
        Color neon = trocarCor ? Color.Lerp(neonA, neonB, (Mathf.Sin(t * velocidadeCor * Mathf.PI * 2f) + 1f) * 0.5f) : neonA;

        // 3) Brilho pulsando + falha geral aleatoria
        if (Time.unscaledTime > falhaAte && Random.value < chanceFalha) falhaAte = Time.unscaledTime + Random.Range(0.04f, 0.18f);
        bool falhando = Time.unscaledTime < falhaAte;
        float brilho = falhando ? 0.15f : 0.75f + Mathf.Sin(t * 6f) * 0.08f + Mathf.PerlinNoise(t * 3f, 0f) * 0.15f;

        mat.SetColor(GlowColor, new Color(neon.r, neon.g, neon.b, brilho));
        mat.SetFloat(GlowPower, brilho);
        mat.SetColor(OutlineColor, contornoPreto ? Color.black : Color.Lerp(neon, Color.black, falhando ? 0.6f : 0f));
        mat.SetColor(UnderlayColor, new Color(neon.r, neon.g, neon.b, falhando ? 0.1f : 0.55f));
        tmp.color = falhando ? new Color(0.5f, 0.45f, 0.5f) : (contornoPreto ? Color.white : Color.Lerp(Color.white, neon, 0.25f));

        // 4) Uma letra "com mau contato" de vez em quando
        if (t > total * tempoPorLetra + 1f) PiscarUmaLetra(total);
    }

    void PiscarUmaLetra(int total)
    {
        if (Time.unscaledTime > proximaFalhaLetra)
        {
            letraFalhando = Random.value < 0.5f ? Random.Range(0, total) : -1;
            proximaFalhaLetra = Time.unscaledTime + Random.Range(0.6f, 2.5f);
        }
        if (letraFalhando < 0 || letraFalhando >= tmp.textInfo.characterCount) return;
        var ci = tmp.textInfo.characterInfo[letraFalhando];
        if (!ci.isVisible) return;
        bool apagada = Mathf.PerlinNoise(Time.unscaledTime * 25f, 3f) > 0.55f;
        if (!apagada) return;
        tmp.ForceMeshUpdate();
        ci = tmp.textInfo.characterInfo[letraFalhando];
        var cores = tmp.textInfo.meshInfo[ci.materialReferenceIndex].colors32;
        var escuro = new Color32(70, 60, 70, 255);
        for (int i = 0; i < 4; i++) cores[ci.vertexIndex + i] = escuro;
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
