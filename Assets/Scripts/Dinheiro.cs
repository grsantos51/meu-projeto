using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Carteira do jogador + HUD de dinheiro no canto da tela.
public class Dinheiro : MonoBehaviour
{
    public static Dinheiro Instancia;
    public TMP_FontAsset fonte;
    public Material materialFonte;
    public int valor;

    TextMeshProUGUI texto, popup, aviso;
    float popupAte, avisoAte;
    float mostrado;

    void Awake()
    {
        Instancia = this;
        valor = PlayerPrefs.GetInt("GRS1_Dinheiro", 0);
        mostrado = valor;
        CriarHUD();
    }

    public void Adicionar(int quanto)
    {
        valor = Mathf.Max(0, valor + quanto);
        PlayerPrefs.SetInt("GRS1_Dinheiro", valor);
        popup.text = (quanto >= 0 ? "+$" : "-$") + Mathf.Abs(quanto).ToString("N0");
        popup.color = quanto >= 0 ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.3f, 0.3f);
        popupAte = Time.time + 1.6f;
    }

    public void Aviso(string msg, float segundos = 2.5f)
    {
        aviso.text = msg;
        avisoAte = Time.time + segundos;
    }

    void Update()
    {
        mostrado = Mathf.MoveTowards(mostrado, valor, Mathf.Max(50f, Mathf.Abs(valor - mostrado) * 3f) * Time.deltaTime);
        texto.text = "$ " + Mathf.RoundToInt(mostrado).ToString("N0");
        float a = Mathf.Clamp01((popupAte - Time.time) / 0.5f);
        popup.alpha = a;
        popup.rectTransform.anchoredPosition = new Vector2(-40, -110 + (1 - a) * 20);
        aviso.alpha = Mathf.Clamp01((avisoAte - Time.time) / 0.4f);
    }

    void CriarHUD()
    {
        var canvas = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var sc = canvas.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        canvas.transform.SetParent(transform, false);

        texto = Texto(canvas.transform, new Vector2(1, 1), new Vector2(-40, -30), 72, new Color(0.45f, 1f, 0.45f), TextAlignmentOptions.TopRight);
        popup = Texto(canvas.transform, new Vector2(1, 1), new Vector2(-40, -110), 52, new Color(1f, 0.9f, 0.3f), TextAlignmentOptions.TopRight);
        popup.alpha = 0;
        aviso = Texto(canvas.transform, new Vector2(0.5f, 0), new Vector2(0, 140), 54, Color.white, TextAlignmentOptions.Bottom);
        aviso.alpha = 0;
    }

    TextMeshProUGUI Texto(Transform pai, Vector2 ancora, Vector2 pos, float tam, Color cor, TextAlignmentOptions al)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(pai, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(1200, 120);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (fonte) t.font = fonte;
        if (materialFonte) t.fontSharedMaterial = materialFonte;
        t.fontSize = tam; t.color = cor; t.alignment = al;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }
}
