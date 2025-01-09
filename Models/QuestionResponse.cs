namespace QuestionGeneratorWithCohere.Models
{
    public class QuestionResponse
    {
        public List<QuestionDetail> Questions { get; set; }
        public string Syllabus { get; set; }
        public string QuestionType { get; set; }
    }
}
