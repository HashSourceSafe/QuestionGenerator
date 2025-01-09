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
        Task<string> GenerateQuestions(string syllabus, string questionType, string NumberOfQuestions,string DificultyLevel);
    }
    public class CohereService : ICohereService
    {
        private readonly HttpClient _httpClient;

        public CohereService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<string> GenerateQuestions(string syllabus, string questionType,string NumberOfQuestions, string DificultyLevel)
        {
            var apiKey = "KHgq4T6JgiDQst0fbv7sJYjszXrsblwimGCGCwuy"; // Replace with your actual API Key

            //var prompt = $"Generate 3 {questionType} questions based on this syllabus: {syllabus}";
            string prompt = "";
            switch (questionType)
            {
                case "Single":
                    prompt = $"Given the following syllabus text:\n\n" +
                         $"{syllabus}\n\n" +
                         $"Generate {NumberOfQuestions} {DificultyLevel} questions suitable for assessment based on the syllabus text. " +
                         $"Provide a single question and its corresponding single, concise correct answer.\n\n" +
                         $"Return them in JSON format as follows:\n\n" +
                         $"{{ \n \"questions\": [ \n {{ \n \"question\": \"<question text>\",\n  \"answer\": \"<correct answer>\"\n  }},  \n ] \n }}";
                    break;
                case "MCQ":
                    prompt = $"Given the following syllabus text:\n\n" +
                          $"{syllabus}\n\n" +
                          $"Generate {NumberOfQuestions} {DificultyLevel} multiple choice questions based on the following syllabus. " +
                          $"Questions should cover different aspects of the syllabus." +
                           $"Include 4 answer choices for each question and identify one of the choices as the correct answer.\n\n" +
                          $"Return them in JSON format as follows:\n\n" +
                          $"{{ \n \"questions\": [ \n {{ \n \"question\": \"<question text>\",\n \"choices\": [\n \"<choice1>\",\n  \"<choice2>\",\n \"<choice3>\",\n  \"<choice4>\"\n],\n  \"correctAnswer\": \"<correct choice>\"\n  }},  \n ] \n }}";
                    break;
                case "Multi-Part":
                    prompt = $"Given the following syllabus text:\n\n" +
                         $"{syllabus}\n\n" +
                         $"Generate {NumberOfQuestions} {DificultyLevel} questions that include multiple parts, designed to assess understanding of the syllabus content." +
                         $"Each multi-part question should have 2 to 3 sub-questions related to the main topic from the syllabus. " +
                         $"Provide a single, concise correct answer for each sub-question.\n\n" +
                          $"Return them in JSON format as follows:\n\n" +
                        $"{{ \n \"questions\": [ \n {{ \n \"mainQuestion\": \"<main question text>\",\n \"subQuestions\":[ \n {{\"text\": \"<sub-question text>\", \"answer\":\"<answer for the sub-question>\"}} , \n {{\"text\": \"<sub-question text>\", \"answer\":\"<answer for the sub-question>\"}}\n  ] \n  }}, \n   \n  ] \n }}";
                    break;
            }

          


        var requestBody = new
            {
                prompt = prompt,
                max_tokens = 10000
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
