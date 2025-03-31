using DJIControlClient;
using OpenAI.Assistants;
using OpenAI.Responses;

namespace ChatGPTDroneControl;

internal static class Program
{
    public static HttpClient HttpClient { get; } = new();
    public static Drone DroneClient { get; } = new(Configuration.File.APIs.DroneIPPort);

    private static OpenAIResponseClient _aiClient;
    private static bool _firstMove = true;
    private static string? previousResponseId = null;

    private static async Task<ResponseItem> GetDroneInfoResponseItem()
    {
        await DroneClient.CaptureShot();
        await Task.Delay(1200);
        string encodedImg = await DroneClient.GetMediaPreview(0);
        byte[] imgData = Convert.FromBase64String(encodedImg);

        ResponseContentPart imgPart = ResponseContentPart.CreateInputImagePart(new BinaryData(imgData), "image/png");

        float alt = 0;
        try { alt = await DroneClient.GetAltitude(); } catch { }
        float heading = await DroneClient.GetHeading();

        string weather = _firstMove ? "\n\nWeather Info:\n" + await GPTTools.GetWeatherInfo() : "";
        _firstMove = false;

        ResponseContentPart statePart = ResponseContentPart.CreateInputTextPart($@"Altitude: {alt}
Heading: {heading}{weather}");

        return ResponseItem.CreateUserMessageItem([statePart, imgPart]);
    }

    public static async Task Main()
    {
        if (string.IsNullOrWhiteSpace(Configuration.File.APIs.DroneIPPort))
        {
            Console.WriteLine("Drone IP:Port is unconfigured. Press any key to exit.");
            Console.ReadKey();
            return;
        }

        _aiClient = new(Configuration.File.Preferences.Model, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        await DroneClient.SetLandingProtection(false);
        await DroneClient.SetMaxSpeed(1);

        ResponseCreationOptions opt = new()
        {
            Instructions = Configuration.File.Preferences.SystemPrompt
        };

        foreach (ResponseTool tool in GPTTools.ResponseTools)
            opt.Tools.Add(tool);

        List<ResponseItem> responseItems = [];
        bool toolResponse = false;

        while (true)
        {
            if (!toolResponse)
            {
                Console.WriteLine("Waiting for user ready...");
                Console.ReadLine();
                Console.WriteLine("Getting current drone information...");
                responseItems.Clear();
                responseItems.Add(await GetDroneInfoResponseItem());
                Console.WriteLine("Drone data updated, waiting for AI response...");
            }

            if (!string.IsNullOrWhiteSpace(previousResponseId))
                opt.PreviousResponseId = previousResponseId;
            var responseData = await _aiClient.CreateResponseAsync(responseItems, opt);

            toolResponse = false;

            foreach (ResponseItem responseItem in responseData.Value.OutputItems)
            {
                if (responseItem is not FunctionCallResponseItem toolCall)
                {
                    if (responseItem is MessageResponseItem msg)
                    {
                        foreach (var contentPart in msg.Content)
                            Console.WriteLine(contentPart.Text);
                    }
                    continue;
                }

                responseItems.Add(responseItem);
                toolResponse = true;

                Console.WriteLine(toolCall.FunctionName + $"({toolCall.FunctionArguments})");

                Console.WriteLine("Is this okay? Respond 'y' for yes, explain why not otherwise to allow the AI to adjust.");
                string? okay = Console.ReadLine();
                if (okay != "y" || okay == null)
                {
                    okay ??= "No response given";
                    responseItems.Add(ResponseItem.CreateFunctionCallOutputItem(toolCall.CallId, $"Override by Human Operator: {okay}"));
                    continue;
                }

                responseItems.Add(await GPTTools.HandleFunctionCall(toolCall));
            }

            if (!toolResponse) // we're not done yet, need to continue first before we save the response id
                previousResponseId = responseData.Value.Id;
        }
    }
}