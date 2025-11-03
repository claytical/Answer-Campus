using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendChip : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI friendName;

    /// <summary>Bind this chip to a Character. 
    /// If you have a portrait lookup later, pass it in; otherwise leave null.</summary>
    public void Bind(Character who, Sprite portrait = null, string displayNameOverride = null)
    {
        if (friendName) friendName.text = string.IsNullOrEmpty(displayNameOverride) ? who.ToString() : displayNameOverride;
        if (icon)
        {
            if (portrait != null)
            {
                icon.sprite = portrait;
                icon.enabled = true;
                icon.gameObject.SetActive(true);
            }
            else
            {
                // If no portrait available, you can hide the icon to keep layout clean
                icon.enabled = false;
                icon.gameObject.SetActive(false);
            }
        }
    }

    // Optional convenience for overflow chips like "+3"
    public void BindOverflow(int extraCount)
    {
        if (friendName) friendName.text = $"+{extraCount}";
        if (icon)
        {
            icon.enabled = false;
            icon.gameObject.SetActive(false);
        }
    }
}