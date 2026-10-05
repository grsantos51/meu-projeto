using UnityEngine;

// Moeda/dinheiro que cai do pedestre. Encoste (a pe ou de carro) para pegar.
public class Moeda : MonoBehaviour
{
    public int valor = 30;
    static Material ouro;
    float inicioY, criado;

    public static void Criar(Vector3 pos, int valor, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = "Moeda";
            Object.Destroy(g.GetComponent<Collider>());
            g.transform.position = pos + new Vector3(Random.Range(-0.8f, 0.8f), 0.6f, Random.Range(-0.8f, 0.8f));
            g.transform.localScale = new Vector3(0.45f, 0.04f, 0.45f);
            g.transform.rotation = Quaternion.Euler(90, Random.Range(0, 360), 0);
            if (ouro == null)
            {
                ouro = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                ouro.SetColor("_BaseColor", new Color(1f, 0.78f, 0.15f));
                ouro.SetFloat("_Metallic", 0.2f); ouro.SetFloat("_Smoothness", 0.6f);
                ouro.EnableKeyword("_EMISSION"); ouro.SetColor("_EmissionColor", new Color(1f, 0.65f, 0.05f) * 2.2f);
            }
            g.GetComponent<Renderer>().sharedMaterial = ouro;
            var col = g.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = 3f; // raio grande (escala da moeda e pequena)
            var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true;
            var m = g.AddComponent<Moeda>(); m.valor = Mathf.Max(1, valor / quantidade);
        }
    }

    void Start() { inicioY = transform.position.y; criado = Time.time; Destroy(gameObject, 60f); }

    void Update()
    {
        transform.Rotate(Vector3.forward, 180f * Time.deltaTime, Space.Self);
        var p = transform.position; p.y = inicioY + Mathf.Sin((Time.time - criado) * 3f) * 0.12f; transform.position = p;
    }

    void OnTriggerEnter(Collider outro)
    {
        if (Time.time - criado < 0.4f) return;
        bool aPe = outro.GetComponent<CharacterController>() != null && outro.GetComponent<EntrarCarro>() != null;
        var carro = outro.GetComponentInParent<CarroSimples>();
        if (!aPe && !(carro && carro.dirigindo)) return;
        if (Dinheiro.Instancia) Dinheiro.Instancia.Adicionar(valor);
        Destroy(gameObject);
    }
}
