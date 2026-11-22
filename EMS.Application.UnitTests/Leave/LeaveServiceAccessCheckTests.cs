using EMS.Application.DTOs.Leave;
using EMS.Application.Services.Leave;
using EMS.Application.Services.Notifications;
using EMS.Domain.DbModels;
using EMS.Domain.Enums;
using EMS.Domain.Repositories.Interface;
using Moq;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Leave;

/// <summary>
/// B06 U16: each leave service action asks the right question of <see cref="ILeaveEmployeeAccessService"/>.
/// Reads of requests and balances use the view check, request changes use the manage check, and every
/// attachment action uses the attachment check. The access mock refuses everything, so each call stops at its
/// check and the test can see which check it asked.
/// </summary>
public class LeaveServiceAccessCheckTests
{
    private const int EmployeeId = 10;
    private const int RequestId = 77;
    private const int AttachmentId = 300;

    private enum Asked
    {
        View,
        Manage,
        Attachments,
    }

    [Test]
    public void LeaveRequestService_GetByEmployee_UsesViewCheck()
    {
        var (sut, access) = CreateRequestService();
        AssertAsked(access, Asked.View, () => sut.GetByEmployeeAsync(EmployeeId, CancellationToken.None));
    }

    [Test]
    public void LeaveRequestService_GetById_UsesViewCheck()
    {
        var (sut, access) = CreateRequestService();
        AssertAsked(access, Asked.View, () => sut.GetByIdAsync(RequestId, CancellationToken.None));
    }

    [Test]
    public void LeaveRequestService_Create_UsesManageCheck()
    {
        var (sut, access) = CreateRequestService();
        var request = new CreateLeaveRequestRequestModel
        {
            EmployeeId = EmployeeId,
            LeaveTypeId = 3,
            StartDateUtc = new DateTime(2026, 5, 11),
            EndDateUtc = new DateTime(2026, 5, 12),
            RequestedAmount = 1,
        };
        AssertAsked(access, Asked.Manage, () => sut.CreateAsync(request, CancellationToken.None));
    }

    [Test]
    public void LeaveRequestService_Update_UsesManageCheck()
    {
        var (sut, access) = CreateRequestService();
        var request = new UpdateLeaveRequestRequestModel
        {
            StartDateUtc = new DateTime(2026, 5, 11),
            EndDateUtc = new DateTime(2026, 5, 12),
            RequestedAmount = 1,
        };
        AssertAsked(access, Asked.Manage, () => sut.UpdateAsync(RequestId, request, CancellationToken.None));
    }

    [Test]
    public void LeaveRequestService_Cancel_UsesManageCheck()
    {
        var (sut, access) = CreateRequestService();
        AssertAsked(access, Asked.Manage, () => sut.CancelAsync(RequestId, CancellationToken.None));
    }

    [Test]
    public void LeaveBalanceService_Reads_UseViewCheck()
    {
        var access = DenyingAccess();
        var sut = new LeaveBalanceService(new Mock<ILeaveBalanceRepository>().Object, access.Object);

        AssertAsked(access, Asked.View, () => sut.GetByEmployeeAsync(EmployeeId, CancellationToken.None));
        access.Invocations.Clear();
        AssertAsked(access, Asked.View, () => sut.GetByEmployeeAndTypeAsync(EmployeeId, 3, CancellationToken.None));
    }

    [Test]
    public void LeaveAttachmentService_EveryAction_UsesAttachmentCheck()
    {
        var access = DenyingAccess();
        var requestRepo = new Mock<IBaseRepository<LeaveRequest>>();
        requestRepo.Setup(x => x.GetByIdAsync(RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(PendingRequest());
        var attachmentRepo = new Mock<ILeaveRequestAttachmentRepository>();
        attachmentRepo.Setup(x => x.GetByIdAsync(AttachmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveRequestAttachment { Id = AttachmentId, LeaveRequestId = RequestId });
        var sut = new LeaveAttachmentService(attachmentRepo.Object, requestRepo.Object, access.Object);

        var actions = new Func<Task>[]
        {
            () => sut.GetByRequestIdAsync(RequestId, CancellationToken.None),
            () => sut.EnsureCanAttachToRequestAsync(RequestId, CancellationToken.None),
            () => sut.AddAsync(RequestId, "note.pdf", "application/pdf", 10, "/attachments/Leave/77/x.pdf", CancellationToken.None),
            () => sut.GetByIdAsync(AttachmentId, CancellationToken.None),
            () => sut.DeleteAsync(AttachmentId, CancellationToken.None),
        };

        foreach (var action in actions)
        {
            access.Invocations.Clear();
            AssertAsked(access, Asked.Attachments, () => action());
        }

        attachmentRepo.Verify(x => x.AddAsync(It.IsAny<LeaveRequestAttachment>(), It.IsAny<CancellationToken>()), Times.Never);
        attachmentRepo.Verify(x => x.Remove(It.IsAny<LeaveRequestAttachment>()), Times.Never);
    }

    private static LeaveRequest PendingRequest() => new()
    {
        Id = RequestId,
        OrganizationId = 1,
        EmployeeId = EmployeeId,
        LeaveTypeId = 3,
        RequestedAmount = 1,
        Status = LeaveRequestStatus.Pending,
    };

    private static Mock<ILeaveEmployeeAccessService> DenyingAccess()
    {
        var access = new Mock<ILeaveEmployeeAccessService>();
        access.Setup(x => x.EnsureCanViewEmployeeLeaveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(LeaveAccessMessages.Denied));
        access.Setup(x => x.EnsureCanManageEmployeeLeaveAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(LeaveAccessMessages.Denied));
        access.Setup(x => x.EnsureCanAccessLeaveAttachmentsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(LeaveAccessMessages.Denied));
        return access;
    }

    private static (LeaveRequestService Sut, Mock<ILeaveEmployeeAccessService> Access) CreateRequestService()
    {
        var access = DenyingAccess();
        var requestRepo = new Mock<ILeaveRequestRepository>();
        requestRepo.Setup(x => x.GetByIdAsync(RequestId, It.IsAny<CancellationToken>())).ReturnsAsync(PendingRequest());

        var sut = new LeaveRequestService(
            requestRepo.Object,
            new Mock<ILeaveBalanceRepository>().Object,
            new Mock<ILeaveTypeRepository>().Object,
            new Mock<IBaseRepository<Employee>>().Object,
            access.Object,
            new Mock<IEmsNotificationProducer>().Object);

        return (sut, access);
    }

    private static void AssertAsked(Mock<ILeaveEmployeeAccessService> access, Asked expected, Func<Task> call)
    {
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() => call());
        Assert.That(ex!.Message, Is.EqualTo(LeaveAccessMessages.Denied));

        access.Verify(
            x => x.EnsureCanViewEmployeeLeaveAsync(EmployeeId, It.IsAny<CancellationToken>()),
            expected == Asked.View ? Times.Once() : Times.Never());
        access.Verify(
            x => x.EnsureCanManageEmployeeLeaveAsync(EmployeeId, It.IsAny<CancellationToken>()),
            expected == Asked.Manage ? Times.Once() : Times.Never());
        access.Verify(
            x => x.EnsureCanAccessLeaveAttachmentsAsync(EmployeeId, It.IsAny<CancellationToken>()),
            expected == Asked.Attachments ? Times.Once() : Times.Never());
    }
}
