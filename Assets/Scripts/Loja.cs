using UnityEngine;
using UnityEngine.InputSystem;

// Porta da loja na cidade: chegue na marca roxa e aperte E para entrar.
public class Loja : MonoBehaviour
{
    public static bool PlayerNaPorta;
    public float raio = 2.4f;
    public GameObject carroVip;          // carro exclusivo (aparece depois de comprado)
    public Material materialCinzaCarro;  // textura cinza do carro ARCADE, usada nas pinturas

    Transform player;
    bool avisou;

    void Awake() { if (materialCinzaCarro) PinturaCarro.materialCinza = materialCinzaCarro; }

    void Start()
    {
        var p = GameObject.Find("Player"); if (p) player = p.transform;
        Inventario.Mudou += AtualizarCarro;
        AtualizarCarro();
    }

    void OnDestroy() { Inventario.Mudou -= AtualizarCarro; PlayerNaPorta = false; }

    void AtualizarCarro()
    {
        if (carroVip && !carroVip.activeSelf && Inventario.Tem("carro_vip")) carroVip.SetActive(true);
    }

    void Update()
    {
        if (player == null || LojaUI.Instancia == null) return;
        if (LojaUI.Instancia.Aberta) { PlayerNaPorta = true; return; }
        Vector3 d = player.position - transform.position; d.y = 0;
        var cc = player.GetComponent<CharacterController>();
        PlayerNaPorta = d.magnitude < raio && cc && cc.enabled;
        if (!PlayerNaPorta) { avisou = false; return; }

        bool procurada = Procurado.Instancia && Procurado.Instancia.estrelas > 0;
        if (!avisou && Dinheiro.Instancia)
            Dinheiro.Instancia.Aviso(procurada ? "A loja nao atende procurados! Despiste a policia." : "Aperte E para entrar na LOJA", 3f);
        avisou = true;

        if (!procurada && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            LojaUI.Instancia.Abrir();
    }
}
