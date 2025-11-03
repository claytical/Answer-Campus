using System.Collections.Generic;
using UnityEngine;
using VNEngine;

public class MessageManager : MonoBehaviour
{
    // Singleton instance of MessageManager
    public static MessageManager Instance { get; private set; }

    public static Dictionary<string, List<TextMessage>> friendConversations = new Dictionary<string, List<TextMessage>>();

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Make this object persistent across scenes
        }
        else
        {
            Destroy(gameObject);  // Destroy extra instances
        }
    }

    // Add a new message to a specific friend's conversation
    public static void SendTextMessage(string friendName, TextMessage message)
    {
        if (!friendConversations.ContainsKey(friendName))
        {
            friendConversations[friendName] = new List<TextMessage>();
        }

        friendConversations[friendName].Add(message);
        StatsManager.Set_String_Stat(friendName + "_message_" + friendConversations[friendName].Count, message.body); // Persist in StatsManager
    }

    // Get all messages from a specific friend
    public static List<TextMessage> GetMessagesFromFriend(string friendName)
    {
        if (friendConversations.ContainsKey(friendName))
        {
            return friendConversations[friendName];
        }
        return new List<TextMessage>(); // Return an empty list if no conversation exists
    }

    // Populate message history from StatsManager if available
    public static void LoadMessagesFromFriend(string friendName)
    {
        if (friendConversations.ContainsKey(friendName))
        {
            return;
        }

        List<TextMessage> messages = new List<TextMessage>();
        int i = 1;
        while (StatsManager.String_Stat_Exists(friendName + "_message_" + i))
        {
            string messageContent = StatsManager.Get_String_Stat(friendName + "_message_" + i);
            Character from = (Character)System.Enum.Parse(typeof(Character), friendName.ToUpper());
            string location = "Unknown"; //TODO: find last location
            TextMessage message = new TextMessage(from, messageContent, location);
            messages.Add(message);
            i++;
        }
        friendConversations[friendName] = messages;
    }
}
