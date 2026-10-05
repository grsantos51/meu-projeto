using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapaGRS1
{
    [MenuItem("GRS 1/Colocar Mapa")]
    public static void Colocar()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        var velho = GameObject.Find("Mapa"); if (velho) Object.DestroyImmediate(velho);
        var m = new GameObject("Mapa").AddComponent<MapaGRS>();
        m.fonte = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Bangers SDF.asset");
        const string caminho = "Assets/Scenes/MapaBrilho.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (mat == null)
        {
            mat = new Material(Shader.Find("GRS1/MapaBrilho"));
            AssetDatabase.CreateAsset(mat, caminho);
        }
        m.materialMapa = mat;
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("GRS 1: mapa colocado (minimapa no canto + tecla M).");
    }

    [MenuItem("GRS 1/Debug - Ir para a Praia")]
    public static void IrPraia()
    {
        if (!Application.isPlaying) return;
        var p = GameObject.Find("Player"); if (!p) return;
        var cc = p.GetComponent<CharacterController>(); cc.enabled = false;
        p.transform.position = new Vector3(0f, 0.3f, 90f);
        p.transform.rotation = Quaternion.identity; // olhando para o mar
        cc.enabled = true;
    }
}
