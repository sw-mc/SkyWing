using SkyWing.Utils;

namespace SkyWing.SkyWing.Language; 

public class Language {
    
    public const string FALLBACK_LANGUAGE = "eng";

    // Return Dictionary <Language Code, Language Name>
    public static Dictionary<string, string> GetLanguageList(string path) {
        if (!Directory.Exists(path))
            throw new LanguageNotFoundException($"Language directory {path} does not exist or is not a directory");
        
        var files = (from file in Directory.GetFiles(path) where file[^4..].Equals(".ini") select Path.GetFileName(file)).ToList();
        var list = new Dictionary<string, string>();

        string code;
        Dictionary<string, string> data;
        foreach (var file in files) {
            if (file == null) continue;

            code = file.Split('.')[0];
            data = LoadLang(path, code);
            if (data.ContainsKey("language.name"))
                list.Add(code, data["language.name"]);
        }

        if (list.Count <= 0)
            throw new LanguageNotFoundException($"Language directory {path} does not contain any language files.");
        
        return list;
    }

    public string Name => Get(LangName);

    private string LangName { get; set; }
    private readonly Dictionary<string, string> lang;
    private readonly Dictionary<string, string> fallback;

    public Language(string language, string path, string fallbackLang = FALLBACK_LANGUAGE) {
        LangName = language.ToLower();

        lang = LoadLang(path, LangName);
        
        fallback = !LangName.Equals(fallbackLang) ? LoadLang(path, fallbackLang) : lang;
    }

    public static Dictionary<string, string> LoadLang(string path, string languageCode) {
        var file = Path.Join(path, languageCode + ".ini");
        if (!File.Exists(file))
            throw new LanguageNotFoundException($"Language {languageCode} not found.");
        
        var languageContent = new Dictionary<string, string>();
        var languageFile = new Config(file, ConfigTypes.Properties);
        foreach (var (key, value) in languageFile.GetAll()) {
            languageContent.Add(key, (string) value!);
        }
        return languageContent;

    }

    public string Get(string id) {
        return InternalGet(id) ?? id;
    }
    
    public string? InternalGet(string id) {
        return lang.ContainsKey(id) ? lang[id] : fallback.ContainsKey(id) ? fallback[id] : null;
    }

    public string Translate(string id, List<string> values) {
        var text = Get(id);

        for (var i = 0; i < values.Count; i++) {
            text = text.Replace("{%" + i + "}", Translate(values[i]));
        }

        return text;
    }

    public string Translate(string id, List<Translatable> values) {
        var text = Get(id);

        for (var i = 0; i < values.Count; i++) {
            text = text.Replace("{%" + i + "}", Translate(values[i]));
        }

        return text;
    }

    public string Translate(string id) {
        return Get(id);
    }

    public string Translate(Translatable id) {
        return Translate(id.Text, id.Parameters);
    }
    
}

public class Translatable {
    
    public string Text { get; set; }
    public List<Translatable> Parameters { get; set; }

    public Translatable(string text, List<string> parameters) {
        Text = text;

        Parameters = new List<Translatable>();
        parameters.ForEach(v => {
            Parameters.Add(new Translatable(v));
        });
    }
    
    public Translatable(string text, List<Translatable> parameters) {
        Text = text;
        Parameters = parameters;
    }
    
    public Translatable(string text) {
        Text = text;
        Parameters = new List<Translatable>();
    }

    public Translatable? GetParameter(int i) {
        return Parameters.Count < i ? Parameters[i] : null;
    }
}

public class LanguageNotFoundException : Exception {

    public LanguageNotFoundException(string message) : base(message) {
    }
    
}