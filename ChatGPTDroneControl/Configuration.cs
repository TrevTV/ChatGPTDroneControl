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
        [TomlProperty("preferences")]
        public Preferences Preferences { get; set; } = new();

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

    [TomlDoNotInlineObject]
    public class Preferences
    {
        [TomlProperty("model")]
        public string Model { get; set; } = "gpt-4o-mini";

        [TomlProperty("system_prompt")]
        public string SystemPrompt { get; set; } = @"You are controlling a drone. I want it to simply roam around the environment.

You may stack multiple changes at once, however keep execution order in mind. Only one will be executed at once.

After all tool calls are executed, you will receive a photo of your current position, as well as position information.

Provide a one-sentence reasoning for the given command(s).

You will always start in an idle state, you must take off before you can move the drone.

Try to turn the drone and using forward, rather than just banking in a given direction. It gives you more of an idea of what is around you.

The drone is a DJI Mini SE, which is quite small and gives you room for movement.

Be careful to avoid obstacles, such as trees and buildings.

Always check weather information before taking off to ensure safe conditions. It will be included in the first user message, no need to request it.";
    }
}