using UnityEngine;

/// <summary>
/// 플레이어와 적 캐릭터가 구현하는 조작 인터페이스
/// </summary>
public interface ICharacterCommands
{
    void SetMove(Vector3 worldDirection, bool sprint);
    void SetAim(Vector3 worldPoint);
    void SetAiming(bool aiming);
    bool TryFire();
    bool TryReload();
    void Stop();
}
