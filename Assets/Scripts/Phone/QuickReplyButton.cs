using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickReplyButton : MonoBehaviour
{
    [Header("Refs")]
    public Button button;                  // root button
    public TMP_Text label;                 // optional
    public Image icon;                     // optional

    /// <summary>
    /// Bind a reply. If you use icon keys, pass an icon resolver; otherwise pass a sprite directly.
    /// </summary>
    public void Bind(
        string replyText,
        Action onClick,
        Sprite iconSprite = null,
        Func<string, Sprite> iconResolver = null,
        string iconKey = null)
    {
        // Label
        if (label)
        {
            if (string.IsNullOrEmpty(replyText))
            {
                label.text = "";
                label.gameObject.SetActive(false);
            }
            else
            {
                label.text = replyText;
                label.gameObject.SetActive(true);
            }
        }

        // Icon
        if (icon)
        {
            var sprite = iconSprite ?? (iconKey != null ? iconResolver?.Invoke(iconKey) : null);
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.enabled = true;
                icon.gameObject.SetActive(true);
            }
            else
            {
                icon.enabled = false;
                icon.gameObject.SetActive(false);
            }
        }

        // Click
        if (button)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }

    /// <summary>Convenience for overflow chips (e.g., "+3").</summary>
    public void BindOverflow(int extraCount, Action onClick = null)
    {
        Bind("+" + extraCount, onClick, iconSprite: null);
    }
}