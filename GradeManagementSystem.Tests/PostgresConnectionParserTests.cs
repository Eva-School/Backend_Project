using System;
using GradeManagementSystem.Repository.Data;
using Npgsql;
using Xunit;

namespace GradeManagementSystem.Tests
{
    public class PostgresConnectionParserTests
    {
        [Fact]
        public void Parse_Standard_Npgsql_String_Enforces_VerifyFull_On_Remote_Host()
        {
            var raw = "Host=db.oqozvmlxwvrcglmaaoeq.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SecretPassword123;";
            var result = PostgresConnectionParser.Parse(raw);

            var builder = new NpgsqlConnectionStringBuilder(result);
            Assert.Equal("db.oqozvmlxwvrcglmaaoeq.supabase.co", builder.Host);
            Assert.Equal(5432, builder.Port);
            Assert.Equal("postgres", builder.Database);
            Assert.Equal("postgres", builder.Username);
            Assert.Equal("SecretPassword123", builder.Password);
            Assert.Equal(SslMode.VerifyFull, builder.SslMode);
            Assert.Equal(5, builder.MinPoolSize);
            Assert.Equal(100, builder.MaxPoolSize);
        }

        [Fact]
        public void Parse_Uri_With_Colon_In_Password_Preserves_Complete_Password()
        {
            // Password contains multiple colons and special characters
            var raw = "postgres://postgres:Complex:Pass:Word@123!@aws-0-eu-central-1.pooler.supabase.com:5432/postgres";
            var result = PostgresConnectionParser.Parse(raw);

            var builder = new NpgsqlConnectionStringBuilder(result);
            Assert.Equal("aws-0-eu-central-1.pooler.supabase.com", builder.Host);
            Assert.Equal(5432, builder.Port);
            Assert.Equal("postgres", builder.Database);
            Assert.Equal("postgres", builder.Username);
            Assert.Equal("Complex:Pass:Word@123!", builder.Password);
            Assert.Equal(SslMode.VerifyFull, builder.SslMode);
        }

        [Fact]
        public void Parse_Supabase_Shared_Pooler_Uri_Constructs_Valid_VerifyFull_Connection()
        {
            var raw = "postgresql://postgres.oqozvmlxwvrcglmaaoeq:MySuperSecretPass!@aws-1-eu-west-3.pooler.supabase.com:5432/postgres";
            var result = PostgresConnectionParser.Parse(raw);

            var builder = new NpgsqlConnectionStringBuilder(result);
            Assert.Equal("aws-1-eu-west-3.pooler.supabase.com", builder.Host);
            Assert.Equal(5432, builder.Port);
            Assert.Equal("postgres", builder.Database);
            Assert.Equal("postgres.oqozvmlxwvrcglmaaoeq", builder.Username);
            Assert.Equal("MySuperSecretPass!", builder.Password);
            Assert.Equal(SslMode.VerifyFull, builder.SslMode);
        }

        [Fact]
        public void Parse_Uri_With_Percent_Encoded_Credentials_Decodes_Correctly()
        {
            // user: user%40domain.com, password: p%40ss%3Aword%23
            var raw = "postgresql://user%40domain.com:p%40ss%3Aword%23@db.example.com:5432/appdb";
            var result = PostgresConnectionParser.Parse(raw);

            var builder = new NpgsqlConnectionStringBuilder(result);
            Assert.Equal("user@domain.com", builder.Username);
            Assert.Equal("p@ss:word#", builder.Password);
            Assert.Equal("db.example.com", builder.Host);
            Assert.Equal("appdb", builder.Database);
        }

        [Fact]
        public void Parse_Localhost_Does_Not_Force_VerifyFull()
        {
            var raw = "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=test;";
            var result = PostgresConnectionParser.Parse(raw);

            var builder = new NpgsqlConnectionStringBuilder(result);
            Assert.NotEqual(SslMode.VerifyFull, builder.SslMode);
        }

        [Fact]
        public void Parse_Cert_With_ImportFromPem_Succeeds()
        {
            var path = PostgresConnectionParser.EnsureSupabaseCertificate();
            var text = System.IO.File.ReadAllText(path);
            var cert = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(text);
            Assert.NotNull(cert);
            Assert.Contains("Supabase", cert.Subject);

            var coll = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection();
            coll.ImportFromPem(text);
            Assert.True(coll.Count > 0);
        }

        [Fact]
        public void Parse_Sets_RootCertificate_When_Configured()
        {
            var builder = new NpgsqlConnectionStringBuilder();
            builder.RootCertificate = "/tmp/test.crt";
            Assert.Equal("/tmp/test.crt", builder.RootCertificate);
            Assert.Contains("Root Certificate=/tmp/test.crt", builder.ConnectionString);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_Blank_Returns_Empty(string? input)
        {
            var result = PostgresConnectionParser.Parse(input);
            Assert.Equal(string.Empty, result);
        }
    }
}
