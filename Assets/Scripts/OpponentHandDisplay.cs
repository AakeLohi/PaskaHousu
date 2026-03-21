using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpponentHandDisplay : MonoBehaviour
{
    [Header("Source")]
    public Player handToDisplay;

    [Header("Prefab")]
    public GameObject heldCardPrefab; // prefab with Image or SpriteRenderer

    [Header("Layout")]
    [Tooltip("Total arc angle in degrees (fan width).")]
    public float arcAngle = 30f;
    [Tooltip("Radius of the arc in local units.")]
    public float radius = 60f;
    [Tooltip("Vertical offset applied to all cards (local units).")]
    public float yOffset = 0f;
    [Tooltip("Scale applied to spawned card prefabs.")]
    public float cardScale = 1f;
    [Tooltip("Whether to show the face of opponent cards (false => show back).")]

    public void UpdateHandDisplay()
    {
        // safety
        if (heldCardPrefab == null)
        {
            Debug.LogWarning("OpponentHandDisplay: heldCardPrefab not set.");
            return;
        }

        // clear previous visuals
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        if (handToDisplay == null || handToDisplay.hand == null || handToDisplay.hand.Count == 0)
            return;

        List<Card> hand = handToDisplay.hand;
        int count = hand.Count;

        // compute start angle and step
        float startAngle = -arcAngle * 0.5f;
        float step = count > 1 ? arcAngle / (count - 1) : 0f;

        // spawn each card
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            float rad = angle * Mathf.Deg2Rad;

            // compute local position on arc (x left-right, y up-down)
            Vector3 localPos = new Vector3(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius + yOffset, 0f);

            GameObject go = Instantiate(heldCardPrefab, transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * 0.5f); // slight rotation to match fan
            go.transform.localScale = Vector3.one * cardScale;
        
            // set sibling index so middle cards are drawn on top, if using UI
            // Move center card to top: gives nice overlap order
            int mid = count / 2;
            int sibling = i;
            if (i <= mid)
                sibling = i;
            else
                sibling = i;

            go.transform.SetSiblingIndex(sibling);
        }
    }

}
