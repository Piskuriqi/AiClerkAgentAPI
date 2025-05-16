using System.Text.Json.Serialization;

namespace AiClerkAgentAPI.Models
{
    public class ProductModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string? ProduktName { get; set; }
        [JsonPropertyName("description")]

        public string? Description { get; set; }
        [JsonPropertyName("category")]

        public string? Category { get; set; }
        [JsonPropertyName("price")]

        public double Price { get; set; }
        [JsonPropertyName("discountPercentage")]

        public double DiscountPercentage { get; set; }
        [JsonPropertyName("rating")]

        public double Rating { get; set; }
        [JsonPropertyName("tags")]

        public List<string>? Tags { get; set; }
        [JsonPropertyName("availabilityStatus")]
        public string? AvailabilityStatus { get; set; }

        [JsonPropertyName("meta")]
        public MetaModel? Meta { get; set; }
    }
    public class MetaModel
    {
        [JsonPropertyName("createdAt")]
        public string? createdAt { get; set; }
    }
}
