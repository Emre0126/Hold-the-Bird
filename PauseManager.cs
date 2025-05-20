using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [Header("Ayarlar")]
    public GameObject pausePanel;
    public Text countdownText;
    public MonoBehaviour[] scriptsToDisable;
    public GameObject MusicOn;
    public GameObject MusicOff;
    public GameObject Resume;

    [Header("Ses")]
    public AudioSource musicSource;
    public Text Score;
    private bool isPaused = false;
    private bool isCountingDown = false;
    private float pausedMusicTime = 0f;
    public static bool wasDraggingDuringCountdown = false;
    private bool musicLockedUntilRestart = false;

    void Awake()
    {
        if (PlayerPrefs.GetInt("MusicMuted", 0) == 1)
        {
            musicSource.Stop();
            musicLockedUntilRestart = true;
        }
    }

    void Start()
    {
        pausePanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        MusicOff.SetActive(false);
        MusicOn.SetActive(false);
        Resume.SetActive(false);
        if (PlayerPrefs.GetInt("MusicMuted", 0) == 0)
        {
            musicSource.Play();
            musicLockedUntilRestart = false;
        }
    }

    public void TogglePause()
    {
        if (isCountingDown) return;

        if (!isPaused)
        {
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            Resume.SetActive(true);
            Score.gameObject.SetActive(false);

            if (musicSource.isPlaying)
            {
                pausedMusicTime = musicSource.time;
                musicSource.Pause();
                MusicOn.SetActive(true);
                MusicOff.SetActive(false);
            }
            else
            {
                MusicOff.SetActive(true);
                MusicOn.SetActive(false);
            }

            foreach (var script in scriptsToDisable)
            {
                var drag = script as DragToMove;
                if (drag != null)
                    drag.inputBlocked = true;
                else
                    script.enabled = false;
            }

            isPaused = true;
        }
        else
        {
            StartCoroutine(ResumeWithCountdown());
        }
    }

    public void OnResumeButtonClicked()
    {
        if (isPaused)
            StartCoroutine(ResumeWithCountdown());
    }

    private float resumeMusicTime = 0f;

    public void TemporarilyPauseMusic()
    {
        if (musicSource.isPlaying)
        {
            resumeMusicTime = musicSource.time;
            musicSource.Pause();
        }
    }

    public void TryPlayMusic()
    {
        if (musicLockedUntilRestart) return;
        if (PlayerPrefs.GetInt("MusicMuted", 0) == 0)
            musicSource.Play();
    }

    public void ResumeMusicIfNotMuted()
    {
        if (musicLockedUntilRestart) return;
        if (PlayerPrefs.GetInt("MusicMuted", 0) == 0)
        {
            musicSource.time = resumeMusicTime;
            musicSource.Play();
        }
    }

    public void ForceStopMusic()
    {
        musicSource.Stop();
        musicLockedUntilRestart = true;
    }

    private IEnumerator ResumeWithCountdown()
    {
        Score.gameObject.SetActive(true);
        isCountingDown = true;
        MusicOff.SetActive(false);
        MusicOn.SetActive(false);
        Resume.SetActive(false);
        countdownText.gameObject.SetActive(true);
        pausePanel.SetActive(false);

        int count = 3;
        while (count > 0)
        {
            countdownText.text = count.ToString();
            yield return new WaitForSecondsRealtime(1f);
            count--;
        }

        countdownText.gameObject.SetActive(false);
        Time.timeScale = 1f;
        if (PlayerPrefs.GetInt("MusicMuted", 0) == 0)
        {
            musicSource.time = pausedMusicTime;
            musicSource.Play();
        }

        foreach (var script in scriptsToDisable)
        {
            var drag = script as DragToMove;
            if (drag != null)
                drag.inputBlocked = false;
            else
                script.enabled = true;
        }

        isPaused = false;
        isCountingDown = false;
        if (PauseManager.wasDraggingDuringCountdown)
        {
            DragToMove[] movers = FindObjectsOfType<DragToMove>();
            foreach (var mover in movers)
                mover.ResumeDragging();
            PauseManager.wasDraggingDuringCountdown = false;
        }
    }

    public void OnMusicOnButtonClicked()
    {
        musicSource.Pause();
        PlayerPrefs.SetInt("MusicMuted", 1);
        MusicOn.SetActive(false);
        MusicOff.SetActive(true);
        musicLockedUntilRestart = true;
    }

    public void OnMusicOffButtonClicked()
    {
        PlayerPrefs.SetInt("MusicMuted", 0);
        MusicOn.SetActive(true);
        MusicOff.SetActive(false);
        musicLockedUntilRestart = false;
        musicSource.Play();
    }
}
