using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Coloca a LOJA na cidade (fachada na avenida x = 20) + tela da loja + acessorios na Arissa.
public static class LojaGRS1
{
    public static Vector3 PosLoja = new Vector3(15.8f, 0.15f, 0f);
    public static Vector3 PosCarroVip = new Vector3(16.6f, 0f, -9f);
    public static float RotCarroVip = 0f;

    [MenuItem("GRS 1/Colocar Loja")]
    public static void Colocar()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        // apaga a loja antiga (inclusive objetos desativados, que o GameObject.Find nao acha)
        foreach (var g in cena.GetRootGameObjects())
            if (g.name == "Loja" || g.name == "LojaUI" || g.name == "CarroExclusivo") Object.DestroyImmediate(g);

        // ----- tela da loja -----
        var ui = new GameObject("LojaUI").AddComponent<LojaUI>();
        ui.fonte = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bangers SDF.asset");
        ui.fonteTitulo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Neonderthaw SDF.asset");
        ui.materialTitulo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Neon Titulo.mat");

        // ----- porta da loja -----
        var loja = new GameObject("Loja");
        loja.transform.position = PosLoja;
        var lj = loja.AddComponent<Loja>();
        lj.materialCinzaCarro = AssetDatabase.LoadAssetAtPath<Material>("Assets/ARCADE - FREE Racing Car/Materials/Color Variations/AFRC_Mat_Col4.mat");

        var marca = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marca.name = "MarcaLoja"; Object.DestroyImmediate(marca.GetComponent<Collider>());
        marca.transform.SetParent(loja.transform, false);
        marca.transform.localScale = new Vector3(3.2f, 0.02f, 3.2f);
        var mm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mm.SetColor("_BaseColor", new Color(0.6f, 0.15f, 1f)); mm.EnableKeyword("_EMISSION"); mm.SetColor("_EmissionColor", new Color(0.6f, 0.15f, 1f) * 1.2f);
        AssetDatabase.DeleteAsset("Assets/Scenes/MarcaLoja.mat");
        AssetDatabase.CreateAsset(mm, "Assets/Scenes/MarcaLoja.mat");
        marca.GetComponent<Renderer>().sharedMaterial = mm;

        // letreiro neon rosa (virado para a avenida)
        var placa = new GameObject("PlacaLoja", typeof(TextMeshPro));
        placa.transform.SetParent(loja.transform, false);
        placa.transform.localPosition = new Vector3(-1.6f, 4.6f, 0f);
        placa.transform.rotation = Quaternion.Euler(0, -90f, 0);
        var tmp = placa.GetComponent<TextMeshPro>();
        tmp.text = "LOJA GRS";
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TiltNeon SDF.asset");
        var baseNeon = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Neon Botao.mat");
        if (baseNeon)
        {
            AssetDatabase.DeleteAsset("Assets/Fonts/Neon Loja.mat");
            var neon = new Material(baseNeon);
            neon.SetColor("_GlowColor", new Color(1f, 0.2f, 0.8f, 1f));
            neon.SetColor("_OutlineColor", new Color(1f, 0.3f, 0.85f, 1f));
            if (neon.HasProperty("_UnderlayColor")) neon.SetColor("_UnderlayColor", new Color(1f, 0.2f, 0.8f, 0.5f));
            AssetDatabase.CreateAsset(neon, "Assets/Fonts/Neon Loja.mat");
            tmp.fontSharedMaterial = neon;
        }
        tmp.color = new Color(1f, 0.75f, 0.95f);
        tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(9, 2.5f);

        var luz = new GameObject("LuzLoja", typeof(Light)).GetComponent<Light>();
        luz.transform.SetParent(loja.transform, false);
        luz.transform.localPosition = new Vector3(0.5f, 3.2f, 0);
        luz.type = LightType.Point; luz.color = new Color(1f, 0.3f, 0.85f); luz.range = 9f; luz.intensity = 3f;

        // ----- carro exclusivo (aparece depois de comprado) -----
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ARCADE - FREE Racing Car/Prefabs (Meshes Only)/Free Racing Car Gray Variant.prefab");
        if (prefab)
        {
            var vip = CarrosGRS1.CriarCarro(prefab, null, PosCarroVip, RotCarroVip, true);
            vip.name = "CarroExclusivo";
            var cs = vip.GetComponent<CarroSimples>();
            cs.velocidadeMax = 42f; cs.aceleracao = 20f; cs.giro = 95f;
            // pintura dourada fixa
            var cinza = lj.materialCinzaCarro;
            if (cinza)
            {
                AssetDatabase.DeleteAsset("Assets/Scenes/CarroVipDourado.mat");
                var ouro = new Material(cinza) { name = "CarroVipDourado" };
                ouro.SetColor("_BaseColor", new Color(1.35f, 1.0f, 0.25f));
                ouro.SetFloat("_Smoothness", 0.85f); ouro.SetFloat("_Metallic", 0.6f);
                AssetDatabase.CreateAsset(ouro, "Assets/Scenes/CarroVipDourado.mat");
                foreach (var r in vip.GetComponentsInChildren<Renderer>())
                {
                    var ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] && ms[i].name.StartsWith("AFRC_Mat_Col")) ms[i] = ouro;
                    r.sharedMaterials = ms;
                }
            }
            vip.SetActive(false);
            lj.carroVip = vip;
        }

        // ----- acessorios na Arissa -----
        var player = GameObject.Find("Player");
        if (player && !player.GetComponent<Acessorios>()) player.AddComponent<Acessorios>();

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("GRS 1: loja colocada. Va ate a marca roxa na avenida e aperte E.");
    }

    [MenuItem("GRS 1/Debug - Ir para a Loja")]
    public static void IrLoja()
    {
        if (!Application.isPlaying) return;
        var p = GameObject.Find("Player"); var l = GameObject.Find("Loja");
        if (!p || !l) return;
        var cc = p.GetComponent<CharacterController>(); cc.enabled = false;
        p.transform.position = l.transform.position + new Vector3(1.2f, 0.1f, 0f);
        p.transform.rotation = Quaternion.Euler(0, -90f, 0);
        cc.enabled = true;
    }

    [MenuItem("GRS 1/Zerar Compras da Loja")]
    public static void Zerar() { Inventario.Zerar(); Debug.Log("GRS 1: compras da loja zeradas."); }
}
