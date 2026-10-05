using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurarArissa
{
    const string Pasta = "Assets/Personagens/Arissa";
    const string Modelo = Pasta + "/Arissa.fbx";
    const string PastaAnim = Pasta + "/Animacoes";
    const string Controller = Pasta + "/ArissaController.controller";

    [MenuItem("GRS 1/Colocar Arissa como Player")]
    public static void Configurar()
    {
        // 1) Modelo: Humanoid + extrair texturas embutidas
        var imp = (ModelImporter)AssetImporter.GetAtPath(Modelo);
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.importAnimation = false;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        imp.SaveAndReimport();
        CorrigirMapeamento(imp);
        string pastaTex = Pasta + "/Texturas";
        if (!AssetDatabase.IsValidFolder(pastaTex)) AssetDatabase.CreateFolder(Pasta, "Texturas");
        imp.ExtractTextures(pastaTex);
        AssetDatabase.Refresh();
        foreach (var t in AssetDatabase.FindAssets("t:Texture2D", new[] { pastaTex }))
        {
            var tp = AssetDatabase.GUIDToAssetPath(t);
            if (tp.ToLower().Contains("nm_normal"))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
                if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
            }
        }
        imp = (ModelImporter)AssetImporter.GetAtPath(Modelo);
        imp.SaveAndReimport();

        var avatar = AssetDatabase.LoadAllAssetsAtPath(Modelo).OfType<Avatar>().FirstOrDefault();

        // 2) Animacoes: Humanoid usando o avatar da Arissa, no lugar (sem root motion)
        AnimationClip idle = ConfigClip("Idle", avatar, true);
        AnimationClip walk = ConfigClip("Walking", avatar, true);
        AnimationClip run = ConfigClip("Running", avatar, true);
        AnimationClip jump = ConfigClip("Jump", avatar, false);
        AnimationClip soco = File.Exists(PastaAnim + "/Punching.fbx") ? ConfigClip("Punching", avatar, false) : null;

        // 3) Animator Controller
        AssetDatabase.DeleteAsset(Controller);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(Controller);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Soco", AnimatorControllerParameterType.Trigger);
        var sm = ac.layers[0].stateMachine;

        var locomocao = ac.CreateBlendTreeInController("Locomocao", out BlendTree bt, 0);
        bt.blendParameter = "Speed";
        bt.useAutomaticThresholds = false;
        bt.AddChild(idle, 0f);
        bt.AddChild(walk, 5f);
        bt.AddChild(run, 9f);
        sm.defaultState = locomocao;
        locomocao.iKOnFeet = true; // corrige pe torto vindo do retarget do Mixamo

        var pulo = sm.AddState("Pulo");
        pulo.motion = jump;
        pulo.iKOnFeet = true;
        var ida = sm.AddAnyStateTransition(pulo);
        ida.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        ida.duration = 0.1f; ida.canTransitionToSelf = false;
        var volta = pulo.AddTransition(locomocao);
        volta.hasExitTime = true; volta.exitTime = 0.8f; volta.duration = 0.2f;

        if (soco)
        {
            var golpe = sm.AddState("Soco");
            golpe.motion = soco; golpe.iKOnFeet = true; golpe.speed = 1.4f;
            var ir = sm.AddAnyStateTransition(golpe);
            ir.AddCondition(AnimatorConditionMode.If, 0, "Soco"); ir.duration = 0.08f; ir.canTransitionToSelf = false;
            var sair = golpe.AddTransition(locomocao);
            sair.hasExitTime = true; sair.exitTime = 0.85f; sair.duration = 0.15f;
        }

        // 4) Colocar no Player da cena Jogo
        var cena = EditorSceneManager.OpenScene("Assets/Scenes/Jogo.unity", OpenSceneMode.Single);
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("Player nao encontrado na cena Jogo"); return; }

        var mf = player.GetComponent<MeshFilter>(); if (mf != null) Object.DestroyImmediate(mf);
        var mr = player.GetComponent<MeshRenderer>(); if (mr != null) Object.DestroyImmediate(mr);
        var velho = player.transform.Find("Arissa"); if (velho != null) Object.DestroyImmediate(velho.gameObject);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Modelo);
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
        modelo.name = "Arissa";
        var cc = player.GetComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.3f;
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.identity;

        var anim = modelo.GetComponent<Animator>();
        if (anim == null) anim = modelo.AddComponent<Animator>();
        anim.runtimeAnimatorController = ac;
        anim.avatar = avatar;
        anim.applyRootMotion = false;

        var ps = player.GetComponent<PlayerSimples>();
        if (ps != null) ps.offsetCamera = new Vector3(0, 2.0f, -4f);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log("GRS 1: Arissa configurada como Player (Idle/Andar/Correr/Pular).");
    }

    // O mapeamento automatico da Arissa erra alguns ossos (ex.: Head -> Neck1),
    // o que entorta pe/cabeca no retarget. Aqui forcamos o mapeamento padrao do Mixamo.
    static void CorrigirMapeamento(ModelImporter imp)
    {
        var mapa = MapaPadrao();
        var desc = imp.humanDescription;
        var ossos = new HashSet<string>(desc.skeleton.Select(b => b.name));
        foreach (var h in desc.human)
            if (h.humanName.Contains("Foot") || h.humanName.Contains("Toes") || h.humanName == "Head" || h.humanName == "Neck")
                Debug.Log($"GRS 1: mapeamento antigo {h.humanName} -> {h.boneName}");

        var lista = new List<HumanBone>();
        foreach (var par in mapa)
        {
            string osso = "mixamorig:" + par.Value;
            if (!ossos.Contains(osso)) continue;
            var hb = new HumanBone { humanName = par.Key, boneName = osso };
            hb.limit.useDefaultValues = true;
            lista.Add(hb);
        }
        desc.human = lista.ToArray();
        imp.humanDescription = desc;
        imp.SaveAndReimport();
        Debug.Log($"GRS 1: mapeamento da Arissa corrigido ({lista.Count} ossos).");
    }

    public static Dictionary<string, string> MapaPadrao()
    {
        var mapa = new Dictionary<string, string>
        {
            {"Hips","Hips"},{"Spine","Spine"},{"Chest","Spine1"},{"UpperChest","Spine2"},{"Neck","Neck"},{"Head","Head"},
            {"LeftUpperLeg","LeftUpLeg"},{"LeftLowerLeg","LeftLeg"},{"LeftFoot","LeftFoot"},{"LeftToes","LeftToeBase"},
            {"RightUpperLeg","RightUpLeg"},{"RightLowerLeg","RightLeg"},{"RightFoot","RightFoot"},{"RightToes","RightToeBase"},
            {"LeftShoulder","LeftShoulder"},{"LeftUpperArm","LeftArm"},{"LeftLowerArm","LeftForeArm"},{"LeftHand","LeftHand"},
            {"RightShoulder","RightShoulder"},{"RightUpperArm","RightArm"},{"RightLowerArm","RightForeArm"},{"RightHand","RightHand"},
        };
        string[] dedos = { "Thumb", "Index", "Middle", "Ring", "Little" };
        string[] partes = { "Proximal", "Intermediate", "Distal" };
        foreach (var lado in new[] { "Left", "Right" })
            for (int d = 0; d < dedos.Length; d++)
                for (int k = 0; k < 3; k++)
                {
                    string mix = dedos[d] == "Little" ? "Pinky" : dedos[d];
                    mapa[$"{lado} {dedos[d]} {partes[k]}"] = $"{lado}Hand{mix}{k + 1}";
                }

        return mapa;
    }

    static AnimationClip ConfigClip(string nome, Avatar avatar, bool loop)
    {
        string path = PastaAnim + "/" + nome + ".fbx";
        var imp = (ModelImporter)AssetImporter.GetAtPath(path);
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.importAnimation = true;
        imp.materialImportMode = ModelImporterMaterialImportMode.None;
        var clips = imp.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].name = nome;
            clips[i].loopTime = loop;
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            // XZ NAO vai para a pose: o deslocamento vira root motion e e ignorado
            // (applyRootMotion = false), assim ela anda "no lugar" sem dar tranco pra frente
            clips[i].lockRootPositionXZ = false;
            clips[i].keepOriginalOrientation = true;
            clips[i].keepOriginalPositionY = true;
            clips[i].keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();

        // As animacoes do Mixamo "sem skin" nao vem em T-pose: o Avatar delas era criado
        // numa pose torta e o pe esquerdo saia virado. Usamos a T-pose da Arissa como pose
        // de referencia (mesmo esqueleto Mixamo) e o mapeamento padrao.
        var arissaImp = (ModelImporter)AssetImporter.GetAtPath(Modelo);
        var tpose = arissaImp.humanDescription.skeleton.ToDictionary(b => b.name, b => b.rotation);
        var desc = imp.humanDescription;
        var esqueleto = desc.skeleton;
        for (int i = 0; i < esqueleto.Length; i++)
            if (esqueleto[i].name.StartsWith("mixamorig:") && esqueleto[i].name != "mixamorig:Head" && tpose.TryGetValue(esqueleto[i].name, out var r))
                esqueleto[i].rotation = r;
        desc.skeleton = esqueleto;
        var nomes = new HashSet<string>(esqueleto.Select(b => b.name));
        desc.human = MapaPadrao().Where(kv => nomes.Contains("mixamorig:" + kv.Value))
            .Select(kv => { var hb = new HumanBone { humanName = kv.Key, boneName = "mixamorig:" + kv.Value }; hb.limit.useDefaultValues = true; return hb; })
            .ToArray();
        imp.humanDescription = desc;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
    }
}
