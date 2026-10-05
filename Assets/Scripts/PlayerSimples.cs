using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Controle simples em 3a pessoa: Setas (ou WASD) anda, mouse gira a camera, Shift corre,
// Espaco pula, ESC volta ao menu.
[RequireComponent(typeof(CharacterController))]
public class PlayerSimples : MonoBehaviour
{
    public float velocidade = 5f;
    public float velocidadeCorrida = 9f;
    public float forcaPulo = 1.6f;
    public float gravidade = -20f;
    public float sensibilidadeMouse = 0.15f;
    public Transform cameraAlvo;
    public Vector3 offsetCamera = new Vector3(0f, 2.2f, -5f);
    [Tooltip("Camera volta sozinha para tras da personagem quando ela anda (estilo GTA)")]
    public bool cameraSegue = true;
    public float velocidadeSeguir = 2.5f;
    public float esperaAposMouse = 0.8f;
    float ultimoMouse = -10f;

    Animator anim;
    CharacterController cc;
    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int GroundedId = Animator.StringToHash("Grounded");
    static readonly int JumpId = Animator.StringToHash("Jump");
    static readonly int SocoId = Animator.StringToHash("Soco");
    float proximoSoco;
    float yaw, pitch = 15f, velY;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (!GetComponent<Natacao>()) gameObject.AddComponent<Natacao>(); // mar funcional (nadar)
        anim = GetComponentInChildren<Animator>();
        if (cameraAlvo == null && Camera.main != null) cameraAlvo = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        GarantirControles();

        if (kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MenuPrincipal");
            return;
        }

        // soco: botao esquerdo do mouse
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Time.time > proximoSoco && cc.isGrounded)
        {
            proximoSoco = Time.time + Acessorios.EsperaSoco;
            if (anim != null) anim.SetTrigger(SocoId);
            Invoke(nameof(AcertarSoco), 0.3f);
        }

        if (mouse != null)
        {
            Vector2 d = mouse.delta.ReadValue() * sensibilidadeMouse;
            if (d.sqrMagnitude > 0.0001f) ultimoMouse = Time.time;
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, -20f, 60f);
        }

        Vector2 mov = Vector2.zero;
        if ((kb.upArrowKey.isPressed || kb.wKey.isPressed)) mov.y += 1;
        if ((kb.downArrowKey.isPressed || kb.sKey.isPressed)) mov.y -= 1;
        if ((kb.rightArrowKey.isPressed || kb.dKey.isPressed)) mov.x += 1;
        if ((kb.leftArrowKey.isPressed || kb.aKey.isPressed)) mov.x -= 1;
        mov = Vector2.ClampMagnitude(mov, 1f);

        Quaternion rotCam = Quaternion.Euler(0, yaw, 0);

        // Estilo GTA: andando para frente/lados (nao de costas), a camera
        // volta devagar para tras dela se o jogador nao mexer o mouse
        if (cameraSegue && mov.y >= 0f && mov.sqrMagnitude > 0.01f && Time.time - ultimoMouse > esperaAposMouse)
        {
            yaw = Mathf.LerpAngle(yaw, transform.eulerAngles.y, velocidadeSeguir * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, 12f, velocidadeSeguir * 0.5f * Time.deltaTime);
        }
        Vector3 dir = rotCam * new Vector3(mov.x, 0, mov.y);
        float vel = kb.leftShiftKey.isPressed ? velocidadeCorrida : velocidade;
        var agua = Natacao.Instancia;
        bool nadando = agua && agua.Nadando;
        if (agua) vel *= agua.Mult; // agua deixa mais lenta

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);

        if (nadando)
        {
            // flutua com a cabeca para fora d'agua
            velY = Mathf.Clamp((agua.AlturaNado - transform.position.y) * 4f, -3f, 3f);
        }
        else if (cc.isGrounded)
        {
            velY = -2f;
            if (kb.spaceKey.wasPressedThisFrame)
            {
                velY = Mathf.Sqrt(forcaPulo * -2f * gravidade);
                if (anim != null) anim.SetTrigger(JumpId);
            }
        }
        if (!nadando) velY += gravidade * Time.deltaTime;

        cc.Move((dir * vel + Vector3.up * velY) * Time.deltaTime);

        if (anim != null)
        {
            float alvo = dir.sqrMagnitude > 0.01f ? vel * mov.magnitude : 0f;
            anim.SetFloat(SpeedId, alvo, 0.1f, Time.deltaTime);
            anim.SetBool(GroundedId, cc.isGrounded || nadando);
        }
    }

    // A pe e sem menu aberto: entrar em carro (E), assaltar (R) e acessorios precisam estar ligados.
    // (corrige o bug em que, depois de ser presa, a Arissa nao conseguia mais entrar nos carros)
    void GarantirControles()
    {
        if (Time.frameCount % 15 != 0) return;
        if (LojaUI.Instancia && LojaUI.Instancia.Aberta) return;
        if (MapaGRS.Instancia && MapaGRS.Instancia.Aberto) return;
        foreach (var m in GetComponents<MonoBehaviour>())
            if (!m.enabled && (m is EntrarCarro || m is AssaltoPedestre || m is Acessorios))
            {
                m.enabled = true;
                Debug.LogWarning("GRS 1: " + m.GetType().Name + " estava desligado - religado.");
            }
    }

    void AcertarSoco()
    {
        float alc = Acessorios.AlcanceSoco; // taco de beisebol alcanca mais longe
        Vector3 centro = transform.position + Vector3.up * 1.1f + transform.forward * 0.9f * alc;
        foreach (var c in Physics.OverlapSphere(centro, 0.8f * alc, ~0, QueryTriggerInteraction.Ignore))
        {
            var ped = c.GetComponentInParent<Pedestre>();
            if (ped && !ped.morto) { ped.Morrer(transform.forward * 4f); break; }
        }
    }

    void LateUpdate()
    {
        if (cameraAlvo == null) return;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 foco = transform.position + Vector3.up * 1.5f;
        Vector3 desejada = foco + rot * new Vector3(0, offsetCamera.y - 1.5f, offsetCamera.z);
        // Evita a camera atravessar paredes
        Vector3 dir = desejada - foco;
        if (Physics.SphereCast(foco, 0.25f, dir.normalized, out RaycastHit hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(transform))
            desejada = foco + dir.normalized * Mathf.Max(0.5f, hit.distance - 0.1f);
        // camera nao entra embaixo d'agua
        if (Natacao.Instancia && Natacao.Instancia.NaAgua) desejada.y = Mathf.Max(desejada.y, Natacao.Instancia.nivelAgua + 0.4f);
        cameraAlvo.position = desejada;
        cameraAlvo.LookAt(foco);
    }
}
