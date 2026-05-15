using UnityEngine;

// Камера следит ТОЛЬКО по горизонтали. Y и Z зафиксированы, поэтому
// при прыжке игрока земля всегда остаётся в кадре. Cinemachine с
// damping=3 на Y всё равно медленно «всплывал» — здесь жёсткая фиксация.
public class CameraFollowX : MonoBehaviour
{
    public Transform target;
    public float fixedY = 3f;
    public float fixedZ = -10f;

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3(target.position.x, fixedY, fixedZ);
    }
}
