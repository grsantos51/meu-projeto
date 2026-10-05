using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DebugCameraGRS1
{
    static float t;

    [MenuItem("GRS 1/Debug - Ver Arissa de Perto")]
    public static void VerArissa()
    {
        var p = GameObject.Find("Player");
        var sv = SceneView.lastActiveSceneView;
        if (p == null || sv == null) return;
        sv.sceneViewState.showFog = false;
        var rot = Quaternion.LookRotation(-p.transform.forward + Vector3.down * 0.2f);
        sv.LookAt(p.transform.position + Vector3.up * 0.5f, rot, 1.4f);
        sv.Repaint();
    }

    [MenuItem("GRS 1/Debug - Pose Andando (proximo frame)")]
    public static void PoseAndando()
    {
        var p = GameObject.Find("Player");
        var anim = p != null ? p.GetComponentInChildren<Animator>() : null;
        var clip = AssetDatabase.LoadAllAssetsAtPath("Assets/Personagens/Arissa/Animacoes/Walking.fbx")
            .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        if (anim == null || clip == null) return;
        if (!AnimationMode.InAnimationMode()) AnimationMode.StartAnimationMode();
        t = (t + 0.1f) % clip.length;
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(anim.gameObject, clip, t);
        AnimationMode.EndSampling();
        VerArissa();
    }

    [MenuItem("GRS 1/Ver Praia")]
    public static void VerPraia()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        sv.sceneViewState.showFog = false;
        sv.LookAt(new Vector3(0, 2f, 80f), Quaternion.Euler(18f, 200f, 0f), 35f);
        sv.Repaint();
    }

    [MenuItem("GRS 1/Debug - Derrubar Pedestre Mais Perto")]
    public static void Derrubar()
    {
        if (!Application.isPlaying) { Debug.Log("Use durante o Play"); return; }
        var p = GameObject.Find("Player");
        var alvo = Object.FindObjectsByType<Pedestre>(FindObjectsSortMode.None).Where(x => !x.morto)
            .OrderBy(x => Vector3.Distance(x.transform.position, p.transform.position)).FirstOrDefault();
        if (alvo == null) return;
        // traz o pedestre pra frente da Arissa e derruba (testa queda + moedas)
        alvo.transform.position = p.transform.position + p.transform.forward * 2.5f;
        alvo.Morrer(p.transform.forward * 4f);
        Debug.Log("GRS 1 teste: derrubou " + alvo.name + " (valor " + alvo.valor + ")");
        var sv = SceneView.lastActiveSceneView;
    }

    [MenuItem("GRS 1/Debug - Ir para o Banco")]
    public static void IrBanco()
    {
        if (!Application.isPlaying) return;
        var p = GameObject.Find("Player"); var b = GameObject.Find("Banco");
        if (!p || !b) return;
        var cc = p.GetComponent<CharacterController>(); cc.enabled = false;
        p.transform.position = b.transform.position + Vector3.up * 0.1f;
        p.transform.rotation = Quaternion.Euler(0, -90f, 0);
        cc.enabled = true;
    }

    [MenuItem("GRS 1/Debug - Ganhar 3 Estrelas")]
    public static void Estrelas()
    {
        if (Application.isPlaying && Procurado.Instancia) Procurado.Instancia.Crime(3);
    }

    [MenuItem("GRS 1/Debug - Parar Pose")]
    public static void Parar()
    {
        if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        t = 0;
    }
}
