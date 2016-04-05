using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.DatabaseConnections;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.MySql.Properties;
using Inedo.Data;
using Inedo.Diagnostics;
using MySql.Data.MySqlClient;

namespace Inedo.BuildMasterExtensions.MySql
{
    [DisplayName("MySQL")]
    [Description("Supports MySQL 4.0 and later.")]
    [CustomEditor(typeof(MySqlDatabaseProviderEditor))]
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

        //public override void ExecuteQueries(string[] queries)
        //{
        //    using (var cmd = this.CreateCommand(string.Empty))
        //    {
        //        try
        //        {
        //            cmd.Connection.Open();
        //            foreach (var sqlCommand in queries)
        //            {
        //                cmd.CommandText = sqlCommand;
        //                cmd.ExecuteNonQuery();
        //            }
        //        }
        //        finally
        //        {
        //            cmd.Connection.Close();
        //        }
        //    }       
        //}
        //public override void ExecuteQuery(string query)
        //{
        //    this.ExecuteQueries(new string[] { query });
        //}
        //public override string ToString()
        //{
        //    try
        //    {
        //        var csb = new MySqlConnectionStringBuilder(this.ConnectionString);
        //        var toString = new StringBuilder();
        //        if (!string.IsNullOrEmpty(csb.Database))
        //            toString.Append("MySql catalog \"" + csb.Database + "\"");
        //        else
        //            toString.Append("MySql");

        //        if (!string.IsNullOrEmpty(csb.Server))
        //            toString.Append(" on server \"" + csb.Server + "\"");

        //        return toString.ToString();
        //    }
        //    catch
        //    {
        //        return "MySql";
        //    }
        //}

        //public void InitializeDatabase()
        //{
        //    if (this.IsDatabaseInitialized())
        //        throw new InvalidOperationException("The database is already initialized.");

        //    this.ExecuteQuery(Properties.Resources.Initialize);
        //}
        //public bool IsDatabaseInitialized()
        //{
        //    this.ValidateConnection();

        //    var tables = this.ExecuteDataTable("SHOW TABLES LIKE '__BuildMaster_DbSchemaChanges'");
        //    return tables.Rows.Count != 0;
        //}
        //public ChangeScript[] GetChangeHistory()
        //{
        //    this.ValidateInitialization();

        //    var tables = this.ExecuteDataTable("SELECT * FROM __BuildMaster_DbSchemaChanges");
        //    var scripts = new MySqlChangeScript[tables.Rows.Count];
        //    for (int i = 0; i < tables.Rows.Count; i++)
        //        scripts[i] = new MySqlChangeScript(tables.Rows[i]);
        //    return scripts;
        //}
        //public long GetSchemaVersion()
        //{
        //    this.ValidateInitialization();

        //    return (long)this.ExecuteDataTable(
        //        "SELECT COALESCE(MAX(Numeric_Release_Number),0) FROM __BuildMaster_DbSchemaChanges"
        //        ).Rows[0][0];
        //}
        //public ExecutionResult ExecuteChangeScript(long numericReleaseNumber, int scriptId, string scriptName, string scriptText)
        //{
        //    this.ValidateInitialization();

        //    var tables = this.ExecuteDataTable("SELECT * FROM __BuildMaster_DbSchemaChanges");
        //    if (tables.Select("Script_Id=" + scriptId.ToString()).Length > 0)
        //        return new ExecutionResult(ExecutionResult.Results.Skipped, scriptName + " already executed.");

        //    Exception ex = null;
        //    try { this.ExecuteQuery(scriptText); }
        //    catch (Exception _ex) { ex = _ex; }

        //    this.ExecuteQuery(string.Format(
        //        "INSERT INTO __BuildMaster_DbSchemaChanges "
        //        + " (Numeric_Release_Number, Script_Id, Script_Name, Executed_Date, Success_Indicator) "
        //        + "VALUES "
        //        + "({0}, {1}, '{2}', NOW(), '{3}')",
        //        numericReleaseNumber,
        //        scriptId,
        //        scriptName.Replace("'", "''"),
        //        ex == null ? "Y" : "N"));

        //    if (ex == null)
        //        return new ExecutionResult(ExecutionResult.Results.Success, scriptName + " executed successfully.");
        //    else
        //        return new ExecutionResult(ExecutionResult.Results.Failed, scriptName + " execution failed:" + ex.Message);
        //}

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
        //private void ValidateInitialization()
        //{
        //    if (!this.IsDatabaseInitialized())
        //        throw new InvalidOperationException("Database Not Initialized");
        //}
    }
}
