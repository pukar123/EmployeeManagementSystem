using EMS.Application.Services.Onboarding;
using EMS.Application.UnitTests.Infrastructure;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Onboarding;

public class OnboardingChecklistServiceTests
{
    [Test]
    public async Task GenerateForEmployeeAsync_CreatesChecklistAndTasks_FromTemplateItems()
    {
        var templateItem1 = new OnboardingChecklistTemplateItem
        {
            Id = 11,
            TemplateId = 1,
            Title = "Sign documents",
            Category = OnboardingChecklistItemCategory.Documents,
            SortOrder = 1,
            DefaultDueDaysFromStart = 3,
            DefaultPriority = TaskPriority.High,
            IsRequired = true,
        };
        var templateItem2 = new OnboardingChecklistTemplateItem
        {
            Id = 12,
            TemplateId = 1,
            Title = "Manager intro",
            Category = OnboardingChecklistItemCategory.ManagerIntro,
            SortOrder = 2,
            IsRequired = true,
        };
        var template = new OnboardingChecklistTemplate
        {
            Id = 1,
            OrganizationId = 1,
            Name = "Standard",
            IsActive = true,
            Items = [templateItem1, templateItem2],
        };

        var templateRepo = new InMemoryRepositoryMock<OnboardingChecklistTemplate>(t => t.Id, (t, id) => t.Id = id);
        templateRepo.Seed(template);

        var checklistRepo = new InMemoryRepositoryMock<EmployeeOnboardingChecklist>(c => c.Id, (c, id) => c.Id = id);
        var taskRepo = new InMemoryRepositoryMock<TaskItem>(t => t.Id, (t, id) => t.Id = id);

        var employee = new Employee
        {
            Id = 5,
            OrganizationId = 1,
            EmploymentStatus = EmploymentStatus.Preboarding,
            DateJoined = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
        };

        var sut = new OnboardingChecklistService(
            templateRepo.CreateMock().Object,
            checklistRepo.CreateMock().Object,
            taskRepo.CreateMock().Object);

        await sut.GenerateForEmployeeAsync(employee, template.Id, assignedByUserId: 99, employee.DateJoined, CancellationToken.None);

        Assert.That(checklistRepo.Items, Has.Count.EqualTo(1));
        Assert.That(taskRepo.Items, Has.Count.EqualTo(2));
        Assert.That(taskRepo.Items.All(t => t.EmployeeId == 5), Is.True);
        Assert.That(taskRepo.Items.All(t => t.OnboardingTemplateItemId.HasValue), Is.True);
        Assert.That(taskRepo.Items.Single(t => t.Title == "Sign documents").DueAtUtc,
            Is.EqualTo(new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void GenerateForEmployeeAsync_Throws_ForNonPreboardingEmployee()
    {
        var sut = CreateServiceWithTemplate();

        var employee = new Employee
        {
            Id = 5,
            OrganizationId = 1,
            EmploymentStatus = EmploymentStatus.Active,
            DateJoined = DateTime.UtcNow,
        };

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await sut.GenerateForEmployeeAsync(employee, 1, 99, employee.DateJoined, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("preboarding"));
    }

    [Test]
    public void GenerateForEmployeeAsync_Throws_ForInactiveTemplate()
    {
        var template = new OnboardingChecklistTemplate
        {
            Id = 1,
            OrganizationId = 1,
            Name = "Inactive",
            IsActive = false,
            Items = [new OnboardingChecklistTemplateItem { Id = 11, TemplateId = 1, Title = "Item", SortOrder = 1 }],
        };

        var templateRepo = new InMemoryRepositoryMock<OnboardingChecklistTemplate>(t => t.Id, (t, id) => t.Id = id);
        templateRepo.Seed(template);

        var sut = new OnboardingChecklistService(
            templateRepo.CreateMock().Object,
            new InMemoryRepositoryMock<EmployeeOnboardingChecklist>(c => c.Id, (c, id) => c.Id = id).CreateMock().Object,
            new InMemoryRepositoryMock<TaskItem>(t => t.Id, (t, id) => t.Id = id).CreateMock().Object);

        var employee = new Employee
        {
            Id = 5,
            OrganizationId = 1,
            EmploymentStatus = EmploymentStatus.Preboarding,
            DateJoined = DateTime.UtcNow,
        };

        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await sut.GenerateForEmployeeAsync(employee, 1, 99, employee.DateJoined, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("not active"));
    }

    [Test]
    public async Task GetProgressAsync_ComputesPercentCompleteAndOverdue()
    {
        var templateItem = new OnboardingChecklistTemplateItem
        {
            Id = 11,
            TemplateId = 1,
            Title = "Collect laptop",
            Category = OnboardingChecklistItemCategory.Equipment,
            SortOrder = 1,
            IsRequired = true,
        };
        var template = new OnboardingChecklistTemplate
        {
            Id = 1,
            OrganizationId = 1,
            Name = "Standard",
            Items = [templateItem],
        };
        var checklist = new EmployeeOnboardingChecklist
        {
            Id = 20,
            EmployeeId = 5,
            TemplateId = 1,
            GeneratedAtUtc = DateTime.UtcNow.AddDays(-2),
            Template = template,
            Tasks =
            [
                new TaskItem
                {
                    Id = 30,
                    EmployeeId = 5,
                    OnboardingTemplateItemId = 11,
                    EmployeeOnboardingChecklistId = 20,
                    Title = "Collect laptop",
                    Status = TaskWorkflowStatus.Assigned,
                    DueAtUtc = DateTime.UtcNow.AddDays(-1),
                },
            ],
        };

        var checklistRepo = new InMemoryRepositoryMock<EmployeeOnboardingChecklist>(c => c.Id, (c, id) => c.Id = id);
        checklistRepo.Seed(checklist);

        var sut = new OnboardingChecklistService(
            Mock.Of<IBaseRepository<OnboardingChecklistTemplate>>(),
            checklistRepo.CreateMock().Object,
            Mock.Of<IBaseRepository<TaskItem>>());

        var progress = await sut.GetProgressAsync(5, CancellationToken.None);

        Assert.That(progress.TotalCount, Is.EqualTo(1));
        Assert.That(progress.CompletedCount, Is.EqualTo(0));
        Assert.That(progress.OverdueCount, Is.EqualTo(1));
        Assert.That(progress.PercentComplete, Is.EqualTo(0));
        Assert.That(progress.TemplateName, Is.EqualTo("Standard"));
    }

    [Test]
    public async Task GetProgressAsync_ReturnsEmpty_WhenNoChecklist()
    {
        var checklistRepo = new InMemoryRepositoryMock<EmployeeOnboardingChecklist>(c => c.Id, (c, id) => c.Id = id);
        var sut = new OnboardingChecklistService(
            Mock.Of<IBaseRepository<OnboardingChecklistTemplate>>(),
            checklistRepo.CreateMock().Object,
            Mock.Of<IBaseRepository<TaskItem>>());

        var progress = await sut.GetProgressAsync(5, CancellationToken.None);

        Assert.That(progress.TotalCount, Is.EqualTo(0));
        Assert.That(progress.Items, Is.Empty);
    }

    private static OnboardingChecklistService CreateServiceWithTemplate()
    {
        var template = new OnboardingChecklistTemplate
        {
            Id = 1,
            OrganizationId = 1,
            Name = "Standard",
            IsActive = true,
            Items = [new OnboardingChecklistTemplateItem { Id = 11, TemplateId = 1, Title = "Item", SortOrder = 1 }],
        };

        var templateRepo = new InMemoryRepositoryMock<OnboardingChecklistTemplate>(t => t.Id, (t, id) => t.Id = id);
        templateRepo.Seed(template);

        return new OnboardingChecklistService(
            templateRepo.CreateMock().Object,
            new InMemoryRepositoryMock<EmployeeOnboardingChecklist>(c => c.Id, (c, id) => c.Id = id).CreateMock().Object,
            new InMemoryRepositoryMock<TaskItem>(t => t.Id, (t, id) => t.Id = id).CreateMock().Object);
    }
}
