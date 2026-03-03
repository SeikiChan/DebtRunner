using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 12f;

    [Header("Adaptive Size / 自适应视野")]
    [LocalizedLabel("启用自适应视野")]
    [SerializeField] private bool adaptiveOrthoSize = true;
    [LocalizedLabel("设计分辨率宽度")]
    [SerializeField, Min(1)] private int designWidth = 1920;
    [LocalizedLabel("设计分辨率高度")]
    [SerializeField, Min(1)] private int designHeight = 1080;
    [LocalizedLabel("设计正交大小")]
    [SerializeField, Min(0.1f)] private float designOrthoSize = 5f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null && cam.orthographic && adaptiveOrthoSize)
        {
            // 记录编辑器中设定的正交大小作为设计值
            if (designOrthoSize <= 0.1f)
                designOrthoSize = cam.orthographicSize;
        }
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        }

        if (adaptiveOrthoSize && cam != null && cam.orthographic)
            UpdateOrthoSize();
    }

    private void UpdateOrthoSize()
    {
        float designAspect = (float)designWidth / designHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect < designAspect)
        {
            // 窗口比设计更窄（竖），需要增大 orthoSize 保证宽度不裁切
            cam.orthographicSize = designOrthoSize * (designAspect / currentAspect);
        }
        else
        {
            // 窗口比设计更宽或相同，保持设计高度
            cam.orthographicSize = designOrthoSize;
        }
    }
}
