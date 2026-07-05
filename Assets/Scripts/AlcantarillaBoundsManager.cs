using UnityEngine;
using Unity.Cinemachine;

public class AlcantarillaBoundsManager : MonoBehaviour
{
    [Header("Referencia a la cámara")]
    [Tooltip("El componente 'Cinemachine Confiner 2D' de tu CinemachineCamera.")]
    public CinemachineConfiner2D confiner;

    private void OnEnable()
    {
        PropVida.OnAlcantarillaDestruida += CambiarLimiteCamara;
    }

    private void OnDisable()
    {
        PropVida.OnAlcantarillaDestruida -= CambiarLimiteCamara;
    }

    private void CambiarLimiteCamara(Collider2D nuevoLimite)
    {
        if (nuevoLimite == null) return;

        // En vez de guardar el límite anterior en una variable propia,
        // lo leemos directamente del Confiner2D en el momento del cambio.
        // Esto es importante: como el sistema de muros rompibles
        // (CameraBoundsManager) también modifica este mismo Confiner2D,
        // preguntarle directamente a la cámara "¿qué límite tienes ahora?"
        // evita que los dos sistemas se desincronicen entre sí.
        Collider2D limiteAnterior = confiner.BoundingShape2D as Collider2D;

        nuevoLimite.gameObject.SetActive(true);

        if (limiteAnterior != null && limiteAnterior.gameObject != nuevoLimite.gameObject)
            limiteAnterior.gameObject.SetActive(false);

        confiner.BoundingShape2D = nuevoLimite;
        confiner.InvalidateBoundingShapeCache();
    }
}