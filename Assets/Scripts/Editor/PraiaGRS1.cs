using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Cria uma praia no lado norte da cidade: calcadao, areia, mar animado, coqueiros e guarda-sois.
public static class PraiaGRS1
{
    const string Pasta = "Assets/Scenes/Praia";
    const float Borda = 65.4f;   // onde ficava a mureta norte
    const float Meia = 66f;      // metade da largura da cidade

    [MenuItem("GRS 1/Criar Praia")]
    public static void Menu()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        var cidade = GameObject.Find("Cidade");
        if (cidade == null) { Debug.LogError("Gere a cidade primeiro."); return; }
        Criar(cidade.transform);
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
    }

    public static void Criar(Transform cidade)
    {
        if (!AssetDatabase.IsValidFolder(Pasta)) AssetDatabase.CreateFolder("Assets/Scenes", "Praia");
        var velha = cidade.Find("Praia"); if (velha) Object.DestroyImmediate(velha.gameObject);
        var praia = new GameObject("Praia").transform; praia.SetParent(cidade);

        // 1) Tira a mureta do lado norte (o mar vira a borda)
        var muros = cidade.Find("Muros");
        if (muros)
            foreach (Transform t in muros.Cast<Transform>().ToArray())
                if (t.position.z > Borda - 2f) Object.DestroyImmediate(t.gameObject);

        // 2) Chao de fora so ate a praia
        var chao = cidade.Find("ChaoExterno");
        if (chao) chao.position = new Vector3(0, -0.02f, Borda - 300f);

        var matAreia = Mat("Areia", new Color(0.93f, 0.83f, 0.6f), 0.15f, TexturaGraos());
        var matMadeira = Mat("Madeira", new Color(0.55f, 0.38f, 0.24f), 0.25f);

        // 3) Calcadao de madeira + areia plana + rampa de areia entrando no mar + fundo do mar
        Bloco(praia, "AreiaCalcadao", new Vector3(0, 0.12f, Borda + 2.3f), new Vector3(Meia * 2, 0.25f, 4.6f), matMadeira);
        Bloco(praia, "AreiaPlana", new Vector3(0, -0.4f, Borda + 4.6f + 10f), new Vector3(Meia * 2 + 40, 1f, 20f), matAreia);
        float z0 = Borda + 24.6f, z1 = z0 + 30f, y0 = 0.1f, y1 = -1.7f;
        var rampa = Bloco(praia, "AreiaRampa", Vector3.zero, new Vector3(Meia * 2 + 40, 1f, Mathf.Sqrt(30f * 30f + (y0 - y1) * (y0 - y1))), matAreia);
        rampa.transform.rotation = Quaternion.Euler(Mathf.Atan2(y0 - y1, 30f) * Mathf.Rad2Deg, 0, 0);
        rampa.transform.position = new Vector3(0, (y0 + y1) / 2f, (z0 + z1) / 2f) - rampa.transform.up * 0.5f;
        Bloco(praia, "AreiaFundoMar", new Vector3(0, y1 - 0.5f, z1 + 40f), new Vector3(Meia * 2 + 40, 1f, 80f), matAreia);

        // 4) Mar
        var mar = GameObject.CreatePrimitive(PrimitiveType.Plane);
        mar.name = "Mar"; mar.transform.SetParent(praia);
        Object.DestroyImmediate(mar.GetComponent<Collider>());
        mar.transform.position = new Vector3(0, -0.35f, Borda + 260f);
        mar.transform.localScale = new Vector3(100f, 1f, 50f);
        mar.GetComponent<Renderer>().sharedMaterial = MatAgua();
        mar.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        mar.AddComponent<AguaAnimada>();

        // 5) Limites: ninguem passa das laterais nem nada pro alto-mar
        // limite no mar raso (agua pela canela): da pra molhar o pe, mas carro nao afunda
        Invisivel(praia, new Vector3(-Meia - 0.4f, 4f, Borda + 18f), new Vector3(1, 8, 36));
        Invisivel(praia, new Vector3(Meia + 0.4f, 4f, Borda + 18f), new Vector3(1, 8, 36));
        Invisivel(praia, new Vector3(0, 4f, Borda + 36f), new Vector3(Meia * 2 + 2, 8, 1));

        // 6) Coqueiros
        var rnd = new System.Random(21);
        var matTronco = Mat("Tronco", new Color(0.45f, 0.33f, 0.22f), 0.1f);
        var matFolha = Mat("Folha", new Color(0.18f, 0.45f, 0.16f), 0.2f);
        var coqueiros = new GameObject("Coqueiros").transform; coqueiros.SetParent(praia);
        for (float x = -Meia + 6f; x < Meia - 4f; x += 9f + (float)rnd.NextDouble() * 4f)
        {
            float z = Borda + 6f + (float)rnd.NextDouble() * 12f;
            Coqueiro(coqueiros, new Vector3(x, 0.1f, z), (float)rnd.NextDouble() * 360f, 6f + (float)rnd.NextDouble() * 3f, matTronco, matFolha, rnd);
        }

        // 7) Guarda-sois e toalhas
        var itens = new GameObject("GuardaSois").transform; itens.SetParent(praia);
        var sois = new[] { "detail-parasol-a", "detail-parasol-b" }
            .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Kenney/Commercial/{n}.fbx")).Where(g => g).ToArray();
        var cores = new[] { new Color(0.9f, 0.2f, 0.3f), new Color(0.2f, 0.5f, 0.9f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.8f, 0.6f) };
        for (int i = 0; i < 14; i++)
        {
            var p = new Vector3(-Meia + 8f + (float)rnd.NextDouble() * (Meia * 2 - 16f), 0.1f, Borda + 12f + (float)rnd.NextDouble() * 14f);
            if (sois.Length > 0)
            {
                var g = (GameObject)PrefabUtility.InstantiatePrefab(sois[rnd.Next(sois.Length)], itens);
                var b = Bounds(g);
                g.transform.localScale = Vector3.one * (2.6f / Mathf.Max(0.01f, b.size.y));
                g.transform.position = p;
            }
            var toalha = Bloco(itens, "Toalha", p + new Vector3(1.2f, 0.01f, 0.4f), new Vector3(0.9f, 0.02f, 1.9f),
                Mat("Toalha" + (i % cores.Length), cores[i % cores.Length], 0.1f));
            Object.DestroyImmediate(toalha.GetComponent<Collider>());
            toalha.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 40f - 20f, 0);
        }

        // 8) Postes no calcadao (ficam em Detalhes, entao acendem no modo noite)
        var luz = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kenney/Roads/light-square.fbx");
        var detalhes = cidade.Find("Detalhes");
        if (luz && detalhes)
            for (float x = -Meia + 5f; x < Meia; x += 14f)
            {
                var l = (GameObject)PrefabUtility.InstantiatePrefab(luz, detalhes);
                l.transform.localScale = Vector3.one * 10f;
                l.transform.rotation = Quaternion.Euler(0, 180f, 0);
                var b = Bounds(l);
                l.transform.position = new Vector3(x, 0.25f, Borda + 4.2f) - new Vector3(b.center.x - l.transform.position.x, b.min.y - l.transform.position.y, b.center.z - l.transform.position.z);
            }

        AssetDatabase.SaveAssets();
        Debug.Log("GRS 1: praia criada no lado norte da cidade.");
    }

    static void Coqueiro(Transform pai, Vector3 pos, float rotY, float altura, Material tronco, Material folha, System.Random rnd)
    {
        var raiz = new GameObject("Coqueiro").transform;
        raiz.SetParent(pai); raiz.position = pos; raiz.rotation = Quaternion.Euler(0, rotY, 0);
        int segs = 7; float seg = altura / segs;
        Vector3 p = pos; Vector3 dir = Vector3.up;
        float curva = 4f + (float)rnd.NextDouble() * 5f;
        for (int i = 0; i < segs; i++)
        {
            dir = Quaternion.AngleAxis(curva, raiz.right) * dir;
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.name = "Tronco"; c.transform.SetParent(raiz);
            float r = Mathf.Lerp(0.32f, 0.2f, i / (float)segs);
            c.transform.localScale = new Vector3(r * 2, seg * 0.55f, r * 2);
            c.transform.up = dir;
            c.transform.position = p + dir * seg * 0.5f;
            c.GetComponent<Renderer>().sharedMaterial = tronco;
            p += dir * seg;
        }
        // folhas
        for (int k = 0; k < 8; k++)
        {
            var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(f.GetComponent<Collider>());
            f.name = "Folha"; f.transform.SetParent(raiz);
            f.transform.localScale = new Vector3(0.7f, 0.06f, 3.4f);
            f.transform.rotation = Quaternion.Euler(25f + (float)rnd.NextDouble() * 15f, k * 45f + (float)rnd.NextDouble() * 15f, 0);
            f.transform.position = p + f.transform.forward * 1.5f + Vector3.down * 0.2f;
            f.GetComponent<Renderer>().sharedMaterial = folha;
        }
        var cocos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(cocos.GetComponent<Collider>());
        cocos.name = "Cocos"; cocos.transform.SetParent(raiz);
        cocos.transform.position = p + Vector3.down * 0.35f; cocos.transform.localScale = Vector3.one * 0.55f;
        cocos.GetComponent<Renderer>().sharedMaterial = tronco;

        var col = raiz.gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, altura * 0.4f, 0); col.height = altura * 0.8f; col.radius = 0.3f;
    }

    static GameObject Bloco(Transform pai, string nome, Vector3 pos, Vector3 tam, Material m)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = nome; c.transform.SetParent(pai);
        c.transform.position = pos; c.transform.localScale = tam;
        c.GetComponent<Renderer>().sharedMaterial = m;
        return c;
    }

    static void Invisivel(Transform pai, Vector3 pos, Vector3 tam)
    {
        var g = new GameObject("ParedeInvisivel", typeof(BoxCollider));
        g.transform.SetParent(pai); g.transform.position = pos;
        g.GetComponent<BoxCollider>().size = tam;
    }

    static Material Mat(string nome, Color cor, float liso, Texture2D tex = null)
    {
        string p = $"{Pasta}/{nome}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_BaseColor", cor); m.SetFloat("_Smoothness", liso);
        if (tex) { m.SetTexture("_BaseMap", tex); m.mainTextureScale = new Vector2(30, 6); }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MatAgua()
    {
        string p = $"{Pasta}/Mar.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetColor("_BaseColor", new Color(0.08f, 0.42f, 0.55f, 0.82f));
        m.SetFloat("_Smoothness", 0.85f);
        m.SetFloat("_Metallic", 0f);
        // sem reflexo do ceu "assado" de dia (deixava o mar ciano brilhante a noite)
        m.SetFloat("_EnvironmentReflections", 0f);
        m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        var n = NormalOndas();
        m.SetTexture("_BumpMap", n); m.SetFloat("_BumpScale", 0.7f);
        m.EnableKeyword("_NORMALMAP");
        m.mainTextureScale = new Vector2(60, 30);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D NormalOndas()
    {
        string p = $"{Pasta}/OndasNormal.png";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t) return t;
        int n = 256; var h = new float[n, n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float u = x / (float)n, v = y / (float)n;
            // ondas que repetem (tileable) usando senos
            h[x, y] = Mathf.Sin((u * 6 + v * 2) * Mathf.PI * 2) * 0.5f + Mathf.Sin((u * 3 - v * 7) * Mathf.PI * 2) * 0.3f + Mathf.Sin((u * 13 + v * 11) * Mathf.PI * 2) * 0.15f;
        }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = h[(x + 1) % n, y] - h[(x - 1 + n) % n, y];
            float dy = h[x, (y + 1) % n] - h[x, (y - 1 + n) % n];
            var nn = new Vector3(-dx * 2f, -dy * 2f, 1f).normalized;
            tex.SetPixel(x, y, new Color(nn.x * 0.5f + 0.5f, nn.y * 0.5f + 0.5f, nn.z * 0.5f + 0.5f, 1));
        }
        System.IO.File.WriteAllBytes(p, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(p);
        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        ti.textureType = TextureImporterType.NormalMap; ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static Texture2D TexturaGraos()
    {
        string p = $"{Pasta}/AreiaGraos.png";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t) return t;
        int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var r = new System.Random(4);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float g = 0.88f + (float)r.NextDouble() * 0.12f;
            tex.SetPixel(x, y, new Color(g, g, g, 1));
        }
        System.IO.File.WriteAllBytes(p, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(p);
        var ti = (TextureImporter)AssetImporter.GetAtPath(p); ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static Bounds Bounds(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
}
