using UnityEngine;
using UnityEngine.InputSystem;

// Fica no Player: aperte E perto de um carro dirigivel para entrar/sair.
public class EntrarCarro : MonoBehaviour
{
    public float distancia = 6f;
    CarroSimples carroAtual;
    PlayerSimples andar;
    CharacterController cc;
    GameObject modelo;
    int frameTroca = -1; // evita entrar e sair no mesmo aperto de E

    void Awake()
    {
        andar = GetComponent<PlayerSimples>();
        cc = GetComponent<CharacterController>();
        var anim = GetComponentInChildren<Animator>();
        if (anim) modelo = anim.gameObject;
    }

    void Update()
    {
        if (carroAtual != null || Time.frameCount == frameTroca) return;
        var kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame || Loja.PlayerNaPorta) return; // na porta da loja, E abre a loja

        CarroSimples maisPerto = null; float melhor = distancia;
        foreach (var c in FindObjectsByType<CarroSimples>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < melhor) { melhor = d; maisPerto = c; }
        }
        if (maisPerto == null) return;

        carroAtual = maisPerto;
        frameTroca = Time.frameCount;
        andar.enabled = false;
        cc.enabled = false;
        if (modelo) modelo.SetActive(false);
        transform.SetParent(carroAtual.transform);
        transform.localPosition = Vector3.zero;
        carroAtual.Entrar(this);
    }

    public void SairDoCarro()
    {
        if (carroAtual == null || Time.frameCount == frameTroca) return;
        frameTroca = Time.frameCount;
        var carro = carroAtual;
        carro.Sair();
        carroAtual = null;
        transform.SetParent(null);
        // sai pela porta do motorista (lado esquerdo)
        // sai pela esquerda; se tiver parede, tenta direita, depois atras
        Vector3 c0 = carro.transform.position + Vector3.up * 0.2f;
        Vector3[] opcoes = { c0 - carro.transform.right * 2.2f, c0 + carro.transform.right * 2.2f, c0 - carro.transform.forward * 3.5f };
        Vector3 saida = opcoes[0];
        foreach (var o in opcoes)
            if (!Physics.CheckCapsule(o + Vector3.up * 0.4f, o + Vector3.up * 1.5f, 0.3f, ~0, QueryTriggerInteraction.Ignore)) { saida = o; break; }
        transform.position = saida;
        transform.rotation = Quaternion.Euler(0, carro.transform.eulerAngles.y, 0);
        if (modelo) modelo.SetActive(true);
        cc.enabled = true;
        andar.enabled = true;
    }
}
