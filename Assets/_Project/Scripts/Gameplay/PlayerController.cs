using UnityEngine;

// Контроллер игрока: горизонтальное движение, прыжок с двойным прыжком,
// проверка земли через OverlapCircle + LayerMask, простая покадровая анимация.
// Тач-управление с экранных кнопок — через публичные SetTouchMove / RequestJump.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    // 1. Параметры движения и прыжка
    [Header("Движение")]
    public float moveSpeed = 7f;
    public float jumpForce = 14f;
    public int maxJumps = 2;

    // 2. Параметры проверки земли
    [Header("Проверка земли")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    // 3. Спрайты для покадровой анимации
    [Header("Спрайты")]
    public Sprite standSprite;
    public Sprite walk1Sprite;
    public Sprite walk2Sprite;
    public Sprite jumpSprite;
    public float walkFrameDuration = 0.15f;

    // 4. Аудио прыжка
    [Header("Аудио")]
    public AudioClip jumpClip;

    // 5. Внутреннее состояние
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private AudioSource audioSource;
    private int jumpsLeft;
    private bool isGrounded;
    private float horizontalInput;
    private float touchMoveInput;
    private bool jumpRequested;
    private float walkFrameTimer;
    private int walkFrame;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        // На паузе (Time.timeScale=0) не читаем ввод — иначе после возобновления
        // игрок «вспомнит» зажатое направление и побежит сам.
        if (Time.timeScale == 0f) return;

        // 6. Клавиатура; тач-движение перебивает, если кнопка зажата
        float keyboard = Input.GetAxisRaw("Horizontal");
        horizontalInput = Mathf.Abs(touchMoveInput) > 0.01f
            ? touchMoveInput : keyboard;

        if (Input.GetKeyDown(KeyCode.Space) && jumpsLeft > 0)
        {
            jumpRequested = true;
        }

        // 7. Отражение спрайта по направлению движения
        if (horizontalInput > 0.01f) sr.flipX = false;
        else if (horizontalInput < -0.01f) sr.flipX = true;

        UpdateSprite();
    }

    void FixedUpdate()
    {
        // 8. Проверка земли через OverlapCircle с маской слоя Ground
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position, groundCheckRadius, groundLayer);

        // 9. Сброс счётчика прыжков при касании земли (только при приземлении)
        if (isGrounded && !wasGrounded)
        {
            jumpsLeft = maxJumps;
        }

        // 10. Горизонтальная скорость
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed, rb.linearVelocity.y);

        // 11. Прыжок (декремент счётчика)
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsLeft--;
            jumpRequested = false;
            if (jumpClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpClip);
            }
        }
    }

    // 12. Публичный API для экранных кнопок (TouchButton.cs)
    public void SetTouchMove(float value)
    {
        touchMoveInput = Mathf.Clamp(value, -1f, 1f);
    }

    public void RequestJump()
    {
        if (jumpsLeft > 0) jumpRequested = true;
    }

    void UpdateSprite()
    {
        // 13. Выбор спрайта по состоянию
        if (!isGrounded && jumpSprite != null)
        {
            sr.sprite = jumpSprite;
            walkFrameTimer = 0f;
            return;
        }

        if (Mathf.Abs(horizontalInput) > 0.01f
            && walk1Sprite != null && walk2Sprite != null)
        {
            walkFrameTimer += Time.deltaTime;
            if (walkFrameTimer >= walkFrameDuration)
            {
                walkFrame = 1 - walkFrame;
                walkFrameTimer = 0f;
            }
            sr.sprite = walkFrame == 0 ? walk1Sprite : walk2Sprite;
        }
        else if (standSprite != null)
        {
            sr.sprite = standSprite;
            walkFrameTimer = 0f;
        }
    }

    // 14. Визуализация ground-check в Editor (помогает при отладке)
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
