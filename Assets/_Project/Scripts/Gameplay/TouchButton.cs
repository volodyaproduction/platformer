using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

// Экранная кнопка тач-управления. Прыжок — на нажатие, движение — пока зажато.
public class TouchButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    public enum Action { Left, Right, Jump }

    public PlayerController player;
    public Action action;

#if UNITY_WEBGL && !UNITY_EDITOR
    // Мост в JS (Assets/_Project/Plugins/WebGL/TouchDetect.jslib).
    // Используем UA вместо Input.touchSupported: на десктопных Chrome/Safari
    // последний выдаёт false positive (тач API без сенсорного экрана).
    [DllImport("__Internal")] private static extern int IsMobileUA();
#endif

    void Awake()
    {
        // Скрываем экранные кнопки на не-тач устройствах.
        bool isTouch;
#if UNITY_WEBGL && !UNITY_EDITOR
        isTouch = IsMobileUA() == 1;
#else
        isTouch = Input.touchSupported;
#endif
        if (!isTouch) gameObject.SetActive(false);
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
