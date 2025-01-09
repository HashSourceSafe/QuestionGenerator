using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using QuestionGeneratorWithCohere.Models;

namespace CohereQuestionGenerator.Services
{
    public interface ICohereService
    {
        //Task<List<string>> GenerateQuestions(string syllabus, string questionType);
        Task<string> GenerateQuestions(string syllabus, string questionType);
    }
    public class CohereService : ICohereService
    {
        private readonly HttpClient _httpClient;

        public CohereService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<string> GenerateQuestions(string syllabus, string questionType)
        {
            var apiKey = "KHgq4T6JgiDQst0fbv7sJYjszXrsblwimGCGCwuy"; // Replace with your actual API Key
            var prompt = $"Generate 3 {questionType} questions based on this syllabus: {syllabus}";

            var requestBody = new
            {
                prompt = prompt,
                max_tokens = 300
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync("https://api.cohere.ai/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Cohere API error: {response.StatusCode}, Details: {errorContent}");
            }

            var responseData = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw Response Data: " + responseData); // Debugging log

            var result = JsonSerializer.Deserialize<Generation>(responseData);

            return result?.text ?? string.Empty;



            //if (result?.Generations == null || !result.Generations.Any())
            //{
            //    Console.WriteLine("Generations is null or empty. Returning fallback questions.");
            //    return new List<string>
            //    {
            //        "What is a binary tree?",
            //        "Explain the time complexity of binary search.",
            //        "What is the difference between BFS and DFS?"
            //    };
            //}

            //return result.Generations
            //      .SelectMany(g => g.text.Split('\n')
            //                            .Where(line => !string.IsNullOrWhiteSpace(line)))
            //      .ToList();
        }
    }

}
