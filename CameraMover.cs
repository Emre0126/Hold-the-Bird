using UnityEngine;
using TMPro;

public class CameraMover : MonoBehaviour
{
    public float startSpeed = 7f;
    public float maxSpeed = 30f;
    private bool isSlowingDown = false;
    public float slowDownDuration = 0.5f;
    public float slowDownTimer = 0f;
    private float initialSlowSpeed;

    public float[] speedCheckpoints = {
        7f, 7.5f, 8f, 8.5f, 9f, 9.5f, 10f, 10.5f, 11f, 11.5f,
        12f, 12.5f, 13f, 13.5f, 14f, 14.5f, 15f, 15.5f, 16f, 16.5f,
        17f, 17.5f, 18f, 18.5f, 19f, 19.5f, 20f, 20.5f, 21f, 21.5f,
        22f, 22.5f, 23f, 23.5f, 24f, 24.5f, 25f, 25.5f, 26f, 26.5f,
        27f, 27.5f, 28f, 28.5f, 29f, 29.5f, 30f, 31f, 32f, 33f, 34f, 35f,
        36f, 37f, 38f, 39f, 40f, 45f
    };

    public float[] timeCheckpoints = {
        10f, 19.8f, 28.8f, 37.2f, 45f, 52.2f, 58.8f, 64.8f, 70.2f, 75f,
        79.2f, 82.8f, 85.8f, 88.2f, 90f, 91.2f, 91.8f, 92.0f, 92.5f, 92.8f,
        93.1f, 93.9f, 94.3f, 94.7f, 95.0f, 95.4f, 95.9f, 96.3f, 96.6f, 97.0f,
        97.5f, 97.9f, 98.5f, 99.0f, 99.7f, 100.0f, 101.0f, 101.7f, 102.0f, 102.6f,
        102.9f, 103.2f, 103.6f, 104.0f, 105.0f, 106.0f, 110.0f, 130.0f, 150.0f, 170.0f,
        190.0f, 210.0f, 230.0f, 250.0f, 270.0f, 290.0f, 360.0f
    };
    public TextMeshProUGUI infoText;
    public GameObject LeaderBoard;
    private float currentSpeed;
    private float elapsedTime;
    private bool shouldMove = false;
    private Vector3 targetPosition;
    private int lastSpeedIndex = 0;

    public bool ShouldMove => shouldMove;
    public float cameraSpeed => currentSpeed;

    void Start()
    {
        currentSpeed = startSpeed;
        targetPosition = transform.position;
        elapsedTime = 0f;
    }

    public Animator cameraAnimator;

    private bool animationStarted = false;

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            RaycastHit2D hit = Physics2D.Raycast(touchPos, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("CameraTrigger"))
            {
                shouldMove = true;

                if (infoText != null)
                    infoText.gameObject.SetActive(false);

                if (LeaderBoard != null)
                    LeaderBoard.SetActive(false);
            }
        }

#if UNITY_WEBGL
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("CameraTrigger"))
            {
                shouldMove = true;

                if (infoText != null)
                    infoText.gameObject.SetActive(false);

                if (LeaderBoard != null)
                    LeaderBoard.SetActive(false);
            }
        }
#endif

        if (isSlowingDown)
        {
            slowDownTimer += Time.deltaTime;
            float t = slowDownTimer / slowDownDuration;
            currentSpeed = Mathf.Lerp(initialSlowSpeed, 0f, t);
            if (t >= 1f)
            {
                currentSpeed = 0f;
                isSlowingDown = false;
                shouldMove = false;
            }
        }
        if (shouldMove)
        {
            if (!animationStarted)
            {
                cameraAnimator.SetTrigger("CameraStart");
                animationStarted = true;
            }

            elapsedTime += Time.deltaTime;

            for (int i = 1; i < timeCheckpoints.Length; i++)
            {
                if (elapsedTime <= timeCheckpoints[i])
                {
                    float t = Mathf.InverseLerp(timeCheckpoints[i - 1], timeCheckpoints[i], elapsedTime);
                    currentSpeed = Mathf.Lerp(speedCheckpoints[i - 1], speedCheckpoints[i], t);

                    if (lastSpeedIndex != i)
                    {
                        lastSpeedIndex = i;
#if UNITY_EDITOR
                        Debug.Log($"[TIME: {Mathf.RoundToInt(elapsedTime)}s] Speed Up: {speedCheckpoints[i]}");
#endif
                    }

                    break;
                }
            }
        }
    }

    public void StartSlowDown(float duration)
    {
        slowDownDuration = duration;
        slowDownTimer = 0f;
        initialSlowSpeed = currentSpeed;
        isSlowingDown = true;
    }

    void LateUpdate()
    {
        if (shouldMove)
        {
            transform.position += Vector3.right * currentSpeed * Time.deltaTime;
        }
    }

}
