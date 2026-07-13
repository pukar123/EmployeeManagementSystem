using Moq;
using Pukar.Notifications.Application.DTOs;
using Pukar.Notifications.Application.Services;
using Pukar.Notifications.Domain.DbModels;
using Pukar.Notifications.Domain.Enums;
using Pukar.Shared;

namespace EMS.Application.UnitTests.Notifications;

public class NotificationServiceTests
{
    [Test]
    public async Task GetForCurrentUserAsync_ReturnsOnlyCurrentUserNotifications()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(
            CreateNotification(1, 10, "A"),
            CreateNotification(2, 20, "B"),
            CreateNotification(3, 10, "C"));

        var sut = CreateService(repo, currentUserId: 10);
        var result = await sut.GetForCurrentUserAsync(new NotificationQueryModel(), CancellationToken.None);

        Assert.That(result.Select(x => x.Title), Is.EquivalentTo(new[] { "A", "C" }));
    }

    [Test]
    public async Task GetUnreadCountAsync_ReturnsUnreadOnly()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(
            CreateNotification(1, 10, "A", isRead: false),
            CreateNotification(2, 10, "B", isRead: true),
            CreateNotification(3, 10, "C", isRead: false));

        var sut = CreateService(repo, currentUserId: 10);
        var result = await sut.GetUnreadCountAsync(CancellationToken.None);

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task MarkReadAsync_MarksSingleNotification()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(CreateNotification(1, 10, "A"));

        var sut = CreateService(repo, currentUserId: 10);
        var result = await sut.MarkReadAsync(1, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsRead, Is.True);
            Assert.That(repo.Items[0].ReadAtUtc, Is.Not.Null);
        });
    }

    [Test]
    public async Task MarkAllReadAsync_MarksAllUnreadForUser()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(
            CreateNotification(1, 10, "A", isRead: false),
            CreateNotification(2, 10, "B", isRead: false),
            CreateNotification(3, 20, "C", isRead: false));

        var sut = CreateService(repo, currentUserId: 10);
        var count = await sut.MarkAllReadAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(2));
            Assert.That(repo.Items.Where(x => x.RecipientUserId == 10).All(x => x.IsRead), Is.True);
            Assert.That(repo.Items.Single(x => x.RecipientUserId == 20).IsRead, Is.False);
        });
    }

    [Test]
    public async Task ArchiveAsync_SetsArchivedFlag()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(CreateNotification(1, 10, "A"));

        var sut = CreateService(repo, currentUserId: 10);
        await sut.ArchiveAsync(1, CancellationToken.None);

        Assert.That(repo.Items[0].IsArchived, Is.True);
    }

    [Test]
    public async Task CreateAsync_ReusesExisting_WhenDedupeKeyMatches()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(new Notification
        {
            Id = 7,
            RecipientUserId = 10,
            TypeKey = "task.assigned",
            Title = "Existing",
            Body = "Existing body",
            DedupeKey = "task-assigned:1",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
        });

        var sut = CreateService(repo, currentUserId: 10);
        var result = await sut.CreateAsync(
            new CreateNotificationRequestModel
            {
                RecipientUserId = 10,
                TypeKey = "task.assigned",
                Title = "New",
                Body = "New body",
                DedupeKey = "task-assigned:1",
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(7));
            Assert.That(result.Title, Is.EqualTo("Existing"));
            Assert.That(repo.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task GetForCurrentUserAsync_ExcludesExpiredNotifications()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(
            CreateNotification(1, 10, "Active", expiresAtUtc: DateTime.UtcNow.AddDays(1)),
            CreateNotification(2, 10, "Expired", expiresAtUtc: DateTime.UtcNow.AddDays(-1)));

        var sut = CreateService(repo, currentUserId: 10);
        var result = await sut.GetForCurrentUserAsync(new NotificationQueryModel(), CancellationToken.None);

        Assert.That(result.Select(x => x.Title), Is.EquivalentTo(new[] { "Active" }));
    }

    [Test]
    public void MarkReadAsync_Throws_WhenNotificationNotOwned()
    {
        var repo = new InMemoryNotificationRepository();
        repo.Seed(CreateNotification(1, 20, "A"));

        var sut = CreateService(repo, currentUserId: 10);
        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () =>
            await sut.MarkReadAsync(1, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("Notification was not found."));
    }

    private static NotificationService CreateService(InMemoryNotificationRepository repo, int? currentUserId)
    {
        var accessor = new Mock<INotificationCurrentUserAccessor>();
        accessor.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        return new NotificationService(repo, accessor.Object);
    }

    private static Notification CreateNotification(
        int id,
        int recipientUserId,
        string title,
        bool isRead = false,
        DateTime? expiresAtUtc = null)
        => new()
        {
            Id = id,
            RecipientUserId = recipientUserId,
            TypeKey = "test",
            Title = title,
            Body = "Body",
            IsRead = isRead,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
        };
}
