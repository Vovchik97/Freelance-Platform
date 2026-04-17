using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Dto.Reviews;

public class AddReviewDto
{
    [Required]
    public int ServiceId { get; set; }

    [Required, Range(1, 5)]
    public int Rating { get; set; }

    [Required, Range(1, 5)]
    public int QualityRating { get; set; }

    [Required, Range(1, 5)]
    public int CommunicationRating { get; set; }

    [Required, Range(1, 5)]
    public int DeadlineRating { get; set; }

    [Required, Range(1, 5)]
    public int PriceRating { get; set; }
    
    public string? Comment { get; set; }
}