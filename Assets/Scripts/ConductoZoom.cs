using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class ConductoZoom : MonoBehaviour
{
    [Header("Referencia a la cámara")]
    [Tooltip("El objeto CinemachineCamera de tu escena.")]
    public CinemachineCamera camaraCinemachine;

    [Header("Valores de Zoom")]
    [Tooltip("Tamaño normal de la cámara, fuera del conducto (ej. 6).")]
    public float sizeNormal = 6f;

    [Tooltip("Tamaño de la cámara dentro del conducto (ej. 3).")]
    public float sizeConducto = 3f;

    [Tooltip("Qué tan rápido ocurre la transición del zoom, en segundos.")]
    public float duracionTransicion = 0.5f;

    [Header("Curva de suavizado")]
    public AnimationCurve curvaEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine transicionActual;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (camaraCinemachine == null)
        {
            Debug.LogError($"[{gameObject.name}] ¡CamaraCinemachine no está asignada!", this);
            return;
        }

        // Solo controlamos el zoom. El límite de cámara (Confiner2D)
        // es responsabilidad exclusiva de CameraBoundsManager, para
        // evitar que dos sistemas distintos se pisen entre sí.
        IniciarTransicion(sizeConducto);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (camaraCinemachine == null) return;

        IniciarTransicion(sizeNormal);
    }

    private void IniciarTransicion(float sizeObjetivo)
    {
        if (transicionActual != null)
            StopCoroutine(transicionActual);

        transicionActual = StartCoroutine(TransicionSize(sizeObjetivo));
    }

    private IEnumerator TransicionSize(float sizeObjetivo)
    {
        float sizeInicial = camaraCinemachine.Lens.OrthographicSize;
        float tiempo = 0f;

        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracionTransicion);
            float progresoSuavizado = curvaEasing.Evaluate(progreso);

            LensSettings lens = camaraCinemachine.Lens;
            lens.OrthographicSize = Mathf.Lerp(sizeInicial, sizeObjetivo, progresoSuavizado);
            camaraCinemachine.Lens = lens;

            yield return null;
        }

        LensSettings lensFinal = camaraCinemachine.Lens;
        lensFinal.OrthographicSize = sizeObjetivo;
        camaraCinemachine.Lens = lensFinal;
    }
}