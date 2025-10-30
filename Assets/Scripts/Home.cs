using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VNEngine;
using System.Linq;
public class Home : MonoBehaviour
{
    public GameObject phone;
    public Sprite phoneNewMessages;
    public Sprite phoneNoNewMessages;
    
    public void Quit()
    {
        FMODAudioManager.Instance.StopMusic();
        SceneManager.LoadScene("Main");
    }
}
