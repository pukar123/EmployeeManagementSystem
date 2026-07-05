using Microsoft.IdentityModel.Tokens;

namespace Pukar.Usermanagement.Application.Services.Jwt;

public interface IJwksProvider
{
    JsonWebKeySet GetJsonWebKeySet();

    SecurityKey GetSigningKey();

    string KeyId { get; }
}
