namespace FreelancePlatform.Dto.WorkItems;

public class UpdateOrderRequest
{
    public List<int> WorkItemIds { get; set; } = new();
    public int? ProjectId { get; set; }
    public int? OrderId { get; set; }
}