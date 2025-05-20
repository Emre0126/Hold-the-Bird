using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("CameraTrigger"))
        {
            triggered = true;
            ScoreManager.Instance.IncreaseScore();
        }
    }
}
