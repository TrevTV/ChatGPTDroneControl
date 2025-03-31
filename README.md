# ChatGPT Drone Control
A POC tool to allow the OpenAI API to control DJI drones.

## Disclaimer
This project is a proof of concept and is not intended for production use. Use it at your own risk. I am not responsible for any damage, injury, or legal issues that may arise from the use of this software. Always comply with local laws and regulations when operating drones.

## Requirements
- OpenAI API key
- A compiled APK of [DJIControlServer](https://github.com/TrevTV/DJIControlServer)
  - Requires a (free) DJI app key
- .NET 8

## Usage
1. Set your OpenAI API key and config folder in the environment:
    ```bash
    export OPENAI_API_KEY=your_api_key
    export CGDC_DATA_DIRECTORY="~/dronecontrol"
    ```
2. Clone and run the application once to generate the config file:
    ```bash
    git clone https://github.com/TrevTV/ChatGPTDroneControl.git
    cd ChatGPTDroneControl/ChatGPTDroneControl
    dotnet run
    ```
3. Find `chatgpt_drone_control.toml` in the data folder and fill in the server IP:Port and optionally the weather.com API info.
4. Set up your drone and connect DJIControlServer.
5. Run the application again and begin operation.

## Licensing
This project is licensed under the MIT License. See the `LICENSE` file for details.

This project uses
- [DJIControlServer](https://github.com/dkapur17/DJIControlServer), under no license.
- [DJIControlClient](https://github.com/TrevTV/DJIControlClient), under the [GPL-3.0 License](https://github.com/TrevTV/DJIControlClient/blob/main/LICENSE).
- [Tomlet](https://github.com/SamboyCoding/Tomlet/), under the [MIT License](https://github.com/SamboyCoding/Tomlet/blob/master/LICENSE).
- [openai-dotnet](https://github.com/openai/openai-dotnet/), under the [MIT License](https://github.com/openai/openai-dotnet/blob/main/LICENSE).