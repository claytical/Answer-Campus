using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace VNEngine
{
    // Not used in real code. Merely a template to copy and paste from when creating new nodes.
    public class NodeMap : Node
    {
        public Character character;
        public string locationScene;
        public bool addLocationToMap = true;

        public override void Run_Node()
        {
            if (string.IsNullOrWhiteSpace(locationScene))
            {
                Debug.LogWarning("[NodeMap] Missing locationScene.");
                Finish_Node();
                return;
            }

            var pins = PlayerPrefsExtra.GetList<CharacterLocation>("characterLocations", new List<CharacterLocation>());

            // Always dedupe for this character+scene first
            pins.RemoveAll(p =>
                EqualityComparer<Character>.Default.Equals(p.character, character) &&
                string.Equals(p.location, locationScene, System.StringComparison.Ordinal));

            if (addLocationToMap)
            {
                pins.Add(new CharacterLocation { character = character, location = locationScene });
                Debug.Log($"[NodeMap] Added pin: {character} @ {locationScene}");
            }
            else
            {
                Debug.Log($"[NodeMap] Removed pin (noop add): {character} @ {locationScene}");
            }

            PlayerPrefsExtra.SetList("characterLocations", pins);
            PlayerPrefs.Save();

            Finish_Node();
        }

        // What happens when the user clicks on the dialogue text or presses spacebar? Either nothing should happen, or you call Finish_Node to move onto the next node
        public override void Button_Pressed()
        {
            //Finish_Node();
        }


        // Do any necessary cleanup here, like stopping coroutines that could still be running and interfere with future nodes
        public override void Finish_Node()
        {
            StopAllCoroutines();

            base.Finish_Node();
        }
    }
}