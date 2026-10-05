using UnityEngine;

// Faz a agua do mar "andar" (rola a textura de ondas) e balancar de leve.
public class AguaAnimada : MonoBehaviour
{
    public Vector2 velocidade = new Vector2(0.01f, 0.02f);
    public float altura = 0.08f;
    Renderer r; Vector3 baseY;

    void Start() { r = GetComponent<Renderer>(); baseY = transform.position; }

    void Update()
    {
        if (r) r.material.mainTextureOffset = velocidade * Time.time;
        transform.position = baseY + Vector3.up * Mathf.Sin(Time.time * 0.6f) * altura;
    }
}
