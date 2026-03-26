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
            Text = "Chăm sóc Khách hàng - CT07_BANK";
            Size = new Size(1100, 800);
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(235, 238, 243);
            ForeColor = Color.FromArgb(31, 43, 74);

            this.Resize += LoginForm_Resize;

            Panel card = new Panel
            {
                Name = "LoginCard",
                Size = new Size(420, 540), // Thu gọn chiều cao card lại một chút cho cân đối
                BackColor = Color.White,
                Padding = new Padding(40)
            };
            card.Paint += DrawCardBorder;
            
            Label lblLogo = new Label
            {
                Text = "🏛",
                Font = new Font("Segoe UI", 24),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 59, 128),
                Size = new Size(54, 54),
                Location = new Point(183, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblLogo.Region = new Region(new Rectangle(0, 0, 54, 54)); 
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddRoundedRectangle(new Rectangle(0, 0, 54, 54), 12);
            lblLogo.Region = new Region(path);

            Label lblTitle = new Label
            {
                Text = "Chăm sóc Khách hàng - CT07_BANK",
                Font = new Font("Segoe UI", 16, FontStyle.Bold), // Chữ hơi dài nên thu nhỏ font size xuống 16
                ForeColor = Color.FromArgb(16, 44, 87),
                AutoSize = true,
            };
            CenterControl(lblTitle, 110, card.Width);

            Label lblSubTitle = new Label
            {
                Text = "Bảo vệ dữ liệu bằng mặt nạ dữ liệu",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 110, 125),
                AutoSize = true,
            };
            CenterControl(lblSubTitle, 146, card.Width);

            Label lblUser = new Label
            {
                Text = "UserName (CSKH/DEV)",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 50, 70),
                AutoSize = true,
                Location = new Point(40, 190)
            };

            Panel pnlUser = CreateInputBox(214, out txtUser);
            txtUser.PlaceholderText = "Nhập ID (cskh hoặc dev)";

            Label lblPass = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 50, 70),
                AutoSize = true,
                Location = new Point(40, 276)
            };

            // --- ĐÂY LÀ ĐOẠN BẠN LỠ TAY XÓA MẤT TRƯỚC ĐÓ ---
            Panel pnlPass = CreateInputBox(300, out txtPass);
            txtPass.PasswordChar = '•'; // Ẩn mật khẩu bằng dấu chấm
            txtPass.KeyDown += TxtPass_KeyDown;
            // --------------------------------------------------

            Button btnLogin = new Button
            {
                Text = "Đăng nhập",
                Location = new Point(40, 380),
                Size = new Size(340, 46),
                BackColor = Color.FromArgb(0, 59, 128),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            Button btnExit = new Button
            {
                Text = "Thoát",
                Location = new Point(40, 436),
                Size = new Size(340, 46),
                BackColor = Color.FromArgb(242, 244, 248),
                ForeColor = Color.FromArgb(100, 110, 125),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => 
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            lblMessage = new Label
            {
                Text = "Vui lòng nhập ID và Password.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(180, 50, 50),
                AutoSize = true,
                Location = new Point(40, 436),
                Visible = false
            };

            card.Controls.Add(lblLogo);
            card.Controls.Add(lblTitle);
            card.Controls.Add(lblSubTitle);
            card.Controls.Add(lblUser);
            card.Controls.Add(pnlUser);
            card.Controls.Add(lblPass);
            card.Controls.Add(pnlPass);
            card.Controls.Add(btnLogin);
            card.Controls.Add(btnExit);
            card.Controls.Add(lblMessage);

            Controls.Add(card);

            AcceptButton = btnLogin;
            CenterCard();
        }

        private void LoginForm_Resize(object? sender, EventArgs e)
        {
            CenterCard();
        }

        private void CenterCard()
        {
            Control[] cards = this.Controls.Find("LoginCard", false);
            if (cards.Length > 0)
            {
                Panel card = (Panel)cards[0];
                card.Location = new Point((this.ClientSize.Width - card.Width) / 2, (this.ClientSize.Height - card.Height) / 2);
            }
        }

        private void CenterControl(Control ctrl, int y, int parentWidth)
        {
            ctrl.Location = new Point((parentWidth - ctrl.PreferredSize.Width) / 2, y);
        }

        private Panel CreateInputBox(int y, out TextBox tb)
        {
            Panel pnl = new Panel
            {
                Location = new Point(40, y),
                Size = new Size(340, 42),
                BackColor = Color.FromArgb(234, 237, 241),
            };

            tb = new TextBox
            {
                Location = new Point(14, 10),
                Width = 312,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(234, 237, 241),
                ForeColor = Color.FromArgb(30, 40, 60)
            };

            pnl.Controls.Add(tb);
            return pnl;
        }

        private void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel == null) return;
            using (Pen borderPen = new Pen(Color.FromArgb(220, 225, 230)))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
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
            lblMessage.Visible = false;

            string user = (txtUser.Text ?? string.Empty).Trim().ToLowerInvariant();
            char[] passChars = CustomStringHelper.ToCharArray(txtPass.Text);

            string hashedInput = new string(CustomHash.ComputeHash(passChars));
            string expectedHash = new string(CustomHash.ComputeHash(CustomStringHelper.ToCharArray("123")));

            if (hashedInput != expectedHash)
            {
                lblMessage.Text = "Sai mật khẩu. Vui lòng thử lại.";
                lblMessage.Visible = true;
                txtPass.SelectAll();
                txtPass.Focus();
                return;
            }

            if (user != "cskh" && user != "dev")
            {
                lblMessage.Text = "Client ID không hợp lệ. Chỉ chấp nhận 'cskh' hoặc 'dev'.";
                lblMessage.Visible = true;
                txtUser.SelectAll();
                txtUser.Focus();
                return;
            }

            SelectedRole = user;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal static class GraphicsPathsHelper
    {
        public static void AddRoundedRectangle(this System.Drawing.Drawing2D.GraphicsPath path, Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
        }
    }
}