using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Assalto ao banco: fique na marca verde em frente ao banco e aperte R.
// Aguente alguns segundos sem sair da marca e leve o dinheiro.
public class Banco : MonoBehaviour
{
    public int premio = 5000;
    public float tempoRoubo = 5f;
    public float tempoRecarga = 120f;
    public float raio = 2.2f;
    public Light alarme;

    Transform player;
    float progresso = -1f, liberadoEm;
    bool avisouEntrada;

    void Start() { var p = GameObject.Find("Player"); if (p) player = p.transform; }

    void Update()
    {
        if (player == null || Dinheiro.Instancia == null) return;
        Vector3 d = player.position - transform.position; d.y = 0;
        var cc = player.GetComponent<CharacterController>();
        bool dentro = d.magnitude < raio && cc && cc.enabled;

        if (progresso < 0f)
        {
            if (dentro)
            {
                if (Time.time < liberadoEm)
                {
                    if (!avisouEntrada) Dinheiro.Instancia.Aviso($"Cofre vazio... volte em {Mathf.CeilToInt(liberadoEm - Time.time)}s");
                }
                else
                {
                    if (!avisouEntrada) Dinheiro.Instancia.Aviso("Aperte R para assaltar o banco", 3f);
                    if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    {
                        progresso = 0f;
                        if (Procurado.Instancia) Procurado.Instancia.Crime(2); // alarme dispara: policia vem
                    }
                }
                avisouEntrada = true;
            }
            else avisouEntrada = false;
            if (alarme) alarme.intensity = 0f;
            return;
        }

        // roubo em andamento
        if (!dentro)
        {
            progresso = -1f;
            Dinheiro.Instancia.Aviso("Assalto cancelado! Voce saiu do banco.", 2.5f);
            return;
        }
        progresso += Time.deltaTime;
        Dinheiro.Instancia.Aviso($"ASSALTANDO... {Mathf.CeilToInt(tempoRoubo - progresso)}s", 0.3f);
        if (alarme) alarme.intensity = Mathf.PingPong(Time.time * 30f, 25f);
        if (progresso >= tempoRoubo)
        {
            progresso = -1f;
            liberadoEm = Time.time + tempoRecarga;
            Dinheiro.Instancia.Adicionar(premio);
            if (Procurado.Instancia) Procurado.Instancia.Crime(1);
            Dinheiro.Instancia.Aviso($"ASSALTO CONCLUIDO! +${premio:N0}  Fuja!", 4f);
            if (alarme) alarme.intensity = 0f;
        }
    }
}
