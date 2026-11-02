using System.Collections.Generic;

public enum SceneType
{
    None,
    Title,
    Hangar,
    Game
}

public static class SceneNames
{
    private static readonly Dictionary<SceneType, string> sceneTable
        = new Dictionary<SceneType, string>()
    {
        { SceneType.Title, "TitleScene" },
        { SceneType.Hangar, "HangarScene" },
        { SceneType.Game, "BattleFieldScene" }
    };

    public static string GetSceneName(SceneType type)
    {
        return sceneTable[type];
    }
}