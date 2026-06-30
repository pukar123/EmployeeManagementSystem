namespace EMS.Domain.DbModels;

public class OrganizationEmployeeNumberSequence
{
    public int OrganizationId { get; set; }

    public int LastAllocatedNumber { get; set; }

    public Organization Organization { get; set; } = null!;
}
