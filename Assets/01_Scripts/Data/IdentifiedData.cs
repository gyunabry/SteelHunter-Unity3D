using UnityEngine;

public class IdentifiedData : ScriptableObject
{
    [Header("½Äº°ÀÚ")]
    [SerializeField] private string id = string.Empty;
    [SerializeField] private string displayName = string.Empty;

    public string Id => id;
    public string DisplayName => displayName;
}
