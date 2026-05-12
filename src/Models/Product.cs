using System.Text.Json;
using System.Text.Json.Serialization;

namespace DCafe.Models
{
    /// <summary>
    /// Represents a product with its details.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Gets or sets the identifier of the product.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title of the product.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the price of the product.
        /// </summary>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the description of the product.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the category of the product.
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the image URL of the product.
        /// </summary>
        [JsonPropertyName("image")]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the rating of the product.
        /// </summary>
        [JsonPropertyName("rating")]
        [JsonConverter(typeof(RatingConverter))]
        public Rating Rating { get; set; } = new Rating();
    }

    /// <summary>
    /// Converts JSON rating value (number or object) to Rating.
    /// </summary>
    public class RatingConverter : JsonConverter<Rating>
    {
        public override Rating Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                var rate = reader.GetDouble();
                return new Rating { Rate = rate, Count = 0 };
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                double rate = 0;
                int count = 0;
                
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;
                    
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propName = reader.GetString();
                        reader.Read();
                        
                        if (propName == "rate" && reader.TokenType == JsonTokenType.Number)
                            rate = reader.GetDouble();
                        else if (propName == "count" && reader.TokenType == JsonTokenType.Number)
                            count = reader.GetInt32();
                    }
                }
                
                return new Rating { Rate = rate, Count = count };
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return new Rating();
            }
            
            reader.Skip();
            return new Rating();
        }

        public override void Write(Utf8JsonWriter writer, Rating value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("rate", value?.Rate ?? 0);
            writer.WriteNumber("count", value?.Count ?? 0);
            writer.WriteEndObject();
        }
    }
}
