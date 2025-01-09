
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace QuestionGeneratorWithCohere.Models
{
    public class CohereApiResponse
    {
        [JsonPropertyName("generations")]
        public List<Generation> Generations { get; set; }
    }

    public class Generation
    {
        //[JsonPropertyName("text")]
        public string text { get; set; }
    }
}
