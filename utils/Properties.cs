namespace SkyWing.SkyWing.Utils; 

public class Properties {
    private readonly Dictionary<string, string> _properties = new Dictionary<string, string>();
    private FileInfo _file { get; set; }

    public Properties(FileInfo file, bool read = true) {
        _file = file;

        if(read) {
            Parse();
        }
    }
    
    public void Create(object properties) {
        if(_file.Exists) 
            return;
        
        // Get the class type of the object.
        var classType = properties.GetType();
        
        // Check if the properties is serializable.
        if(!classType.IsSerializable)
            return;
            
        // Serialize the properties into a foreach-able object.
        var objectProperties = classType.GetProperties();
        foreach(var property in objectProperties) {
            var value = property.GetValue(properties);
            if(value != null) {
                _properties.Add(property.Name, value.ToString() ?? "");
            }
        }
        
        // Write the file to the file system.
        WriteToFile();
    }

    public void Parse() {
        if(!_file.Exists) {
            return;
        }
        
        // Read the content of the file.
        var lines = File.ReadAllLines(_file.FullName);

        // Import all lines as a property.
        foreach(var line in lines) {
            if(!line.Contains("=") || line.StartsWith("#")) {
                continue;
            }
            
            // Get the property name and value.
            var split = line.Split('=');
            _properties.Add(split[0], split[1]);
        }
    }

    public bool ValueExists(string key) {
        return _properties.ContainsKey(key);
    }
    
    public string GetValue(string key, string fallback = "") {
        return _properties.ContainsKey(key) ? _properties[key] : fallback;
    }
    
    public void SetValue(string key, string value) {
        if(_properties.ContainsKey(key)) {
            _properties[key] = value;
        } else {
            _properties.Add(key, value);
        }
    }
    
    public void WriteToFile() {
        // Create a collection of lines from properties.
        var lines = new List<string>();
        foreach(var property in _properties) {
            lines.Add($"{property.Key}={property.Value}");
        }
        
        // Write all the lines to the file.
        File.WriteAllLines(_file.FullName, lines);
    }
}