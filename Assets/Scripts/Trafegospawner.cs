using UnityEngine;

// Opcional: cria vários carros espalhados ao longo de um caminho.
public class TrafegoSpawner : MonoBehaviour
{
    public CaminhoCarros caminho;
    public GameObject[] prefabsCarros;
    public int quantidade = 4;
    public Vector2 faixaVelocidade = new Vector2(7f, 12f);

    void Start()
    {
        if (caminho == null || caminho.Quantidade < 2 || prefabsCarros.Length == 0) return;

        int n = caminho.Quantidade;
        int qtd = Mathf.Min(quantidade, n); // no máximo 1 carro por waypoint
        for (int i = 0; i < qtd; i++)
        {
            int p = i * n / qtd;
            var prefab = prefabsCarros[Random.Range(0, prefabsCarros.Length)];
            var carro = Instantiate(prefab, caminho.Ponto(p), Quaternion.identity);

            var ia = carro.GetComponent<CarroAutonomo>();
            if (!ia) ia = carro.AddComponent<CarroAutonomo>();
            ia.caminho = caminho;
            ia.pontoInicial = p;
            ia.posicionarNoInicio = true;
            ia.velocidadeMax = Random.Range(faixaVelocidade.x, faixaVelocidade.y);
        }
    }
}