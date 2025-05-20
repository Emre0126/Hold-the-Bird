using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public Text scoreText;
    public Text highScoreText;

    private int score = 0;
    public bool canScore = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        UpdateHighScoreUI();
    }

    public int GetScore() => score;

    public void IncreaseScore()
    {
        if (!canScore) return;

        score++;
        scoreText.text = "Score: " + score;
        UpdateHighScore();
    }
    public void ReportHighScoreToLeaderboard()
    {
        long highScore = PlayerPrefs.GetInt("HighScore", 0);
        Social.ReportScore(highScore, "CgkIxoiAs4QdEAIQAg", success => {
            Debug.Log("Score gönderildi: " + success);
        });
    }
    public void ShowLeaderboard()
    {
        if (Social.localUser.authenticated)
        {
            Social.ShowLeaderboardUI();
        }
        else
        {
            Debug.Log("Oturum açýlmamýþ.");
        }
    }

    private void UpdateHighScore()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (score > highScore)
        {
            PlayerPrefs.SetInt("HighScore", score);
            UpdateHighScoreUI();
        }
    }

    private void UpdateHighScoreUI()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }
    }
}