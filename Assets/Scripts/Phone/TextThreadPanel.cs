// TextThreadPanel.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public static class QuickReplyIconLibrary
{
    static Dictionary<string, Sprite> cache;
    public static Sprite Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        cache ??= Build();
        return cache.TryGetValue(key, out var s) ? s : null;
    }

    static Dictionary<string, Sprite> Build()
    {
        // TODO: load from Resources, Addressables, or assign via inspector
        return new Dictionary<string, Sprite>
        {
            // { "thumbs_up", Resources.Load<Sprite>("Icons/thumbs_up") }
        };
    }
}

public class TextThreadPanel : MonoBehaviour
{
    [Header("Wiring")]
    public Transform contentRoot;           // vertical layout group for bubbles
    public GameObject npcBubblePrefab;      // has Text body
    public GameObject playerBubblePrefab;   // has Text body (right-aligned)
    public Transform quickReplyRoot;        // horizontal/vertical group for reply buttons
    public GameObject quickReplyButtonPrefab; // has Button + Text + optional Image

    Character current;

    public void Show(Character other)
    {
        current = other;
        Render();
        gameObject.SetActive(true);
    }

    public void Render()
    {
        // Clear
        foreach (Transform c in contentRoot) Destroy(c.gameObject);
        foreach (Transform c in quickReplyRoot) Destroy(c.gameObject);

        var msgs = TextThreads.GetThread(current);
        QuickReply[] pending = null;

        foreach (var m in msgs)
        {
            var prefab = m.isPlayer ? playerBubblePrefab : npcBubblePrefab;
            var go = Instantiate(prefab, contentRoot);
            go.GetComponentInChildren<TMPro.TMP_Text>().text = m.body;

            if (!m.isPlayer && m.quickReplies != null && m.quickReplies.Count > 0)
                pending = m.quickReplies.ToArray();
        }

        if (pending != null && pending.Length > 0)
        {
            foreach (var qr in pending)
            {
                var btnGO = Instantiate(quickReplyButtonPrefab, quickReplyRoot);
                var txt = btnGO.GetComponentInChildren<TMPro.TMP_Text>();
                txt.text = qr.label;

                // optional icon mapping
                var img = btnGO.GetComponentInChildren<Image>(true);
                if (img != null) img.sprite = QuickReplyIconLibrary.Get(qr.iconKey); // your sprite atlas lookup

                btnGO.GetComponent<Button>().onClick.AddListener(() =>
                {
                    TextThreads.SendPlayerResponse(current, qr);
                    Render(); // refresh to show the new player bubble and hide buttons
                });
            }
        }
    }
}
