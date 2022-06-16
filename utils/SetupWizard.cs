using System.Net;
using SkyWing.SkyWing.language;
using SkyWing.SkyWing.player;
using SkyWing.Utils;

namespace SkyWing.SkyWing.Utils;

using Language;
using static Console;

public class SetupWizard {

    private readonly string path;
    private Language? Lang { get; set; }

    public SetupWizard(string path) {
        this.path = path;
    }

    public bool Run() {
        WriteLine(VersionInfo.NAME + " set-up wizard.");

        Dictionary<string, string> langList;
        try {
            langList = Language.GetLanguageList(path);
        }
        catch (LanguageNotFoundException) {
            Error("No language files found, please use correct builds or clone recursively.");
            return false;
        }
        
        WriteLine("Please select a language.");
        foreach (var (key, value) in langList) {
            WriteLine($" {value} => {key}");
        }
        
        string? langInput;
        do {
            Write("[?] Language (" + Language.FALLBACK_LANGUAGE + "): ");
            langInput = ReadLine() ?? Language.FALLBACK_LANGUAGE;
            langInput = !langInput.Equals("") ? langInput : Language.FALLBACK_LANGUAGE;
            if (langList.ContainsKey(langInput))
                continue;
            
            Error("Couldn't find the language");
            langInput = null;
        } while (langInput == null);

        Lang = new Language(langInput, CoreConstants.LocalDataPath);
        Message(Lang.Translate(KnownTranslationFactory.LanguageHasBeenSelected));

        var config = new Config(Path.Join(CoreConstants.DataPath, "server.properties"), ConfigTypes.Properties);
        config.Set("language", langInput);
        config.Save();
        
        Welcome();
        GenerateBaseConfig();
        GenerateUserFiles();
        NetworkFunctions();
        
        PrintIpDetails();
        EndWizard();
        return true;
    }

    private void Welcome() {
        Message(Lang!.Translate(KnownTranslationFactory.SettingUpServerNow));
        Message(Lang!.Translate(KnownTranslationFactory.DefaultValuesInfo));
        Message(Lang!.Translate(KnownTranslationFactory.ServerProperties));
    }

    private void GenerateBaseConfig() {
        var config = new Config(Path.Join(CoreConstants.DataPath, "server.properties"), ConfigTypes.Properties);

        Write("[?] " + Lang!.Translate(KnownTranslationFactory.NameYourServer(Server.DEFAULT_SERVER_NAME)) + ": ");
        var name = ReadLine() ?? Server.DEFAULT_SERVER_NAME;
        config.Set("motd", name.Equals("") ? Server.DEFAULT_SERVER_NAME : name);
        config.Set("server-name", name.Equals("") ? Server.DEFAULT_SERVER_NAME : name);
        
        config.Set("server-port", AskPort(KnownTranslationFactory.ServerPortV4(Server.DEFAULT_PORT_IPV4), Server.DEFAULT_PORT_IPV4));
        
        Message(Lang!.Translate(KnownTranslationFactory.GamemodeInfo));
        string input;
        GameMode? gameMode;
        do {
            Write("[?] " + Lang!.Translate(KnownTranslationFactory.DefaultGamemode(0)) + ": ");
            input = ReadLine() ?? GameModeIdMap.ToId(GameMode.FromString("survival")!).ToString();
            gameMode = GameModeIdMap.FromId(
                int.Parse(input.Equals("") ? GameModeIdMap.ToId(GameMode.FromString("survival")!).ToString() : input));
        } while (gameMode == null);

        Write("[?] " + Lang!.Translate(KnownTranslationFactory.MaxPlayers(Server.DEFAULT_MAX_PLAYERS)) + ": ");
        input = ReadLine() ?? Server.DEFAULT_MAX_PLAYERS.ToString();
        config.Set("max-players", int.Parse(input.Equals("") ? Server.DEFAULT_MAX_PLAYERS.ToString() : input));

        Write("[?] " + Lang!.Translate(KnownTranslationFactory.ViewDistance(Server.DEFAULT_MAX_VIEW_DISTANCE)) + ": ");
        input = ReadLine() ?? Server.DEFAULT_MAX_VIEW_DISTANCE.ToString();
        config.Set("view-distance", int.Parse(input.Equals("") ? Server.DEFAULT_MAX_VIEW_DISTANCE.ToString() : input));

        config.Save();
    }
    
    private void GenerateUserFiles() {
        Message(Lang!.Translate(KnownTranslationFactory.OpInfo));

        Write("[?] " + Lang!.Translate(KnownTranslationFactory.OpWho) + ": ");
        var op = (ReadLine() ?? "").ToLower();
        if (op.Equals("")) {
            Error(Lang!.Translate(KnownTranslationFactory.OpWarning));
        }
        else {
            var ops = new Config(Path.Join(CoreConstants.DataPath, "ops.txt"), ConfigTypes.Enum);
            ops.Set(op, true);
            ops.Save();
        }
        
        Message(Lang!.Translate(KnownTranslationFactory.WhitelistInfo));
        
        var config = new Config(Path.Join(CoreConstants.DataPath, "server.properties"), ConfigTypes.Properties);
        Write("[?] " + Lang!.Translate(KnownTranslationFactory.WhitelistEnable("n")) + ": ");
        if ((ReadLine() ?? "").ToLower().Equals("y")) {
            Error(Lang!.Translate(KnownTranslationFactory.WhitelistWarning));
            config.Set("white-list", true);
        }
        else {
            config.Set("white-list", false);
        }
        config.Save();
    }

    private void NetworkFunctions() {
        var config = new Config(Path.Join(CoreConstants.DataPath, "server.properties"), ConfigTypes.Properties);
        Error(Lang!.Translate(KnownTranslationFactory.QueryWarning1));
        Error(Lang!.Translate(KnownTranslationFactory.QueryWarning2));
        Write("[?] " + Lang!.Translate(KnownTranslationFactory.QueryDisable("n")) + ": ");
        config.Set("enable-query", !(ReadLine() ?? "").ToLower().Equals("y"));
        config.Save();
    }

    private void PrintIpDetails() {
        Message(Lang!.Translate(KnownTranslationFactory.IpGet));
        
        //var externalIp = Encoding.UTF8.GetString(new WebClient().DownloadData("http://whatismyip.com/automation/n09230945.asp"));
        var internalIp = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();

        Error(Lang!.Translate(KnownTranslationFactory.IpWarning()));
        Error(Lang!.Translate(KnownTranslationFactory.IpConfirm));
        ReadLine();
    }

    private void EndWizard() {
        Message(Lang!.Translate(KnownTranslationFactory.YouHaveFinished));
        Message(Lang!.Translate(KnownTranslationFactory.SkyWingWillStart(VersionInfo.NAME)));
        
        WriteLine("");
        WriteLine("");
        
        Thread.Sleep(4000);
    }

    private int AskPort(Translatable prompt, int defPort) {
        while (true) {
            Write("[?] " + Lang!.Translate(prompt) + ": ");
            var input = ReadLine() ?? defPort.ToString();
            var port = int.Parse(input.Equals("") ? defPort.ToString() : input);
            if (port is > 0 and <= 65535)
                return port;
            
            Error(Lang!.Translate(KnownTranslationFactory.InvalidPort));
        }
    }

    private static void Message(string message) {
        WriteLine("[*] " + message);
    }

    private static void Error(string message) {
        ForegroundColor = ConsoleColor.Red;
        WriteLine("[!] " + message);
        ResetColor();
    }
}