using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataMaskingSystem
{
    public class LoginForm : Form
    {
        readonly TextBox txtUser;
        readonly TextBox txtPass;
        readonly Label lblMessage;

        public string SelectedRole { get; private set; } = string.Empty;

        public LoginForm()
        {
            Text = "Đăng nhập hệ thống";
            Size = new Size(460, 320);
            MinimumSize = new Size(460, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(14, 24, 38);
            ForeColor = Color.White;

            Label lblTitle = new Label
            {
                Text = "ĐĂNG NHẬP HỆ THỐNG MASKING",
                Font = new Font("Segoe UI Semibold", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 242, 255),
                AutoSize = true,
                Location = new Point(52, 24)
            };

            Label lblUser = new Label
            {
                Text = "User",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(58, 86)
            };

            txtUser = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(126, 82),
                Width = 260,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(239, 248, 252)
            };

            Label lblPass = new Label
            {
                Text = "Pass",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(58, 130)
            };

            txtPass = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(126, 126),
                Width = 260,
                PasswordChar = '*',
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(239, 248, 252)
            };
            txtPass.KeyDown += TxtPass_KeyDown;

            lblMessage = new Label
            {
                Text = "User hợp lệ: cskh hoặc dev | Pass: 123",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(255, 214, 107),
                AutoSize = true,
                Location = new Point(58, 172)
            };

            Button btnLogin = new Button
            {
                Text = "Đăng nhập",
                Width = 328,
                Height = 42,
                Location = new Point(58, 205),
                BackColor = Color.FromArgb(255, 132, 66),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            Controls.Add(lblTitle);
            Controls.Add(lblUser);
            Controls.Add(txtUser);
            Controls.Add(lblPass);
            Controls.Add(txtPass);
            Controls.Add(lblMessage);
            Controls.Add(btnLogin);

            AcceptButton = btnLogin;
        }

        private void TxtPass_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AttemptLogin();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            AttemptLogin();
        }

        private void AttemptLogin()
        {
            string user = (txtUser.Text ?? string.Empty).Trim().ToLowerInvariant();
            char[] passChars = CustomStringHelper.ToCharArray(txtPass.Text);

            string hashedInput = new string(CustomHash.ComputeHash(passChars));
            string expectedHash = new string(CustomHash.ComputeHash(CustomStringHelper.ToCharArray("123")));

            if (hashedInput != expectedHash)
            {
                lblMessage.Text = "Sai mật khẩu. Vui lòng thử lại.";
                lblMessage.ForeColor = Color.FromArgb(255, 138, 138);
                txtPass.SelectAll();
                txtPass.Focus();
                return;
            }

            if (user != "cskh" && user != "dev")
            {
                lblMessage.Text = "User không hợp lệ. Chỉ chấp nhận cskh hoặc dev.";
                lblMessage.ForeColor = Color.FromArgb(255, 138, 138);
                txtUser.SelectAll();
                txtUser.Focus();
                return;
            }

            SelectedRole = user;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
