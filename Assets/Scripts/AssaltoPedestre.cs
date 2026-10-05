using UnityEngine;
using UnityEngine.InputSystem;

// Fica no Player: aperte R perto de um pedestre para assaltar (ele entrega o dinheiro e foge).
public class AssaltoPedestre : MonoBehaviour
{
    public float alcance = 2.2f;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.rKey.wasPressedThisFrame) return;
        var cc = GetComponent<CharacterController>();
        if (cc == null || !cc.enabled) return; // dentro de carro nao
        // na marca do banco o R e do banco
        var banco = FindAnyObjectByType<Banco>();
        if (banco && Vector3.Distance(banco.transform.position, transform.position) < banco.raio + 0.3f) return;

        Pedestre melhor = null; float d = alcance;
        foreach (var p in Pedestre.Todos)
        {
            if (p.morto || p.fugindo || p.perseguindo) continue;
            float dist = Vector3.Distance(p.transform.position, transform.position);
            if (dist < d) { d = dist; melhor = p; }
        }
        if (melhor == null) return;

        if (melhor.policial)
        {
            if (Dinheiro.Instancia) Dinheiro.Instancia.Aviso("Assaltar policial?! Ele chamou reforco!", 2.5f);
            if (Procurado.Instancia) Procurado.Instancia.Crime(2);
            melhor.Perseguir(true);
            return;
        }
        int valor = melhor.valor + Random.Range(10, 60);
        if (Dinheiro.Instancia) { Dinheiro.Instancia.Adicionar(valor); Dinheiro.Instancia.Aviso("Assalto! A vitima fugiu chamando a policia", 2.5f); }
        if (Procurado.Instancia) Procurado.Instancia.Crime(1);
        melhor.Fugir(transform.position);
    }
}
