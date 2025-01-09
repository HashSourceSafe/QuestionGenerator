using CohereQuestionGenerator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuestionGeneratorWithCohere.Models;
using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QuestionGeneratorWithCohere.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICohereService _cohereService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public HomeController(ICohereService cohereService, HttpClient httpClient, IConfiguration config)
        {
            _cohereService = cohereService;
            _httpClient = httpClient;
            _config = config;

        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Generate(QuestionRequest request)
        {
            if (!ModelState.IsValid)
                return View("Index", request);

            try
            {
                var questionsJson = await _cohereService.GenerateQuestions(request.Syllabus, request.QuestionType, request.NoOfQuestions,request.DificultyLevel);
                questionsJson = questionsJson.Replace(@"\", @"\\");
                questionsJson = RemoveTrailingCommas(questionsJson);



                Console.WriteLine("Raw Response Data: " + questionsJson);

                Dictionary<string, List<JsonElement>> jsonData;
                try
                {
                    jsonData = JsonSerializer.Deserialize<Dictionary<string, List<JsonElement>>>(questionsJson)
                              ?? throw new JsonException("Invalid JSON format.");
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError("", $"Error processing JSON data: {ex.Message}");
                    return View("Index", request);
                }




                if (!jsonData.ContainsKey("questions"))
                {
                    ModelState.AddModelError("", "No questions generated. Please try again.");
                    return View("Index", request);
                }

                var questions = new List<QuestionDetail>();

                foreach (var q in jsonData["questions"])
                {
                    try
                    {
                        var questionText = q.TryGetProperty("question", out var questionElement)
                         ? questionElement.GetString() ?? string.Empty
                         : q.TryGetProperty("mainQuestion", out var mainQuestionElement)
                             ? mainQuestionElement.GetString() ?? string.Empty
                             : string.Empty;

                        if (request.QuestionType == "Single")
                        {
                            var answer = q.TryGetProperty("answer", out var answerElement)
                                        ? answerElement.GetString()
                                        : string.Empty;

                            questions.Add(new QuestionDetail
                            {
                                Text = questionText,
                                Answer = answer
                            });
                        }
                        else if (request.QuestionType == "MCQ")
                        {
                            if (q.TryGetProperty("choices", out var choicesElement) && choicesElement.ValueKind == JsonValueKind.Array)
                            {
                                var choices = choicesElement.EnumerateArray()
                                                             .Select(choice => choice.GetString() ?? string.Empty)
                                                             .ToList();

                                var correctAnswer = q.TryGetProperty("correctAnswer", out var correctAnswerElement)
                                                    ? correctAnswerElement.GetString()
                                                    : string.Empty;

                                questions.Add(new QuestionDetail
                                {
                                    Text = questionText,
                                    Answer = correctAnswer,
                                    Choices = choices
                                });
                            }
                        }
                        else if (request.QuestionType == "Multi-Part")
                        {
                            var answer = q.TryGetProperty("correctAnswer", out var correctAnswerElement)
                                        ? correctAnswerElement.GetString()
                                        : string.Empty;

                            var subQuestions = q.TryGetProperty("subQuestions", out var subQuestionsElement) && subQuestionsElement.ValueKind == JsonValueKind.Array
                            ? subQuestionsElement.EnumerateArray()
                                .Select(subQ =>
                                {
                                    var subText = subQ.TryGetProperty("text", out var subTextElement) ? subTextElement.GetString() ?? string.Empty : string.Empty;
                                    var subAnswer = subQ.TryGetProperty("answer", out var subAnswerElement) ? subAnswerElement.GetString() ?? string.Empty : string.Empty;
                                    return new SubQuestion
                                    {
                                        Text = subText,
                                        Answer = subAnswer
                                    };
                                })
                                .ToList()
                            : new List<SubQuestion>();

                            questions.Add(new QuestionDetail
                            {
                                Text = questionText,
                                Answer = answer,
                                SubQuestions = subQuestions // This now works because SubQuestions is of type List<SubQuestion>
                            });
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"Error processing question: {innerEx.Message}");
                    }
                }

                var saveRequest = new SaveRequest
                {
                    Syllabus = request.Syllabus,
                    QuestionType = request.QuestionType,
                    DificultyLevel= request.DificultyLevel,
                    Questions = questions
                };

                return View("Result", saveRequest);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View("Index", request);
            }
        }



        [HttpPost]
        public async Task<IActionResult> SaveQuestions([FromBody] SaveRequest request)
        {
            SqlConnection con = new();

            string mycon = _config.GetConnectionString("qbmsAICon") ?? string.Empty;

            try
            {
                using (var connection = new SqlConnection(mycon))
                {
                    await connection.OpenAsync();

                    foreach (var question in request.Questions)
                    {
                        var questionId = Guid.NewGuid();
                        var questionType = question.QuestionType;

                        using (var command = new SqlCommand("InsertQuestion_AI", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;


                            command.Parameters.AddWithValue("@QuestionText", question.Text);
                            command.Parameters.AddWithValue("@QuestionType", request.QuestionType);
                            command.Parameters.AddWithValue("@AnswerKey", question.Answer);
                            command.Parameters.AddWithValue("@PaperCode", request.Syllabus);


                            if (question.Choices != null && question.Choices.Count > 0)
                            {
                                command.Parameters.AddWithValue("@QuestionPartText", string.Join(", ", question.Choices));
                                command.Parameters.AddWithValue("@QuestionPartAnswerKey", question.Answer);
                            }
                            if (question.SubQuestions != null && question.SubQuestions.Count > 0)
                            {
                                var subquestionText = "";
                                var subAnswerText = "";
                                for (int i = 0; i < question.SubQuestions.Count; i++)
                                {
                                    subquestionText += question.SubQuestions[i].Text + ", ";
                                    subAnswerText += question.SubQuestions[i].Answer + ", ";
                                }

                                command.Parameters.AddWithValue("@QuestionPartText", string.Join(", ", subquestionText));
                                command.Parameters.AddWithValue("@QuestionPartAnswerKey", question.Answer);
                            }

                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    return Json(new { success = true, message = "Data saved successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private string RemoveTrailingCommas(string json)
        {
            // Remove trailing commas in arrays and objects using Regex
            var cleanedJson = Regex.Replace(json, @"(?<=[}\]])\s*,\s*(?=[}\]])", string.Empty);
            return cleanedJson;
        }



    }
}
