using Pukar.Usermanagement.Contracts.Invitations;

namespace Pukar.Usermanagement.Application.Services.Invitations;

public interface IInvitationService
{
    Task<InvitationResponseModel> CreateOrSendAsync(CreateInvitationRequestModel request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvitationResponseModel>> ListByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    Task<InvitationResponseModel> ResendAsync(int invitationId, CancellationToken cancellationToken = default);

    Task RevokeAsync(int invitationId, CancellationToken cancellationToken = default);

    Task AcceptAsync(AcceptInvitationRequestModel request, CancellationToken cancellationToken = default);

    Task RevokePendingByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
}
