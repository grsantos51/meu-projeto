using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Troca o material dos kits Kenney por um com textura de detalhe e janelas que acendem.
public static class VisualGRS1
{
    const string Pasta = "Assets/Scenes/Visual";

    [MenuItem("GRS 1/Melhorar Visual (texturas + janelas)")]
    public static void Aplicar()
    {
        if (!AssetDatabase.IsValidFolder(Pasta)) AssetDatabase.CreateFolder("Assets/Scenes", "Visual");
        var shader = Shader.Find("GRS1/CidadeKenney");
        if (shader == null) { Debug.LogError("Shader GRS1/CidadeKenney nao compilou"); return; }
        var ruido = Ruido();

        // kit -> (forca do detalhe, escala, brilho, tem janelas)
        Remapear("Commercial", shader, ruido, 0.18f, 0.45f, 0.25f, true);
        Remapear("Industrial", shader, ruido, 0.24f, 0.35f, 0.15f, true);
        Remapear("Roads", shader, ruido, 0.35f, 0.6f, 0.1f, false);

        // aplica janelas conforme o ceu atual (noite = acesas)
        bool noite = RenderSettings.skybox != null && RenderSettings.skybox.name.Contains("Noite");
        Janelas(noite ? 2.5f : 0f);
        AssetDatabase.SaveAssets();
        Debug.Log("GRS 1: visual melhorado (detalhe de concreto/asfalto, sombras nos cantos, janelas " + (noite ? "acesas" : "apagadas") + ").");
    }

    public static void Janelas(float intensidade)
    {
        foreach (var k in new[] { "Commercial", "Industrial" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{Pasta}/Cidade_{k}.mat");
            if (m) { m.SetFloat("_JanelaIntensidade", intensidade); EditorUtility.SetDirty(m); }
        }
    }

    static void Remapear(string kit, Shader shader, Texture2D ruido, float forca, float escala, float brilho, bool janelas)
    {
        string pastaKit = "Assets/Kenney/" + kit;
        if (!AssetDatabase.IsValidFolder(pastaKit)) return;
        string mp = $"{Pasta}/Cidade_{kit}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, mp); }
        mat.shader = shader;
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(pastaKit + "/Textures/colormap.png"));
        mat.SetTexture("_DetailTex", ruido);
        mat.SetFloat("_DetailStrength", forca);
        mat.SetFloat("_DetailScale", escala);
        mat.SetFloat("_Smoothness", brilho);
        if (!janelas) mat.SetFloat("_JanelaChance", 0f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);

        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { pastaKit }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;
            bool mudou = false;
            foreach (var em in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
            {
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), em.name);
                imp.AddRemap(id, mat);
                mudou = true;
            }
            if (mudou) imp.SaveAndReimport();
        }
    }

    // ruido "granulado" que repete sem emenda (para concreto/asfalto)
    static Texture2D Ruido()
    {
        string p = $"{Pasta}/RuidoDetalhe.png";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t) return t;
        int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
        var rnd = new System.Random(9);
        var ondas = Enumerable.Range(0, 24).Select(i => (fx: rnd.Next(1, 9), fy: rnd.Next(1, 9), fase: (float)rnd.NextDouble() * 6.28f, amp: 1f / (1 + i * 0.25f))).ToArray();
        float soma = ondas.Sum(o => o.amp);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float u = x / (float)n * 6.2831853f, v = y / (float)n * 6.2831853f, h = 0;
            foreach (var o in ondas) h += Mathf.Sin(u * o.fx + v * o.fy + o.fase) * o.amp;
            h = h / soma * 0.5f + 0.5f;
            h = Mathf.Lerp(h, (float)rnd.NextDouble(), 0.35f); // grao fino
            tex.SetPixel(x, y, new Color(h, h, h, 1));
        }
        System.IO.File.WriteAllBytes(p, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(p);
        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        ti.wrapMode = TextureWrapMode.Repeat; ti.sRGBTexture = false; ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
}
