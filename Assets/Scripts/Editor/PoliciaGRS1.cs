using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoliciaGRS1
{
    [MenuItem("GRS 1/Colocar Policia (procurado + perseguicao)")]
    public static void Colocar()
    {
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);

        // 1) NavMesh (mapa de caminhos) para viaturas e policiais
        foreach (var nome in new[] { "Carros", "Populacao", "Player", "Noite", "Banco", "Dinheiro", "Procurado" })
        {
            var g = GameObject.Find(nome); if (g == null) continue;
            var mod = g.GetComponent<NavMeshModifier>();
            if (mod == null) mod = g.AddComponent<NavMeshModifier>();
            mod.ignoreFromBuild = true; mod.applyToChildren = true;
        }
        var nav = GameObject.Find("NavMesh") ?? new GameObject("NavMesh");
        var surf = nav.GetComponent<NavMeshSurface>(); if (surf == null) surf = nav.AddComponent<NavMeshSurface>();
        surf.collectObjects = CollectObjects.All;
        surf.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surf.BuildNavMesh();
        const string np = "Assets/Scenes/Jogo_NavMesh.asset";
        AssetDatabase.DeleteAsset(np);
        AssetDatabase.CreateAsset(surf.navMeshData, np);

        // 2) Gerente de procurado + viatura modelo
        var velho = GameObject.Find("Procurado"); if (velho) Object.DestroyImmediate(velho);
        var pg = new GameObject("Procurado");
        var proc = pg.AddComponent<Procurado>();
        var policia = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kenney/Cars/police.fbx");
        var modelo = CarrosGRS1.CriarCarro(policia, pg.transform, new Vector3(0, -50, 0), 0f, false);
        modelo.name = "ViaturaModelo";
        modelo.SetActive(false);
        proc.modeloViatura = modelo;
        var modPg = pg.AddComponent<NavMeshModifier>(); modPg.ignoreFromBuild = true; modPg.applyToChildren = true;

        // 3) Assaltar pedestre com R
        var player = GameObject.Find("Player");
        if (player && player.GetComponent<AssaltoPedestre>() == null) player.AddComponent<AssaltoPedestre>();

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log("GRS 1: policia pronta (NavMesh + estrelas + viaturas + captura). R perto de pedestre = assalto.");
    }
}
