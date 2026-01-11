using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalIconItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    public void Set(Sprite sprite, string count)
    {
        if (icon != null) icon.sprite = sprite;
        if (countText != null) countText.text = count;
    }
}
