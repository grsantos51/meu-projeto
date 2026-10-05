using UnityEngine;
using UnityEngine.AI;

// Viatura: persegue a Arissa pelo NavMesh com sirene piscando.
public class PoliciaCarro : MonoBehaviour
{
    public float velocidade = 15f;
    NavMeshAgent agente;
    Light vermelha, azul;
    bool indoEmbora;
    float proximaRota;

    void Start()
    {
        agente = gameObject.AddComponent<NavMeshAgent>();
        agente.speed = velocidade; agente.acceleration = 25f; agente.angularSpeed = 220f;
        agente.radius = 1.1f; agente.height = 1.6f; agente.stoppingDistance = 3f;
        agente.autoBraking = true;
        vermelha = Sirene(new Vector3(-0.35f, 1.9f, 0), Color.red);
        azul = Sirene(new Vector3(0.35f, 1.9f, 0), new Color(0.2f, 0.4f, 1f));
    }

    Light Sirene(Vector3 pos, Color cor)
    {
        var l = new GameObject("Sirene", typeof(Light)).GetComponent<Light>();
        l.transform.SetParent(transform, false); l.transform.localPosition = pos;
        l.type = LightType.Point; l.color = cor; l.range = 14f; l.shadows = LightShadows.None;
        return l;
    }

    public void IrEmbora()
    {
        indoEmbora = true;
        Destroy(gameObject, 6f);
    }

    void Update()
    {
        bool fase = Mathf.Repeat(Time.time * 4f, 1f) > 0.5f;
        if (vermelha) vermelha.intensity = fase ? 30f : 0f;
        if (azul) azul.intensity = fase ? 0f : 30f;
        if (agente == null || !agente.isOnNavMesh || Time.time < proximaRota) return;
        proximaRota = Time.time + 0.4f;
        var alvo = Procurado.Instancia ? Procurado.Instancia.PosicaoAlvo() : transform.position;
        if (indoEmbora) alvo = transform.position + (transform.position - alvo).normalized * 60f;
        if (NavMesh.SamplePosition(alvo, out NavMeshHit hit, 6f, NavMesh.AllAreas)) agente.SetDestination(hit.position);
    }
}
