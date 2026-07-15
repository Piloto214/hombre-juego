using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class BossArenaCameraSwitch : MonoBehaviour
{
    [Header("Referencia a la cámara")]
    public CinemachineConfiner2D confiner;

    [Header("Límites de cámara")]
    [Tooltip("Polígono que cubre únicamente el pasillo de entrada.")]
    public Collider2D limiteEntrada;

    [Tooltip("Polígono que cubre únicamente el área de la arena de pelea.")]
    public Collider2D limiteArena;

    [Header("Sincronización con transiciones de cámara")]
    [Tooltip("Duración durante la cual se reinvalida el caché repetidamente. Debe cubrir cualquier transición de tamaño/posición de cámara que ocurra al mismo tiempo (ej. el encuadre lateral del Diálogo).")]
    public float duracionReinvalidacion = 1.5f;

    private Coroutine reinvalidacionActual;

    // Llamado por MiniBossController al iniciar el combate (ej. en DetectarJugador,
    // justo antes o al mismo tiempo que BloquearControl()).
    public void CambiarAArena()
    {
        CambiarLimite(limiteArena);
    }

    // Llamado por MiniBossController cuando el ArenaGate se reabre
    // (boss derrotado o jugador respawnea antes de iniciar combate).
    public void CambiarAEntrada()
    {
        CambiarLimite(limiteEntrada);
    }

    private void CambiarLimite(Collider2D nuevoLimite)
    {
        if (nuevoLimite == null || confiner == null) return;

        nuevoLimite.gameObject.SetActive(true);

        Collider2D limiteAnterior = confiner.BoundingShape2D as Collider2D;
        if (limiteAnterior != null && limiteAnterior.gameObject != nuevoLimite.gameObject)
            limiteAnterior.gameObject.SetActive(false);

        confiner.BoundingShape2D = nuevoLimite;
        confiner.InvalidateBoundingShapeCache();

        // Reinvalidamos varias veces durante un breve periodo, para
        // cubrir cualquier cambio simultáneo de tamaño/posición de la
        // cámara (por ejemplo, el encuadre lateral que se activará
        // junto con este cambio de límite durante el Diálogo del boss).
        if (reinvalidacionActual != null)
            StopCoroutine(reinvalidacionActual);

        reinvalidacionActual = StartCoroutine(ReinvalidarDurante(duracionReinvalidacion));
    }

    private IEnumerator ReinvalidarDurante(float duracion)
    {
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            confiner.InvalidateBoundingShapeCache();
            tiempo += Time.deltaTime;
            yield return null;
        }

        confiner.InvalidateBoundingShapeCache();
    }
}