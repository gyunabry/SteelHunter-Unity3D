using UnityEngine;

[CreateAssetMenu(
    fileName = "TestUsableItem",
    menuName = "SteelHunter/Test Usable Item")]
public class TestUsableItemData : ItemData
{
    public override bool TryUse(GameObject user)
    {
        if (user == null)
            return false;

        Debug.Log($"{user.name}: {DisplayName} 사용 성공", user);
        return true;
    }
}
