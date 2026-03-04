using System.Text.Json.Serialization;

namespace CallHubApp.Models
{
    public class UserModel
    {
        [JsonPropertyName("callerId")]
        public string CallerId { get; set; }
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }


        [JsonPropertyName("productId")]
        public Guid ProductId { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = "";

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = "";

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = "";

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = "";


        [JsonPropertyName("email")]
        public string Email { get; set; } = "";


        [JsonPropertyName("host")]
        public string Host { get; set; }

        
    }
}
