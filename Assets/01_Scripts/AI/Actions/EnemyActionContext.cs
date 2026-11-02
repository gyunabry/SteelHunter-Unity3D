namespace SteelHunter.AI.Character
{
    public sealed class EnemyActionContext
    {
        public CharacterRoot Self { get; }
        public EnemyCharacterData Profile { get; }
        public EnemyBlackboard Memory { get; }
        public CharacterNavigator Navigator { get; }
        public EnemyFireControl FireControl { get; }
        public CoverSelector Cover { get; }
        public CharacterGun Gun { get; }
        public CharacterMover Mover { get; }
        public ICharacterCommands Commands { get; }

        public EnemyActionContext(CharacterRoot self, EnemyCharacterData profile,
            EnemyBlackboard memory, CharacterNavigator navigator, EnemyFireControl fireControl,
            CoverSelector cover, CharacterGun gun, CharacterMover mover, ICharacterCommands commands)
        {
            Self = self;
            Profile = profile;
            Memory = memory;
            Navigator = navigator;
            FireControl = fireControl;
            Cover = cover;
            Gun = gun;
            Mover = mover;
            Commands = commands;
        }
    }
}
