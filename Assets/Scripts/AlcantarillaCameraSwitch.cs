using UnityEngine;
using Unity.Cinemachine;

public class AlcantarillaCamaraSwitch : MonoBehaviour
{
    [Header("Referencia a la cámara")]
    public CinemachineConfiner2D confiner;

    [Header("Límites de cámara")]
    [Tooltip("Polígono chico: cubre solo el interior de la alcantarilla.")]
    public Collider2D limiteDentro;

    [Tooltip("Polígono grande: cubre la alcantarilla + el tramo de pasillo alrededor.")]
    public Collider2D limiteFuera;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CambiarLimite(limiteDentro);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CambiarLimite(limiteFuera);
    }

    private void CambiarLimite(Collider2D nuevoLimite)
    {
        if (nuevoLimite == null || confiner == null) return;

        confiner.BoundingShape2D = nuevoLimite;
        confiner.InvalidateBoundingShapeCache();
    }
}