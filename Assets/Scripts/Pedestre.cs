using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Pedestre que anda em volta do quarteirao pela calcada.
public class Pedestre : MonoBehaviour
{
    public Vector3[] pontos;
    public int alvo;
    public float velocidade = 1.3f;
    public float chanceParar = 0.2f;
    public bool policial;
    public int valor = 30;

    [HideInInspector] public bool morto;
    [HideInInspector] public bool perseguindo, fugindo;
    public static readonly List<Pedestre> Todos = new List<Pedestre>();
    NavMeshAgent agente;
    float fugaAte;

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }
    Vector3 inicioPos; Quaternion inicioRot; int inicioAlvo;

    Animator anim;
    Transform player;
    float paradoAte;
    static readonly int SpeedId = Animator.StringToHash("Speed");

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        var p = GameObject.Find("Player");
        if (p) player = p.transform;
        if (anim) anim.SetFloat("Ciclo", Random.value); // desencontra os passos
        inicioPos = transform.position; inicioRot = transform.rotation; inicioAlvo = alvo;
        if (policial) VestirPolicia();
    }

    // Atropelado ou socado: cai, solta dinheiro e volta depois de um tempo
    public void Morrer(Vector3 empurrao)
    {
        if (morto) return;
        morto = true;
        PararAgente();
        perseguindo = fugindo = false;
        if (Procurado.Instancia) Procurado.Instancia.Crime(policial ? 2 : 1);
        if (anim) anim.enabled = false;
        var col = GetComponent<Collider>(); if (col) col.isTrigger = true;
        Moeda.Criar(transform.position, valor, policial ? 5 : Random.Range(1, 4));
        StartCoroutine(Cair(empurrao));
    }

    System.Collections.IEnumerator Cair(Vector3 empurrao)
    {
        empurrao.y = 0;
        if (empurrao.sqrMagnitude < 0.01f) empurrao = -transform.forward;
        Vector3 dir = empurrao.normalized;
        Vector3 eixo = Vector3.Cross(Vector3.up, dir);
        Quaternion de = transform.rotation, para = Quaternion.AngleAxis(88f, eixo) * de;
        Vector3 p0 = transform.position, p1 = p0 + dir * Mathf.Clamp(empurrao.magnitude * 0.25f, 0.5f, 4f);
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.45f)
        {
            transform.rotation = Quaternion.Slerp(de, para, t * t);
            transform.position = Vector3.Lerp(p0, p1, Mathf.Sqrt(t)) + Vector3.up * 0.25f * t;
            yield return null;
        }
        transform.rotation = para;
        yield return new WaitForSeconds(25f);
        // volta a andar do inicio do caminho
        transform.SetPositionAndRotation(inicioPos, inicioRot);
        alvo = inicioAlvo;
        var col = GetComponent<Collider>(); if (col) col.isTrigger = false;
        if (anim) anim.enabled = true;
        morto = false;
    }

    // ---- perseguicao (policial) e fuga (vitima de assalto) ----
    public void Perseguir(bool sim)
    {
        if (morto) return;
        if (sim && !perseguindo) { perseguindo = true; LigarAgente(4.2f); }
        else if (!sim && perseguindo) { perseguindo = false; Voltar(); }
    }

    public void Fugir(Vector3 deOnde)
    {
        if (morto) return;
        fugindo = true; fugaAte = Time.time + 8f;
        LigarAgente(4.5f);
        Vector3 longe = transform.position + (transform.position - deOnde).normalized * 25f;
        if (agente && agente.isOnNavMesh && NavMesh.SamplePosition(longe, out NavMeshHit h, 10f, NavMesh.AllAreas)) agente.SetDestination(h.position);
    }

    void LigarAgente(float vel)
    {
        if (agente == null)
        {
            agente = gameObject.AddComponent<NavMeshAgent>();
            agente.radius = 0.3f; agente.height = 1.8f; agente.angularSpeed = 540f; agente.acceleration = 14f; agente.stoppingDistance = 0.9f;
        }
        agente.speed = vel;
        agente.enabled = true;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit h, 3f, NavMesh.AllAreas)) agente.Warp(h.position);
    }

    void PararAgente() { if (agente) agente.enabled = false; }

    void AtualizarAgente()
    {
        if (agente == null || !agente.enabled || !agente.isOnNavMesh) return;
        if (perseguindo && Procurado.Instancia && Time.frameCount % 15 == 0)
            agente.SetDestination(Procurado.Instancia.PosicaoAlvo());
        if (fugindo && Time.time > fugaAte) { fugindo = false; Voltar(); return; }
        if (anim) anim.SetFloat(SpeedId, agente.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    void Voltar()
    {
        PararAgente();
        transform.SetPositionAndRotation(inicioPos, inicioRot);
        alvo = inicioAlvo;
    }

    void VestirPolicia()
    {
        foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>())
            foreach (var m in r.materials)
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.GetColor("_BaseColor") * new Color(0.35f, 0.45f, 1f));
                else if (m.HasProperty("_Color")) m.color *= new Color(0.35f, 0.45f, 1f);
            }
        var cabeca = anim ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
        if (cabeca)
        {
            var bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(bone.GetComponent<Collider>());
            bone.name = "Quepe";
            bone.transform.SetParent(cabeca, false);
            bone.transform.localPosition = new Vector3(0, 0.17f, 0.01f);
            bone.transform.localScale = new Vector3(0.24f, 0.045f, 0.26f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.05f, 0.08f, 0.25f));
            bone.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    void Update()
    {
        if (morto) return;
        if (perseguindo || fugindo) { AtualizarAgente(); return; }
        if (pontos == null || pontos.Length < 2) return;
        float vel = velocidade;

        if (Time.time < paradoAte) vel = 0f;

        // para se a Arissa estiver bem na frente
        if (player && vel > 0f)
        {
            Vector3 d = player.position - transform.position; d.y = 0;
            if (d.magnitude < 1.4f && Vector3.Dot(transform.forward, d.normalized) > 0.5f) vel = 0f;
        }

        Vector3 destino = pontos[alvo];
        Vector3 para = destino - transform.position; para.y = 0;
        if (para.magnitude < 0.3f)
        {
            alvo = (alvo + 1) % pontos.Length;
            if (Random.value < chanceParar) paradoAte = Time.time + Random.Range(1.5f, 4f);
        }
        else if (vel > 0f)
        {
            Vector3 dir = para.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
            transform.position += transform.forward * vel * Time.deltaTime;
        }

        // gruda no chao (calcada/rua)
        if (Time.frameCount % 4 == 0 && Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform) && Mathf.Abs(hit.point.y - transform.position.y) < 0.6f) transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);

        if (anim) anim.SetFloat(SpeedId, vel, 0.15f, Time.deltaTime);
    }
}
