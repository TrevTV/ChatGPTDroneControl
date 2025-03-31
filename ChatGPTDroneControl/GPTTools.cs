using DJIControlClient;
using OpenAI.Chat;
using OpenAI.Responses;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ChatGPTDroneControl;

public static class GPTTools
{
    private static HttpClient _client => Program.HttpClient;
    private static Drone _drone => Program.DroneClient;

    public static ResponseTool[] ResponseTools { get; } =
    {
        ResponseTool.CreateFunctionTool(
            functionName: nameof(GetWeatherInfo),
            functionDescription: "Get the weather info for the location of the drone.",
            BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {},
                "required": []
            }
            """u8.ToArray())
        )
    };

    public static async Task<FunctionCallOutputResponseItem> HandleFunctionCall(FunctionCallResponseItem toolCall)
    {
        string toolOutput = "success";

        toolOutput = toolCall.FunctionName switch
        {
            nameof(GetWeatherInfo) => await GetWeatherInfo(),
            _ => throw new NotImplementedException(),
        };
        return ResponseItem.CreateFunctionCallOutputItem(toolCall.CallId, toolOutput);
    }

    #region Weather

    public static readonly ResponseTool GetWeatherInfoTool = 

    public static async Task<string> GetWeatherInfo()
    {
        if (string.IsNullOrWhiteSpace(Configuration.File.APIs.WeatherAPIKey) || string.IsNullOrWhiteSpace(Configuration.File.APIs.WeatherStationId))
            return "Weather info has not been configured.";

        const string WEATHER_API_FORMAT = "https://api.weather.com/v2/pws/observations/current?apiKey={0}&stationId={1}&numericPrecision=decimal&format=json&units=e";

        string apiUrl = string.Format(WEATHER_API_FORMAT, Configuration.File.APIs.WeatherAPIKey, Configuration.File.APIs.WeatherStationId);

        HttpResponseMessage resp = await _client.GetAsync(apiUrl);
        WeatherStationInfo? info = await resp.Content.ReadFromJsonAsync<WeatherStationInfo>();
        if (info == null)
            return "Weather info is currently unavailable.";

        Observation obs = info.Observations.First();

        return $@"Time Observed (UTC): {obs.ObsTimeUtc}
Wind Direction: {obs.WindDirection}
Wind Speed (MPH): {obs.Imperial["windSpeed"]}
Wind Gust (MPH): {obs.Imperial["windGust"]}
Humidity: {obs.Humidity}
Precipitation Rate: {obs.Imperial["precipRate"]}
Precipitation Total: {obs.Imperial["precipTotal"]}";
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private class WeatherStationInfo
    {
        [JsonPropertyName("observations")]
        public Observation[] Observations { get; set; }
    }

    private class Observation
    {
        [JsonPropertyName("stationID")]
        public string StationId { get; set; }

        [JsonPropertyName("obsTimeUtc")]
        public string ObsTimeUtc { get; set; }

        [JsonPropertyName("obsTimeLocal")]
        public string ObsTimeLocal { get; set; }

        [JsonPropertyName("neighborhood")]
        public string Neighborhood { get; set; }

        [JsonPropertyName("softwareType")]
        public string SoftwareType { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("solarRadiation")]
        public double SolarRadiation { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("realtimeFrequency")]
        public object RealtimeFrequency { get; set; }

        [JsonPropertyName("epoch")]
        public long Epoch { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("winddir")]
        public long WindDirection { get; set; }

        [JsonPropertyName("humidity")]
        public float Humidity { get; set; }

        [JsonPropertyName("imperial")]
        public Dictionary<string, double> Imperial { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    #endregion
}