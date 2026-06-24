using System.Text.Json.Serialization;

namespace Test.Models
{
    public class Machine
    {
        [JsonPropertyName("machineId")]
         public string MachineId { get; set; } = "";

        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = "";

        [JsonPropertyName("machineType")]
        public string MachineType { get; set; } = "";

        [JsonPropertyName("location")]
        public string Location { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("imageBase64")]
        public string ImageBase64 { get; set; } = "";

        [JsonPropertyName("measurements")]
        public List<Measurement> Measurements { get; set; } = new();
    }

    public class Measurement
    {
        [JsonPropertyName("datetime")]
        public string Datetime { get; set; } = "";

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = "";
    }
}
