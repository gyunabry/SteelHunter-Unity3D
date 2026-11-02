public interface IAmmoSource
{
    int GetQuantity(AmmoData ammo);
    int ConsumeUpTo(AmmoData ammo, int requestedRounds);
}
