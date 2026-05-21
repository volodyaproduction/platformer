using UnityEngine;
using UnityEngine.UI;

// Всплывающий текст «+1» / «-2» при сборе монеты или ударе о ловушку.
// Поднимается вверх и затухает, после lifetime — Destroy. World-space Canvas,
// чтобы текст рисовался поверх спрайтов в мировом масштабе.
public class FloatingText : MonoBehaviour
{
    public Text text;
    public float lifetime = 0.8f;
    public float floatSpeed = 1.5f;

    float t;
    Color baseColor;

    public void Init(string message, Color color)
    {
        text.text = message;
        text.color = color;
        baseColor = color;
    }

    void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        // Затухание альфы у текста; outline (если есть) затухает синхронно
        var alpha = 1f - Mathf.Clamp01(t / lifetime);
        var c = baseColor;
        c.a = alpha;
        text.color = c;
        var outline = text.GetComponent<Outline>();
        if (outline != null)
            outline.effectColor = new Color(0f, 0f, 0f, alpha);

        if (t >= lifetime) Destroy(gameObject);
    }
}
