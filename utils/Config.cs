using System.Collections;
using Newtonsoft.Json;

namespace SkyWing.Utils;

public class Config {

    private Dictionary<string, object?> config = new();
    private readonly Dictionary<string, object?> nestedCache = new();

    private FileStream? file;
    private StreamReader? reader;
    private StreamWriter? writer;

    private ConfigTypes type = ConfigTypes.Detect;

    public bool Changed { get; private set; }

    private static readonly Dictionary<string, ConfigTypes> formats = new() {
        {"properties", ConfigTypes.Properties},
        {"cnf", ConfigTypes.Cnf},
        {"conf", ConfigTypes.Cnf},
        {"config", ConfigTypes.Cnf},
        {"json", ConfigTypes.Json},
        {"js", ConfigTypes.Json},
    };

    public Config(string file, ConfigTypes type, Dictionary<string, object?>? def = null) {
        Load(file, type, def ?? new Dictionary<string, object?>());
    }

    private async void Load(string filePath, ConfigTypes configType, Dictionary<string, object?> def) {
        type = configType;

        if (type == ConfigTypes.Detect) {
            var extension = Path.GetExtension(filePath).ToLower();
            if (formats.ContainsKey(extension))
                type = formats[extension];
        }
        else {
            throw new ArgumentException("Cannot detect config type of " + file);
        }

        file = File.Open(filePath, File.Exists(filePath) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite);
        reader = new StreamReader(file);
        writer = new StreamWriter(file);


        if (!File.Exists(filePath)) {
            config = def;
            Save();
        }
        else {
            try {
                var content = await reader.ReadToEndAsync();
                switch (this.type) {
                    case ConfigTypes.Properties:
                        config = ReadProperties(content);
                        break;
                    case ConfigTypes.Json:
                        var jsonReader = new JsonTextReader(new StringReader(content));
                        config = new Dictionary<string, object?>();

                        await jsonReader.ReadAsync(); // Opens default JSON object (this does not have a property name)
                        CreateJsonObject(config, jsonReader);
                        break;
                    default:
                        throw new ArgumentException("Invalid config type specified.", nameof(type));
                }
                if (config == null || config.GetType() != typeof(Dictionary<string, object?>))
                    throw new ConfigLoadException("Failed to load config. Possible corruption or syntax error.");

                if (await FillDefaults(def, config) > 0) {
                    Save();
                }
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

    public async void SetDefaults(Dictionary<string, object?> def) {
        await FillDefaults(def, config);
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    

    private async Task<int> FillDefaults(Dictionary<string, object?> def, Dictionary<string, object?> data) {
        return await Task.Run(async () => {
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

                    changed += await FillDefaults((Dictionary<string, object?>) value,
                        (Dictionary<string, object?>) data[key]!);
                }
                else if (def.GetType() == typeof(IList)) {
                    if (!data.ContainsKey(key) || data[key]!.GetType() != typeof(IList))
                        data[key] = new List<object?>();

                    changed += await FillDefaults((List<object?>) value, (List<object?>) data[key]!);

                }
                else if (!data.ContainsKey(key)) {
                    data[key] = value;
                    ++changed;
                }
            }

            return changed;
        });
    }

    private async Task<int> FillDefaults(IReadOnlyList<object?> def, IList<object?> data) {
        return await Task.Run(async () => {
            var changed = 0;
            for (var i = 0; i < def.Count; i++) {
                if (def[i]!.GetType() == typeof(IDictionary)) {
                    if (data.Count < i || data[i]!.GetType() != typeof(IDictionary))
                        data[i] = new Dictionary<string, object?>();

                    changed += await FillDefaults((Dictionary<string, object?>) def[i]!,
                        (Dictionary<string, object?>) data[i]!);
                }
                else if (def[i]!.GetType() == typeof(IList)) {
                    if (data.Count < i || data[i]!.GetType() != typeof(IList))
                        data[i] = new List<object?>();

                    changed += await FillDefaults((List<object?>) def[i]!, (List<object?>) data[i]!);
                }
                else if (data.Count < i) {
                    data[i] = def[i];
                }
            }

            return changed;
        });
    }

    public async void Save() {
        switch (type) {
            case ConfigTypes.Json:
                await writer!.WriteAsync(JsonConvert.SerializeObject(config, Formatting.Indented));
                break;
            case ConfigTypes.Properties:
                await writer!.WriteAsync(WriteProperties(config));
                break;
        }

        await writer!.FlushAsync();
    }

    private async void CreateJsonObject(Dictionary<string, object?> location, JsonTextReader jsonReader) {
        string? name = null;
        while (await jsonReader.ReadAsync()) {
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

    private async void CreateJsonArray(List<object?> location, JsonTextReader jsonReader) {
        while (await jsonReader.ReadAsync()) {
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
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("'") || !line.Contains('='))
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
}

public class ConfigLoadException : Exception {

    public ConfigLoadException(string message) : base(message) {
    }
}

public enum ConfigTypes {
    
    Detect = -1,
    Properties,Cnf = 0,
    Json = 1,
    
}

public enum JsonSaveOptions {
    
    PrettyPrint = 0,
    Default = 1
    
}
