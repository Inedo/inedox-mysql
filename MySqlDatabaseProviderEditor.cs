using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.MySql
{
    internal sealed class MySqlDatabaseProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtConnectionString;

        public override void BindToForm(ProviderBase extension)
        {
            this.EnsureChildControls();

            var mysql = (MySqlDatabaseProvider)extension;
            this.txtConnectionString.Text = mysql.ConnectionString;
        }
        public override ProviderBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new MySqlDatabaseProvider
            {
                ConnectionString = this.txtConnectionString.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtConnectionString = new ValidatingTextBox
            {
                Width = 300,
                Required = true
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Connection String",
                    "The connection string to the MySql database. The standard format for this is:<br /><br />"
                    + "<em>Server=myServerAddress; Database=myDataBase; Uid=myUsername; Pwd=myPassword;</em>",
                    false,
                    new StandardFormField(string.Empty, this.txtConnectionString)
                )
            );

            base.CreateChildControls();
        }
    }
}
