using Microsoft.Data.SqlClient;

namespace NandanLabRawData.Configuration
{
    /// <summary>
    /// SQL Server connection configuration
    /// </summary>
    public static class DatabaseConfiguration
    {
        /// <summary>
        /// SQL Server connection string
        /// Update this with your actual database credentials
        /// </summary>
        public const string ConnectionString =
            "Data Source=103.191.208.18; Initial Catalog=NandanLabDbDev; Integrated Security=False;User ID=developer;Password=p@$$w0rd;" +
            "Encrypt=True;TrustServerCertificate=True;";

        /// <summary>
        /// Database name
        /// </summary>
        public const string DatabaseName = "NandanLabDbDev";

        /// <summary>
        /// Server address
        /// </summary>
        public const string ServerAddress = "103.191.208.18";

        /// <summary>
        /// Gets connection string with custom timeout
        /// </summary>
        public static string GetConnectionStringWithTimeout(int timeoutSeconds = 30)
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString)
            {
                ConnectTimeout = timeoutSeconds
            };
            return builder.ConnectionString;
        }
    }
}
