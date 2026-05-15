using UnityEngine;

// Триггер-зона смерти под уровнем: при касании игроком перезапускает сцену.
[RequireComponent(typeof(Collider2D))]
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
    }
}
