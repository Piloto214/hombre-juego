using UnityEngine;

public class ArenaGate : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Collider2D físico que bloquea el paso cuando la reja está cerrada (NO debe ser Trigger).")]
    [SerializeField] private Collider2D colliderBloqueo;

    [Tooltip("Opcional: SpriteRenderer de la reja, para mostrarla/ocultarla visualmente.")]
    [SerializeField] private SpriteRenderer spriteReja;

    public bool EstaCerrada { get; private set; } = false;

    private void Awake()
    {
        Abrir();
    }

    public void Cerrar()
    {
        EstaCerrada = true;

        if (colliderBloqueo != null)
            colliderBloqueo.enabled = true;

        if (spriteReja != null)
            spriteReja.enabled = true;

        Debug.Log("ArenaGate: reja CERRADA.");
    }

    public void Abrir()
    {
        EstaCerrada = false;

        if (colliderBloqueo != null)
            colliderBloqueo.enabled = false;

        if (spriteReja != null)
            spriteReja.enabled = false;

        Debug.Log("ArenaGate: reja ABIERTA.");
    }
}