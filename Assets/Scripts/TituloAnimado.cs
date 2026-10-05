using TMPro;
using UnityEngine;

// Animacao do titulo: entra caindo com quique, depois pulsa, balanca
// e as cores do degrade ficam "brilhando".
[RequireComponent(typeof(TextMeshProUGUI))]
public class TituloAnimado : MonoBehaviour
{
    public float duracaoEntrada = 0.9f;
    public float pulso = 0.05f;
    public float velocidadePulso = 2.2f;
    public float balanco = 2.5f;
    public Color corA = new Color(1f, 0.93f, 0.25f);
    public Color corB = new Color(1f, 0.45f, 0.1f);
    public Color corC = new Color(1f, 0.25f, 0.55f);

    TextMeshProUGUI tmp;
    RectTransform rt;
    Vector2 posFinal;
    float t0;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        rt = (RectTransform)transform;
        posFinal = rt.anchoredPosition;
    }

    void OnEnable()
    {
        t0 = Time.unscaledTime;
        tmp.enableVertexGradient = true;
    }

    void Update()
    {
        float t = Time.unscaledTime - t0;

        // Entrada: cai de cima com quique
        float e = Mathf.Clamp01(t / duracaoEntrada);
        float quique = 1f - Mathf.Pow(1f - e, 3f) * Mathf.Cos(e * Mathf.PI * 2.5f);
        rt.anchoredPosition = posFinal + new Vector2(0f, (1f - quique) * 500f);

        // Pulso e balanco
        float s = 1f + Mathf.Sin(t * velocidadePulso * Mathf.PI) * pulso;
        rt.localScale = new Vector3(s, s, 1f) * Mathf.Lerp(1.6f, 1f, e);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.3f) * balanco - 4f);

        // Degrade animado
        float k = (Mathf.Sin(t * 1.7f) + 1f) * 0.5f;
        Color topo = Color.Lerp(corA, Color.white, k * 0.35f);
        Color baixo = Color.Lerp(corB, corC, k);
        tmp.colorGradient = new VertexGradient(topo, topo, baixo, baixo);
    }
}
