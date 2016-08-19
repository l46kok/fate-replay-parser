using System.Data.Entity.Core.EntityClient;
using MySql.Data.MySqlClient;

namespace FateReplayParser.Database
{
    public partial class frsDb
    {
        private static EntityConnectionStringBuilder _entityBuilder;

        private frsDb(string connectionString)
        : base(connectionString)
        {
        }

        /// <summary>
        /// Initializers EF database connection from given parameters
        /// </summary>
        public static void InitDatabaseConnection(string server, uint port, string userId, string password,
                                                  string databaseName)
        {
            // Initialize the connection string builder for the
            // underlying provider.
            var sqlBuilder =
                new MySqlConnectionStringBuilder
                {
                    Server = server,
                    Port = port,
                    UserID = userId,
                    Password = password,
                    PersistSecurityInfo = true,
                    Database = databaseName
                };
            // Set the properties for the data source.
            // Build the SqlConnection connection string.
            string providerString = sqlBuilder.ToString();
            // Initialize the EntityConnectionStringBuilder.
            _entityBuilder = new EntityConnectionStringBuilder
            {
                ProviderConnectionString = providerString,
                Provider = "MySql.Data.MySqlClient",
                Metadata = @"res://*/Database.frsDb.csdl|res://*/Database.frsDb.ssdl|res://*/Database.frsDb.msl",
            };
        }

        /// <summary>
        /// Create a new EF6 dynamic data context using the specified provider connection string.
        /// </summary>
        /// <param name="providerConnectionString">Provider connection string to use. Usually a standart ADO.NET connection string.</param>
        /// <returns></returns>
        public static frsDb Create()
        {
            return new frsDb(_entityBuilder.ConnectionString);
        }
    }
}
