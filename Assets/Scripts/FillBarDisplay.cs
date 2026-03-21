using UnityEngine;
using UnityEngine.UI;
public class FillBarDisplay : MonoBehaviour
{
    public Image fillBar;

    public Player playerToTrack;

    public void UpdateFillbar()
    {
        if (playerToTrack == null || GameManager.Instance == null) return;

        float fillValue = Mathf.Clamp((float)playerToTrack.handsCleared / (float)GameManager.Instance.handsClearedToWin, 0f, 1f);
        fillBar.fillAmount = fillValue;

    }
}
