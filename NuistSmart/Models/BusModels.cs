using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

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
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public int Id { get; set; }

        [JsonPropertyName("start")]
        public string? Start { get; set; }

        [JsonPropertyName("end")]
        public string? End { get; set; }

        [JsonPropertyName("remain")]
        public int Remain { get; set; }

        [JsonPropertyName("departureTime")]
        public string? DepartureTime { get; set; }
    }

    public partial class BusRunDisplay : ObservableObject
    {
        public string Id { get; set; } = "";

        [ObservableProperty]
        private string _displayText = "";

        public string DepartureTime { get; set; } = "";

        [ObservableProperty]
        private int _remainSeats;

        [ObservableProperty]
        private bool _isSelected;

        public string Start { get; set; } = "";
        
        public string End { get; set; } = "";
    }
}
