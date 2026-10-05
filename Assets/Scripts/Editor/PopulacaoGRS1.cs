using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PopulacaoGRS1
{
    const string Pasta = "Assets/Personagens/NPCs";
    const string Controller = Pasta + "/PedestreController.controller";
    const float Tile = 10f; const int Cells = 13; const int Quadra = 4;
    const int PorQuadra = 4;

    [MenuItem("GRS 1/Colocar Pessoas Andando")]
    public static void Colocar()
    {
        // 1) Configura os modelos como Humanoid
        var modelos = new List<GameObject>();
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Pasta }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            ConfigurarModelo(path);
            modelos.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }
        if (modelos.Count == 0) { Debug.LogError("Nenhum personagem em " + Pasta); return; }

        // 2) Animator de pedestre (parado/andando) usando as animacoes ja corrigidas da Arissa
        var idle = Clip("Assets/Personagens/Arissa/Animacoes/Idle.fbx");
        var walk = Clip("Assets/Personagens/Arissa/Animacoes/Walking.fbx");
        AssetDatabase.DeleteAsset(Controller);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(Controller);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Ciclo", AnimatorControllerParameterType.Float);
        var st = ac.CreateBlendTreeInController("Andar", out BlendTree bt, 0);
        bt.blendParameter = "Speed"; bt.useAutomaticThresholds = false;
        var run = Clip("Assets/Personagens/Arissa/Animacoes/Running.fbx");
        bt.AddChild(idle, 0f); bt.AddChild(walk, 1.3f);
        if (run) bt.AddChild(run, 4f);   // correndo (fuga / policial perseguindo)
        st.iKOnFeet = true;
        st.cycleOffsetParameterActive = true; st.cycleOffsetParameter = "Ciclo";

        // 3) Espalha pessoas nas calcadas de cada quarteirao
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        var velho = GameObject.Find("Populacao"); if (velho) Object.DestroyImmediate(velho);
        var raiz = new GameObject("Populacao").transform;
        Physics.SyncTransforms();

        var rnd = new System.Random(5);
        float origem = -(Cells - 1) * Tile * 0.5f;
        int blocos = (Cells - 1) / Quadra, total = 0;
        for (int bi = 0; bi < blocos; bi++)
        for (int bj = 0; bj < blocos; bj++)
        {
            float x0 = origem + bi * Quadra * Tile + 4.95f, x1 = origem + (bi + 1) * Quadra * Tile - 4.95f;
            float z0 = origem + bj * Quadra * Tile + 4.95f, z1 = origem + (bj + 1) * Quadra * Tile - 4.95f;
            var cantos = new[] { new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), new Vector3(x1, 0, z1), new Vector3(x0, 0, z1) };

            for (int n = 0; n < PorQuadra; n++)
            {
                bool inverso = rnd.NextDouble() < 0.5;
                var pts = inverso ? cantos.Reverse().ToArray() : cantos.ToArray();
                int k = rnd.Next(4);
                float t = (float)rnd.NextDouble();
                var pos = Vector3.Lerp(pts[k], pts[(k + 1) % 4], t);
                pos.y = Chao(pos);

                var npc = new GameObject("Pedestre");
                npc.transform.SetParent(raiz);
                npc.transform.position = pos;
                var dir = pts[(k + 1) % 4] - pts[k]; dir.y = 0;
                npc.transform.rotation = Quaternion.LookRotation(dir);

                var prefab = modelos[rnd.Next(modelos.Count)];
                var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab, npc.transform);
                m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
                var anim = m.GetComponent<Animator>(); if (anim == null) anim = m.AddComponent<Animator>();
                anim.runtimeAnimatorController = ac;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                var col = npc.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0); col.height = 1.8f; col.radius = 0.3f;
                var rb = npc.AddComponent<Rigidbody>(); rb.isKinematic = true;

                var ped = npc.AddComponent<Pedestre>();
                ped.pontos = pts.Select(p => new Vector3(p.x, 0, p.z)).ToArray();
                ped.alvo = (k + 1) % 4;
                ped.velocidade = 1.1f + (float)rnd.NextDouble() * 0.4f;
                ped.policial = rnd.NextDouble() < 0.25;          // 1 em cada 4 e policial
                ped.valor = ped.policial ? 150 : 20 + rnd.Next(40);
                if (ped.policial) npc.name = "Policial";
                total++;
            }
        }

        int praia = PessoasNaPraia(raiz, modelos, ac, rnd);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log($"GRS 1: {total} pessoas nas calcadas + {praia} na praia ({modelos.Count} modelos diferentes).");
    }

    static float Chao(Vector3 p)
    {
        var ruas = GameObject.Find("Ruas");
        var h = Physics.RaycastAll(p + Vector3.up * 5f, Vector3.down, 10f)
            .Where(x => (ruas && x.collider.transform.IsChildOf(ruas.transform)) || x.collider.name.StartsWith("Areia"))
            .OrderBy(x => x.distance).FirstOrDefault();
        return h.collider ? h.point.y : 0.1f;
    }

    // Gente na praia: uns caminhando na beira da areia (vai e volta), outros parados perto dos guarda-sois
    static int PessoasNaPraia(Transform raiz, List<GameObject> modelos, AnimatorController ac, System.Random rnd)
    {
        var praia = GameObject.Find("Praia");
        if (praia == null) return 0;
        var grupo = new GameObject("Praia").transform; grupo.SetParent(raiz);
        const float borda = 65.4f;
        int n = 0;

        // caminhando (vai e volta ao longo da praia)
        for (int i = 0; i < 12; i++)
        {
            float z = borda + 6f + (float)rnd.NextDouble() * 22f;
            float xa = -60f + (float)rnd.NextDouble() * 60f, xb = xa + 25f + (float)rnd.NextDouble() * 35f;
            var a = new Vector3(xa, 0, z); var b = new Vector3(Mathf.Min(xb, 60f), 0, z + ((float)rnd.NextDouble() - 0.5f) * 6f);
            var pos = Vector3.Lerp(a, b, (float)rnd.NextDouble()); pos.y = Chao(pos);
            var ped = Pessoa(grupo, modelos[rnd.Next(modelos.Count)], ac, pos, Quaternion.LookRotation(b - a));
            ped.pontos = new[] { a, b }; ped.alvo = 1;
            ped.velocidade = 0.9f + (float)rnd.NextDouble() * 0.4f;
            ped.chanceParar = 0.5f;
            n++;
        }

        // parados curtindo a praia, perto dos guarda-sois, olhando pro mar
        var sois = praia.transform.Find("GuardaSois");
        if (sois)
            foreach (Transform s in sois)
            {
                if (s.name == "Toalha" || rnd.NextDouble() < 0.35) continue;
                var pos = s.position + new Vector3(((float)rnd.NextDouble() - 0.5f) * 3f, 0, -1.2f - (float)rnd.NextDouble());
                pos.y = Chao(pos);
                var rot = Quaternion.Euler(0, (float)rnd.NextDouble() * 70f - 35f, 0); // olhando pro mar (+Z)
                var ped = Pessoa(grupo, modelos[rnd.Next(modelos.Count)], ac, pos, rot);
                ped.pontos = null;
                n++;
            }
        return n;
    }

    static Pedestre Pessoa(Transform pai, GameObject prefab, AnimatorController ac, Vector3 pos, Quaternion rot)
    {
        var npc = new GameObject("Banhista");
        npc.transform.SetParent(pai);
        npc.transform.position = pos; npc.transform.rotation = rot;
        var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab, npc.transform);
        m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity;
        var anim = m.GetComponent<Animator>(); if (anim == null) anim = m.AddComponent<Animator>();
        anim.runtimeAnimatorController = ac; anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        var col = npc.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.9f, 0); col.height = 1.8f; col.radius = 0.3f;
        npc.AddComponent<Rigidbody>().isKinematic = true;
        return npc.AddComponent<Pedestre>();
    }

    static AnimationClip Clip(string p) =>
        AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));

    static void ConfigurarModelo(string path)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(path);
        if (imp.animationType == ModelImporterAnimationType.Human && imp.userData == "grs1" && !imp.importCameras) return;
        imp.importCameras = false; // alguns FBX do Mixamo trazem camera/luz junto
        imp.importLights = false;
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.importAnimation = false;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        imp.SaveAndReimport();

        // mapeamento padrao Mixamo (evita cabeca/pe torto)
        var desc = imp.humanDescription;
        var ossos = new HashSet<string>(desc.skeleton.Select(b => b.name));
        desc.human = ConfigurarArissa.MapaPadrao().Where(kv => ossos.Contains("mixamorig:" + kv.Value))
            .Select(kv => { var hb = new HumanBone { humanName = kv.Key, boneName = "mixamorig:" + kv.Value }; hb.limit.useDefaultValues = true; return hb; }).ToArray();
        imp.humanDescription = desc;

        // texturas embutidas -> pasta
        string nome = System.IO.Path.GetFileNameWithoutExtension(path);
        string pastaTex = Pasta + "/Texturas_" + nome;
        if (!AssetDatabase.IsValidFolder(pastaTex)) AssetDatabase.CreateFolder(Pasta, "Texturas_" + nome);
        imp.ExtractTextures(pastaTex);
        imp.userData = "grs1";
        imp.SaveAndReimport();
        AssetDatabase.Refresh();
        foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { pastaTex }))
        {
            var tp = AssetDatabase.GUIDToAssetPath(g);
            if (tp.ToLower().Contains("normal"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
                if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }
        }
        ((ModelImporter)AssetImporter.GetAtPath(path)).SaveAndReimport();
    }
}
