using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class AlcantarillaCamaraSwitch : MonoBehaviour
{
    [Header("Referencia a la cámara")]
    public CinemachineConfiner2D confiner;

    [Header("Límites de cámara")]
    [Tooltip("Polígono chico: cubre solo el interior de la alcantarilla.")]
    public Collider2D limiteDentro;

    [Tooltip("Polígono grande: cubre la alcantarilla + el tramo de pasillo alrededor.")]
    public Collider2D limiteFuera;

    [Header("Sincronización con el zoom")]
    [Tooltip("Debe ser igual o un poco mayor a la 'Duracion Transicion' del ConductoZoom en este mismo objeto.")]
    public float duracionReinvalidacion = 1.5f;

    private Coroutine reinvalidacionActual;

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

        // El zoom (ConductoZoom) tarda un tiempo en llegar a su tamaño
        // final. Si solo invalidamos el caché una vez, de inmediato,
        // el Confiner2D puede quedarse con un cálculo hecho para un
        // tamaño de cámara que ya no es el actual, mientras el zoom
        // sigue en marcha. Por eso reinvalidamos varias veces durante
        // ese mismo periodo, para forzar el recálculo en cada punto
        // del cambio de tamaño.
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

        // Una última invalidación al terminar, para asegurar
        // el estado final correcto.
        confiner.InvalidateBoundingShapeCache();
    }
}