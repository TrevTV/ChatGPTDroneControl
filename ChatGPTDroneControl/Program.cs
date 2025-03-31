using DJIControlClient;
using OpenAI.Responses;

namespace ChatGPTDroneControl;

internal static class Program
{
    private const string SYSTEM = @"You are controlling a drone. I want it to take some scenic pictures around the location.

You may stack multiple changes at once, however keep execution order in mind.

For each movement change, you will receive a photo of your current position, as well as position information.

Provide a one-sentence reasoning for the given command(s).

You will always start in an idle state, you must take off before you can move the drone.

The drone is a DJI Mini SE, which is quite small and gives you room for movement.

Be careful to avoid obstacles, such as trees and buildings.

Always check weather information before taking off to ensure safe conditions. It will be included in the first user message, no need to request it. Be very strict, you want fully dry and very calm wind.";

    public static HttpClient HttpClient { get; } = new();
    public static Drone DroneClient { get; } = new(Configuration.File.APIs.DroneIPPort);

    private static readonly OpenAIResponseClient _aiClient = new("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    private static bool _firstMove = true;
    private static string? previousResponseId = null;

    private static async Task<ResponseItem> GetDroneInfoResponseItem()
    {
        if (_firstMove)
        {
            _firstMove = false;
            return ResponseItem.CreateUserMessageItem(await GPTTools.GetWeatherInfo());
        }

        await DroneClient.CaptureShot();
        string encodedImg = await DroneClient.GetMediaPreview(0);
        byte[] imgData = Convert.FromBase64String(encodedImg);

        ResponseContentPart imgPart = ResponseContentPart.CreateInputImagePart(new BinaryData(imgData), "image/png");

        float alt = 0;
        try { alt = await DroneClient.GetAltitude(); } catch { }
        float heading = await DroneClient.GetHeading();

        ResponseContentPart statePart = ResponseContentPart.CreateInputTextPart($@"Altitude: {alt}
Heading: {heading}");

        return ResponseItem.CreateUserMessageItem([statePart, imgPart]);
    }

    public static async Task Main()
    {
        await DroneClient.SetLandingProtection(false);
        await DroneClient.SetMaxSpeed(1);

        ResponseCreationOptions opt = new()
        {
            Instructions = SYSTEM
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
                Console.WriteLine("Continuing...");
                responseItems.Clear();
                responseItems.Add(await GetDroneInfoResponseItem());
                Console.WriteLine("Drone data updated.");
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

                Console.WriteLine(toolCall.FunctionName);
                Console.WriteLine(toolCall.FunctionArguments);

                Console.WriteLine("Is this okay?");
                Console.ReadKey();

                responseItems.Add(await GPTTools.HandleFunctionCall(toolCall));
            }

            if (!toolResponse) // we're not done yet, need to continue first before we save the response id
                previousResponseId = responseData.Value.Id;
        }
    }
}