namespace EMS.Domain.DbModels;

public class PositionRole
{
    public int Id { get; set; }
    public int JobPositionId { get; set; }
    public int RoleId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public JobPosition JobPosition { get; set; } = null!;
}
