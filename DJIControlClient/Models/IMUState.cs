using System.Text.Json.Serialization;

namespace DJIControlClient.Models
{
    public partial class IMUState
    {
        [JsonPropertyName("yaw")]
        public float Yaw { get; set; }

        [JsonPropertyName("pitch")]
        public float Pitch { get; set; }

        [JsonPropertyName("roll")]
        public float Roll { get; set; }

        [JsonPropertyName("velX")]
        public float VelocityX { get; set; }

        [JsonPropertyName("velY")]
        public float VelocityY { get; set; }

        [JsonPropertyName("velZ")]
        public float VelocityZ { get; set; }
    }

    public partial class IMUState
    {
        public override string ToString()
        {
            return $"Yaw: {Yaw}\nPitch: {Pitch}\nRoll: {Roll}\nVelocityX: {VelocityX}\nVelocityY: {VelocityY}\nVelocityZ: {VelocityZ}";
        }
    }
}
