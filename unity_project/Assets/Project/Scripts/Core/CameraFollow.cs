using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public float smoothSpeed = 12f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Clamp (world-space map bounds)")]
    public bool useClamp = true;
    public bool clampToCameraView = true;
    public Vector2 minPosition = new Vector2(0f, -15f);
    public Vector2 maxPosition = new Vector2(27f, 0f);

    private Camera attachedCamera;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (useClamp)
        {
            float minX = minPosition.x;
            float maxX = maxPosition.x;
            float minY = minPosition.y;
            float maxY = maxPosition.y;

            if (clampToCameraView && attachedCamera != null && attachedCamera.orthographic)
            {
                float verticalHalf = attachedCamera.orthographicSize;
                float horizontalHalf = verticalHalf * attachedCamera.aspect;
                minX += horizontalHalf;
                maxX -= horizontalHalf;
                minY += verticalHalf;
                maxY -= verticalHalf;
            }

            if (maxX < minX)
            {
                float centerX = (minPosition.x + maxPosition.x) * 0.5f;
                minX = centerX;
                maxX = centerX;
            }

            if (maxY < minY)
            {
                float centerY = (minPosition.y + maxPosition.y) * 0.5f;
                minY = centerY;
                maxY = centerY;
            }

            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime);
    }
}
