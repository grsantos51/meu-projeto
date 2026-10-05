using UnityEngine;

// Carro de transito: anda sozinho pelas ruas da cidade (mao direita),
// escolhe caminho nos cruzamentos e para se tiver algo na frente.
// Se a Arissa apertar E perto dele, ela "rouba" o carro (o CarroSimples assume).
public class TrafegoCarro : MonoBehaviour
{
    public float velocidadeMax = 9f;
    public float faixa = 1.0f;          // distancia do centro da rua (mao direita)
    public float alturaRua = 0.1f;

    // grade de ruas (preenchida pelo editor)
    public float origem = -60f;
    public float passo = 40f;
    public int nos = 4;

    [SerializeField] int ni, nj;          // proximo cruzamento
    [SerializeField] Vector2Int dir;      // direcao atual (x,z)
    Vector3 a, c, b;           // curva atual (bezier): entrada, controle, saida
    float t, compCurva;
    bool naCurva;
    float vel, paradoDesde = -1f;
    Rigidbody rb;
    Transform ruas;

    // Cruzamento "reservado": so 1 carro por vez faz a curva (evita todo mundo travar no meio)
    static readonly System.Collections.Generic.Dictionary<Vector2Int, TrafegoCarro> reservas = new System.Collections.Generic.Dictionary<Vector2Int, TrafegoCarro>();
    Vector2Int noReservado = new Vector2Int(-99, -99);

    bool Reservar(int i, int j)
    {
        var k = new Vector2Int(i, j);
        if (reservas.TryGetValue(k, out var dono) && dono != null && dono != this && dono.enabled && dono.naCurva && dono.noReservado == k) return false;
        reservas[k] = this; noReservado = k; return true;
    }

    void Liberar()
    {
        if (reservas.TryGetValue(noReservado, out var dono) && dono == this) reservas.Remove(noReservado);
        noReservado = new Vector2Int(-99, -99);
    }

    void OnDisable() { Liberar(); }

    public void Iniciar(int i, int j, Vector2Int d)
    {
        ni = i; nj = j; dir = d; naCurva = false;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>(); vel = velocidadeMax * 0.5f;
        var r = GameObject.Find("Ruas"); if (r) ruas = r.transform;
    }

    Vector3 No(int i, int j) => new Vector3(origem + i * passo, alturaRua, origem + j * passo);
    static Vector3 V(Vector2Int d) => new Vector3(d.x, 0, d.y);
    static Vector3 Direita(Vector2Int d) => new Vector3(d.y, 0, -d.x);
    bool Valido(int i, int j) => i >= 0 && j >= 0 && i < nos && j < nos;

    void FixedUpdate()
    {
        if (rb == null || !rb.isKinematic) return; // alguem esta dirigindo

        // obstaculo na frente?
        float alvoVel = velocidadeMax;
        if (Physics.BoxCast(transform.position + Vector3.up * 0.8f, new Vector3(0.9f, 0.45f, 0.1f), transform.forward,
                out RaycastHit hit, transform.rotation, 7f, ~0, QueryTriggerInteraction.Ignore) &&
            !hit.transform.IsChildOf(transform) && hit.collider.name != "ChaoExterno" && !(ruas && hit.transform.IsChildOf(ruas)))
        {
            // dentro do cruzamento, nao para por causa de outro carro do transito esperando (senao trava tudo)
            var outro = hit.collider.GetComponentInParent<TrafegoCarro>();
            bool ignorar = naCurva && outro != null && outro.enabled && !outro.naCurva;
            if (!ignorar) alvoVel = hit.distance < 3.5f ? 0f : velocidadeMax * 0.35f;
        }
        if (naCurva) alvoVel = Mathf.Min(alvoVel, 5f);

        // engarrafado (ex.: carro abandonado na faixa)? Quando ninguem estiver vendo,
        // o carro "some" e reaparece em outra rua longe da Arissa (como no GTA).
        if (alvoVel == 0f && vel < 0.1f)
        {
            if (paradoDesde < 0) paradoDesde = Time.time;
            float preso = Time.time - paradoDesde;
            if ((preso > 5f && !Visivel()) || preso > 25f) { Reposicionar(); return; }
        }
        else paradoDesde = -1f;

        vel = Mathf.MoveTowards(vel, alvoVel, (alvoVel < vel ? 14f : 4f) * Time.fixedDeltaTime);
        float passoDist = vel * Time.fixedDeltaTime;

        Vector3 pos = rb.position;
        Vector3 frente;
        if (!naCurva)
        {
            Vector3 no = No(ni, nj);
            Vector3 entrada = no - V(dir) * 7f + Direita(dir) * faixa;
            Vector3 para = entrada - pos; para.y = 0;
            if (para.magnitude <= passoDist + 0.05f)
            {
                pos = entrada;
                if (Reservar(ni, nj)) EscolherCurva();
                else vel = 0f; // espera o cruzamento liberar
                frente = V(dir);
            }
            else { frente = para.normalized; pos += frente * passoDist; }
        }
        else
        {
            t += passoDist / Mathf.Max(0.1f, compCurva);
            if (t >= 1f)
            {
                pos = b; naCurva = false;
                Liberar();
                frente = V(dir);
            }
            else
            {
                pos = Bezier(t);
                frente = (Bezier(Mathf.Min(1f, t + 0.02f)) - pos).normalized;
            }
        }
        pos.y = alturaRua;
        rb.MovePosition(pos);
        if (frente.sqrMagnitude > 0.001f) rb.MoveRotation(Quaternion.LookRotation(new Vector3(frente.x, 0, frente.z)));
    }

    void EscolherCurva()
    {
        Vector3 no = No(ni, nj);
        var op = new System.Collections.Generic.List<Vector2Int>();
        var direita = new Vector2Int(dir.y, -dir.x);
        var esquerda = new Vector2Int(-dir.y, dir.x);
        foreach (var d in new[] { dir, dir, direita, esquerda }) // reto tem mais chance
            if (Valido(ni + d.x, nj + d.y)) op.Add(d);
        var nova = op.Count > 0 ? op[Random.Range(0, op.Count)] : -dir; // beco: volta

        a = no - V(dir) * 7f + Direita(dir) * faixa;
        b = no + V(nova) * 7f + Direita(nova) * faixa;
        c = nova == dir ? no + Direita(dir) * faixa
          : nova == -dir ? no + Direita(dir) * faixa * -3f
          : no + Direita(dir) * faixa + Direita(nova) * faixa;
        a.y = b.y = c.y = alturaRua;
        compCurva = (Vector3.Distance(a, c) + Vector3.Distance(c, b)) * 0.9f;
        t = 0f; naCurva = true;
        dir = nova; ni += nova.x; nj += nova.y;
    }

    bool Visivel()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) if (r.isVisible) return true;
        return false;
    }

    void Reposicionar()
    {
        var player = GameObject.Find("Player");
        Vector3 pp = player ? player.transform.position : Vector3.zero;
        var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        for (int tent = 0; tent < 30; tent++)
        {
            int i = Random.Range(0, nos), j = Random.Range(0, nos);
            var d = dirs[Random.Range(0, 4)];
            if (!Valido(i + d.x, j + d.y)) continue;
            Vector3 p = No(i, j) + V(d) * Random.Range(9f, passo - 9f) + Direita(d) * faixa;
            if (Vector3.Distance(p, pp) < 45f) continue;
            if (Physics.CheckBox(p + Vector3.up * 0.9f, new Vector3(1.2f, 0.6f, 3f), Quaternion.LookRotation(V(d)), ~0, QueryTriggerInteraction.Ignore)) continue;
            rb.position = p; transform.position = p;
            transform.rotation = Quaternion.LookRotation(V(d)); rb.rotation = transform.rotation;
            Liberar();
            ni = i + d.x; nj = j + d.y; dir = d; naCurva = false; vel = 0f; paradoDesde = -1f;
            return;
        }
        paradoDesde = Time.time; // tenta de novo daqui a pouco
    }

    Vector3 Bezier(float u) => (1 - u) * (1 - u) * a + 2 * (1 - u) * u * c + u * u * b;
}
