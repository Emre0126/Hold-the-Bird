using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

#if !UNITY_WEBGL || UNITY_EDITOR
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
#endif

[Serializable]
public class LeaderboardEntry
{
    public string key;
    public string name;
    public int score;
    public int timestamp;
}

[Serializable]
public class LeaderboardEntryArrayWrapper
{
    public LeaderboardEntry[] items;
}

public class LeaderboardManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject addLeaderboardButton;
    public TMP_InputField nameInput;
    public GameObject namePanel;
    public GameObject leaderboardPanel;
    public Transform leaderboardContent;
    public GameObject leaderboardItemPrefab;
    public MonoBehaviour[] scriptsToDisableWhileLeaderboardOpen;
    public Button closeNameButton;
    public Button submitScoreButton;
    public Button closeLeaderboardButton;
    private int maxNameLength = 7;
    private List<GameObject> pooledItems = new List<GameObject>();
#if UNITY_WEBGL && !UNITY_EDITOR
  [DllImport("__Internal")]
  private static extern void ShowOverlayInput();
  [DllImport("__Internal")]
  private static extern void HideOverlayInput();
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void Firebase_StartListener(string goName, string methodName);
    [DllImport("__Internal")] private static extern void Firebase_SubmitScore(string name, int score, int ts);
    [DllImport("__Internal")] private static extern void Firebase_StopListener();
#else
    private DatabaseReference dbRef;
#endif
    void Update()
    {
        if (namePanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                PressButton(closeNameButton);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                PressButton(submitScoreButton);
        }
        else if (leaderboardPanel.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                PressButton(closeLeaderboardButton);
        }
    }

    private void PressButton(Button btn)
    {
        var ptr = new PointerEventData(EventSystem.current)
        {
            pointerId = -1,
            position = RectTransformUtility.WorldToScreenPoint(
                null,
                btn.transform.position
            )
        };
        ExecuteEvents.Execute(btn.gameObject, ptr, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(btn.gameObject, ptr, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(btn.gameObject, ptr, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(btn.gameObject, ptr, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.Execute(btn.gameObject, ptr, ExecuteEvents.pointerExitHandler);
    }
    void Awake()
    {
        var trigger = nameInput.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var entrySelect = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        entrySelect.callback.AddListener(_ => OpenKeyboard());
        trigger.triggers.Add(entrySelect);
        var entryDeselect = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
        entryDeselect.callback.AddListener(_ => CloseKeyboard());
        trigger.triggers.Add(entryDeselect);
        this.gameObject.name = "LeaderboardManager";
    }
    private void OpenKeyboard()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowOverlayInput();
#else
#endif
    }

    private void CloseKeyboard()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HideOverlayInput();
#endif
    }
    public void OnOverlaySubmit(string nameFromOverlay)
    {
        nameInput.text = nameFromOverlay;
        SubmitScore();
    }
    public void OnOverlayHide()
    {
    }
    public void OnOverlayTextChanged(string text)
    {
        nameInput.text = text;
    }

    void Start()
    {
        nameInput.characterLimit = maxNameLength;

#if UNITY_WEBGL && !UNITY_EDITOR
        Firebase_StartListener(gameObject.name, nameof(OnFirebaseData));
#else
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task => {
                if (task.Result == DependencyStatus.Available)
                {
                    dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                    dbRef.Child("leaderboard").ValueChanged += OnLeaderboardChanged;
                }
                else Debug.LogError("Firebase init error: " + task.Result);
            });
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void OnFirebaseData(string json)
    {
        var wrapper = JsonUtility.FromJson<LeaderboardEntryArrayWrapper>("{\"items\":" + json + "}");
        BuildLeaderboardFromEntries(wrapper.items);
    }
#else
    private void OnLeaderboardChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError("Listener error: " + e.DatabaseError.Message);
            return;
        }

        var entries = e.Snapshot.Children
            .Select(c => new LeaderboardEntry
            {
                key = c.Key,
                name = c.Child("name").Value.ToString(),
                score = int.Parse(c.Child("score").Value.ToString()),
                timestamp = int.Parse(c.Child("timestamp").Value.ToString())
            })
            .ToArray();

        BuildLeaderboardFromEntries(entries);
    }
#endif

    private void BuildLeaderboardFromEntries(LeaderboardEntry[] entries)
    {
        pooledItems.ForEach(Destroy);
        pooledItems.Clear();

        int now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        int last24h = now - 24 * 60 * 60;

        var all = entries
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.timestamp)
            .ToList();

        AddTitle("Top 10 of All Time", Color.cyan);
        for (int i = 0; i < Mathf.Min(10, all.Count); i++)
            CreateEntry(i + 1, all[i], PlayerPrefs.GetString("BestLocalTimestamp", ""));

        AddTitle("Top 40 in Last 24 Hours", Color.green);
        var recent = all.Where(x => x.timestamp >= last24h).Take(40).ToList();
        for (int i = 0; i < recent.Count; i++)
            CreateEntry(i + 1, recent[i], PlayerPrefs.GetString("BestLocalTimestamp", ""));
    }

    private void AddTitle(string text, Color color)
    {
        var title = Instantiate(leaderboardItemPrefab, leaderboardContent);
        var txt = title.GetComponent<TMP_Text>();
        txt.text = text;
        txt.fontSize = 88;
        txt.alignment = TextAlignmentOptions.Left;
        txt.color = color;
        pooledItems.Add(title);
    }

    private void CreateEntry(int index, LeaderboardEntry entry, string bestTs)
    {
        string name = entry.name.Length > maxNameLength
            ? entry.name.Substring(0, maxNameLength)
            : entry.name;

        var item = Instantiate(leaderboardItemPrefab, leaderboardContent);
        var txt = item.GetComponent<TMP_Text>();
        txt.text = $"{index}. {name} - {entry.score}";
        txt.fontSize = 100;

        if (entry.timestamp.ToString() == bestTs)
        {
            txt.color = Color.yellow;
            txt.fontStyle = FontStyles.Bold;
        }
        else
        {
            txt.color = new Color32(67, 67, 67, 255);
            txt.fontStyle = FontStyles.Normal;
        }

        pooledItems.Add(item);
    }

    public void OnAddLeaderboardButton()
    {
        FindObjectOfType<PauseManager>()?.TemporarilyPauseMusic();
        namePanel.SetActive(true);
    }

    public void SubmitScore()
    {
        if (addLeaderboardButton != null)
            addLeaderboardButton.SetActive(false);
        string playerName = nameInput.text;
        int score = ScoreManager.Instance.GetScore();

#if UNITY_WEBGL && !UNITY_EDITOR
        int timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();  // saniye
        int bestLocal = PlayerPrefs.GetInt("BestLocalScore", 0);
        if (score > bestLocal)
        {
            PlayerPrefs.SetInt("BestLocalScore", score);
            PlayerPrefs.SetString("BestLocalTimestamp", timestamp.ToString());
            PlayerPrefs.Save();
        }
        Firebase_SubmitScore(playerName, score, timestamp);
        namePanel.SetActive(false);
        ShowLeaderboard();
#else
        int timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string key = dbRef.Child("leaderboard").Push().Key;
        var entry = new Dictionary<string, object> {
            { "name",      playerName },
            { "score",     score      },
            { "timestamp", timestamp  }
        };
        dbRef.Child("leaderboard").Child(key)
            .SetValueAsync(entry)
            .ContinueWithOnMainThread(task => {
                if (task.IsCompleted)
                {
                    int best = PlayerPrefs.GetInt("BestLocalScore", 0);
                    if (score > best)
                    {
                        PlayerPrefs.SetInt("BestLocalScore", score);
                        PlayerPrefs.SetString("BestLocalTimestamp", timestamp.ToString());
                        PlayerPrefs.Save();
                    }
                    namePanel.SetActive(false);
                    ShowLeaderboard();
                }
                else Debug.LogError("Submit error: " + task.Exception);
            });
#endif
    }

    public void ShowLeaderboard()
    {
        FindObjectOfType<PauseManager>()?.TemporarilyPauseMusic();
        foreach (var s in scriptsToDisableWhileLeaderboardOpen) s.enabled = false;
        leaderboardPanel.SetActive(true);
    }

    public void CloseLeaderboard()
    {
        FindObjectOfType<PauseManager>()?.ResumeMusicIfNotMuted();
        foreach (var s in scriptsToDisableWhileLeaderboardOpen) s.enabled = true;
        leaderboardPanel.SetActive(false);
    }

    public void CloseName()
    {
        FindObjectOfType<PauseManager>()?.ResumeMusicIfNotMuted();
        namePanel.SetActive(false);
    }

    void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Firebase_StopListener();
#else
        if (dbRef != null)
            dbRef.Child("leaderboard").ValueChanged -= OnLeaderboardChanged;
#endif
    }
}