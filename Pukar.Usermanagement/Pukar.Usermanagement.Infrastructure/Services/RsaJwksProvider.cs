using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pukar.Usermanagement.Application.Options;
using Pukar.Usermanagement.Application.Services.Jwt;

namespace Pukar.Usermanagement.Infrastructure.Services;

public sealed class RsaJwksProvider : IJwksProvider
{
    private readonly RsaSecurityKey _signingKey;

    public RsaJwksProvider(IOptions<JwtTokenOptions> options, IHostEnvironment? environment = null)
    {
        var opt = options.Value;
        var pem = opt.SigningKeyPem;
        if (string.IsNullOrWhiteSpace(pem) && !string.IsNullOrWhiteSpace(opt.SigningKeyPemFile))
        {
            var basePath = environment?.ContentRootPath ?? Directory.GetCurrentDirectory();
            var path = Path.IsPathRooted(opt.SigningKeyPemFile)
                ? opt.SigningKeyPemFile
                : Path.Combine(basePath, opt.SigningKeyPemFile);
            pem = File.ReadAllText(path);
        }

        if (string.IsNullOrWhiteSpace(pem))
            throw new InvalidOperationException("Jwt:SigningKeyPem or Jwt:SigningKeyPemFile is required for RSA signing.");

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        _signingKey = new RsaSecurityKey(rsa) { KeyId = opt.SigningKeyId };
        KeyId = opt.SigningKeyId;
    }

    public string KeyId { get; }

    public SecurityKey GetSigningKey() => _signingKey;

    public JsonWebKeySet GetJsonWebKeySet()
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(_signingKey);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        return new JsonWebKeySet { Keys = { jwk } };
    }
}
