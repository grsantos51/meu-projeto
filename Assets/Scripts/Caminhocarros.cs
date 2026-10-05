using UnityEngine;

// Coloque num GameObject vazio. Os FILHOS dele são os waypoints, na ordem da hierarquia.
public class CaminhoCarros : MonoBehaviour
{
    public bool loop = true;
    public Color corGizmo = Color.yellow;

    public int Quantidade => transform.childCount;
    public Vector3 Ponto(int i) => transform.GetChild(i).position;

    public int Proximo(int i)
    {
        int n = Quantidade;
        if (i + 1 < n) return i + 1;
        return loop ? 0 : i;
    }

    void OnDrawGizmos()
    {
        int n = transform.childCount;
        if (n == 0) return;
        Gizmos.color = corGizmo;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = transform.GetChild(i).position;
            Gizmos.DrawSphere(p, 0.6f);
            if (i + 1 < n) Gizmos.DrawLine(p, transform.GetChild(i + 1).position);
            else if (loop && n > 1) Gizmos.DrawLine(p, transform.GetChild(0).position);
        }
    }
}