using System.ComponentModel.DataAnnotations;

namespace QuestionGeneratorWithCohere.Models
{
    public class QuestionRequest
    {
        [Required]
        public string? Syllabus { get; set; }

        [Required]
        public string? QuestionType { get; set; }

        public string NoOfQuestions { get; set; }

        public string? DificultyLevel { get; set; }
    }
}
