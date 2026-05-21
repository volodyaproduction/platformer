using UnityEngine;

// Ловушка-«огонёк»: при касании игроком отнимает penalty монет и отбрасывает
// игрока вверх + в сторону, противоположную движению. Кратковременная
// неуязвимость (invulnDuration) — чтобы knockback успел отнести от тригера
// и чтобы не задеть несколько раз подряд.
[RequireComponent(typeof(Collider2D))]
public class Trap : MonoBehaviour
{
    [Header("Параметры")]
    public int penalty = 2;
    public float knockbackHForce = 8f;
    public float knockbackVForce = 9f;
    public float invulnDuration = 0.6f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var pc = other.GetComponent<PlayerController>();
        if (pc == null || pc.IsInvulnerable) return;

        // 1. Штраф очков
        if (GameManager.Instance != null)
            GameManager.Instance.AddPenalty(penalty);

        // 2. Knockback: вверх + назад относительно текущего движения
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            float sign = rb.linearVelocity.x >= 0 ? -1f : 1f;
            rb.linearVelocity = new Vector2(sign * knockbackHForce, knockbackVForce);
        }

        // 3. Включаем неуязвимость — PlayerController.FixedUpdate во время
        //    неё не переписывает горизонтальную скорость, knockback работает.
        pc.StartInvulnerability(invulnDuration);
    }
}
