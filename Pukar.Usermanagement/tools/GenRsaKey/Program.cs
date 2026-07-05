using System.Security.Cryptography;

var pem = RSA.Create(2048).ExportPkcs8PrivateKeyPem();
var outPath = args.Length > 0 ? args[0] : "dev-rsa-key.pem";
await File.WriteAllTextAsync(outPath, pem);
Console.WriteLine($"Wrote {outPath}");
