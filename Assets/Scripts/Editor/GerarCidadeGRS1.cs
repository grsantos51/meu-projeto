using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Gera uma cidade com os kits da Kenney (Roads + Industrial + Commercial/Suburban se existirem)
public static class GerarCidadeGRS1
{
    const string Roads = "Assets/Kenney/Roads";
    const float Tile = 10f;          // tamanho de cada quadrado da cidade em metros
    const int Cells = 13;            // cidade de 13x13 quadrados
    const int Quadra = 4;            // rua a cada 4 quadrados

    [MenuItem("GRS 1/Gerar Cidade")]
    public static void Gerar()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);

        // Limpa o cenario antigo
        foreach (var n in new[] { "Chao" }) { var g = GameObject.Find(n); if (g) Object.DestroyImmediate(g); }
        foreach (var g in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(o => o && o.name.StartsWith("Caixa ")).ToArray()) Object.DestroyImmediate(g);
        var velha = GameObject.Find("Cidade"); if (velha) Object.DestroyImmediate(velha);

        var cidade = new GameObject("Cidade").transform;
        var ruas = new GameObject("Ruas").transform; ruas.SetParent(cidade);
        var predios = new GameObject("Predios").transform; predios.SetParent(cidade);
        var detalhes = new GameObject("Detalhes").transform; detalhes.SetParent(cidade);

        var straight = Load(Roads + "/road-straight.fbx");
        var cross = Load(Roads + "/road-crossroad.fbx");
        var tileLow = Load(Roads + "/tile-low.fbx");
        var luz = Load(Roads + "/light-square.fbx");
        var semaforo = Load(Roads + "/traffic-light.fbx");

        float tamRua = Footprint(straight);
        float escala = Tile / tamRua;
        Debug.Log($"GRS 1: tile da rua = {tamRua:F2}, escala = {escala:F2}");

        // Predios por tipo
        var arranhaCeus = Modelos("Assets/Kenney/Commercial", n => n.Contains("skyscraper"));
        var comerciais = Modelos("Assets/Kenney/Commercial", n => n.StartsWith("building-") && !n.Contains("skyscraper"));
        var industriais = Modelos("Assets/Kenney/Industrial", n => n.StartsWith("building-"));
        var casas = Modelos("Assets/Kenney/Suburban", n => n.StartsWith("building"));
        foreach (var b in arranhaCeus.Take(2).Concat(comerciais.Take(2))) Debug.Log($"GRS 1: {b.name} footprint={Footprint(b):F2} altura={Altura(b):F2}");

        var rnd = new System.Random(7);
        float origem = -(Cells - 1) * Tile * 0.5f;

        for (int i = 0; i < Cells; i++)
        for (int j = 0; j < Cells; j++)
        {
            bool ruaX = i % Quadra == 0, ruaZ = j % Quadra == 0;
            var pos = new Vector3(origem + i * Tile, 0, origem + j * Tile);

            if (ruaX && ruaZ)
            {
                Colocar(cross, ruas, pos, 0, escala);
                if (rnd.NextDouble() < 0.5 && semaforo)
                    Colocar(semaforo, detalhes, pos + new Vector3(Tile * 0.42f, 0, Tile * 0.42f), 225, escala, false);
            }
            else if (ruaX || ruaZ)
            {
                // ruaX = coluna de rua (corre no eixo Z); ruaZ = linha de rua (corre no eixo X)
                Colocar(straight, ruas, pos, ruaX ? 90 : 0, escala);
                if (luz && ((ruaX ? j : i) % 2 == 1))
                {
                    var lado = ruaX ? new Vector3(Tile * 0.45f, 0, 0) : new Vector3(0, 0, Tile * 0.45f);
                    Colocar(luz, detalhes, pos + lado, ruaX ? 270 : 180, escala, false);
                }
            }
            else
            {
                // Quadra: piso + predio
                if (tileLow) Colocar(tileLow, ruas, pos, 0, escala);
                // Centro = arranha-ceus, em volta = comercio, cantos = industrial
                int bi = i / Quadra, bj = j / Quadra, meio = (Cells / Quadra) / 2;
                int dist = Mathf.Abs(bi - meio) + Mathf.Abs(bj - meio);
                List<GameObject> lista;
                if (dist == 0) lista = rnd.NextDouble() < 0.7 && arranhaCeus.Count > 0 ? arranhaCeus : comerciais;
                else if (dist == 1) lista = rnd.NextDouble() < 0.8 && comerciais.Count > 0 ? comerciais : (arranhaCeus.Count > 0 && rnd.NextDouble() < 0.5 ? arranhaCeus : industriais);
                else lista = casas.Count > 0 && rnd.NextDouble() < 0.4 ? casas : industriais;
                if (lista.Count == 0) lista = industriais.Count > 0 ? industriais : comerciais;
                if (lista.Count > 0 && rnd.NextDouble() < 0.95)
                {
                    var b = lista[rnd.Next(lista.Count)];
                    // Vira o predio para a rua mais proxima
                    int di = i % Quadra, dj = j % Quadra;
                    float rot = di == 1 ? 270 : di == Quadra - 1 ? 90 : dj == 1 ? 180 : 0;
                    var go = Colocar(b, predios, pos, rot, escala, true, Tile * 0.9f);
                }
            }
        }

        // Chao grande em volta (para nao cair no vazio)
        var chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "ChaoExterno";
        chao.transform.SetParent(cidade);
        chao.transform.position = new Vector3(0, -0.02f, 0);
        chao.transform.localScale = Vector3.one * 60;
        var matChao = AssetDatabase.LoadAssetAtPath<Material>("Assets/Scenes/ChaoMaterial.mat");
        if (matChao != null) { matChao.color = new Color(0.32f, 0.36f, 0.25f); chao.GetComponent<Renderer>().sharedMaterial = matChao; }

        AdicionarMuros(cidade);
        PraiaGRS1.Criar(cidade);
        AjustarLuzPorDoSol();

        // Player no primeiro cruzamento
        var player = GameObject.Find("Player");
        if (player) player.transform.position = new Vector3(origem + Quadra * Tile, 0.3f, origem + Quadra * Tile + Tile * 0.5f);

        foreach (var t in cidade.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log($"GRS 1: cidade gerada com {predios.childCount} predios.");
    }

    [MenuItem("GRS 1/Ver Cidade de Cima")]
    public static void VerDeCima()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        sv.sceneViewState.showFog = false;
        sv.LookAt(Vector3.zero, Quaternion.Euler(50f, 45f, 0f), 55f);
        sv.Repaint();
    }

    [MenuItem("GRS 1/Colocar Muros na Borda")]
    public static void MurosMenu()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        var cidade = GameObject.Find("Cidade");
        if (cidade == null) { Debug.LogError("Gere a cidade primeiro."); return; }
        AdicionarMuros(cidade.transform);
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
    }

    // Mureta de concreto em volta da cidade + parede invisivel alta (ninguem sai do mapa)
    static void AdicionarMuros(Transform cidade)
    {
        var velho = cidade.Find("Muros"); if (velho) Object.DestroyImmediate(velho.gameObject);
        var muros = new GameObject("Muros").transform;
        muros.SetParent(cidade);

        const string mp = "Assets/Scenes/MuroConcreto.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, mp); }
        mat.SetColor("_BaseColor", new Color(0.62f, 0.6f, 0.58f));
        mat.SetFloat("_Smoothness", 0.1f);
        const string fp = "Assets/Scenes/FaixaMuro.mat";
        var faixa = AssetDatabase.LoadAssetAtPath<Material>(fp);
        if (faixa == null) { faixa = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(faixa, fp); }
        faixa.SetColor("_BaseColor", new Color(1f, 0.75f, 0.1f));
        faixa.EnableKeyword("_EMISSION");
        faixa.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.05f) * 1.5f);

        float meio = (Cells - 1) * Tile * 0.5f + Tile * 0.5f + 0.4f; // logo depois da ultima rua
        float comp = meio * 2f + 1f;
        var lados = new[]
        {
            (new Vector3(0, 0, meio), new Vector3(comp, 1, 1)), (new Vector3(0, 0, -meio), new Vector3(comp, 1, 1)),
            (new Vector3(meio, 0, 0), new Vector3(1, 1, comp)), (new Vector3(-meio, 0, 0), new Vector3(1, 1, comp)),
        };
        foreach (var (pos, dir) in lados)
        {
            // mureta visivel (1,1 m) com faixa amarela no topo
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m.name = "Mureta"; m.transform.SetParent(muros);
            m.transform.position = pos + Vector3.up * 0.55f;
            m.transform.localScale = new Vector3(dir.x > 1 ? comp : 0.8f, 1.1f, dir.z > 1 ? comp : 0.8f);
            m.GetComponent<Renderer>().sharedMaterial = mat;

            var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = "FaixaAmarela"; f.transform.SetParent(muros);
            Object.DestroyImmediate(f.GetComponent<Collider>());
            f.transform.position = pos + Vector3.up * 1.13f;
            f.transform.localScale = new Vector3(dir.x > 1 ? comp : 0.82f, 0.06f, dir.z > 1 ? comp : 0.82f);
            f.GetComponent<Renderer>().sharedMaterial = faixa;

            // parede invisivel de 8 m (carro nao pula a mureta)
            var inv = new GameObject("ParedeInvisivel", typeof(BoxCollider));
            inv.transform.SetParent(muros);
            inv.transform.position = pos + Vector3.up * 4f;
            inv.GetComponent<BoxCollider>().size = new Vector3(dir.x > 1 ? comp : 1f, 8f, dir.z > 1 ? comp : 1f);
        }
        Debug.Log("GRS 1: muros colocados na borda da cidade.");
    }

    static List<GameObject> Modelos(string pasta, System.Func<string, bool> filtro)
    {
        var l = new List<GameObject>();
        if (!AssetDatabase.IsValidFolder(pasta)) return l;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { pasta }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (filtro(System.IO.Path.GetFileNameWithoutExtension(p))) l.Add(Load(p));
        }
        return l;
    }

    static GameObject Load(string p) => AssetDatabase.LoadAssetAtPath<GameObject>(p);

    static Bounds BoundsDe(GameObject prefab)
    {
        var rs = prefab.GetComponentsInChildren<MeshFilter>(true);
        var b = new Bounds(Vector3.zero, Vector3.zero); bool first = true;
        foreach (var mf in rs)
        {
            if (mf.sharedMesh == null) continue;
            var mb = mf.sharedMesh.bounds;
            var m = prefab.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            foreach (var c in Cantos(mb))
            {
                var w = m.MultiplyPoint3x4(c);
                if (first) { b = new Bounds(w, Vector3.zero); first = false; } else b.Encapsulate(w);
            }
        }
        return b;
    }
    static IEnumerable<Vector3> Cantos(Bounds b)
    {
        for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
            yield return new Vector3(x == 0 ? b.min.x : b.max.x, y == 0 ? b.min.y : b.max.y, z == 0 ? b.min.z : b.max.z);
    }
    static float Footprint(GameObject p) { var b = BoundsDe(p); return Mathf.Max(b.size.x, b.size.z); }
    static float Altura(GameObject p) => BoundsDe(p).size.y;

    static GameObject Colocar(GameObject prefab, Transform pai, Vector3 pos, float rotY, float escala, bool centralizar = true, float caberEm = 0f)
    {
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        float s = escala;
        if (caberEm > 0f)
        {
            float fp = Footprint(prefab) * escala;
            if (fp > caberEm) s *= caberEm / fp;
        }
        go.transform.localScale = Vector3.one * s;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);
        go.transform.position = pos;
        if (centralizar)
        {
            var b = BoundsDe(prefab);
            var centro = go.transform.rotation * Vector3.Scale(b.center, go.transform.localScale);
            go.transform.position = pos - new Vector3(centro.x, b.min.y * s, centro.z);
        }
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<MeshCollider>();
        return go;
    }

    static void AjustarLuzPorDoSol()
    {
        var sol = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sol)
        {
            sol.transform.rotation = Quaternion.Euler(18f, -35f, 0f);
            sol.color = new Color(1f, 0.72f, 0.48f);
            sol.intensity = 2.2f;
            sol.shadows = LightShadows.Soft;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.95f, 0.8f, 0.7f);
        RenderSettings.ambientEquatorColor = new Color(0.75f, 0.62f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.38f, 0.32f, 0.28f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.98f, 0.76f, 0.6f);
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance = 220f;
        var sky = RenderSettings.skybox;
        if (sky != null && sky.HasProperty("_SkyTint"))
        {
            const string cp = "Assets/Scenes/CeuPorDoSol.mat";
            var novo = AssetDatabase.LoadAssetAtPath<Material>(cp);
            if (novo == null) { novo = new Material(sky); AssetDatabase.CreateAsset(novo, cp); }
            novo.SetColor("_SkyTint", new Color(0.55f, 0.45f, 0.6f));
            if (novo.HasProperty("_Exposure")) novo.SetFloat("_Exposure", 1.25f);
            novo.SetFloat("_AtmosphereThickness", 1.3f);
            EditorUtility.SetDirty(novo);
            RenderSettings.skybox = novo;
        }
    }
}
