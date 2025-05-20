using UnityEngine;

public class DragToMove : MonoBehaviour
{
    [Header("SmoothDamp Settings")]
    public float maxSpeed = 5f;
    public float smoothTime = 0.2f;
    private Camera cam;
    private Vector3 targetPos;
    private Vector3 velocity = Vector3.zero;
    private bool isMoving = false;
    private bool dragging = false;
    public bool inputBlocked = false;

    void Awake()
    {
        cam = Camera.main;
    }
    void Update()
    {
        if (inputBlocked) return;

#if UNITY_WEBGL
        HandleMouse();
#else
    if (Application.isMobilePlatform)
        HandleTouch();
    else
        HandleMouse();
#endif

        if (isMoving && dragging)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref velocity,
                smoothTime,
                maxSpeed
            );

            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                isMoving = false;
                velocity = Vector3.zero;
            }
        }
    }
    void LateUpdate()
    {
        if (inputBlocked && Input.GetMouseButton(0))
        {
            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(wp, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                PauseManager.wasDraggingDuringCountdown = true;
            }
        }
    }


    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(wp, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
                dragging = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
        if (Input.GetMouseButton(0) && dragging)
        {
            UpdateTarget(Input.mousePosition);
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 wp = cam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(wp, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (Time.timeScale == 0f && !PauseManager.wasDraggingDuringCountdown)
                {
                    PauseManager.wasDraggingDuringCountdown = true;
                }
            }
        }

    }
    public void ResumeDragging()
    {
#if UNITY_WEBGL
        if (Input.GetMouseButton(0))
#else
    if (Input.touchCount > 0)
#endif
        {
            dragging = true;
        }
    }

    private void HandleTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch touchInput = Input.GetTouch(0);
            Vector3 worldPos = cam.ScreenToWorldPoint(touchInput.position);


            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (Time.timeScale == 0f && !PauseManager.wasDraggingDuringCountdown)
                {
                    PauseManager.wasDraggingDuringCountdown = true;
                }
            }
        }


        if (Input.touchCount == 0) return;
        Touch t = Input.GetTouch(0);
        Vector2 sp = t.position;
        Vector3 wp = cam.ScreenToWorldPoint(sp);
        switch (t.phase)
        {
            case TouchPhase.Began:
                RaycastHit2D hit = Physics2D.Raycast(wp, Vector2.zero);
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                    dragging = true;
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (dragging)
                    UpdateTarget(sp);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                dragging = false;
                break;
        }
    }

    private void UpdateTarget(Vector2 screenPos)
    {
        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        world.z = transform.position.z;

        targetPos = ClampTargetPosition(world);
        isMoving = true;
    }

    private Vector3 ClampTargetPosition(Vector3 pos)
    {
        Vector3 clamped = pos;

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

        float halfWidth = 0.5f;
        float halfHeight = 0.5f;

        clamped.x = Mathf.Clamp(clamped.x, bottomLeft.x + halfWidth, topRight.x - halfWidth);
        clamped.y = Mathf.Clamp(clamped.y, bottomLeft.y + halfHeight, topRight.y - halfHeight);
        return clamped;
    }

}