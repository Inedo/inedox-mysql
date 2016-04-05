using Inedo.BuildMaster.Extensibility.DatabaseConnections;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.MySql
{
    internal sealed class MySqlDatabaseProviderEditor : DatabaseConnectionEditorBase
    {
        private ValidatingTextBox txtConnectionString;

        public override void BindToForm(DatabaseConnection extension)
        {
            var mysql = (MySqlDatabaseProvider)extension;
            this.txtConnectionString.Text = mysql.ConnectionString;
        }
        public override DatabaseConnection CreateFromForm()
        {
            return new MySqlDatabaseProvider
            {
                ConnectionString = this.txtConnectionString.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtConnectionString = new ValidatingTextBox { Required = true };

            this.Controls.Add(
                new SlimFormField("Connection string:", this.txtConnectionString)
                {
                    HelpText = "The connection string to the MySql database. The standard format for this is:<br /><br />"
                        + "<em>Server=myServerAddress; Database=myDataBase; Uid=myUsername; Pwd=myPassword;</em>"
                }
            );
        }
    }
}
