using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Deixa a cidade a noite com postes acesos, faixas neon nos predios e bloom.
public static class NoiteNeonGRS1
{
    const string Pasta = "Assets/Scenes/Noite";

    [MenuItem("GRS 1/Cidade a Noite (neon)")]
    public static void Noite()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        var cidade = GameObject.Find("Cidade");
        if (cidade == null) { Debug.LogError("Gere a cidade primeiro (GRS 1 > Gerar Cidade)."); return; }
        if (!AssetDatabase.IsValidFolder(Pasta)) AssetDatabase.CreateFolder("Assets/Scenes", "Noite");

        LimparNoite();
        var raiz = new GameObject("Noite").transform;

        // --- Luz e ceu ---
        var sol = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sol)
        {
            sol.transform.rotation = Quaternion.Euler(35f, 140f, 0f);
            sol.color = new Color(0.45f, 0.55f, 0.95f);
            sol.intensity = 0.25f;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.18f, 0.16f, 0.35f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.1f, 0.22f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.12f, 0.07f, 0.2f);
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 180f;

        var ceu = AssetDatabase.LoadAssetAtPath<Material>(Pasta + "/CeuNoite.mat");
        if (ceu == null)
        {
            ceu = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(ceu, Pasta + "/CeuNoite.mat");
        }
        ceu.SetColor("_SkyTint", new Color(0.25f, 0.15f, 0.45f));
        ceu.SetColor("_GroundColor", new Color(0.05f, 0.04f, 0.08f));
        ceu.SetFloat("_Exposure", 0.35f);
        ceu.SetFloat("_AtmosphereThickness", 0.6f);
        ceu.SetFloat("_SunSize", 0.02f);
        EditorUtility.SetDirty(ceu);
        RenderSettings.skybox = ceu;

        // --- Postes acesos ---
        var detalhes = cidade.transform.Find("Detalhes");
        int postes = 0;
        if (detalhes)
            foreach (Transform p in detalhes)
            {
                if (!p.name.StartsWith("light")) continue;
                var b = BoundsMundo(p.gameObject);
                var luz = new GameObject("LuzPoste", typeof(Light)).GetComponent<Light>();
                luz.transform.SetParent(raiz);
                luz.transform.position = new Vector3(b.center.x, b.max.y - 0.4f, b.center.z);
                luz.type = LightType.Point;
                luz.color = new Color(1f, 0.75f, 0.45f);
                luz.range = 14f;
                luz.intensity = 6f;
                luz.shadows = LightShadows.None;
                postes++;
            }

        // --- Faixas neon nos predios ---
        var cores = new[] { new Color(1f, 0.1f, 0.7f), new Color(0.1f, 0.9f, 1f), new Color(0.6f, 0.2f, 1f), new Color(1f, 0.5f, 0.1f) };
        var mats = cores.Select((c, i) => MaterialNeon("Neon" + i, c)).ToArray();
        var rnd = new System.Random(3);
        var predios = cidade.transform.Find("Predios");
        int neons = 0;
        if (predios)
            foreach (Transform pr in predios)
            {
                if (rnd.NextDouble() > 0.55) continue;
                var b = BoundsMundo(pr.gameObject);
                if (b.size.y < 4f) continue;
                int ci = rnd.Next(cores.Length);
                // faixa no topo (contorno do predio) e as vezes uma no meio
                Faixa(raiz, b, b.max.y - 0.3f, mats[ci]);
                if (b.size.y > 15f && rnd.NextDouble() < 0.6) Faixa(raiz, b, b.min.y + b.size.y * 0.5f, mats[(ci + 1) % mats.Length]);
                // brilho colorido na rua
                var l = new GameObject("BrilhoNeon", typeof(Light)).GetComponent<Light>();
                l.transform.SetParent(raiz);
                l.transform.position = new Vector3(b.center.x, Mathf.Min(b.max.y, 6f), b.center.z);
                l.type = LightType.Point; l.color = cores[ci]; l.range = Mathf.Max(b.size.x, b.size.z) * 0.9f + 6f; l.intensity = 4f;
                l.shadows = LightShadows.None;
                neons++;
            }

        // --- Bloom (brilho do neon) ---
        var vol = new GameObject("VolumeNoite", typeof(Volume)).GetComponent<Volume>();
        vol.transform.SetParent(raiz);
        vol.isGlobal = true;
        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Pasta + "/PerfilNoite.asset");
        if (perfil == null) { perfil = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(perfil, Pasta + "/PerfilNoite.asset"); }
        if (!perfil.TryGet(out Bloom bloom)) bloom = perfil.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(1.8f); bloom.threshold.Override(0.9f); bloom.scatter.Override(0.75f);
        if (!perfil.TryGet(out Tonemapping tm)) tm = perfil.Add<Tonemapping>(true);
        tm.mode.Override(TonemappingMode.ACES);
        if (!perfil.TryGet(out Vignette vg)) vg = perfil.Add<Vignette>(true);
        vg.intensity.Override(0.3f);
        if (!perfil.TryGet(out ColorAdjustments ca)) ca = perfil.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.6f); ca.saturation.Override(15f);
        EditorUtility.SetDirty(perfil);
        vol.sharedProfile = perfil;

        var cam = Camera.main;
        if (cam)
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
        }

        VisualGRS1.Janelas(2.5f);
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log($"GRS 1: noite aplicada ({postes} postes acesos, {neons} predios com neon).");
    }

    [MenuItem("GRS 1/Cidade de Dia (por do sol)")]
    public static void Dia()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        LimparNoite();
        VisualGRS1.Janelas(0f);
        GerarCidadeGRS1.Gerar(); // recria a cidade com a luz de por do sol
    }

    static void LimparNoite()
    {
        var velho = GameObject.Find("Noite");
        if (velho) Object.DestroyImmediate(velho);
    }

    static void Faixa(Transform pai, Bounds b, float y, Material m)
    {
        float e = 0.25f, a = 0.35f;
        var lados = new[]
        {
            (new Vector3(b.center.x, y, b.max.z + e * 0.5f), new Vector3(b.size.x + e * 2, a, e)),
            (new Vector3(b.center.x, y, b.min.z - e * 0.5f), new Vector3(b.size.x + e * 2, a, e)),
            (new Vector3(b.max.x + e * 0.5f, y, b.center.z), new Vector3(e, a, b.size.z + e * 2)),
            (new Vector3(b.min.x - e * 0.5f, y, b.center.z), new Vector3(e, a, b.size.z + e * 2)),
        };
        foreach (var (pos, tam) in lados)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "FaixaNeon";
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(pai);
            c.transform.position = pos;
            c.transform.localScale = tam;
            var r = c.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static Material MaterialNeon(string nome, Color cor)
    {
        string p = Pasta + "/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_BaseColor", cor * 0.3f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetColor("_EmissionColor", cor * 6f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Bounds BoundsMundo(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
