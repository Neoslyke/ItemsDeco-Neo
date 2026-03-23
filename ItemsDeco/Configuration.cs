using Newtonsoft.Json;
using TShockAPI;

namespace ItemsDeco;

public class Configuration
{
    private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "ItemsDeco.json");

    [JsonProperty("ItemAboveHead")]
    public ItemAboveHeadConfig ItemAboveHead { get; set; } = new();

    [JsonProperty("ItemText")]
    public ItemTextConfig ItemText { get; set; } = new();

    [JsonProperty("ItemChat")]
    public ItemChatConfig ItemChat { get; set; } = new();

    public static Configuration Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var config = new Configuration();
                config.Save();
                return config;
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<Configuration>(json) ?? new Configuration();
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[ItemsDeco] Error loading config: {ex.Message}");
            return new Configuration();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[ItemsDeco] Error saving config: {ex.Message}");
        }
    }
}

public class ItemAboveHeadConfig
{
    [JsonProperty("Enabled")]
    public bool Enabled { get; set; } = true;
}

public class ItemTextConfig
{
    [JsonProperty("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("ShowName")]
    public bool ShowName { get; set; } = true;

    [JsonProperty("ShowDamage")]
    public bool ShowDamage { get; set; } = true;

    [JsonProperty("DamageText")]
    public string DamageText { get; set; } = "Damage";

    [JsonProperty("DefaultColor")]
    public ColorConfig DefaultColor { get; set; } = new() { R = 255, G = 255, B = 255 };

    [JsonProperty("RarityColors")]
    public Dictionary<int, ColorConfig> RarityColors { get; set; } = new()
    {
        { -1, new ColorConfig { R = 169, G = 169, B = 169 } },
        { 0, new ColorConfig { R = 255, G = 255, B = 255 } },
        { 1, new ColorConfig { R = 0, G = 128, B = 0 } },
        { 2, new ColorConfig { R = 0, G = 112, B = 221 } },
        { 3, new ColorConfig { R = 128, G = 0, B = 128 } },
        { 4, new ColorConfig { R = 255, G = 128, B = 0 } },
        { 5, new ColorConfig { R = 255, G = 0, B = 0 } },
        { 6, new ColorConfig { R = 255, G = 215, B = 0 } },
        { 7, new ColorConfig { R = 255, G = 105, B = 180 } },
        { 8, new ColorConfig { R = 255, G = 215, B = 0 } },
        { 9, new ColorConfig { R = 0, G = 255, B = 255 } },
        { 10, new ColorConfig { R = 255, G = 105, B = 180 } },
        { 11, new ColorConfig { R = 75, G = 0, B = 130 } }
    };
}

public class ItemChatConfig
{
    [JsonProperty("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("ShowName")]
    public bool ShowName { get; set; } = true;

    [JsonProperty("ShowDamage")]
    public bool ShowDamage { get; set; } = true;

    [JsonProperty("ItemColor")]
    public ColorConfig ItemColor { get; set; } = new() { R = 255, G = 255, B = 255 };

    [JsonProperty("DamageColor")]
    public ColorConfig DamageColor { get; set; } = new() { R = 0, G = 255, B = 255 };
}

public class ColorConfig
{
    [JsonProperty("R")]
    public int R { get; set; } = 255;

    [JsonProperty("G")]
    public int G { get; set; } = 255;

    [JsonProperty("B")]
    public int B { get; set; } = 255;

    public string ToHex()
    {
        return $"{R:X2}{G:X2}{B:X2}";
    }
}