using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MapView : MonoBehaviour
{
    [Header("Prefabs & Roots")]
    public Transform locationsRoot;            // container for location buttons
    public GameObject locationButtonPrefab;    // prefab that has LocationButton on it

    [Header("Events")]
    public StringEvent onLocationSelected;     // (string locationName)
    [Serializable] public class StringEvent : UnityEvent<string> {}

    /// <summary>
    /// Render one button per location; each button shows its friend chips.
    /// </summary>
    public void Render(List<CharacterLocation> characterLocations)
    {
        // Clear
        foreach (Transform child in locationsRoot) Destroy(child.gameObject);
        if (characterLocations == null || characterLocations.Count == 0) return;

        var byLocation = characterLocations
            .Where(cl => !string.IsNullOrWhiteSpace(cl.location))
            .GroupBy(cl => cl.location)
            .OrderBy(g => g.Key);

        foreach (var group in byLocation)
        {
            string locationName = group.Key;

            // distinct friends at this location
            var friends = group.Select(cl => cl.character)
                .Distinct()
                .OrderBy(c => c.ToString())
                .ToList();

            var go = Instantiate(locationButtonPrefab, locationsRoot);
            var lb = go.GetComponent<LocationButton>();
            if (lb == null)
            {
                Debug.LogError("LocationButton component missing on locationButtonPrefab.");
                continue;
            }

            lb.Bind(locationName, friends, () =>
            {
                onLocationSelected?.Invoke(locationName);
            });
        }
    }
}