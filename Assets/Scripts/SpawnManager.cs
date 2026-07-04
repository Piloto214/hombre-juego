using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Enemigo a spawnear")]
    [SerializeField] private GameObject prefabEnemigo;

    [Header("Puntos de spawn")]
    [SerializeField] private Transform[] puntosSpawn;

    [Header("Waypoints de patrulla (opcional)")]
    [SerializeField] private Transform[] waypointsA;
    [SerializeField] private Transform[] waypointsB;

    [Header("Delay al spawnear")]
    [SerializeField] private float delaySpawn = 0.5f;

    private void OnEnable()
    {
        MiniBossController.OnMuerte += SpawnearEnemigos;
    }

    private void OnDisable()
    {
        MiniBossController.OnMuerte -= SpawnearEnemigos;
    }

    private void SpawnearEnemigos()
    {
        Debug.Log("MINI-BOSS MUERTO. Spawneando enemigos en " + delaySpawn + " segundos...");

        StartCoroutine(SpawnearConDelay());
    }

    private System.Collections.IEnumerator SpawnearConDelay()
    {
        yield return new WaitForSeconds(delaySpawn);

        for (int i = 0; i < puntosSpawn.Length; i++)
        {
            if (puntosSpawn[i] == null || prefabEnemigo == null) continue;

            // Spawnear el enemigo
            GameObject enemigo = Instantiate(prefabEnemigo, puntosSpawn[i].position, Quaternion.identity);

            // Asignar waypoints si existen
            EnemyPatrol patrol = enemigo.GetComponent<EnemyPatrol>();
            if (patrol != null && i < waypointsA.Length && i < waypointsB.Length)
            {
                if (waypointsA[i] != null && waypointsB[i] != null)
                {
                    patrol.AsignarWaypoints(waypointsA[i], waypointsB[i]);
                    Debug.Log("Enemigo spawneado en: " + puntosSpawn[i].name + " con waypoints asignados");
                }
            }
            else
            {
                Debug.Log("Enemigo spawneado en: " + puntosSpawn[i].name + " (sin waypoints asignados)");
            }
        }
    }
}