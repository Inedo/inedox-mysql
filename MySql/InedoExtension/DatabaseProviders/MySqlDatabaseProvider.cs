using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Data;
using Inedo.Diagnostics;
using Inedo.Extensibility.DatabaseConnections;
using Inedo.Extensions.MySQL.Properties;
using Inedo.Serialization;
using MySql.Data.MySqlClient;

namespace Inedo.Extensions.MySql
{
    [DisplayName("MySQL")]
    [Description("Supports MySQL 4.0 and later.")]
    [PersistFrom("Inedo.BuildMasterExtensions.MySql.MySqlDatabaseProvider,MySql")]
    public sealed class MySqlDatabaseProvider : DatabaseConnection, IChangeScriptExecuter
    {
        private static readonly Task Complete = Task.FromResult<object>(null);

        public int MaxChangeScriptVersion => 1;

        public override Task ExecuteQueryAsync(string query, CancellationToken cancellationToken)
        {
            using (var cmd = this.CreateCommand(string.Empty))
            {
                try
                {
                    cmd.Connection.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    cmd.Connection.Close();
                }
            }

            return Complete;
        }

        public Task ExecuteChangeScriptAsync(ChangeScriptId scriptId, string scriptName, string scriptText, CancellationToken cancellationToken)
        {
            var state = this.GetStateAsync(cancellationToken).Result;
            if (!state.IsInitialized)
                throw new InvalidOperationException("The database has not been initialized.");

            if (state.Scripts.Any(s => s.Id.ScriptId == scriptId.ScriptId))
            {
                this.LogInformation(scriptName + " already executed. Skipping...");
                return Complete;
            }

            Exception ex = null;
            try
            {
                this.ExecuteQueryAsync(scriptText, cancellationToken);
                this.LogInformation(scriptName + " executed successfully.");
            }
            catch (Exception _ex)
            {
                ex = _ex;
                this.LogError(scriptName + " failed: " + ex.Message);
            }

            this.ExecuteQueryAsync(
                string.Format(
                    "INSERT INTO __BuildMaster_DbSchemaChanges "
                    + " (Numeric_Release_Number, Script_Id, Script_Name, Executed_Date, Success_Indicator) "
                    + "VALUES "
                    + "({0}, {1}, '{2}', NOW(), '{3}')",
                    scriptId.LegacyReleaseSequence,
                    scriptId.ScriptId,
                    scriptName.Replace("'", "''"),
                    ex == null ? "Y" : "N"
                ),
                cancellationToken
            );

            return Complete;
        }
        public Task<ChangeScriptState> GetStateAsync(CancellationToken cancellationToken)
        {
            var tables = this.ExecuteDataTable("SHOW TABLES LIKE '__BuildMaster_DbSchemaChanges'");
            if (tables.Rows.Count == 0)
                return Task.FromResult(new ChangeScriptState(false));

            var scripts = new List<ChangeScriptExecutionRecord>();
            var table = this.ExecuteDataTable("SELECT * FROM __BuildMaster_DbSchemaChanges");
            foreach (DataRow row in table.Rows)
            {
                scripts.Add(
                    new ChangeScriptExecutionRecord(
                        new ChangeScriptId((int)row["Script_Id"], (long)row["Numeric_Release_Number"]),
                        (string)row["Script_Name"],
                        ((DateTime)row["Executed_Date"]).ToUniversalTime(),
                        (YNIndicator)(string)row["Success_Indicator"]
                    )
                );
            }

            return Task.FromResult(new ChangeScriptState(1, scripts));
        }
        public Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            var state = this.GetStateAsync(cancellationToken).Result;
            if (state.IsInitialized)
                return Complete;

            return this.ExecuteQueryAsync(Resources.Initialize, cancellationToken);
        }
        public Task UpgradeSchemaAsync(IReadOnlyDictionary<int, Guid> canoncialGuids, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private MySqlConnection CreateConnection() => new MySqlConnection(this.ConnectionString);
        private MySqlCommand CreateCommand(string cmdText)
        {
            return new MySqlCommand
            {
                CommandTimeout = 0,
                CommandText = cmdText,
                Connection = this.CreateConnection()
            };
        }
        private DataTable ExecuteDataTable(string sqlCommand)
        {
            var dt = new DataTable();
            using (var cmd = this.CreateCommand(string.Empty))
            {
                try
                {
                    cmd.Connection.Open();
                    cmd.CommandText = sqlCommand;
                    dt.Load(cmd.ExecuteReader());
                    return dt;
                }
                finally
                {
                    cmd.Connection.Close();
                }
            }
        }
    }
}
