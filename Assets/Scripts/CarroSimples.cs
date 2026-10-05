using UnityEngine;
using UnityEngine.InputSystem;

// Carro arcade simples: setas (ou W/S) acelera e da re, setas (ou A/D) vira, Espaco freia, E sai do carro.
[RequireComponent(typeof(Rigidbody))]
public class CarroSimples : MonoBehaviour
{
    public float velocidadeMax = 28f;     // m/s (~100 km/h)
    public float aceleracao = 12f;
    public float re = 8f;
    public float giro = 80f;              // graus por segundo
    public float aderencia = 6f;          // evita derrapar de lado
    public Vector3 offsetCamera = new Vector3(0f, 3.2f, -8f);

    [HideInInspector] public bool dirigindo;
    [HideInInspector] public EntrarCarro motorista;

    Rigidbody rb;
    Transform ruas, chaoExterno, praia;
    float vy;

    float paradoDesde = -1f;
    Transform cam;
    float yawCam;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.4f, 0);
        rb.interpolation = RigidbodyInterpolation.Interpolate; // movimento liso para a camera
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // sem atrito: quem controla a velocidade e o script (senao o carro "gruda" no asfalto)
        var liso = new PhysicsMaterial("CarroLiso") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
        foreach (var c in GetComponents<Collider>()) c.sharedMaterial = liso;

        // "Suspensao": a caixa de colisao fica 30 cm acima do chao (passa por meio-fio)
        // e o carro e mantido na altura do asfalto por raycast
        var box = GetComponent<BoxCollider>();
        if (box) { box.center += Vector3.up * 0.15f; box.size -= Vector3.up * 0.3f; }
        rb.useGravity = false;
        var r = GameObject.Find("Ruas"); if (r) ruas = r.transform;
        var c0 = GameObject.Find("ChaoExterno"); if (c0) chaoExterno = c0.transform;
        var pr = GameObject.Find("Praia"); if (pr) praia = pr.transform;

        // Carro parado fica "estacionado" (kinematic) ate alguem entrar: economiza fisica
        rb.isKinematic = true;
    }

    public void Entrar(EntrarCarro quem)
    {
        motorista = quem;
        dirigindo = true;
        var trafego = GetComponent<TrafegoCarro>();
        if (trafego) trafego.enabled = false; // carro do transito "roubado"
        var viatura = GetComponent<PoliciaCarro>();
        if (viatura) viatura.enabled = false;  // viatura roubada!
        var ag = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (ag) ag.enabled = false;
        rb.isKinematic = false;
        paradoDesde = -1f;
        if (quem != null && quem.name == "Player") PinturaCarro.Aplicar(this); // pintura/neon da loja
        cam = Camera.main != null ? Camera.main.transform : null;
        yawCam = transform.eulerAngles.y;
    }

    public void Sair()
    {
        dirigindo = false;
        motorista = null;
    }

    void Update()
    {
        if (!dirigindo) return;
        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame && motorista != null) motorista.SairDoCarro();
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;
        // depois que o motorista sai e o carro para, volta a ficar estacionado
        if (!dirigindo)
        {
            if (rb.linearVelocity.magnitude < 0.2f)
            {
                if (paradoDesde < 0f) paradoDesde = Time.time;
                else if (Time.time - paradoDesde > 1f) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; return; }
            }
            else paradoDesde = -1f;
        }
        var kb = Keyboard.current;
        float acel = 0f, vira = 0f;
        bool freio = false;
        if (dirigindo && kb != null)
        {
            if ((kb.upArrowKey.isPressed || kb.wKey.isPressed)) acel += 1f;
            if ((kb.downArrowKey.isPressed || kb.sKey.isPressed)) acel -= 1f;
            if ((kb.rightArrowKey.isPressed || kb.dKey.isPressed)) vira += 1f;
            if ((kb.leftArrowKey.isPressed || kb.aKey.isPressed)) vira -= 1f;
            freio = kb.spaceKey.isPressed;
        }

        Vector3 v = rb.linearVelocity;
        float frente = Vector3.Dot(v, transform.forward);
        float lado = Vector3.Dot(v, transform.right);

        // aceleracao / re
        if (acel > 0) frente = Mathf.MoveTowards(frente, velocidadeMax, aceleracao * Time.fixedDeltaTime);
        else if (acel < 0) frente = Mathf.MoveTowards(frente, -velocidadeMax * 0.4f, (frente > 0 ? aceleracao * 1.5f : re) * Time.fixedDeltaTime);
        else frente = Mathf.MoveTowards(frente, 0f, 4f * Time.fixedDeltaTime);
        if (freio) frente = Mathf.MoveTowards(frente, 0f, 25f * Time.fixedDeltaTime);

        lado = Mathf.Lerp(lado, 0f, aderencia * Time.fixedDeltaTime);
        Vector3 nova = transform.forward * frente + transform.right * lado;
        nova.y = AlturaSuspensao();
        rb.linearVelocity = nova;

        // Batidas em meio-fio/poste nao podem deixar o carro rodando sozinho:
        // quem gira o carro e so o volante (A/D)
        rb.angularVelocity = Vector3.zero;

        // direcao: so vira andando, e vira menos em alta velocidade
        float abs = Mathf.Abs(frente);
        float fator = Mathf.Clamp01(abs / 5f) * Mathf.Lerp(1f, 0.45f, abs / velocidadeMax) * Mathf.Sign(frente);
        if (Mathf.Abs(vira) > 0.01f && abs > 0.2f)
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0, vira * giro * fator * Time.fixedDeltaTime, 0));
    }

    // atropelou alguem?
    void OnCollisionEnter(Collision c)
    {
        if (!dirigindo) return;
        var ped = c.collider.GetComponentInParent<Pedestre>();
        if (ped && !ped.morto && c.relativeVelocity.magnitude > 3f) ped.Morrer(rb.linearVelocity);
    }

    float AlturaSuspensao()
    {
        float chao = float.NegativeInfinity;
        foreach (var h in Physics.RaycastAll(transform.position + Vector3.up * 1.2f, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            var t = h.collider.transform;
            bool valido = (ruas && t.IsChildOf(ruas)) || (chaoExterno && t == chaoExterno) || (praia && t.IsChildOf(praia) && t.name.StartsWith("Areia"));
            if (valido && h.point.y > chao) chao = h.point.y;
        }
        if (float.IsNegativeInfinity(chao)) { vy -= 9.8f * Time.fixedDeltaTime; return vy; }
        vy = 0f;
        return Mathf.Clamp((chao - transform.position.y) / Time.fixedDeltaTime, -6f, 6f);
    }

    float distCam = -1f;

    void LateUpdate()
    {
        if (!dirigindo || cam == null) return;
        // so o giro e suavizado; a distancia fica fixa (sem "zoom" ao acelerar)
        yawCam = Mathf.LerpAngle(yawCam, transform.eulerAngles.y, 4f * Time.deltaTime);
        Quaternion r = Quaternion.Euler(10f, yawCam, 0);
        Vector3 foco = transform.position + Vector3.up * 1.2f;
        Vector3 offset = r * new Vector3(0, offsetCamera.y - 1.2f, offsetCamera.z);
        float distMax = offset.magnitude;
        Vector3 dir = offset / distMax;

        // obstaculo fixo (parede, coqueiro, poste) entre o carro e a camera: aproxima na hora
        float alvo = distMax;
        foreach (var h in Physics.SphereCastAll(foco, 0.3f, dir, distMax, ~0, QueryTriggerInteraction.Ignore))
            if (h.distance > 0f && h.collider.attachedRigidbody == null && !h.transform.IsChildOf(transform))
                alvo = Mathf.Min(alvo, Mathf.Max(1.5f, h.distance - 0.2f));
        if (distCam < 0f) distCam = distMax;
        distCam = alvo < distCam ? alvo : Mathf.MoveTowards(distCam, alvo, 6f * Time.deltaTime); // volta devagar

        cam.position = foco + dir * distCam;
        cam.LookAt(foco);
    }
}
