using System;
using System.Data;
using Inedo.BuildMaster.Extensibility.Providers.Database;

namespace Inedo.BuildMasterExtensions.MySql
{
    /// <summary>
    /// Represents a MySql change script.
    /// </summary>
    [Serializable]
    public sealed class MySqlChangeScript : ChangeScript
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlChangeScript"/> class.
        /// </summary>
        /// <param name="dr">The data row.</param>
        public MySqlChangeScript(DataRow dr)
            : base((long)dr["Numeric_Release_Number"], (int)dr["Script_Id"], (string)dr["Script_Name"], ((DateTime)dr["Executed_Date"]).ToUniversalTime(), dr["Success_Indicator"].ToString() == "Y")
        {
        }
    }
}
