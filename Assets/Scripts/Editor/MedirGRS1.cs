using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MedirGRS1
{
    [MenuItem("GRS 1/Debug - Medir Tamanhos")]
    public static void Medir()
    {
        Physics.SyncTransforms();
        var p = GameObject.Find("Player");
        if (p) Debug.Log($"GRS 1: Arissa altura = {Altura(p.transform.Find("Arissa")?.gameObject):F2} m, escala = {p.transform.Find("Arissa")?.lossyScale}");
        var c = GameObject.Find("CarroDoJogador");
        if (c) Debug.Log($"GRS 1: carro altura = {Altura(c):F2} m, comprimento = {B(c).size.z:F2} m, base do modelo y = {B(c).min.y:F2}");
        var rua = GameObject.Find("Ruas")?.transform.Cast<Transform>().FirstOrDefault(t => t.name == "road-straight");
        if (rua && Physics.Raycast(rua.position + Vector3.up * 5, Vector3.down, out var hit, 10))
            Debug.Log($"GRS 1: superficie da rua y = {hit.point.y:F2}");
        var pr = GameObject.Find("Predios");
        if (pr) Debug.Log($"GRS 1: predios altura media = {pr.transform.Cast<Transform>().Average(t => Altura(t.gameObject)):F1} m");
    }
    static Bounds B(GameObject g) { var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
    static float Altura(GameObject g) => g ? B(g).size.y : -1;
}
