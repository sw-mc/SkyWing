using SkyWing.SkyWing.language;
using SkyWing.SkyWing.Language;

namespace SkyWing.SkyWing.player; 

public enum GameModeTypes {
    Adventure,
    Creative,
    Spectator,
    Survival
}

public sealed class GameMode {
    
    private static Dictionary<string, GameMode> AliasMap { get; } = new ();
    
    public GameModeTypes Type { get; }
    public string EnglishName { get; }
    public Translatable TranslatableName { get; }
    private string[] Aliases { get; }

    public GameMode(GameModeTypes type, string name, Translatable translatableName, string[] aliases) {
        Type = type;
        EnglishName = name;
        TranslatableName = translatableName;
        Aliases = aliases;
    }

    private static void Setup() {
        Register(new GameMode(GameModeTypes.Survival, "Survival", KnownTranslationFactory.GamemodeSurvival, new []{"survival", "s", "0"}));
        Register(new GameMode(GameModeTypes.Creative, "Survival", KnownTranslationFactory.GamemodeCreative, new []{"creative", "c", "1"}));
        Register(new GameMode(GameModeTypes.Adventure, "Survival", KnownTranslationFactory.GamemodeAdventure, new []{"adventure", "a", "2"}));
        Register(new GameMode(GameModeTypes.Spectator, "Survival", KnownTranslationFactory.GamemodeSpectator, new []{"spectator", "view", "3"}));
    }

    private static void Register(GameMode member) {
        foreach (var alias in member.Aliases) {
            AliasMap[alias] = member;
        }
    }

    public static GameMode? FromString(string str) {
        if (!CheckInit()) Setup();
        return AliasMap.ContainsKey(str) ? AliasMap[str] : null;
    }

    private static bool CheckInit() {
        return !(AliasMap.Count <= 0);
    }
}

public static class GameModeIdMap {

    public static GameMode? FromId(int id) {
        return id switch {
            0 => GameMode.FromString("survival"),
            1 => GameMode.FromString("creative"),
            2 => GameMode.FromString("adventure"),
            3 => GameMode.FromString("spectator"),
            _ => null
        };
    }

    public static int ToId(GameMode gameMode) {
        return gameMode.Type switch {
            GameModeTypes.Survival => 0,
            GameModeTypes.Creative => 1,
            GameModeTypes.Adventure => 2,
            GameModeTypes.Spectator => 3,
            _ => throw new ArgumentException("Game mode is not mapped.", nameof(gameMode))
        };
    }
}