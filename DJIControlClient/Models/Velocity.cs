using System.Text.Json.Serialization;

namespace DJIControlClient.Models
{
    public partial class Velocity
    {
        [JsonPropertyName("yawRate")]
        public float YawRate { get; set; }

        [JsonPropertyName("velX")]
        public float VelocityX { get; set; }

        [JsonPropertyName("velY")]
        public float VelocityY { get; set; }

        [JsonPropertyName("velZ")]
        public float VelocityZ { get; set; }
    }

    public partial class Velocity
    {
        public override string ToString()
        {
            return $"YawRate: {YawRate}\n\nVelocityX: {VelocityX}\nVelocityY: {VelocityY}\nVelocityZ: {VelocityZ}";
        }
    }
}
