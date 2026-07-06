using UnityEngine;
using TMPro;

public class BarraVidaUI : MonoBehaviour
{
    [Header("Referencia al Player")]
    public PlayerHealth playerHealth;

    [Header("Configuración visual")]
    public string caracterVidaLlena = "*";   // asterisco en vez de punto
    public string caracterVidaVacia = "-";   // guion en vez de circulo
    public string prefijo = "VIDA: ";

    [Header("Referencia UI")]
    public TextMeshProUGUI textoVida;

    void Update()
    {
        if (playerHealth == null || textoVida == null) return;

        int vidaActual = playerHealth.VidasActuales;
        int vidaMaxima = playerHealth.VidasMaximas;

        string resultado = prefijo;

        for (int i = 0; i < vidaMaxima; i++)
        {
            if (i < vidaActual)
                resultado += caracterVidaLlena;
            else
                resultado += caracterVidaVacia;
        }

        textoVida.text = resultado;
    }
}