using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using QuestionGeneratorWithCohere.Models;

namespace CohereQuestionGenerator.Services
{
    public interface ICohereService
    {
        Task<string> GenerateQuestions(string syllabus, string questionType, string NumberOfQuestions, string DifficultyLevel);
    }

    public class CohereService : ICohereService
    {
        private readonly HttpClient _httpClient;

        public CohereService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateQuestions(string syllabus, string questionType, string NumberOfQuestions, string DifficultyLevel)
        {
            var apiKey = "KHgq4T6JgiDQst0fbv7sJYjszXrsblwimGCGCwuy"; // Replace with your actual API Key

            string message = "";
            switch (questionType)
            {
                case "Single":
                    message = $"Given the following syllabus text:\n\n" +
                    $"{syllabus}\n\n" +
                    $"Generate {NumberOfQuestions} {DifficultyLevel} questions in JSON format based on the syllabus text. " +
                    $"Each question should include:\n" +
                    $"- A single, concise correct answer.\n" +
                    $"- The corresponding Course Outcome (CO) it assesses.\n" +
                    $"- The appropriate Bloom’s Taxonomy Level (BL) (e.g., Remember, Understand, Apply, Analyze, Evaluate, Create).\n\n" +
                    $"Return them in JSON format as follows:\n\n" +
                    $"{{ \"questions\": [ " +
                    $"{{ \"question\": \"<question text>\", " +
                    $"\"answer\": \"<correct answer>\", " +
                    $"\"CO\": \"<course outcome>\", " +
                    $"\"BL\": \"<Bloom’s Taxonomy level>\" }} ] }}";

                    break;
                case "MCQ":
                    message = $"Given the following syllabus text:\n\n" +
                    $"{syllabus}\n\n" +
                    $"Generate {NumberOfQuestions} {DifficultyLevel} multiple choice questions in JSON format based on the syllabus. " +
                    $"Each question should include:\n" +
                    $"- Four answer choices.\n" +
                    $"- One correct answer.\n" +
                    $"- The corresponding Course Outcome (CO) it assesses.\n" +
                    $"- The appropriate Bloom’s Taxonomy Level (BL) (e.g., Remember, Understand, Apply, Analyze, Evaluate, Create).\n\n" +
                    $"Return them in JSON format as follows:\n\n" +
                    $"{{ \"questions\": [ " +
                    $"{{ \"question\": \"<question text>\", " +
                    $"\"choices\": [\"<choice1>\", \"<choice2>\", \"<choice3>\", \"<choice4>\"], " +
                    $"\"correctAnswer\": \"<correct choice>\", " +
                    $"\"CO\": \"<course outcome>\", " +
                    $"\"BL\": \"<Bloom’s Taxonomy level>\" }} ] }}";

                    break;
                case "Multi-Part":
                    message = $"Given the following syllabus text:\n\n" +
                    $"{syllabus}\n\n" +
                    $"Generate {NumberOfQuestions} {DifficultyLevel} questions in JSON format that include multiple parts. " +
                    $"Each question should have 2 to 3 sub-questions related to the main topic.\n" +
                    $"Each sub-question should include:\n" +
                    $"- A single, concise correct answer.\n" +
                    $"- The corresponding Course Outcome (CO) it assesses.\n" +
                    $"- The appropriate Bloom’s Taxonomy Level (BL) (e.g., Remember, Understand, Apply, Analyze, Evaluate, Create).\n\n" +
                    $"Return them in JSON format as follows:\n\n" +
                    $"{{ \"questions\": [ " +
                    $"{{ \"mainQuestion\": \"<main question text>\", " +
                    $"\"subQuestions\": [ " +
                    $"{{ \"text\": \"<sub-question text>\", \"answer\":\"<answer>\", \"CO\": \"<course outcome>\", \"BL\": \"<Bloom’s Taxonomy level>\" }} " +
                    $"] }} ] }}";

                    break;
            }

            var maxTokens = Math.Max(512, 4096 - (message.Length / 4) - 100); // Subtract an additional buffer

            var requestBody = new
            {
                model = "command-r-plus-08-2024", 
                message = message,
                max_tokens = maxTokens

            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync("https://api.cohere.ai/v1/chat", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Cohere API error: {response.StatusCode}, Details: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw Response Data: " + responseData); // Debugging log

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseData);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("text", out var textElement))
                {
                    var generatedJson = textElement.GetString();
                    if (!string.IsNullOrEmpty(generatedJson))
                    {
                        // Parse the generated JSON if needed
                        using var generatedJsonDoc = JsonDocument.Parse(generatedJson);
                        var generatedRoot = generatedJsonDoc.RootElement;
                        return generatedJson;
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Error parsing JSON response: {ex.Message}");
            }

            return string.Empty;
        }
    }
}
