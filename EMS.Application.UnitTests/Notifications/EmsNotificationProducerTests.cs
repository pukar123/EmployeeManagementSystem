using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using Pukar.Notifications.Application.DTOs;
using Pukar.Notifications.Application.Services;

namespace EMS.Application.UnitTests.Notifications;

public class EmsNotificationProducerTests
{
    [Test]
    public async Task NotifyTaskAssignedAsync_Skips_WhenEmployeeNotLinked()
    {
        var notifications = new Mock<INotificationService>();
        var employees = new Mock<IBaseRepository<Employee>>();
        employees.Setup(x => x.GetQueryable()).Returns(new List<Employee>
        {
            new() { Id = 5, ExternalIdentityKey = null },
        }.BuildMock());

        var sut = CreateProducer(notifications.Object, employees.Object);
        await sut.NotifyTaskAssignedAsync(new TaskItem { Id = 1, EmployeeId = 5, Title = "Task" }, CancellationToken.None);

        notifications.Verify(
            x => x.CreateAsync(It.IsAny<CreateNotificationRequestModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task NotifyTaskAssignedAsync_CreatesNotification_WhenEmployeeLinked()
    {
        var notifications = new Mock<INotificationService>();
        var employees = new Mock<IBaseRepository<Employee>>();
        employees.Setup(x => x.GetQueryable()).Returns(new List<Employee>
        {
            new() { Id = 5, ExternalIdentityKey = "42" },
        }.BuildMock());

        var sut = CreateProducer(notifications.Object, employees.Object);
        await sut.NotifyTaskAssignedAsync(new TaskItem { Id = 9, EmployeeId = 5, Title = "Prepare report" }, CancellationToken.None);

        notifications.Verify(
            x => x.CreateAsync(
                It.Is<CreateNotificationRequestModel>(m =>
                    m.RecipientUserId == 42
                    && m.TypeKey == EmsNotificationTypeKeys.TaskAssigned
                    && m.DedupeKey == "task-assigned:9"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task NotifyLeaveSubmittedAsync_NotifiesManager_WhenManagerLinked()
    {
        var notifications = new Mock<INotificationService>();
        var employees = new Mock<IBaseRepository<Employee>>();
        employees.Setup(x => x.GetQueryable()).Returns((IQueryable<Employee>)new List<Employee>
        {
            new() { Id = 10, FirstName = "Alex", LastName = "Lee", ManagerId = 20 },
            new() { Id = 20, ExternalIdentityKey = "77" },
        }.BuildMock());

        var sut = CreateProducer(notifications.Object, employees.Object);
        await sut.NotifyLeaveSubmittedAsync(
            new LeaveRequest { Id = 3, EmployeeId = 10 },
            CancellationToken.None);

        notifications.Verify(
            x => x.CreateAsync(
                It.Is<CreateNotificationRequestModel>(m =>
                    m.RecipientUserId == 77
                    && m.TypeKey == EmsNotificationTypeKeys.LeaveSubmitted
                    && m.DedupeKey == "leave-submitted:3"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static EmsNotificationProducer CreateProducer(
        INotificationService notifications,
        IBaseRepository<Employee> employees)
        => new(
            notifications,
            new EmployeeNotificationRecipientResolver(employees),
            employees,
            NullLogger<EmsNotificationProducer>.Instance);
}
