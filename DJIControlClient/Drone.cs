using DJIControlClient.Converters;
using DJIControlClient.Exceptions;
using DJIControlClient.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace DJIControlClient
{
    // Root
    public partial class Drone
    {
        internal HttpClient _httpClient;

        internal readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters =
            {
                new ControlModeConverter(),
                new VelocityProfileConverter()
            }
        };

        public Drone(string ipPort)
        {
            if (!ipPort.EndsWith('/'))
                ipPort += '/';

            ipPort = "http://" + ipPort;

            _httpClient = new()
            {
                BaseAddress = new Uri(ipPort)
            };
        }

        internal async Task<CommandCompleted> Call(string endpoint)
        {
            try
            {
                CommandCompleted? result = await _httpClient.GetFromJsonAsync<CommandCompleted>(endpoint, _jsonOptions) ?? throw new NotConnectedException();
                return result;
            }
            catch (HttpRequestException)
            {
                throw new NotConnectedException();
            }
        }

        internal async Task<CommandCompleted<T>> Call<T>(string endpoint)
        {
            try
            {
                CommandCompleted<T>? result = await _httpClient.GetFromJsonAsync<CommandCompleted<T>>(endpoint, _jsonOptions) ?? throw new NotConnectedException();
                return result;
            }
            catch (HttpRequestException)
            {
                throw new NotConnectedException();
            }
        }
    }

    // "Properties"
    public partial class Drone
    {
        /// <summary>
        /// Returns current heading angle.
        /// </summary>
        /// <returns>Current heading angle with respect to true north</returns>
        public async Task<float> GetHeading()
        {
            CommandCompleted<float> result = await Call<float>("getHeading");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        /// <summary>
        /// Returns current altitude.
        /// </summary>
        /// <returns>Current altitude in meters</returns>
        public async Task<float> GetAltitude()
        {
            CommandCompleted<float> result = await Call<float>("getAltitude");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        #region Landing Protection

        /// <summary>
        /// Returns true if landing protection is enabled, else false.
        /// </summary>
        /// <returns>Landing protection status</returns>
        public async Task<bool> IsLandingProtectionEnabled()
        {
            CommandCompleted<bool?> result = await Call<bool?>("isLandingProtectionEnabled");
            if (!result.Completed)
                throw result.ParseError();

            return result.State != null && result.State.Value;
        }

        /// <summary>
        /// Enables or disables landing protection.
        /// </summary>
        /// <param name="enabled">Wanted landing protection status</param>
        public async Task SetLandingProtection(bool enabled)
        {
            string endpoint = enabled ? "enableLandingProtection" : "disableLandingProtection";

            CommandCompleted result = await Call(endpoint);
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion

        #region Control Mode

        /// <summary>
        /// Returns the current control mode. Defaults to POSITION on app launch.
        /// </summary>
        /// <returns>Current control mode</returns>
        public async Task<ControlMode> GetControlMode()
        {
            CommandCompleted<ControlMode> result = await Call<ControlMode>("getControlMode");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        /// <summary>
        /// Sets the current control mode.
        /// </summary>
        /// <param name="mode">Wanted control mode</param>
        public async Task SetControlMode(ControlMode mode)
        {
            CommandCompleted result = await Call($"setControlMode/{mode}");
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion

        #region Max Speed

        /// <summary>
        /// Gets max allowable speed.
        /// </summary>
        /// <returns>Max allowable speed in meters per second</returns>
        public async Task<float> GetMaxSpeed()
        {
            CommandCompleted<float> result = await Call<float>("getMaxSpeed");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        /// <summary>
        /// Sets max allowable speed.
        /// </summary>
        /// <param name="speed">Wanted max speed in meters per second</param>
        public async Task SetMaxSpeed(float speed)
        {
            CommandCompleted result = await Call($"setMaxSpeed/{speed}");
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion

        #region Max Angular Speed

        /// <summary>
        /// Get max allowable angular speed.
        /// </summary>
        /// <returns>Max allowable angular speed in degrees per second</returns>
        public async Task<float> GetMaxAngularSpeed()
        {
            CommandCompleted<float> result = await Call<float>("getMaxAngularSpeed");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        /// <summary>
        /// Sets max allowable angular speed.
        /// </summary>
        /// <param name="speed">Wanted max speed in degress per second</param>
        public async Task SetMaxAngularSpeed(float speed)
        {
            CommandCompleted result = await Call($"setMaxAngularSpeed/{speed}");
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion

        #region Velocity Profile

        /// <summary>
        /// Gets the current velocity profile used to generate velocity commands for movement. Defaults to CONSTANT on app launch.
        /// </summary>
        /// <returns>Current velocity profile</returns>
        public async Task<VelocityProfile> GetVelocityProfile()
        {
            CommandCompleted<VelocityProfile> result = await Call<VelocityProfile>("getVelocityProfile");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        /// <summary>
        /// Sets the current velocity profile.
        /// </summary>
        /// <param name="profile">Wanted velocity profile</param>
        public async Task SetVelocityProfile(VelocityProfile profile)
        {
            CommandCompleted result = await Call($"setVelocityProfile/{profile}");
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion
    }

    // Methods
    public partial class Drone
    {
        /// <summary>
        /// Checks if the API is available.
        /// </summary>
        /// <returns>API connection status</returns>
        public async Task<bool> IsConnected()
        {
            try
            {
                string? connected = await _httpClient.GetStringAsync("");
                return connected != null;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reboot the core component of the aircraft.
        /// For Matrice 300 RTK, reboot the aircraft.
        /// For Mavic Mini, DJI Mini SE, reboot the airlink.
        /// For Mavic Air 2, reboot the battery.
        /// Others, reboot the flight controller.
        /// </summary>
        public async Task Reboot()
        {
            CommandCompleted result = await Call("reboot");
            if (!result.Completed)
                throw result.ParseError();
        }

        #region Takeoff and Landing

        /// <summary>
        /// Initiate take off. Returns when takeoff is successfully initiated.
        /// </summary>
        public async Task Takeoff()
        {
            CommandCompleted result = await Call("takeoff");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Initiate landing.
        /// If landing protection is enabled, it descends to 30cm off the ground and awaits landing confirmation.
        /// If landing protection is disabled causes the drone to land. Returns when landing is successfully initiated.
        /// </summary>
        public async Task Land()
        {
            CommandCompleted result = await Call("land");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Needed to confirm landing only if landing protection is enabled. Returns when landing confirmation is successfully initiated.
        /// </summary>
        public async Task ConfirmLanding()
        {
            CommandCompleted result = await Call("confirmLanding");
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion

        #region IMU

        /// <summary>
        /// Starts a coroutine on the server that collects the current IMU state (X velocity, Y velocity, Z velocity, Roll, Pitch, Yaw) every interval.
        /// </summary>
        /// <param name="interval">Milliseconds between collection</param>
        public async Task StartCollectingIMUState(int interval)
        {
            CommandCompleted result = await Call($"startCollectingIMUState/{interval}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Stops collection of IMU states.
        /// </summary>
        public async Task StopCollectingIMUState()
        {
            CommandCompleted result = await Call("stopCollectingIMUState");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Returns an array of IMU states collected so far.
        /// </summary>
        /// <returns>Array of IMUStates</returns>
        public async Task<IMUState[]> GetCollectedIMUStates()
        {
            CommandCompleted<IMUState[]> result = await Call<IMUState[]>("getCollectedIMUStates");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        /// <summary>
        /// Clears the collected IMU states list.
        /// </summary>
        public async Task ClearCollectedIMUStates()
        {
            CommandCompleted result = await Call("clearCollectedIMUStates");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Gets current IMU state (X velocity, Y velocity, Z velocity, Roll, Pitch, Yaw).
        /// Not recommended, since there is a transmission delay between the client to the server, which would mean the IMU State returned is of a different time than both request and response times.
        /// </summary>
        /// <returns>Current IMUState</returns>
        public async Task<IMUState> GetCurrentIMUState()
        {
            CommandCompleted<IMUState> result = await Call<IMUState>("getCurrentIMUState");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        #endregion

        #region Movement

        /// <summary>
        /// Move forward. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="dist">Distance in meters</param>
        public async Task MoveForward(float dist)
        {
            CommandCompleted result = await Call($"moveForward/{dist}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Move backward. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="dist">Distance in meters</param>
        public async Task MoveBackward(float dist)
        {
            CommandCompleted result = await Call($"moveBackward/{dist}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Move left. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="dist">Distance in meters</param>
        public async Task MoveLeft(float dist)
        {
            CommandCompleted result = await Call($"moveLeft/{dist}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Move right. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="dist">Distance in meters</param>
        public async Task MoveRight(float dist)
        {
            CommandCompleted result = await Call($"moveRight/{dist}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Move up. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="dist">Distance in meters</param>
        public async Task MoveUp(float dist)
        {
            CommandCompleted result = await Call($"moveUp/{dist}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Move down. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="dist">Distance in meters</param>
        public async Task MoveDown(float dist)
        {
            CommandCompleted result = await Call($"moveDown/{dist}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Rotate clockwise by given angle. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        public async Task RotateClockwise(float angle)
        {
            CommandCompleted result = await Call($"rotateClockwise/{angle}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Rotate counter clockwise by given angle. Returns when the planned flight paths is entirely traversed.
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        public async Task RotateCounterClockwise(float angle)
        {
            CommandCompleted result = await Call($"rotateCounterClockwise/{angle}");
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion

        #region Velocity Control

        /// <summary>
        /// Starts a coroutine on the server that moves drone with currently set velocity vector.
        /// </summary>
        public async Task StartVelocityControl()
        {
            CommandCompleted result = await Call("startVelocityControl");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Sets the velocity vector given the parameters. Can only be called after calling <see cref="StartVelocityControl"/>.
        /// </summary>
        /// <param name="xVelocity">Velocity on X axis</param>
        /// <param name="yVelocity">Velocity on Y axis</param>
        /// <param name="zVelocity">Velocity on Z axis</param>
        /// <param name="yawRate">Yaw rate</param>
        public async Task SetVelocity(float xVelocity, float yVelocity, float zVelocity, float yawRate)
        {
            CommandCompleted result = await Call($"setVelocityCommand/{xVelocity}/{yVelocity}/{zVelocity}/{yawRate}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Fetches the current Velocity (X Velocity, Y Velocity, Z Velocity, Yaw Rate)
        /// </summary>
        public async Task<Velocity> GetCurrentVelocity()
        {
            CommandCompleted<Velocity> result = await Call<Velocity>("getCurrentVelocityCommand");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        /// <summary>
        /// Stops moving drone with currently set velocity vector.
        /// </summary>
        public async Task StopVelocityControl()
        {
            CommandCompleted result = await Call("stopVelocityControl");
            if (!result.Completed)
                throw result.ParseError();
        }

        #endregion

        #region Camera & Gimbal

        /// <summary>
        /// Captures an image.
        /// </summary>
        public async Task CaptureShot()
        {
            CommandCompleted result = await Call("captureShot");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Starts recording.
        /// </summary>
        public async Task StartVideoRecording()
        {
            CommandCompleted result = await Call("startVideoRecording");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Stops recording.
        /// </summary>
        public async Task StopVideoRecording()
        {
            CommandCompleted result = await Call("stopVideoRecording");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Sets the gimbal's pitch.
        /// </summary>
        /// <param name="angle">Wanted gimbal angle</param>
        public async Task SetGimbalPitch(float angle)
        {
            CommandCompleted result = await Call($"pitchGimbal/{angle}");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Captures a panorama.
        /// </summary>
        public async Task CapturePanorama()
        {
            CommandCompleted result = await Call("capturePanorama");
            if (!result.Completed)
                throw result.ParseError();
        }

        /// <summary>
        /// Gets the preview of media file at a given index.
        /// </summary>
        /// <param name="index">Index of media file</param>
        /// <returns>Base64 encoded string of preview image</returns>
        // TODO: Check returned format
        public async Task<string> GetMediaPreview(int index)
        {
            CommandCompleted<string> result = await Call<string>($"fetchPreviewFromIndex/{index}");
            if (!result.Completed)
                throw result.ParseError();

            return result.State;
        }

        #endregion
    }
}