using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public string cenaDoJogo = "Jogo";

    public void Jogar()
    {
        SceneManager.LoadScene(cenaDoJogo);
    }

    public void Sair()
    {
        Debug.Log("Saiu do jogo");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
