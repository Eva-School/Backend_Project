using System;
using Npgsql;

namespace GradeManagementSystem.Repository.Data
{
    /// <summary>
    /// Production-grade connection string parser and validator for PostgreSQL.
    /// Handles standard Npgsql key-value format and postgres:// / postgresql:// URIs,
    /// preserving passwords with colons/special characters, and enforcing certificate-validating TLS.
    /// </summary>
    public static class PostgresConnectionParser
    {
        public static string Parse(string? connectionString, bool enforceVerifyFull = true)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return string.Empty;
            }

            var trimmed = connectionString.Trim();
            NpgsqlConnectionStringBuilder builder;

            if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                // Robust parsing of postgres URI supporting unencoded special characters in passwords
                builder = new NpgsqlConnectionStringBuilder();

                var schemeEnd = trimmed.IndexOf("://", StringComparison.OrdinalIgnoreCase);
                var afterScheme = trimmed.Substring(schemeEnd + 3);

                // Split path & query
                string authority;
                string pathAndQuery = "";
                var firstSlash = afterScheme.IndexOf('/');
                var firstQuestion = afterScheme.IndexOf('?');

                int pathStart = -1;
                if (firstSlash >= 0 && firstQuestion >= 0) pathStart = Math.Min(firstSlash, firstQuestion);
                else if (firstSlash >= 0) pathStart = firstSlash;
                else if (firstQuestion >= 0) pathStart = firstQuestion;

                if (pathStart >= 0)
                {
                    authority = afterScheme.Substring(0, pathStart);
                    pathAndQuery = afterScheme.Substring(pathStart);
                }
                else
                {
                    authority = afterScheme;
                }

                // In authority, find the LAST '@' which separates userinfo from host:port
                var lastAt = authority.LastIndexOf('@');
                string hostPortPart;
                if (lastAt >= 0)
                {
                    var userInfo = authority.Substring(0, lastAt);
                    hostPortPart = authority.Substring(lastAt + 1);

                    var firstColon = userInfo.IndexOf(':');
                    if (firstColon >= 0)
                    {
                        builder.Username = Uri.UnescapeDataString(userInfo.Substring(0, firstColon));
                        builder.Password = Uri.UnescapeDataString(userInfo.Substring(firstColon + 1));
                    }
                    else
                    {
                        builder.Username = Uri.UnescapeDataString(userInfo);
                    }
                }
                else
                {
                    hostPortPart = authority;
                }

                // Host and Port
                var hostColon = hostPortPart.LastIndexOf(':');
                if (hostColon >= 0 && int.TryParse(hostPortPart.Substring(hostColon + 1), out var parsedPort))
                {
                    builder.Host = hostPortPart.Substring(0, hostColon);
                    builder.Port = parsedPort;
                }
                else
                {
                    builder.Host = hostPortPart;
                    builder.Port = 5432;
                }

                // Database name
                var path = pathAndQuery;
                var queryIndex = path.IndexOf('?');
                string dbName = "";
                string queryStr = "";
                if (queryIndex >= 0)
                {
                    dbName = path.Substring(0, queryIndex).TrimStart('/');
                    queryStr = path.Substring(queryIndex + 1);
                }
                else
                {
                    dbName = path.TrimStart('/');
                }

                if (!string.IsNullOrWhiteSpace(dbName))
                {
                    builder.Database = dbName;
                }

                // Query parameters
                if (!string.IsNullOrWhiteSpace(queryStr))
                {
                    var pairs = queryStr.Split('&', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pair in pairs)
                    {
                        var kv = pair.Split('=', 2);
                        var key = Uri.UnescapeDataString(kv[0]).ToLowerInvariant();
                        var val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";

                        if (key == "sslmode" && Enum.TryParse<SslMode>(val, true, out var parsedMode))
                        {
                            builder.SslMode = parsedMode;
                        }
                        else if (key == "sslrootcert" || key == "rootcertificate")
                        {
                            builder.RootCertificate = val;
                        }
                    }
                }
            }
            else
            {
                builder = new NpgsqlConnectionStringBuilder(trimmed);
            }

            // Local development fallback
            var isLocal = string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(builder.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);

            if (isLocal)
            {
                if (!builder.ContainsKey("SSL Mode"))
                {
                    builder.SslMode = SslMode.Prefer;
                }
            }
            else if (enforceVerifyFull)
            {
                // Enforce certificate-validating TLS (VerifyFull)
                builder.SslMode = SslMode.VerifyFull;

                var isSupabase = builder.Host != null && (
                    builder.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) ||
                    builder.Host.EndsWith(".supabase.com", StringComparison.OrdinalIgnoreCase));

                if (isSupabase && string.IsNullOrEmpty(builder.RootCertificate))
                {
                    builder.RootCertificate = EnsureSupabaseCertificate();
                }
            }

            // Connection pool & stability defaults
            if (builder.MinPoolSize <= 0) builder.MinPoolSize = 5;
            if (builder.MaxPoolSize <= 0) builder.MaxPoolSize = 50;
            if (builder.Timeout <= 0) builder.Timeout = 60;
            if (builder.CommandTimeout <= 0) builder.CommandTimeout = 60;
            if (builder.KeepAlive <= 0) builder.KeepAlive = 30;

            return builder.ConnectionString;
        }

        public const string SupabaseCertBundlePem =
@"-----BEGIN CERTIFICATE-----
MIIDxDCCAqygAwIBAgIUbLxMod62P2ktCiAkxnKJwtE9VPYwDQYJKoZIhvcNAQEL
BQAwazELMAkGA1UEBhMCVVMxEDAOBgNVBAgMB0RlbHdhcmUxEzARBgNVBAcMCk5l
dyBDYXN0bGUxFTATBgNVBAoMDFN1cGFiYXNlIEluYzEeMBwGA1UEAwwVU3VwYWJh
c2UgUm9vdCAyMDIxIENBMB4XDTIxMDQyODEwNTY1M1oXDTMxMDQyNjEwNTY1M1ow
azELMAkGA1UEBhMCVVMxEDAOBgNVBAgMB0RlbHdhcmUxEzARBgNVBAcMCk5ldyBD
YXN0bGUxFTATBgNVBAoMDFN1cGFiYXNlIEluYzEeMBwGA1UEAwwVU3VwYWJhc2Ug
Um9vdCAyMDIxIENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqQXW
QyHOB+qR2GJobCq/CBmQ40G0oDmCC3mzVnn8sv4XNeWtE5XcEL0uVih7Jo4Dkx1Q
DmGHBH1zDfgs2qXiLb6xpw/CKQPypZW1JssOTMIfQppNQ87K75Ya0p25Y3ePS2t2
GtvHxNjUV6kjOZjEn2yWEcBdpOVCUYBVFBNMB4YBHkNRDa/+S4uywAoaTWnCJLUi
cvTlHmMw6xSQQn1UfRQHk50DMCEJ7Cy1RxrZJrkXXRP3LqQL2ijJ6F4yMfh+Gyb4
O4XajoVj/+R4GwywKYrrS8PrSNtwxr5StlQO8zIQUSMiq26wM8mgELFlS/32Uclt
NaQ1xBRizkzpZct9DwIDAQABo2AwXjALBgNVHQ8EBAMCAQYwHQYDVR0OBBYEFKjX
uXY32CztkhImng4yJNUtaUYsMB8GA1UdIwQYMBaAFKjXuXY32CztkhImng4yJNUt
aUYsMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggEBAB8spzNn+4VU
tVxbdMaX+39Z50sc7uATmus16jmmHjhIHz+l/9GlJ5KqAMOx26mPZgfzG7oneL2b
VW+WgYUkTT3XEPFWnTp2RJwQao8/tYPXWEJDc0WVQHrpmnWOFKU/d3MqBgBm5y+6
jB81TU/RG2rVerPDWP+1MMcNNy0491CTL5XQZ7JfDJJ9CCmXSdtTl4uUQnSuv/Qx
Cea13BX2ZgJc7Au30vihLhub52De4P/4gonKsNHYdbWjg7OWKwNv/zitGDVDB9Y2
CMTyZKG3XEu5Ghl1LEnI3QmEKsqaCLv12BnVjbkSeZsMnevJPs1Ye6TjjJwdik5P
o/bKiIz+Fq8=
-----END CERTIFICATE-----";

        public static string EnsureSupabaseCertificate()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Certificates", "prod-supabase.crt"),
                Path.Combine(Directory.GetCurrentDirectory(), "Certificates", "prod-supabase.crt"),
                Path.Combine(Directory.GetCurrentDirectory(), "Backend_Project", "GradeManagementSystem.Repository", "Certificates", "prod-supabase.crt")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var fallbackDir = Path.Combine(Path.GetTempPath(), "eva_school_certs");
            Directory.CreateDirectory(fallbackDir);
            var fallbackPath = Path.Combine(fallbackDir, "prod-supabase.crt");
            if (!File.Exists(fallbackPath) || new FileInfo(fallbackPath).Length < 100)
            {
                File.WriteAllText(fallbackPath, SupabaseCertBundlePem);
            }
            return fallbackPath;
        }
    }
}
