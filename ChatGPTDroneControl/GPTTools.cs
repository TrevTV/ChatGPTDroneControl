using DJIControlClient;
using OpenAI.Chat;
using OpenAI.Responses;
using System.Net.Http.Json;
using System.Text.Json;
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
            """u8.ToArray())),
        ResponseTool.CreateFunctionTool(
            functionName: nameof(Takeoff),
            functionDescription: "Turns on the drone rotors and sends it up a few meters.",
            BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {},
                "required": []
            }
            """u8.ToArray())),
        ResponseTool.CreateFunctionTool(
            functionName: nameof(Land),
            functionDescription: "Tells the drone to descend, before turning off the rotors. Only use when above a very flat and open area.",
            BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {},
                "required": []
            }
            """u8.ToArray())),
        ResponseTool.CreateFunctionTool(
            functionName: nameof(GetWeatherInfo),
            functionDescription: "Get the weather info for the location of the drone.",
            BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {},
                "required": []
            }
            """u8.ToArray())),
        ResponseTool.CreateFunctionTool(
            functionName: nameof(MoveDirection),
            functionDescription: "Moves the drone in the given direction. Returns when the planned flight paths is entirely traversed.",
            BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "direction": {
                        "type": "string",
                        "enum": ["Forward", "Backward", "Left", "Right", "Up", "Down"],
                        "description": "The direction for the drone to move in."
                    },
                    "distance": {
                        "type": "number",
                        "description": "The distance to move, in meters."
                    }
                },
                "required": ["direction", "distance"],
                "additionalProperties": false
            }
            """u8.ToArray()),
            functionSchemaIsStrict: true),
        ResponseTool.CreateFunctionTool(
            functionName: nameof(TurnDirection),
            functionDescription: "Turns the drone in the given direction. Returns when the planned flight paths is entirely traversed.",
            BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "direction": {
                        "type": "string",
                        "enum": ["Clockwise", "CounterClockwise"],
                        "description": "The direction for the drone to turn."
                    },
                    "angle": {
                        "type": "number",
                        "description": "The angle to turn, in degrees."
                    }
                },
                "required": ["direction", "angle"],
                "additionalProperties": false
            }
            """u8.ToArray()),
            functionSchemaIsStrict: true),
        ResponseTool.CreateFunctionTool(
            functionName: nameof(SetCameraPitch),
            functionDescription: "Sets the camera's vertical rotation.",
            BinaryData.FromBytes("""
            {
                "type": "object",
                "properties": {
                    "angle": {
                        "type": "number",
                        "description": "The angle to turn, in degrees. 90 is straight down. 0 is pointing forwards. 0-90 is the valid range."
                    }
                },
                "required": ["angle"],
                "additionalProperties": false
            }
            """u8.ToArray()),
            functionSchemaIsStrict: true),
    };

    public static async Task<FunctionCallOutputResponseItem> HandleFunctionCall(FunctionCallResponseItem toolCall)
    {
        string toolOutput = toolCall.FunctionName switch
        {
            nameof(GetWeatherInfo) => await GetWeatherInfo(),
            nameof(Takeoff) => await Takeoff(),
            nameof(Land) => await Land(),
            nameof(MoveDirection) => await MoveDirection(toolCall),
            nameof(TurnDirection) => await TurnDirection(toolCall),
            nameof(SetCameraPitch) => await SetCameraPitch(toolCall),
            _ => throw new NotImplementedException(),
        };
        return ResponseItem.CreateFunctionCallOutputItem(toolCall.CallId, toolOutput);
    }

    #region Drone - Takeoff + Landing

    private static async Task<string> Takeoff()
    {
        await _drone.Takeoff();
        return "success";
    }

    private static async Task<string> Land()
    {
        await _drone.Land();
        return "success";
    }

    #endregion

    #region Drone - SetCameraPitch
    private static async Task<string> SetCameraPitch(FunctionCallResponseItem toolCall)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
        bool hasAngle = argumentsJson.RootElement.TryGetProperty("angle", out JsonElement rawAngle);
        if (!hasAngle)
            throw new ArgumentNullException("angle");

        float angle = 0f;

        if (rawAngle.TryGetDouble(out double dAngle))
            angle = (float)dAngle;
        else
            throw new Exception($"Angle {rawAngle} is not valid");


        await _drone.SetGimbalPitch(angle);

        return "success";
    }
    #endregion

    #region Drone - TurnDirection

    private enum TurnDir
    {
        CounterClockwise,
        Clockwise
    }

    private static async Task<string> TurnDirection(FunctionCallResponseItem toolCall)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
        bool hasDirection = argumentsJson.RootElement.TryGetProperty("direction", out JsonElement rawDirection);
        bool hasAngle = argumentsJson.RootElement.TryGetProperty("angle", out JsonElement rawAngle);
        if (!hasDirection)
            throw new ArgumentNullException("direction");
        if (!hasAngle)
            throw new ArgumentNullException("angle");

        TurnDir turnDir = TurnDir.Clockwise;
        float angle = 0f;

        if (Enum.TryParse(typeof(TurnDir), rawDirection.GetString(), false, out object? possibleDir))
            turnDir = (TurnDir)possibleDir;
        else
            throw new Exception($"Direction {rawDirection.GetString()} is not valid");

        if (rawAngle.TryGetDouble(out double dAngle))
            angle = (float)dAngle;
        else
            throw new Exception($"Angle {rawAngle} is not valid");


        switch (turnDir)
        {
            case TurnDir.Clockwise:
                await _drone.RotateClockwise(angle);
                break;
            case TurnDir.CounterClockwise:
                await _drone.RotateCounterClockwise(angle);
                break;
        }

        return "success";
    }

    #endregion

    #region Drone - MoveDirection

    private enum Direction
    {
        Forward,
        Backward,
        Left,
        Right,
        Up,
        Down
    }

    private static async Task<string> MoveDirection(FunctionCallResponseItem toolCall)
    {
        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
        bool hasDirection = argumentsJson.RootElement.TryGetProperty("direction", out JsonElement rawDirection);
        bool hasDistance = argumentsJson.RootElement.TryGetProperty("distance", out JsonElement rawDistance);
        if (!hasDirection)
            throw new ArgumentNullException("direction");
        if (!hasDistance)
            throw new ArgumentNullException("distance");

        Direction direction = Direction.Up;
        float distance = 0f;

        if (Enum.TryParse(typeof(Direction), rawDirection.GetString(), false, out object? possibleDir))
            direction = (Direction)possibleDir;
        else
            throw new Exception($"Direction {rawDirection.GetString()} is not valid");

        if (rawDistance.TryGetDouble(out double dDistance))
            distance = (float)dDistance;
        else
            throw new Exception($"Distance {rawDistance} is not valid");


        switch (direction)
        {
            case Direction.Forward:
                await _drone.MoveForward(distance);
                break;
            case Direction.Backward:
                await _drone.MoveBackward(distance);
                break;
            case Direction.Left:
                await _drone.MoveLeft(distance);
                break;
            case Direction.Right:
                await _drone.MoveRight(distance);
                break;
            case Direction.Up:
                await _drone.MoveUp(distance);
                break;
            case Direction.Down:
                await _drone.MoveDown(distance);
                break;
        }

        return "success";
    }

    #endregion

    #region Weather

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