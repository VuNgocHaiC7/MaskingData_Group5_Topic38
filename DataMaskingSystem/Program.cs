using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataMaskingSystem
{
    /// <summary>
    /// A data class to hold references to all the UI components managed by MainFormUiManager.
    /// This makes it easier to pass UI controls between methods.
    /// </summary>
    public class MainFormUiComponents
    {
        public Panel PnlCSKH { get; set; }
        public Panel PnlDEV { get; set; }
        public TextBox TxtSearchID { get; set; }
        public ComboBox CboSearchType { get; set; }
        public Button BtnSearch { get; set; }
        public Button BtnExport { get; set; }
        public Button BtnSaveFile { get; set; }
        public Button BtnLogout { get; set; }
        public Label LblRole { get; set; }
        public RichTextBox TxtConsole { get; set; }
        public DataGridView DgvDev { get; set; }
        public DataGridView DgvCustomerDetails { get; set; }
        public PictureBox PicPortrait { get; set; }
        public Label LblMaskedCard { get; set; }
        public Label LblBalance { get; set; }
        public Label LblProfileName { get; set; }
        public Label LblProfileDob { get; set; }
        public Label LblProfileGender { get; set; }
        public Label LblProfileCccd { get; set; }
        public Label LblProfilePhone { get; set; }
        public Label LblProfileEmail { get; set; }
    }

    public class MainForm : Form
    {
        private readonly MainFormUiComponents _ui;
        private readonly string _selectedRole;
        public bool ShouldLogout { get; private set; }

        public MainForm(string selectedRole)
        {
            _selectedRole = selectedRole;
            _ui = MainFormUiManager.Build(this, OnSearchClick, OnExportClick, OnLogoutClick);
            ApplyRolePermissions();
        }

        private void ApplyRolePermissions()
        {
            _ui.PnlCSKH.Visible = (_selectedRole == "cskh");
            _ui.PnlDEV.Visible = (_selectedRole == "dev");

            if (_selectedRole == "cskh")
            {
                _ui.LblRole.Text = "Vai trò hiện tại: CSKH | Dynamic Data Masking";
                _ui.TxtConsole.Text = "Đăng nhập thành công. Quyền CSKH đã được cấp.";
            }
            else if (_selectedRole == "dev")
            {
                _ui.LblRole.Text = "Vai trò hiện tại: DEV/TESTER | Static Data Masking";
                _ui.TxtConsole.Text = "Đăng nhập thành công. Quyền DEV/TESTER đã được cấp.";
            }
            else
            {
                _ui.LblRole.Text = "Vai trò hiện tại: Không hợp lệ";
                _ui.TxtConsole.Text = "Cảnh báo: vai trò không hợp lệ.";
            }
        }

        private async Task OnSearchClick()
        {
            await MainFormUiManager.SearchAndDisplay(_ui, _ui.CboSearchType.SelectedIndex, _ui.TxtSearchID.Text);
        }

        private async Task OnExportClick()
        {
            await MainFormUiManager.ExportAndDisplay(_ui);
        }

        private void OnLogoutClick(object? sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc muốn đăng xuất không?",
                "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            ShouldLogout = true;
            Close();
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            while (true)
            {
                AuthSession.Clear();
                string selectedRole = "";
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        selectedRole = loginForm.SelectedRole;
                    }
                    else
                    {
                        // If login is cancelled or closed, exit the application
                        return;
                    }
                }

                // If login was successful, show the main form
                var mainForm = new MainForm(selectedRole);
                Application.Run(mainForm);

                // If the main form was closed for logout, the loop will continue
                // and the login form will be shown again.
                // If it was closed for any other reason (e.g., clicking the 'X' button
                // where ShouldLogout is false), we should exit.
                if (!mainForm.ShouldLogout)
                {
                    break;
                }
            }
        }
    }
}
