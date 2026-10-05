using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CarrosGRS1
{
    const string Pasta = "Assets/Kenney/Cars";
    const float Tile = 10f; const int Cells = 13; const int Quadra = 4;
    // Kenney: so os utilitarios (os carros comuns agora sao do pacote ARCADE, mais arredondados)
    static readonly string[] Modelos = { "taxi", "police", "van", "delivery", "truck", "ambulance", "suv" };
    const string Arcade = "Assets/ARCADE - FREE Racing Car/Prefabs (Meshes Only)";

    [MenuItem("GRS 1/Colocar Carros")]
    public static void Colocar()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        var velho = GameObject.Find("Carros"); if (velho) Object.DestroyImmediate(velho);
        var raiz = new GameObject("Carros").transform;

        ConverterArcadeParaURP();
        var kenney = Modelos.Select(m => AssetDatabase.LoadAssetAtPath<GameObject>($"{Pasta}/{m}.fbx")).Where(p => p).ToList();
        var arcade = AssetDatabase.IsValidFolder(Arcade)
            ? AssetDatabase.FindAssets("t:Prefab", new[] { Arcade }).Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToList()
            : new List<GameObject>();
        // ~60% carros ARCADE (5 cores), ~40% utilitarios Kenney
        var prefabs = new List<GameObject>();
        for (int r = 0; r < 3; r++) prefabs.AddRange(arcade);
        prefabs.AddRange(kenney);
        if (prefabs.Count == 0) { Debug.LogError("Nenhum modelo de carro encontrado"); return; }

        var rnd = new System.Random(11);
        var pl = GameObject.Find("Player");
        Vector3 inicio = pl ? pl.transform.position : Vector3.one * 9999f;
        float origem = -(Cells - 1) * Tile * 0.5f;
        int n = 0;

        // Carros estacionados nas ruas retas, encostados na calcada
        for (int i = 0; i < Cells; i++)
        for (int j = 0; j < Cells; j++)
        {
            bool ruaX = i % Quadra == 0, ruaZ = j % Quadra == 0;
            if (ruaX == ruaZ) continue;            // so ruas retas (nao cruzamento, nao quadra)
            if (rnd.NextDouble() > 0.45) continue;
            var pos = new Vector3(origem + i * Tile, 0, origem + j * Tile);
            int lado = rnd.NextDouble() < 0.5 ? 1 : -1;
            Vector3 dirRua = ruaX ? Vector3.forward : Vector3.right;
            Vector3 dirLado = ruaX ? Vector3.right : Vector3.forward;
            var p = pos + dirLado * (lado * 3.1f);
            if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(inicio.x, 0, inicio.z)) < 12f) continue;
            float rot = Quaternion.LookRotation(dirRua * (lado > 0 ? 1 : -1)).eulerAngles.y;
            CriarCarro(prefabs[rnd.Next(prefabs.Count)], raiz, p, rot, false);
            n++;
        }

        // Carro do jogador ao lado da Arissa
        var player = GameObject.Find("Player");
        if (player)
        {
            var esportivo = AssetDatabase.LoadAssetAtPath<GameObject>($"{Arcade}/Free Racing Car Red Variant.prefab")
                            ?? AssetDatabase.LoadAssetAtPath<GameObject>($"{Pasta}/sedan-sports.fbx");
            var pos = player.transform.position + new Vector3(1.6f, 0, 3.5f);
            pos.y = 0;
            var meu = CriarCarro(esportivo, raiz, pos, 0f, true);
            meu.name = "CarroDoJogador";
            var ec = player.GetComponent<EntrarCarro>();
            if (ec == null) ec = player.AddComponent<EntrarCarro>();
            ec.distancia = 6f;
        }

        int transito = ColocarTransito(raiz, prefabs, rnd, origem);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log($"GRS 1: {transito} carros no transito + {n + 1} estacionados;, todos dirigiveis (aperte E perto de qualquer um).");
    }

    public static GameObject CriarCarro(GameObject prefab, Transform pai, Vector3 pos, float rotY, bool dirigivel)
    {
        var raiz = new GameObject(prefab.name);
        raiz.transform.SetParent(pai);
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);

        // Escala: comprimento do carro ~4.6 m
        var b = Bounds(modelo);
        float comp = Mathf.Max(b.size.x, b.size.z);
        bool grande = prefab.name.Contains("truck") || prefab.name.Contains("ambulance") || prefab.name.Contains("delivery") || prefab.name.Contains("van");
        bool arcadeCar = prefab.name.Contains("Racing Car");
        float s = (grande ? 5.8f : arcadeCar ? 4.6f : 4.6f) / comp;
        // Kenney e "gordinho": achata so um pouco; o ARCADE ja tem proporcao boa
        float alturaMax = grande ? 2.7f : prefab.name.Contains("suv") ? 1.85f : 1.7f;
        float sy = arcadeCar ? s : Mathf.Min(s, alturaMax / Mathf.Max(0.01f, b.size.y));
        modelo.transform.localScale = new Vector3(s, sy, s);
        // se o comprimento estiver no eixo X, gira para ficar no Z
        if (b.size.x > b.size.z) modelo.transform.localRotation = Quaternion.Euler(0, 90, 0);
        b = Bounds(modelo);
        modelo.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

        // apoia no asfalto (a rua fica um pouco acima de y = 0)
        Physics.SyncTransforms();
        var ruas = GameObject.Find("Ruas");
        var chao = Physics.RaycastAll(pos + Vector3.up * 5f, Vector3.down, 10f)
            .Where(h => ruas != null && h.collider.transform.IsChildOf(ruas.transform))
            .OrderBy(h => h.distance).ToArray();
        if (chao.Length > 0) pos.y = chao[0].point.y;
        raiz.transform.position = pos;
        raiz.transform.rotation = Quaternion.Euler(0, rotY, 0);

        b = Bounds(modelo, raiz.transform);
        var col = raiz.AddComponent<BoxCollider>();
        col.center = b.center;
        col.size = b.size;

        // Todos os carros podem ser dirigidos (aperte E perto de qualquer um)
        {
            var rb = raiz.AddComponent<Rigidbody>();
            rb.mass = 1200f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            raiz.AddComponent<CarroSimples>();
        }
        return raiz;
    }

    // Carros andando sozinhos pelas ruas
    static int ColocarTransito(Transform raiz, List<GameObject> prefabs, System.Random rnd, float origem)
    {
        var grupo = new GameObject("Transito").transform; grupo.SetParent(raiz);
        int nos = (Cells - 1) / Quadra + 1;            // 4 ruas em cada eixo
        float passo = Quadra * Tile;                    // 40 m entre ruas
        var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        int total = 0;
        for (int k = 0; k < 14; k++)
        {
            // cruzamento de partida + direcao valida
            int i, j; Vector2Int d;
            do { i = rnd.Next(nos); j = rnd.Next(nos); d = dirs[rnd.Next(4)]; }
            while (i + d.x < 0 || j + d.y < 0 || i + d.x >= nos || j + d.y >= nos);
            var no = new Vector3(origem + i * passo, 0, origem + j * passo);
            var dv = new Vector3(d.x, 0, d.y); var dir = new Vector3(d.y, 0, -d.x);
            var pos = no + dv * (8f + (float)rnd.NextDouble() * 20f) + dir * 1.0f;
            var car = CriarCarro(prefabs[rnd.Next(prefabs.Count)], grupo, pos, Quaternion.LookRotation(dv).eulerAngles.y, false);
            car.name = "Transito_" + car.name;
            var tc = car.AddComponent<TrafegoCarro>();
            tc.origem = origem; tc.passo = passo; tc.nos = nos;
            tc.alturaRua = car.transform.position.y;
            tc.velocidadeMax = 7f + (float)rnd.NextDouble() * 4f;
            tc.Iniciar(i + d.x, j + d.y, d);
            total++;
        }
        return total;
    }

    // O pacote ARCADE vem com shader Standard (fica rosa no URP): troca para URP/Lit
    static void ConverterArcadeParaURP()
    {
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/ARCADE - FREE Racing Car/Materials" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
            if (m == null || m.shader == urp) continue;
            var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            var cor = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            var emTex = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
            var emCor = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            float brilho = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.5f;
            m.shader = urp;
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", cor);
            m.SetFloat("_Smoothness", Mathf.Max(0.55f, brilho)); // pintura com brilho
            if (emCor.maxColorComponent > 0.01f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", emTex); m.SetColor("_EmissionColor", emCor);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
    }

    // Bounds em espaco local de "referencia" (padrao: pai do modelo)
    static Bounds Bounds(GameObject go, Transform referencia = null)
    {
        if (referencia == null) referencia = go.transform.parent;
        var mfs = go.GetComponentsInChildren<MeshFilter>();
        Bounds b = default; bool first = true;
        foreach (var mf in mfs)
        {
            if (!mf.sharedMesh) continue;
            var m = referencia.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            var mb = mf.sharedMesh.bounds;
            for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
            {
                var c = m.MultiplyPoint3x4(new Vector3(x == 0 ? mb.min.x : mb.max.x, y == 0 ? mb.min.y : mb.max.y, z == 0 ? mb.min.z : mb.max.z));
                if (first) { b = new Bounds(c, Vector3.zero); first = false; } else b.Encapsulate(c);
            }
        }
        return b;
    }
}
