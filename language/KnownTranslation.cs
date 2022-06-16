using SkyWing.SkyWing.Language;

namespace SkyWing.SkyWing.language; 

public static class KnownTranslationFactory {

    public static Translatable DefaultGamemode(int defGamemode) =>
        new(KnownTranslationKeys.DEFAULT_GAMEMODE, new List<string> { defGamemode.ToString() });
    public static Translatable DefaultValuesInfo => new(KnownTranslationKeys.DEFAULT_VALUES_INFO);
    public static Translatable GamemodeAdventure => new(KnownTranslationKeys.GAMEMODE_ADVENTURE);
    public static Translatable GamemodeCreative => new(KnownTranslationKeys.GAMEMODE_CREATIVE);
    public static Translatable GamemodeInfo => new(KnownTranslationKeys.GAMEMODE_INFO);
    public static Translatable GamemodeSpectator => new(KnownTranslationKeys.GAMEMODE_SPECTATOR);
    public static Translatable GamemodeSurvival => new(KnownTranslationKeys.GAMEMODE_SURVIVAL);
    public static Translatable InvalidPort => new(KnownTranslationKeys.INVALID_PORT);
    public static Translatable IpConfirm => new(KnownTranslationKeys.IP_CONFIRM);
    public static Translatable IpGet => new(KnownTranslationKeys.IP_GET);
    public static Translatable IpWarning() => 
        new(KnownTranslationKeys.IP_WARNING);
    public static Translatable LanguageHasBeenSelected => new(KnownTranslationKeys.LANGUAGE_HAS_BEEN_SELECTED);
    public static Translatable LanguageName => new(KnownTranslationKeys.LANGUAGE_NAME);
    public static Translatable MaxPlayers(int defCount) =>
        new(KnownTranslationKeys.MAX_PLAYERS, new List<string> { defCount.ToString() });
    public static Translatable NameYourServer(string name) => 
        new(KnownTranslationKeys.NAME_YOUR_SERVER, new List<string> { name });
    public static Translatable OpInfo => new(KnownTranslationKeys.OP_INFO);
    public static Translatable OpWarning => new(KnownTranslationKeys.OP_WARNING);
    public static Translatable OpWho => new(KnownTranslationKeys.OP_WHO);
    public static Translatable QueryDisable(string defOption) =>
        new(KnownTranslationKeys.QUERY_DISABLE, new List<string> {defOption});
    public static Translatable QueryWarning1 => new(KnownTranslationKeys.QUERY_WARNING_1);
    public static Translatable QueryWarning2 => new(KnownTranslationKeys.QUERY_WARNING_2);
    public static Translatable SettingUpServerNow => new(KnownTranslationKeys.SETTING_UP_SERVER_NOW);
    public static Translatable ServerProperties => new(KnownTranslationKeys.SERVER_PROPERTIES);
    public static Translatable ServerPortV4(int defPort) => 
        new(KnownTranslationKeys.SERVER_PORT_V4, new List<string> { defPort.ToString() });
    public static Translatable SkyWingWillStart(string name) =>
        new(KnownTranslationKeys.SKYWING_WILL_START, new List<string> {name});
    public static Translatable ViewDistance(int defDistance) =>
        new(KnownTranslationKeys.VIEW_DISTANCE, new List<string> {defDistance.ToString()});
    public static Translatable WhitelistEnable(string defOption) => 
        new(KnownTranslationKeys.WHITELIST_ENABLE, new List<string> { defOption });
    public static Translatable WhitelistInfo => new(KnownTranslationKeys.WHITELIST_INFO);
    public static Translatable WhitelistWarning => new(KnownTranslationKeys.WHITELIST_WARNING);
    public static Translatable YouHaveFinished => new(KnownTranslationKeys.YOU_HAVE_FINISHED);

}

public interface KnownTranslationKeys {

    public const string DEFAULT_GAMEMODE = "default-gamemode";
    public const string DEFAULT_VALUES_INFO = "default-values-info";
    public const string GAMEMODE_ADVENTURE = "gamemode.adventure";
    public const string GAMEMODE_CREATIVE = "gamemode.creative";
    public const string GAMEMODE_INFO = "gamemode-info";
    public const string GAMEMODE_SPECTATOR = "gamemode.spectator";
    public const string GAMEMODE_SURVIVAL = "gamemode.survival";
    public const string INVALID_PORT = "invalid-port";
    public const string IP_CONFIRM = "ip-confirm";
    public const string IP_GET = "ip-get";
    public const string IP_WARNING = "ip-warning";
    public const string LANGUAGE_HAS_BEEN_SELECTED = "language-has-been-selected";
    public const string LANGUAGE_NAME = "language.name";
    public const string MAX_PLAYERS = "max-players";
    public const string NAME_YOUR_SERVER = "name-your-server";
    public const string OP_INFO = "op-info";
    public const string OP_WARNING = "op-warning";
    public const string OP_WHO = "op-who";
    public const string QUERY_DISABLE = "query-disable";
    public const string QUERY_WARNING_1 = "query-warning-1";
    public const string QUERY_WARNING_2 = "query-warning-2";
    public const string SETTING_UP_SERVER_NOW = "setting-up-server-now";
    public const string SERVER_PROPERTIES = "server-properties";
    public const string SERVER_PORT_V4 = "server-port";
    public const string SKYWING_WILL_START = "skywing-will-start";
    public const string VIEW_DISTANCE = "view-distance";
    public const string WHITELIST_ENABLE = "whitelist-enable";
    public const string WHITELIST_INFO = "whitelist-info";
    public const string WHITELIST_WARNING = "whitelist-warning";
    public const string YOU_HAVE_FINISHED = "you-have-finished";

} 