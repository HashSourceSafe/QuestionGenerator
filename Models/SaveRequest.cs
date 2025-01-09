using System.Text.Json.Serialization;

namespace QuestionGeneratorWithCohere.Models
{
    public class SaveRequest
    {
        public string Syllabus { get; set; }
        public string QuestionType { get; set; }
        public string DificultyLevel { get; set; }
        public List<QuestionDetail> Questions { get; set; }
    }


    public class QuestionDetail
    {
        public string Text { get; set; }
        public string Answer { get; set; }
        public List<string> Choices { get; set; }
        public List<SubQuestion> SubQuestions { get; set; }
        public string? QuestionType { get; set; }
    }

    public class SubQuestion
    {
        public string Text { get; set; }
        public string Answer { get; set; }
    }

}


