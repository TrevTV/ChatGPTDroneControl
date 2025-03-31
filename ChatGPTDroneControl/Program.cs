using DJIControlClient;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using System.Text.Json;

namespace ChatGPTDroneControl;

internal static class Program
{
    private const string SYSTEM = @"You are controlling a drone. I want it to take some scenic pictures around the location.

For each movement change, you will receive a photo of your current position, as well as the IMU state.

Provide a one-sentence reasoning for that specific command.

You will always start in an idle state, you must take off before you can move the drone.

The drone is a DJI Mini SE, which is quite small and gives you room for movement.

Be careful to avoid obstacles, such as trees and buildings.

Ideally check weather information as your first move to ensure safe conditions.";

    public static HttpClient HttpClient { get; } = new();
    public static Drone DroneClient { get; } = new(Configuration.File.APIs.DroneIPPort);

    private static readonly OpenAIResponseClient _aiClient = new("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    private static ResponseItem GetDroneInfoResponseItem()
    {
        // TODO: grab drone info and image
        return ResponseItem.CreateUserMessageItem("What is your first move?");
    }

    public static async Task Main()
    {
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
                Console.ReadKey();
                responseItems.Clear();
                responseItems.Add(GetDroneInfoResponseItem());
            }

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

                responseItems.Add(toolCall);
                toolResponse = true;

                Console.WriteLine(toolCall.FunctionName);
                Console.WriteLine(toolCall.FunctionArguments);

                await GPTTools.HandleFunctionCall(toolCall);
            }
        }
    }
}