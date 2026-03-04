using System.Text.Json.Serialization;

namespace CallHubApp.Models
{
    public class AnswerEventModel
    {
        [JsonPropertyName("callerId")]
        public string CallerId { get; set; }

        [JsonPropertyName("jobs")]
        public string Jobs { get; set; }

        [JsonPropertyName("candidateIds")]
        public string CandidateIds { get; set; }

        [JsonPropertyName("fullname")]
        public string Fullname { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }
    }


}
