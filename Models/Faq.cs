using System.ComponentModel.DataAnnotations;

namespace InnovestHolding.Models;

public class Faq
{
    public int Id { get; set; }

    [Required]
    public string QuestionAr { get; set; } = string.Empty;

    public string? QuestionEn { get; set; }

    [Required]
    public string AnswerAr { get; set; } = string.Empty;

    public string? AnswerEn { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
