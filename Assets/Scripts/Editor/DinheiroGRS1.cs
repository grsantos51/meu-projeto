using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DinheiroGRS1
{
    [MenuItem("GRS 1/Zerar Dinheiro Salvo")]
    public static void Zerar()
    {
        PlayerPrefs.DeleteKey("GRS1_Dinheiro");
        PlayerPrefs.Save();
        Debug.Log("GRS 1: dinheiro salvo zerado.");
    }

    [MenuItem("GRS 1/Colocar Dinheiro e Banco")]
    public static void Colocar()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);

        // HUD de dinheiro
        var velho = GameObject.Find("Dinheiro"); if (velho) Object.DestroyImmediate(velho);
        var din = new GameObject("Dinheiro").AddComponent<Dinheiro>();
        din.fonte = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bangers SDF.asset");
        din.materialFonte = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Bangers Titulo.mat");

        // Banco: fachada do quarteirao central, de frente pra avenida x = -20
        var vb = GameObject.Find("Banco"); if (vb) Object.DestroyImmediate(vb);
        var banco = new GameObject("Banco");
        banco.transform.position = new Vector3(-16.2f, 0.15f, 0f);
        var b = banco.AddComponent<Banco>();

        // marca verde no chao
        var marca = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marca.name = "MarcaAssalto"; Object.DestroyImmediate(marca.GetComponent<Collider>());
        marca.transform.SetParent(banco.transform, false);
        marca.transform.localScale = new Vector3(3.2f, 0.02f, 3.2f);
        var mm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mm.SetColor("_BaseColor", new Color(0.1f, 0.8f, 0.2f)); mm.EnableKeyword("_EMISSION"); mm.SetColor("_EmissionColor", new Color(0.1f, 1f, 0.2f) * 2f);
        AssetDatabase.CreateAsset(mm, "Assets/Scenes/MarcaBanco.mat");
        marca.GetComponent<Renderer>().sharedMaterial = mm;

        // placa BANCO na fachada (texto virado pra avenida)
        var placa = new GameObject("PlacaBanco", typeof(TextMeshPro));
        placa.transform.SetParent(banco.transform, false);
        placa.transform.localPosition = new Vector3(1.6f, 4.6f, 0f);
        placa.transform.rotation = Quaternion.Euler(0, 90f, 0);
        var tmp = placa.GetComponent<TextMeshPro>();
        tmp.text = "$ BANCO $";
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TiltNeon SDF.asset");
        var neon = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fonts/Neon Botao.mat");
        if (neon) tmp.fontSharedMaterial = neon;
        tmp.color = new Color(0.4f, 1f, 0.5f);
        tmp.fontSize = 14; tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(9, 2.5f);

        // luz de alarme (pisca durante o assalto)
        var alarme = new GameObject("Alarme", typeof(Light)).GetComponent<Light>();
        alarme.transform.SetParent(banco.transform, false);
        alarme.transform.localPosition = new Vector3(0.8f, 3.5f, 0);
        alarme.type = LightType.Point; alarme.color = Color.red; alarme.range = 18f; alarme.intensity = 0f;
        b.alarme = alarme;

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("GRS 1: dinheiro (HUD) + banco colocados. Atropele/soque pedestres para pegar moedas; assalte o banco com R.");
    }
}
