using UnityEngine;

// Coloque no carro. Ele segue o CaminhoCarros sozinho, sem física (kinematic).
public class CarroAutonomo : MonoBehaviour
{
    [Header("Caminho")]
    public CaminhoCarros caminho;
    public int pontoInicial = 0;
    public bool posicionarNoInicio = true;   // teleporta o carro para o waypoint inicial
    public float distanciaChegada = 2.5f;

    [Header("Movimento")]
    public float velocidadeMax = 10f;
    public float aceleracao = 4f;
    public float frenagem = 12f;
    public float velocidadeGiro = 90f;        // graus por segundo
    [Range(0.1f, 1f)] public float reducaoNaCurva = 0.4f;

    [Header("Sensor (freia se tiver algo na frente)")]
    public float distanciaSeguranca = 8f;
    public float raioSensor = 0.6f;
    public float alturaSensor = 0.6f;
    public LayerMask camadasObstaculo = ~0;

    [Header("Rodas (opcional, só visual)")]
    public Transform[] rodas;
    public float raioRoda = 0.35f;
    public Vector3 eixoRoda = Vector3.right;  // troque se a roda girar no eixo errado

    int alvo;
    float velocidade;

    void Start()
    {
        // Desliga a física do asset de corrida para não brigar com o movimento
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        foreach (var wc in GetComponentsInChildren<WheelCollider>()) wc.enabled = false;

        if (caminho == null || caminho.Quantidade < 2)
        {
            Debug.LogWarning($"{name}: CarroAutonomo precisa de um CaminhoCarros com 2+ waypoints.");
            enabled = false;
            return;
        }

        pontoInicial = Mathf.Clamp(pontoInicial, 0, caminho.Quantidade - 1);
        if (posicionarNoInicio)
        {
            transform.position = caminho.Ponto(pontoInicial);
            alvo = caminho.Proximo(pontoInicial);
            Vector3 d = caminho.Ponto(alvo) - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }
        else alvo = pontoInicial;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        Vector3 destino = caminho.Ponto(alvo);
        destino.y = transform.position.y;
        Vector3 dir = destino - transform.position;

        if (dir.magnitude < distanciaChegada)
        {
            int prox = caminho.Proximo(alvo);
            if (prox == alvo) { velocidade = 0; return; } // fim do caminho (sem loop)
            alvo = prox;
            destino = caminho.Ponto(alvo);
            destino.y = transform.position.y;
            dir = destino - transform.position;
        }

        // Virar em direção ao waypoint
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, velocidadeGiro * dt);
        }

        // Diminui na curva
        float angulo = Vector3.Angle(transform.forward, dir);
        float velAlvo = velocidadeMax * Mathf.Lerp(1f, reducaoNaCurva, angulo / 90f);

        // Freia se tiver carro/pessoa na frente
        Vector3 origem = transform.position + Vector3.up * alturaSensor;
        if (Physics.SphereCast(origem, raioSensor, transform.forward, out RaycastHit hit,
                               distanciaSeguranca, camadasObstaculo, QueryTriggerInteraction.Ignore)
            && hit.transform.root != transform.root)
        {
            float fator = Mathf.Clamp01((hit.distance - 2f) / (distanciaSeguranca - 2f));
            velAlvo = Mathf.Min(velAlvo, velocidadeMax * fator);
        }

        float taxa = velAlvo > velocidade ? aceleracao : frenagem;
        velocidade = Mathf.MoveTowards(velocidade, velAlvo, taxa * dt);
        transform.position += transform.forward * velocidade * dt;

        // Gira as rodas
        if (rodas != null && raioRoda > 0f)
        {
            float graus = velocidade / raioRoda * Mathf.Rad2Deg * dt;
            foreach (var r in rodas) if (r) r.Rotate(eixoRoda, graus, Space.Self);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 o = transform.position + Vector3.up * alturaSensor;
        Gizmos.DrawLine(o, o + transform.forward * distanciaSeguranca);
        Gizmos.DrawWireSphere(o + transform.forward * distanciaSeguranca, raioSensor);
    }
}