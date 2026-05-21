using UnityEngine;

// Монета: при касании игроком увеличивает счётчик в GameManager,
// спавнит VFX-префаб (частицы) и уничтожается.
[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    public GameObject pickupVfxPrefab;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Реагируем только на игрока (по тегу)
        if (!other.CompareTag("Player")) return;

        // 2. Обновляем счётчик через GameManager + всплывающий «+1»
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCoin();
            GameManager.Instance.SpawnFloatingText(
                transform.position + Vector3.up * 0.3f, "+1",
                new Color(1f, 0.85f, 0.2f));
        }

        // 3. Спавним эффект частиц (бонус — VFX-префаб)
        if (pickupVfxPrefab != null)
        {
            Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
