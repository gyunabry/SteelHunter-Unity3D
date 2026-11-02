using UnityEngine;

public interface IInteractable
{
    Transform PromptAnchor { get; }
    string PromptText { get; }

    bool CanInteract();
    void OnInteract();
}