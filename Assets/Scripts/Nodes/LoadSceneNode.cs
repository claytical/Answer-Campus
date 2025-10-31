using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace VNEngine
{
    // Loads the specified scene. This should be the last component you want, as all conversations will be lost after this.
    public class LoadSceneNode : Node
    {
        public string level_to_load;
        private string autoSaveScene = "Cutscene";

        public bool async_loading = false;  // If you want to use a loading screen, set this to true


        public override void Run_Node()
        {
            FMODAudioManager.Instance.FadeOutAmbient(0);
            FMODAudioManager.Instance.FadeOutMusic(0);
            // Simply loads the specified scene
            Debug.Log("Switching level: " + level_to_load + " after playing cutscene...");
            PlayerPrefs.SetString("Scene After Save", level_to_load);
            Time.timeScale = 1;

            if (!async_loading)
            {
                SceneManager.LoadScene(level_to_load, LoadSceneMode.Single);
            }
            else
            {
                StartCoroutine(Async_Load_Level(level_to_load));
            }
        }


        IEnumerator Async_Load_Level(string target)
        {
            UIManager.ui_manager.loading_icon.SetActive(true);
            UIManager.ui_manager.loading_text.gameObject.SetActive(true);
            string active_scene = SceneManager.GetActiveScene().name;
            AsyncOperation AO = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
            AO.allowSceneActivation = false;
            int progress = (int)(AO.progress * 100f);
            while (!AO.isDone)
            {
                progress = Mathf.Max(progress, (int)(AO.progress * 100f));
                UIManager.ui_manager.loading_text.text = "Loading... " + progress + "%";
                yield return null;
            }
            AO.allowSceneActivation = true;
            UIManager.ui_manager.loading_icon.SetActive(false);
            UIManager.ui_manager.loading_text.gameObject.SetActive(false);
            Debug.Log("Done Async loading & switching to level: " + autoSaveScene);
        }


        public override void Button_Pressed()
        {

        }


        public override void Finish_Node()
        {
            StopAllCoroutines();

            base.Finish_Node();
        }
    }
}