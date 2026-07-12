using UnityEngine;
using TMPro;

public class BarraVidaUI : MonoBehaviour
{
    [Header("Referencia al Player")]
    public PlayerHealth playerHealth;

    [Header("Configuracion visual")]
    public string caracterVidaLlena = "*";
    public string caracterVidaVacia = "-";
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