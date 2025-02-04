using System.Text.Json.Serialization;

namespace QuestionGeneratorWithCohere.Models
{
    public class SaveRequest
    {
        public string Syllabus { get; set; }
        public string QuestionType { get; set; }
        public string DificultyLevel { get; set; }
        public string CO { get; set; }
        public string BL { get; set; }
        public List<QuestionDetail> Questions { get; set; }
    }


    public class QuestionDetail
    {
        public string Text { get; set; }
        public string Answer { get; set; }
        public List<string> Choices { get; set; }
        public List<SubQuestion> SubQuestions { get; set; }
        public string? QuestionType { get; set; }
        public string COText { get; set; }
        public string BLText { get; set; }
    }

    public class SubQuestion
    {
        public string Text { get; set; }
        public string Answer { get; set; }
        public string SubCOText { get; set; }
        public string SubBLText { get; set; }
    }

}


