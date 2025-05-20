using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using System.Collections;
using UnityEngine.UI;
public class PlayerCollisionHandler : MonoBehaviour
{
    public MonoBehaviour[] scriptsToDisable;
    public float fallSpeed = 1f;
    private bool hasCrashed = false;
    private Vector3 crashPos;
    public CameraMover cameraMover;
    public GameObject PauseButton;
    public GameObject Leaderboard;
    private InterstitialAd interstitialAd;
    private int collisionCount;
    private int nextAdThreshold;
    private Quaternion targetRotation;
    public float rotationSpeed = 180f;
    public Button restartButton;
    private PauseManager pauseManager;

    void Start()
    {
        pauseManager = FindObjectOfType<PauseManager>();

        MobileAds.Initialize(initStatus => { });
        RequestInterstitialAd();

        collisionCount = PlayerPrefs.GetInt("CollisionCount", 0);
        nextAdThreshold = PlayerPrefs.GetInt("NextAdThreshold", UnityEngine.Random.Range(4, 7));

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
    }


    void Update()
    {
        if (hasCrashed)
        {
            crashPos.y -= fallSpeed * Time.deltaTime;
            transform.position = crashPos;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasCrashed) return;

        if (collision.collider.CompareTag("Obstacle"))
        {

            hasCrashed = true;
            pauseManager?.ForceStopMusic();

            if (cameraMover != null)
            {
                cameraMover.StartSlowDown(0.1f);
            }
            PauseButton.SetActive(false);
            Leaderboard.SetActive(true);
            ScoreManager.Instance.canScore = false;
            ScoreManager.Instance.scoreText.text = "Score: " + ScoreManager.Instance.GetScore();
            ScoreManager.Instance.scoreText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            ScoreManager.Instance.highScoreText.rectTransform.anchoredPosition = new Vector2(0f, -200f);
            ScoreManager.Instance.ReportHighScoreToLeaderboard();
            ScoreManager.Instance.highScoreText.gameObject.SetActive(true);
            foreach (var script in scriptsToDisable)
            {
                script.enabled = false;
            }

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;
            }

            targetRotation = Quaternion.Euler(0f, 0f, 90f);

            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            crashPos = transform.position;

            CountCrashAndMaybeShowAd();

        }
    }

    private void CountCrashAndMaybeShowAd()
    {
        collisionCount++;
        PlayerPrefs.SetInt("CollisionCount", collisionCount);

        if (collisionCount >= nextAdThreshold)
        {
            collisionCount = 0;
            nextAdThreshold = UnityEngine.Random.Range(4, 7);
            PlayerPrefs.SetInt("CollisionCount", collisionCount);
            PlayerPrefs.SetInt("NextAdThreshold", nextAdThreshold);

            StartCoroutine(ShowAdAndEnableRestartButton());
        }
        else
        {
            EnableRestartButton();
        }
    }

    private void EnableRestartButton()
    {
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }
    private IEnumerator ShowAdAndEnableRestartButton()
    {
        yield return new WaitForSeconds(0.5f);

        if (interstitialAd != null)
        {
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                EnableRestartButton();
            };

            interstitialAd.Show();
            interstitialAd = null;
        }
        else
        {
            EnableRestartButton();
        }
    }
    public void RestartSceneManually()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    private IEnumerator RestartSceneAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator ShowAdThenRestartAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        if (interstitialAd != null)
        {
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            };

            interstitialAd.Show();
            interstitialAd = null;
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }



    private void RequestInterstitialAd()
    {
#if UNITY_ANDROID
        string adUnitId = "ca-app-pub-8869277409030391/4542816987"; // Gerçek ID ile deðiþtir
#elif UNITY_IOS
        string adUnitId = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
#else
        string adUnitId = "unexpected_platform";
#endif

        AdRequest adRequest = new AdRequest();

        InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("InterstitialAd failed to load: " + error);
                return;
            }

            interstitialAd = ad;

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial Ad closed.");
            };

            Debug.Log("Interstitial Ad loaded.");
        });
    }


}
