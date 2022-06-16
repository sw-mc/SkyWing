using System.Collections;
using Newtonsoft.Json;

namespace SkyWing.Utils;

public class Config {

    private Dictionary<string, object?> config = new();
    private readonly Dictionary<string, object?> nestedCache = new();

    private string? filePath;

    private ConfigTypes type = ConfigTypes.Detect;

    public bool Changed { get; private set; }

    private static readonly Dictionary<string, ConfigTypes> formats = new() {
        {"properties", ConfigTypes.Properties},
        {"cnf", ConfigTypes.Cnf},
        {"conf", ConfigTypes.Cnf},
        {"config", ConfigTypes.Cnf},
        {"json", ConfigTypes.Json},
        {"js", ConfigTypes.Json},
        {"txt", ConfigTypes.Enum}
    };

    public Config(string file, ConfigTypes type, Dictionary<string, object?>? def = null) {
        Load(file, type, def ?? new Dictionary<string, object?>());
    }

    private void Load(string path, ConfigTypes configType, Dictionary<string, object?> def) {
        filePath = path;
        type = configType;

        if (type == ConfigTypes.Detect) {
            var extension = Path.GetExtension(filePath).ToLower();
            if (formats.ContainsKey(extension)) {
                type = formats[extension];
            }
            else {
                throw new ArgumentException("Cannot detect config type of " + path);
            }
        }

        if (!File.Exists(filePath)) {
            config = def;
            Save();
        }
        else {
            try {
                var content = File.ReadAllText(filePath);
                switch (type) {
                    case ConfigTypes.Properties:
                        config = ReadProperties(content);
                        break;
                    case ConfigTypes.Json:
                        var jsonReader = new JsonTextReader(new StringReader(content));
                        config = new Dictionary<string, object?>();

                        jsonReader.ReadAsync(); // Opens default JSON object (this does not have a property name)
                        CreateJsonObject(config, jsonReader);
                        break;
                    case ConfigTypes.Enum:
                        config = ReadList(content);
                        break;
                    default:
                        throw new ArgumentException("Invalid config type specified.", nameof(type));
                }
                if (config == null || config.GetType() != typeof(Dictionary<string, object?>))
                    throw new ConfigLoadException("Failed to load config. Possible corruption or syntax error.");

                //if  FillDefaults(def, config) > 0) {
                 //   Save();
                //}
            }
            catch (Exception e) {
                throw new ConfigLoadException("Could not load config: " + e.Message);
            }
        }
    }

    public bool Exists(string key) => config.ContainsKey(key);

    public T Get<T>(string key, T def) {
        if (!config.ContainsKey(key)) return def;

        try {
            return (T) Convert.ChangeType(config[key]!, typeof(T));
        }
        catch (InvalidCastException) {
            return def;
        }
    }

    public T GetNested<T>(string key, T def) {
        if (nestedCache.ContainsKey(key)) {
            try {
                return (T) Convert.ChangeType(nestedCache[key]!, typeof(T));
            }
            catch (InvalidCastException) {
                return def;
            }
        }

        object? b;
        var steps = key.Split('.');
        key = steps[0];
        if (config.ContainsKey(key)) {
            b = config[key];
        }
        else {
            return def;
        }
        
        for (var i = 1; i < steps.Length; i++) {
            key = steps[i];
            if (b != null && b.GetType() == typeof(IDictionary) && ((IDictionary) b).Contains(key)) {
                b = ((IDictionary) b)[key];
            }
            else {
                return def;
            }
        }
        
        try {
            nestedCache[key] = b;
            return (T) Convert.ChangeType(b!, typeof(T));
        }
        catch (InvalidCastException) {
            return def;
        }
    }

    public void Set(string key, object? value) {
        config[key] = value;
        Changed = true;
    }

    public void SetNested(string key, object? value) {
        var steps = key.Split('.');
        var b = config;
        for (var i = 0; i < steps.Length-1; i++) {
            if (!b.ContainsKey(steps[i])) {
                b[steps[i]] = new Dictionary<string, object?>();
            }
            b = (Dictionary<string, object?>) b[steps[i]]!;
        }

        b[steps[^1]] = value;
        nestedCache.Clear();
        Changed = true;
    }

    public void SetAll(Dictionary<string, object?> values) {
        config = values;
        Changed = true;
    }

    public void Remove(string key) {
        config.Remove(key);
        Changed = true;
    }

    public void RemoveNested(string key) {
        nestedCache.Clear();
        Changed = true;
        var steps = key.Split('.');
        var b = config;
        for (var i = 0; i < steps.Length; i++) {
            key = steps[i];
            if (!b.ContainsKey(key))
                break;
            
            if (i + 1 >= steps.Length) {
                b.Remove(key);
            } else if (b[key]!.GetType() == typeof(IDictionary)) {
                b = (Dictionary<string, object?>) b[key]!;
            }
        }
    }

    public Dictionary<string, object?> GetAll() {
        return config;
    }

    public List<object?> GetAllNoKeys() {
        return config.Values.ToList();
    }

    public void SetDefaults(Dictionary<string, object?> def) {
        FillDefaults(def, config);
    }

    private int FillDefaults(Dictionary<string, object?> def, Dictionary<string, object?> data) {
        var changed = 0;
        foreach (var (key, value) in def) {
            if (!data.ContainsKey(key) && value == null) {
                data[key] = value;
                ++changed;
            }
            else if (value == null) {

            }
            else if (value.GetType() == typeof(IDictionary)) {
                if (!data.ContainsKey(key) || data[key]!.GetType() != typeof(IDictionary))
                    data[key] = new Dictionary<string, object?>();

                changed += FillDefaults((Dictionary<string, object?>) value,
                    (Dictionary<string, object?>) data[key]!);
            }
            else if (def.GetType() == typeof(IList)) {
                if (!data.ContainsKey(key) || data[key]!.GetType() != typeof(IList))
                    data[key] = new List<object?>();

                changed += FillDefaults((List<object?>) value, (List<object?>) data[key]!);

            }
            else if (!data.ContainsKey(key)) {
                data[key] = value;
                ++changed;
            }
        }

        return changed;
    }

    private int FillDefaults(IReadOnlyList<object?> def, IList<object?> data) {
        var changed = 0;
        for (var i = 0; i < def.Count; i++) {
            if (def[i]!.GetType() == typeof(IDictionary)) {
                if (data.Count < i || data[i]!.GetType() != typeof(IDictionary))
                    data[i] = new Dictionary<string, object?>();

                changed += FillDefaults((Dictionary<string, object?>) def[i]!,
                    (Dictionary<string, object?>) data[i]!);
            }
            else if (def[i]!.GetType() == typeof(IList)) {
                if (data.Count < i || data[i]!.GetType() != typeof(IList))
                    data[i] = new List<object?>();

                changed += FillDefaults((List<object?>) def[i]!, (List<object?>) data[i]!);
            }
            else if (data.Count < i) {
                data[i] = def[i];
            }
        }

        return changed;
    }

    public void Save() {
        switch (type) {
            case ConfigTypes.Json:
                File.WriteAllText(filePath!, JsonConvert.SerializeObject(config, Formatting.Indented));
                break;
            case ConfigTypes.Properties:
                File.WriteAllText(filePath!, WriteProperties(config));
                break;
            case ConfigTypes.Enum:
                File.WriteAllText(filePath!, WriteList(config));
                break;
        }
    }

    private void CreateJsonObject(Dictionary<string, object?> location, JsonTextReader jsonReader) {
        string? name = null;
        while (jsonReader.Read()) {
            switch (jsonReader.TokenType) {
                case JsonToken.StartObject:
                    var collection = new Dictionary<string, object?>();
                    CreateJsonObject(collection, jsonReader);
                    location.Add(
                        name ?? throw new ConfigLoadException(
                            "Error loading JSON file: json property must have a name."), collection);
                    break;
                case JsonToken.StartArray:
                    var list = new List<object?>();
                    CreateJsonArray(list, jsonReader);
                    location.Add(
                        name ?? throw new ConfigLoadException(
                            "Error loading JSON file: json property must have a name."), list);
                    break;
                case JsonToken.PropertyName:
                    name = (string) (jsonReader.Value ??
                                     throw new ConfigLoadException(
                                         "Error loading JSON file: expecting property name got NULL."));
                    break;
                case JsonToken.EndObject:
                    return;
                case JsonToken.Raw:
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Null:
                case JsonToken.Undefined:
                case JsonToken.Date:
                case JsonToken.Bytes:
                default: // Value
                    location.Add(
                        name ?? throw new ConfigLoadException(
                            "Error loading JSON file: json property must have a name."), jsonReader.Value);
                    break;
            }
        }
    }

    private void CreateJsonArray(List<object?> location, JsonTextReader jsonReader) {
        while (jsonReader.Read()) {
            switch (jsonReader.TokenType) {
                case JsonToken.StartObject:
                    var collection = new Dictionary<string, object?>();
                    CreateJsonObject(collection, jsonReader);
                    location.Add(collection);
                    break;
                case JsonToken.StartArray:
                    var list = new List<object?>();
                    CreateJsonArray(list, jsonReader);
                    location.Add(list);
                    break;
                case JsonToken.EndObject:
                    return;
                case JsonToken.EndArray:
                    return;
                case JsonToken.Raw:
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Null:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    location.Add(jsonReader.Value);
                    break;
            }
        }
    }

    public static string WriteProperties(Dictionary<string, object?> config) {
        var writeBuffer = "";
        foreach (var (key, value) in config) {
            writeBuffer += $"{key}={value}\n";
        }
        return writeBuffer;
    }

    public static Dictionary<string, object?> ReadProperties(string content) {
        var config = new Dictionary<string, object?>();
        foreach (var line in content.Split("\n")) {
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#") ||
                line.StartsWith("'") ||
                !line.Contains('='))
                continue;

            var equalIndex = line.IndexOf('=');
            var key = line[..equalIndex].Trim();
            object value = line[(equalIndex + 1)..].Trim();
            if (((string) value).StartsWith("\"") && ((string) value).EndsWith("\"") ||
                ((string) value).StartsWith("'") && ((string) value).EndsWith("'")) {
                value = ((string) value).Substring(1, ((string) value).Length - 2);
            }

            switch ((string) value) {
                case "on":
                case "yes":
                case "true":
                    value = true;
                    break;
                case "off":
                case "no":
                case "false":
                    value = false;
                    break;
                default: {
                    if (int.TryParse((string) value, out var number)) {
                        value = number;
                    }
                    break;
                }
            }

            config.Add(key, value);
        }

        return config;

    }

    public static string WriteList(Dictionary<string, object?> config) {
        return string.Join("\n", config.Keys.ToArray());
    }

    public static Dictionary<string, object?> ReadList(string content) {
        var split = content.Replace("\r\n", "\n").Split("\n");
        return split.ToDictionary<string, string, object?>(value => value, _ => true);
        
    }
}

public class ConfigLoadException : Exception {

    public ConfigLoadException(string message) : base(message) {
    }
}

public enum ConfigTypes {
    
    Detect = -1,
    Properties,Cnf = 0,
    Json = 1,
    Enum = 2
    
}

public enum JsonSaveOptions {
    
    PrettyPrint = 0,
    Default = 1
    
}
