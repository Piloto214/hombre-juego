using UnityEngine;
using Unity.Cinemachine;

public class CameraBoundsManager : MonoBehaviour
{
    [Header("Referencia a la cámara")]
    [Tooltip("El componente 'Cinemachine Confiner 2D' que ya agregaste a tu CinemachineCamera.")]
    public CinemachineConfiner2D confiner;

    [Header("Límite inicial")]
    [Tooltip("El polígono que la cámara debe usar desde el inicio (ej. Bounds_Etapa1). Debe ser el mismo objeto que ya tienes asignado en el campo 'Bounding Shape 2D' del Confiner2D.")]
    public GameObject limiteInicial;

    // Guarda internamente cuál es el polígono activo en este momento,
    // para poder desactivarlo cuando cambiemos al siguiente.
    private GameObject limiteActivoActual;

    private void OnEnable()
    {
        // Nos "suscribimos" al evento de PropVida: cada vez que CUALQUIER
        // rejilla en la escena se rompa, este script se entera automáticamente.
        PropVida.OnRejillaDestruida += CambiarLimiteCamara;
    }

    private void OnDisable()
    {
        // Buena práctica: cancelar la suscripción si este objeto se desactiva,
        // para evitar errores si la escena se recarga.
        PropVida.OnRejillaDestruida -= CambiarLimiteCamara;
    }

    private void Start()
    {
        limiteActivoActual = limiteInicial;
    }

    private void CambiarLimiteCamara(Collider2D nuevoLimite)
    {
        // Seguridad: si una rejilla no tiene asignado un nuevo límite
        // (campo vacío por error), no hacemos nada en vez de fallar.
        if (nuevoLimite == null) return;

        // 1. Activamos el nuevo polígono (estaba desactivado en la escena).
        nuevoLimite.gameObject.SetActive(true);

        // 2. Desactivamos el polígono anterior, ya no se necesita.
        if (limiteActivoActual != null)
            limiteActivoActual.SetActive(false);

        // 3. Le decimos al Confiner2D que use el nuevo polígono.
        confiner.BoundingShape2D = nuevoLimite;

        // 4. Paso crítico: Cinemachine calcula y guarda internamente una
        // versión "cacheada" (pre-calculada) del polígono anterior por
        // rendimiento. Si no invalidamos ese caché, la cámara seguirá
        // usando el límite viejo aunque ya le hayamos asignado uno nuevo.
        confiner.InvalidateBoundingShapeCache();

        // 5. Actualizamos cuál es el límite activo, para la próxima vez.
        limiteActivoActual = nuevoLimite.gameObject;
    }
}