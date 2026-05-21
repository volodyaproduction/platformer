using UnityEngine;

// Триггер финиша: при касании игроком завершает раунд (один из трёх способов).
[RequireComponent(typeof(Collider2D))]
public class FinishZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance != null)
            GameManager.Instance.EndRound(GameManager.EndReason.Finished);
    }
}
