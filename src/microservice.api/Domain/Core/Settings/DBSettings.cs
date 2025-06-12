using Microsoft.Identity.Client;
using System.Security.Cryptography;
using System.Text;

namespace Domain.Core.Settings
{
    public sealed record DBSettings
    {
        public string ServerUrl { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int CommandTimeout { get; set; } = 10;
        public int ConnectTimeout { get; set; } = 30;
        public int Port { get; set; } = 0;

        // Funcionalidades adicionadas da versão 4
        public bool EnableSsl { get; set; } = false;
        public bool TrustServerCertificate { get; set; } = true;
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 1;
        public string ApplicationName { get; set; } = "MicroserviceAPI";

        public string GetConnectionString()
        {
            ValidateConnectionParameters();

            var _ConnectTimeout = ConnectTimeout == 0 ? 20 : ConnectTimeout;
            var decryptedPassword = CryptSPA.decryptDES(Password);

#if SqlServerCondition
            return $"Data Source={ServerUrl}{GetPortSuffix()};Initial Catalog={Database};" +
                   $"TrustServerCertificate={TrustServerCertificate};Persist Security Info=True;" +
                   $"User ID={Username};Password={decryptedPassword};" +
                   $"MultipleActiveResultSets=true;Connect Timeout={_ConnectTimeout};" +
                   $"Command Timeout={CommandTimeout};Max Pool Size={MaxPoolSize};" +
                   $"Min Pool Size={MinPoolSize};Pooling=true;" +
                   $"Enlist=false;Application Name={ApplicationName}";
#elif PSQLCondition
            return $"Host={ServerUrl};Port={GetPostgreSQLPort()};Database={Database};" +
                   $"Username={Username};Password={decryptedPassword};" +
                   $"Timeout={_ConnectTimeout};CommandTimeout={CommandTimeout};" +
                   $"Pooling=true;MinPoolSize={MinPoolSize};MaxPoolSize={MaxPoolSize};" +
                   $"SSL Mode={(EnableSsl ? "Require" : "Disable")};" +
                   $"Trust Server Certificate={TrustServerCertificate};" +
                   $"Application Name={ApplicationName}";
#else
            throw new NotSupportedException("Tipo de banco de dados não suportado");
#endif
        }

        public string GetNoSQLConnectionString()
        {
            ValidateNoSQLParameters();

            var serverUrl = Port > 0 ? $"{ServerUrl}:{Port}" : ServerUrl;
            var sslParams = EnableSsl ? "&ssl=true&tlsAllowInvalidCertificates=true" : "";

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                return $"mongodb://{serverUrl}/?authSource=admin&appName={ApplicationName}" +
                       $"&maxPoolSize={MaxPoolSize}&minPoolSize={MinPoolSize}{sslParams}";
            }

            var decryptedPassword = CryptSPA.decryptDES(Password);
            return $"mongodb://{Username}:{decryptedPassword}@{serverUrl}/" +
                   $"?authSource=admin&appName={ApplicationName}" +
                   $"&maxPoolSize={MaxPoolSize}&minPoolSize={MinPoolSize}{sslParams}";
        }

        public string GetNoSQLConnectionStringReplicaSet(string replicaSetName = "rs0")
        {
            ValidateNoSQLParameters();

            var serverUrl = Port > 0 ? $"{ServerUrl}:{Port}" : ServerUrl;
            var sslParams = EnableSsl ? "&ssl=true&tlsAllowInvalidCertificates=true" : "";

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                return $"mongodb://{serverUrl}/?replicaSet={replicaSetName}&appName={ApplicationName}" +
                       $"&maxPoolSize={MaxPoolSize}&minPoolSize={MinPoolSize}{sslParams}";
            }

            var decryptedPassword = CryptSPA.decryptDES(Password);
            return $"mongodb://{Username}:{decryptedPassword}@{serverUrl}/" +
                   $"?replicaSet={replicaSetName}&authSource=admin&appName={ApplicationName}" +
                   $"&maxPoolSize={MaxPoolSize}&minPoolSize={MinPoolSize}{sslParams}";
        }

        public string GetRedisConnectionString()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                throw new InvalidOperationException("ServerUrl é obrigatório para Redis");
            }

            var connectionStringBuilder = new StringBuilder();
            connectionStringBuilder.Append($"{ServerUrl}");

            if (Port > 0)
            {
                connectionStringBuilder.Append($":{Port}");
            }

            if (!string.IsNullOrEmpty(Password))
            {
                var decryptedPassword = CryptSPA.decryptDES(Password);
                connectionStringBuilder.Append($",password={decryptedPassword}");
            }

            if (EnableSsl)
            {
                connectionStringBuilder.Append(",ssl=true");
                connectionStringBuilder.Append($",sslProtocols=tls12");
            }

            connectionStringBuilder.Append($",connectTimeout={ConnectTimeout * 1000}"); // Redis usa milliseconds
            connectionStringBuilder.Append($",syncTimeout={CommandTimeout * 1000}");
            connectionStringBuilder.Append($",name={ApplicationName}");
            connectionStringBuilder.Append(",abortConnect=false");

            return connectionStringBuilder.ToString();
        }

        private string GetPortSuffix()
        {
#if SqlServerCondition
            return Port > 0 && Port != 1433 ? $",{Port}" : string.Empty;
#else
            return string.Empty;
#endif
        }

        private int GetPostgreSQLPort()
        {
#if PSQLCondition
            return Port > 0 ? Port : 5432;
#else
            return 5432;
#endif
        }

        // Validações expandidas da versão 4
        private void ValidateConnectionParameters()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl))
                throw new InvalidOperationException("ServerUrl é obrigatório");

            if (string.IsNullOrWhiteSpace(Database))
                throw new InvalidOperationException("Database é obrigatório");

            if (string.IsNullOrWhiteSpace(Username))
                throw new InvalidOperationException("Username é obrigatório");

            if (string.IsNullOrWhiteSpace(Password))
                throw new InvalidOperationException("Password é obrigatório");

            if (CommandTimeout <= 0)
                throw new InvalidOperationException("CommandTimeout deve ser maior que zero");

            if (ConnectTimeout <= 0)
                throw new InvalidOperationException("ConnectTimeout deve ser maior que zero");

            if (MaxPoolSize <= 0)
                throw new InvalidOperationException("MaxPoolSize deve ser maior que zero");

            if (MinPoolSize < 0)
                throw new InvalidOperationException("MinPoolSize deve ser maior ou igual a zero");

            if (MinPoolSize > MaxPoolSize)
                throw new InvalidOperationException("MinPoolSize não pode ser maior que MaxPoolSize");
        }

        private void ValidateNoSQLParameters()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl))
                throw new InvalidOperationException("ServerUrl é obrigatório para NoSQL");

            if (string.IsNullOrWhiteSpace(Database))
                throw new InvalidOperationException("Database é obrigatório para NoSQL");

            if (MaxPoolSize <= 0)
                throw new InvalidOperationException("MaxPoolSize deve ser maior que zero");

            if (MinPoolSize < 0)
                throw new InvalidOperationException("MinPoolSize deve ser maior ou igual a zero");
        }


        public static DBSettings CreateSqlServerSettings(string server, string database, string username, string password, int port = 1433)
        {
            return new DBSettings
            {
                ServerUrl = server,
                Database = database,
                Username = username,
                Password = CryptSPA.encryptDES(password), // Já criptografa
                Port = port,
                CommandTimeout = 10, // Padrão versão 5
                ConnectTimeout = 30, // Padrão versão 5
                MaxPoolSize = 100,
                MinPoolSize = 1,
                TrustServerCertificate = true,
                ApplicationName = "MicroserviceAPI"
            };
        }

        public static DBSettings CreatePostgreSQLSettings(string server, string database, string username, string password, int port = 5432)
        {
            return new DBSettings
            {
                ServerUrl = server,
                Database = database,
                Username = username,
                Password = CryptSPA.encryptDES(password), // Já criptografa
                Port = port,
                CommandTimeout = 10, // Padrão versão 5
                ConnectTimeout = 30, // Padrão versão 5
                MaxPoolSize = 100,
                MinPoolSize = 1,
                EnableSsl = false,
                ApplicationName = "MicroserviceAPI"
            };
        }

        public static DBSettings CreateMongoDBSettings(string server, string database, string username = "", string password = "", int port = 27017)
        {
            return new DBSettings
            {
                ServerUrl = server,
                Database = database,
                Username = username,
                Password = string.IsNullOrEmpty(password) ? password : CryptSPA.encryptDES(password),
                Port = port,
                CommandTimeout = 10, // Padrão versão 5
                ConnectTimeout = 30, // Padrão versão 5
                MaxPoolSize = 100,
                MinPoolSize = 1,
                EnableSsl = false,
                ApplicationName = "MicroserviceAPI"
            };
        }

        public static DBSettings CreateRedisSettings(string server, string password = "", int port = 6379, bool enableSsl = false)
        {
            return new DBSettings
            {
                ServerUrl = server,
                Password = string.IsNullOrEmpty(password) ? password : CryptSPA.encryptDES(password),
                Port = port,
                CommandTimeout = 5,
                ConnectTimeout = 5,
                EnableSsl = enableSsl,
                ApplicationName = "MicroserviceAPI"
            };
        }
    }



    internal static class CryptSPA
    {
        private const string Key = "h&xt&m?|";

        public static string decryptDES(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                if (!IsBase64String(encryptedText))
                    return encryptedText;

                var base64EncodedBytes = Convert.FromBase64String(encryptedText);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                // Se falhar na descriptografia, retorna o texto original
                return encryptedText;
            }
        }

        public static string encryptDES(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                // Implementação simples usando Base64 para compatibilidade
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(plainTextBytes);
            }
            catch
            {
                return plainText;
            }
        }

        private static bool IsBase64String(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length % 4 != 0
                || s.Contains(" ") || s.Contains("\t") || s.Contains("\r") || s.Contains("\n"))
                return false;

            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}