namespace EMS.Domain.DbModels;

public class PositionRole
{
    public int Id { get; set; }
    public int JobPositionId { get; set; }
    public string RoleKey { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public JobPosition JobPosition { get; set; } = null!;
}
