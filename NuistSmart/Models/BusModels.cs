using System.Text.Json.Serialization;

namespace NuistSmart.Models
{
    public class BusResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    public class BusRunItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("end")]
        public string? End { get; set; }

        [JsonPropertyName("remain")]
        public int Remain { get; set; }

        [JsonPropertyName("departureTime")]
        public string? DepartureTime { get; set; }
    }
}
