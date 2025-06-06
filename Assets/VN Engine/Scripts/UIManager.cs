using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Video;

namespace VNEngine
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager ui_manager;

        // CSV to load so our UI can be put into the proper language
        public TextAsset Localized_UI_CSV;
        // Each language has a dictionary, then that dictionary is searched for a specific key
        [HideInInspector]
        public Dictionary<string, Dictionary<string, string>> Localized_UI_Dictionaries = new Dictionary<string, Dictionary<string, string>>();
        [HideInInspector]
        List<LocalizeTextElement> localized_ui_elements = new List<LocalizeTextElement>();  // Elements that were localized. Get localized again if the language changes

        public Font[] fonts;

        public Transform actor_parent;
        public RectTransform dialogue_text_panel_container; // the actual full panel
        public Text dialogue_text; // the Text inside

        public RectTransform speaker_panel_container;
        public Text speaker_name_text;
        public GameObject choice_panel;
        public Image background;    // Background image
        public Image foreground;    // Image appears in front of ALL ui elements
        public Text log_text;   // Log containing all dialogue spoken
        public Text scroll_log; // Log accessed by using the mouse scroll wheel when hovering over the dialogue panel
        public ScrollRect scroll_log_rect;
        public GameObject item_grid;   // Item grid where Items are placed
        public Text item_description;
        public Image large_item_image;
        public GameObject ingame_topbar;
        public GameObject item_menu;
        public GameObject log_menu;
        public GameObject entire_UI_panel;
        public GameObject ingame_UI_menu;
        public VideoPlayer video_player;

        public Transform foreground_static_image_parent;
        public Transform background_static_image_parent;

        public Image autoplay_image;
        public Sprite autoplay_on_image;

        public Button continueButton;

        // Used by ChoiceNode
        public Text choice_text_banner;
        public Button[] choice_buttons;

        public GameObject actor_positions;

        // Text options
        public Slider text_scroll_speed_slider;
        public Slider text_size_slider;
        public Toggle text_autosize;

        public FontDropDown font_drop_down;

        public Dropdown language_dropdown;

        public Button save_button;

        public GameObject loading_icon;
        public Text loading_text;

        // Canvas properties
        public Canvas canvas;
        public float canvas_width;
        public float canvas_height;
        private string lastSpeakerName = "";
        private string lastSpeakerNameForDialogueText = "";

        public void MoveSpeakerPanelToActor(string actorName)
        {
            if (lastSpeakerName == actorName)
                return; // No need to animate again

            lastSpeakerName = actorName;

            Actor actor = ActorManager.Get_Actor(actorName);

            if (speaker_panel_container.anchorMin != new Vector2(0.5f, 1f))
            {
                speaker_panel_container.anchorMin = new Vector2(0.5f, 1f);
                speaker_panel_container.anchorMax = new Vector2(0.5f, 1f);
                speaker_panel_container.pivot = new Vector2(0.5f, 1f);
            }

            if (actor == null)
            {
                Debug.LogWarning($"Actor {actorName} not found. Centering Speaker Panel.");
                Vector2 centerPosition = speaker_panel_container.anchoredPosition;
                centerPosition.x = 0;
                StopAllCoroutines();
                StartCoroutine(SmoothMoveSpeakerPanel(centerPosition));
                return;
            }

            // ✨ Convert the Actor's World Position into Canvas Space
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, actor.transform.position);

            Vector2 localPoint;
            RectTransform canvasRect = speaker_panel_container.root.GetComponent<Canvas>().GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, Camera.main, out localPoint);

            Vector2 targetPosition = speaker_panel_container.anchoredPosition;
            targetPosition.x = Mathf.Clamp(localPoint.x, -canvas_width / 2 + 150, canvas_width / 2 - 150);

            StopAllCoroutines();
            StartCoroutine(SmoothMoveSpeakerPanel(targetPosition));
        }

private IEnumerator SmoothMoveSpeakerPanel(Vector2 targetPosition)
{
    RectTransform speakerRect = speaker_panel_container.GetComponent<RectTransform>();
    CanvasGroup speakerCanvasGroup = speaker_panel_container.GetComponent<CanvasGroup>();

    if (speakerCanvasGroup == null)
        speakerCanvasGroup = speaker_panel_container.gameObject.AddComponent<CanvasGroup>();

    Vector2 startPos = speakerRect.anchoredPosition;
    float moveTime = 0f;
    float moveDuration = 0.3f;

    Vector3 originalScale = Vector3.one;
    Vector3 targetScale = originalScale * 1.1f; // Grow slightly during movement

    // Fade out at start (optional)
    speakerCanvasGroup.alpha = 0f;

    // === MOVEMENT AND BOUNCE ===
    while (moveTime < moveDuration)
    {
        moveTime += Time.deltaTime;

        float t = moveTime / moveDuration;

        // Smoothstep for natural ease in/out
        t = t * t * (3f - 2f * t);

        speakerRect.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);

        // Gentle grow and shrink (optional mini-bounce)
        if (t < 0.5f)
            speakerRect.localScale = Vector3.Lerp(originalScale, targetScale, t * 2f);
        else
            speakerRect.localScale = Vector3.Lerp(targetScale, originalScale, (t - 0.5f) * 2f);

        // Fade in
        speakerCanvasGroup.alpha = Mathf.Clamp01(t);

        yield return null;
    }

    speakerRect.anchoredPosition = targetPosition;
    speakerRect.localScale = originalScale;
    speakerCanvasGroup.alpha = 1f;

    // === ARRIVAL SQUASH STRETCH ===
    // Little squash at the end for polish
    Vector3 squashed = new Vector3(1.15f, 0.85f, 1f);
    Vector3 stretched = new Vector3(0.9f, 1.1f, 1f);

    float squashDuration = 0.1f;
    float squashTime = 0f;

    while (squashTime < squashDuration)
    {
        squashTime += Time.deltaTime;
        float t = squashTime / squashDuration;
        speakerRect.localScale = Vector3.Lerp(originalScale, squashed, t);
        yield return null;
    }

    squashTime = 0f;
    while (squashTime < squashDuration)
    {
        squashTime += Time.deltaTime;
        float t = squashTime / squashDuration;
        speakerRect.localScale = Vector3.Lerp(squashed, stretched, t);
        yield return null;
    }

    squashTime = 0f;
    while (squashTime < squashDuration)
    {
        squashTime += Time.deltaTime;
        float t = squashTime / squashDuration;
        speakerRect.localScale = Vector3.Lerp(stretched, originalScale, t);
        yield return null;
    }

    speakerRect.localScale = originalScale;
}

        void Awake()
        {
            ui_manager = this;

            if (canvas == null)
            {
                canvas = this.GetComponentInChildren<Canvas>();
                Debug.LogError("No canvas, please fill the Canvas field with a DialogueCanvas", this.gameObject);
            }
            canvas_width = canvas.GetComponent<CanvasScaler>().referenceResolution.x;
            canvas_height = canvas.GetComponent<CanvasScaler>().referenceResolution.y;

            if (Localized_UI_CSV != null)
                Localized_UI_Dictionaries = CSVReader.Generate_Localized_Dictionary(Localized_UI_CSV);
            else
                Debug.Log("No Localized_UI_CSV specified", this.gameObject);

            // Get the current language stored in player prefs
            Set_Language(PlayerPrefs.GetString("Language", LocalizationManager.Supported_Languages[0]));
        }

        public void AnimateDialogueTextPanel(string currentSpeakerName)
        {
            // Only animate if the speaker has changed
            if (lastSpeakerNameForDialogueText == currentSpeakerName)
                return; // Same speaker, don't reanimate

            lastSpeakerNameForDialogueText = currentSpeakerName;

            RectTransform dialogueRect = dialogue_text_panel_container;
            CanvasGroup dialogueCanvasGroup = dialogue_text_panel_container.GetComponent<CanvasGroup>();

            if (dialogueCanvasGroup == null)
                dialogueCanvasGroup = dialogue_text_panel_container.gameObject.AddComponent<CanvasGroup>();

            Vector2 originalPosition = dialogueRect.anchoredPosition;
            Vector2 startPosition = originalPosition + new Vector2(0, -30f); // slide up by 30 pixels
            dialogueRect.anchoredPosition = startPosition;

            dialogueCanvasGroup.alpha = 0f;

            StopCoroutine("AnimateDialogueRoutine");
            StartCoroutine(AnimateDialogueRoutine(dialogueRect, originalPosition, dialogueCanvasGroup));
        }

        private IEnumerator AnimateDialogueRoutine(RectTransform rect, Vector2 targetPos, CanvasGroup canvasGroup)
        {
            float time = 0f;
            float duration = 0.3f; // same as speaker panel movement

            Vector2 startPos = rect.anchoredPosition;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;

                // Use smoothstep for natural easing
                t = t * t * (3f - 2f * t);

                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                canvasGroup.alpha = Mathf.Clamp01(t);

                yield return null;
            }

            rect.anchoredPosition = targetPos;
            canvasGroup.alpha = 1f;
        }
        public void AnimateChoiceButtons(List<Button> buttons)
        {
            StartCoroutine(AnimateChoiceButtonsRoutine(buttons));
        }

        private IEnumerator AnimateChoiceButtonsRoutine(List<Button> buttons)
        {
            float delayBetweenButtons = 0.05f; // 50 ms between each button appearance
            float duration = 0.3f; // how long each button takes to animate in

            foreach (Button btn in buttons)
            {
                RectTransform rect = btn.GetComponent<RectTransform>();
                CanvasGroup cg = btn.GetComponent<CanvasGroup>();

                if (cg == null)
                    cg = btn.gameObject.AddComponent<CanvasGroup>();

                Vector2 originalPos = rect.anchoredPosition;
                Vector2 startPos = originalPos + new Vector2(0, -30f); // start 30 pixels lower
                rect.anchoredPosition = startPos;

                cg.alpha = 0f;

                StartCoroutine(AnimateSingleChoiceButton(rect, originalPos, cg, duration));

                yield return new WaitForSeconds(delayBetweenButtons);
            }
        }

        private IEnumerator AnimateSingleChoiceButton(RectTransform rect, Vector2 targetPos, CanvasGroup cg, float duration)
        {
            float time = 0f;
            Vector2 startPos = rect.anchoredPosition;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;

                t = t * t * (3f - 2f * t); // smoothstep for nice easing

                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                cg.alpha = Mathf.Clamp01(t);

                yield return null;
            }

            rect.anchoredPosition = targetPos;
            cg.alpha = 1f;
        }


        // Allows you to set the language in LocalizedManager.Language. Be sure to change the support languages in LocalizationManager.cs
        public void Set_Language(string language)
        {
            LocalizationManager.Set_Language(language);

            // Set the language dropdown correctly in the menu
            if (language_dropdown != null)
            {
                int x = 0;
                foreach (string s in LocalizationManager.Supported_Languages)
                {
                    if (s == LocalizationManager.Language)
                    {
                        language_dropdown.value = x;
                        break;
                    }
                    x++;
                }
            }

            Apply_Localization_Change();
        }
        // Takes an int which is used to index the supported languages array in LocalizationManager.cs
        public void Set_Language(int language)
        {
            Set_Language(LocalizationManager.Supported_Languages[language]);
        }


        public void Apply_Localization_Change()
        {
            if (VNSceneManager.scene_manager != null
                    && VNSceneManager.current_conversation != null
                    && VNSceneManager.current_conversation.Get_Current_Node() != null)
            {
                if (VNSceneManager.current_conversation.Get_Current_Node().GetType() == typeof(DialogueNode))
                {
                    DialogueNode n = (DialogueNode)VNSceneManager.current_conversation.Get_Current_Node();
                    n.GetLocalizedText(true);
                }
                else if (VNSceneManager.current_conversation.Get_Current_Node().GetType() == typeof(ChoiceNode))
                {
                    ChoiceNode n = (ChoiceNode)VNSceneManager.current_conversation.Get_Current_Node();
                    n.LanguageChanged();
                }
            }

            foreach (LocalizeTextElement e in localized_ui_elements)
            {
                if (e != null)
                {
                    e.LocalizeText();
                }
            }
        }


        // Returns the associated value with the given key for the language that has been set
        // Returns "" if the key is null or empty
        public string Get_Localized_UI_Entry(string key)
        {
            if (Localized_UI_Dictionaries == null || Localized_UI_Dictionaries.Count == 0)
            {
                Debug.Log("Could not find Localized UI CSV for key " + key + ". Please drag in a localization CSV into the UI Manager", this.gameObject);
                return "";
            }

            // Check for any potential errors
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("Get_Localized_UI_Entry key passed in is null or empty", this.gameObject);
                return "";
            }
            if (!Localized_UI_Dictionaries.ContainsKey(LocalizationManager.Language))
            {
                Debug.LogError("Get_Localized_UI_Entry could not find language " + LocalizationManager.Language, this.gameObject);
                return "";
            }
            if (!Localized_UI_Dictionaries[LocalizationManager.Language].ContainsKey(key))
            {
                Debug.LogError("Get_Localized_UI_Entry could not find the key " + key, this.gameObject);
                return "";
            }

            return Localized_UI_Dictionaries[LocalizationManager.Language][key];
        }


        public void Add_Localized_UI_Element_To_List(LocalizeTextElement element)
        {
            if (!localized_ui_elements.Contains(element))
            {
                localized_ui_elements.Add(element);
            }
        }


        // Fades in an image over a set amount of time
        public IEnumerator Fade_In_Image(Image img, float over_time)
        {
            Color tmp_color = img.color;
            tmp_color.a = 0;
            float value = 0;

            while (value < over_time)
            {
                value += Time.deltaTime;
                tmp_color.a = Mathf.Lerp(0, 1, value / over_time);
                img.color = tmp_color;
                yield return null;
            }
            yield break;
        }
        // Fades in an image out over a set amount of time
        public IEnumerator Fade_Out_Image(Image img, float over_time, bool destroy_after)
        {
            Color tmp_color = img.color;
            tmp_color.a = 1;
            float value = 0;

            while (value < over_time)
            {
                value += Time.deltaTime;
                tmp_color.a = Mathf.Lerp(1, 0, value / over_time);
                img.color = tmp_color;
                yield return null;
            }

            if (destroy_after)
                Destroy(img.gameObject);

            yield break;
        }
    }
    
    
}