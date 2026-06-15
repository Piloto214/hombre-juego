using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootCooldown = 0.3f;

    private float lastShootTime;
    private int direction = 1;

    void Update()
    {
        // Leer dirección del player
        if (transform.localScale.x < 0) direction = -1;
        else direction = 1;

        // Disparar con F
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (Time.time > lastShootTime + shootCooldown)
            {
                lastShootTime = Time.time;
                Shoot();
            }
        }
    }

    void Shoot()
    {
        Debug.Log("=== SHOOT() LLAMADO ===");

        if (bulletPrefab == null)
        {
            Debug.LogError("ERROR: bulletPrefab es NULL. Arrastra SpriteBala al campo Bullet Prefab en el Inspector.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogError("ERROR: firePoint es NULL. Arrastra FirePoint al campo Fire Point en el Inspector.");
            return;
        }

        Debug.Log("Creando bala...");
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Debug.Log("Bala creada: " + bullet.name + " en posicion: " + firePoint.position);

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript == null)
        {
            Debug.LogError("ERROR: La bala no tiene script Bullet. Agregalo al prefab SpriteBala.");
            return;
        }

        Vector2 shootDirection = new Vector2(direction, 0);
        Debug.Log("Disparando hacia: " + shootDirection);

        bulletScript.Shoot(shootDirection);
        Debug.Log("=== DISPARO COMPLETADO ===");
    }
}