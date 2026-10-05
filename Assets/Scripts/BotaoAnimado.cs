using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Botao cresce e muda de cor ao passar o mouse.
public class BotaoAnimado : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float escalaHover = 1.12f;
    public Color corNormal = Color.white;
    public Color corHover = new Color(1f, 0.85f, 0.2f);

    TextMeshProUGUI texto;
    float alvo = 1f, atual = 1f;
    bool hover;

    void Awake() { texto = GetComponentInChildren<TextMeshProUGUI>(); }

    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; }
    public void OnSelect(BaseEventData e) { hover = true; }
    public void OnDeselect(BaseEventData e) { hover = false; }

    void Update()
    {
        alvo = hover ? escalaHover : 1f;
        atual = Mathf.Lerp(atual, alvo, 12f * Time.unscaledDeltaTime);
        transform.localScale = Vector3.one * atual;
        if (texto != null) texto.color = Color.Lerp(texto.color, hover ? corHover : corNormal, 12f * Time.unscaledDeltaTime);
    }
}
