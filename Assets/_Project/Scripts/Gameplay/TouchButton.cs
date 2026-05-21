using UnityEngine;
using UnityEngine.EventSystems;

// Экранная кнопка тач-управления. Прыжок — на нажатие, движение — пока зажато.
public class TouchButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    public enum Action { Left, Right, Jump }

    public PlayerController player;
    public Action action;

    void Awake()
    {
        // Прячем экранные кнопки на устройствах без сенсора (десктоп-браузер).
        // Документация Unity рекомендует именно Input.touchSupported вместо
        // проверки платформы — на WebGL он корректно возвращает false на ПК
        // и true в мобильных браузерах.
        if (!Input.touchSupported) gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (player == null) return;
        switch (action)
        {
            case Action.Left:  player.SetTouchMove(-1f); break;
            case Action.Right: player.SetTouchMove(+1f); break;
            case Action.Jump:  player.RequestJump();     break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (player == null) return;
        if (action == Action.Left || action == Action.Right)
        {
            player.SetTouchMove(0f);
        }
    }
}
