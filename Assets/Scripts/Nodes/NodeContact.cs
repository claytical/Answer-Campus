using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace VNEngine
{
    // Not used in real code. Merely a template to copy and paste from when creating new nodes.
    public class NodeContact : Node
    {
        public Character character;


        // Called initially when the node is run, put most of your logic here
        public override void Run_Node()
        {
            Friend friendship = new Friend();
            friendship.characterName = character.ToString();
            friendship.relationship = Relationship.FRIEND;
            friendship.AddToContacts();
            StatsManager.Set_Boolean_Stat("PhoneHasNewActivity", true);
            Debug.Log($"Added contact {character} to list");
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