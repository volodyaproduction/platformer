using UnityEngine;

// Триггер финиша: при касании игроком вызывает GameManager.Win().
[RequireComponent(typeof(Collider2D))]
public class FinishZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Win();
        }
    }
}
