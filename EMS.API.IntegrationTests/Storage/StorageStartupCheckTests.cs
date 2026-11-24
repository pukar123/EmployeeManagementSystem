using EMS.API.IntegrationTests.Harness;
using Microsoft.AspNetCore.Hosting;

namespace EMS.API.IntegrationTests.Storage;

/// <summary>B08 S1: the API refuses to start when <c>Storage:PrivateRoot</c> is inside the web root.</summary>
[TestFixture]
public sealed class StorageStartupCheckTests
{
    [Test]
    public void S1_PrivateRootInsideWebRoot_ApiDoesNotStart()
    {
        using var temp = new TempFolder();
        var webRoot = temp.Combine("wwwroot");
        Directory.CreateDirectory(webRoot);

        var factory = new EmsApiFactory(new Dictionary<string, string?>
        {
            [WebHostDefaults.WebRootKey] = webRoot,
            ["Storage:PrivateRoot"] = Path.Combine(webRoot, "private"),
        });

        try
        {
            var error = Assert.Catch(() => _ = factory.Services);
            Assert.That(Flatten(error).Any(e => e.Message.Contains("must not be the web root", StringComparison.Ordinal)), Is.True,
                $"Expected the private root check to stop start-up. Got: {error}");
            Assert.That(Directory.Exists(Path.Combine(webRoot, "private")), Is.False, "No private folder may be created inside the web root.");
        }
        finally
        {
            try
            {
                factory.Dispose();
            }
            catch (Exception)
            {
                // The host never started; disposing it may throw again. That is not part of this check.
            }
        }
    }

    private static IEnumerable<Exception> Flatten(Exception? error)
    {
        while (error is not null)
        {
            yield return error;
            if (error is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions.SelectMany(Flatten))
                    yield return inner;
                yield break;
            }

            error = error.InnerException;
        }
    }
}
