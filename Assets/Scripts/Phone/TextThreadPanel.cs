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
    [SerializeField] private GameObject root;         // the panel GameObject
    [SerializeField] private CanvasGroup canvasGroup; // optional, if present

    public void Show(Character other)
    {
        current = other;
        if (root) root.SetActive(true);
        if (canvasGroup) { canvasGroup.alpha = 1; canvasGroup.interactable = true; canvasGroup.blocksRaycasts = true; }
        Render();
    }

    public void Hide()
    {
        current = default;
        // optional: clear children so it’s fresh next time
        if (contentRoot)
            for (int i = contentRoot.childCount - 1; i >= 0; i--) Destroy(contentRoot.GetChild(i).gameObject);
        if (quickReplyRoot)
            for (int i = quickReplyRoot.childCount - 1; i >= 0; i--) Destroy(quickReplyRoot.GetChild(i).gameObject);

        if (canvasGroup) { canvasGroup.alpha = 0; canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false; }
        if (root) root.SetActive(false);
    }
    
public void Render()
{
    // Clear
    foreach (Transform c in contentRoot) Destroy(c.gameObject);
    foreach (Transform c in quickReplyRoot) Destroy(c.gameObject);

    var msgs = TextThreads.GetThread(current);
    QuickReply[] pending = null;
    string pendingTargetScene = null; // <-- capture the scene from the NPC prompt

    foreach (var m in msgs)
    {
        var prefab = m.isPlayer ? playerBubblePrefab : npcBubblePrefab;
        var go = Instantiate(prefab, contentRoot);

        var label = go.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (label) label.text = m.body ?? "";

        // If this NPC message offers quick replies, remember replies + its target scene
        if (!m.isPlayer && m.quickReplies != null && m.quickReplies.Count > 0)
        {
            pending = m.quickReplies.ToArray();
// In your TextThreadPanel (or wherever you build quick replies for a thread)
            bool HasInvite(string sceneName)
            {
                var pins = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());
                return pins.Exists(p => string.Equals(p.location, sceneName, System.StringComparison.Ordinal));
            }

// Before enabling “ok / go” quick-replies:
            if (!HasInvite(pendingTargetScene))
            {
                // Hide or disable the quick reply buttons
                quickReplyRoot.gameObject.SetActive(false);
            }
            
            pendingTargetScene = m.location; // <-- sceneName key to route to
        }
    }

    if (pending != null && pending.Length > 0)
    {
        foreach (var qr in pending)
        {
            var btnGO = Instantiate(quickReplyButtonPrefab, quickReplyRoot);

            var txt = btnGO.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (txt) txt.text = qr.label ?? "";

            // optional icon mapping
            var img = btnGO.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (img != null) img.sprite = QuickReplyIconLibrary.Get(qr.iconKey);

            var button = btnGO.GetComponent<UnityEngine.UI.Button>();
            if (button == null) continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // 1) record player's reply in the thread
                TextThreads.SendPlayerResponse(current, qr);

                // 2) navigate if the NPC message specified a destination
                if (!string.IsNullOrWhiteSpace(pendingTargetScene))
                {
                    Hide(); // prevent overlay flicker and block raycasts before scene load
                    LocationRouter.Go(pendingTargetScene);
                }
                else
                {
                    // no destination on the prompt; just refresh UI
                    Render();
                }
            });
        }
    }
}


}
