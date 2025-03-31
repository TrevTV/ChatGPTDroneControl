using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlet.Attributes;
using Tomlet;

namespace ChatGPTDroneControl;

public static class Configuration
{
    public static Config File { get => _file; }
    private static readonly Config _file;

    public static string DataFolder { get; private set; }

    static Configuration()
    {
        string? dataFolder = Environment.GetEnvironmentVariable("CGDC_DATA_DIRECTORY") ?? null;
        if (dataFolder == null || dataFolder.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            throw new Exception("No data folder given. Please provide one in the environment variable 'CGDC_DATA_DIRECTORY'.");

        CreateDirectory(dataFolder);

        DataFolder = dataFolder;

        string tomlPath = Path.Combine(DataFolder, "chatgpt_drone_control.toml");

        _file = System.IO.File.Exists(tomlPath) ? TomletMain.To<Config>(System.IO.File.ReadAllText(tomlPath)) : new();

        // update the saved file with any new config options
        System.IO.File.WriteAllText(tomlPath, TomletMain.TomlStringFrom(_file));
    }

    private static void CreateDirectory(string dir)
    {
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public class Config
    {
        [TomlProperty("apis")]
        public APIs APIs { get; set; } = new();
    }

    [TomlDoNotInlineObject]
    public class APIs
    {
        [TomlProperty("weather_apikey")]
        public string WeatherAPIKey { get; set; } = "";

        [TomlProperty("weather_station_id")]
        public string WeatherStationId { get; set; } = "";

        [TomlProperty("drone_ip_port")]
        public string DroneIPPort { get; set; } = "";
    }
}