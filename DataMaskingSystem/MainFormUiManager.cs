using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DataMaskingSystem
{
    public class MainFormUiComponents
    {
        public Panel PnlLogin { get; set; }
        public Panel PnlCSKH { get; set; }
        public Panel PnlDEV { get; set; }
        public TextBox TxtUser { get; set; }
        public TextBox TxtPass { get; set; }
        public TextBox TxtSearchID { get; set; }
        public Button BtnLogin { get; set; }
        public Button BtnSearch { get; set; }
        public Button BtnExport { get; set; }
        public Label LblResultCSKH { get; set; }
        public Label LblRole { get; set; }
        public RichTextBox TxtConsole { get; set; }
        public DataGridView DgvDev { get; set; }
    }

    public static class MainFormUiManager
    {
        public static MainFormUiComponents Build(Form host, EventHandler onLoginClick, EventHandler onSearchClick, EventHandler onExportClick)
        {
            host.Text = "HỆ THỐNG QUẢN LÝ DỮ LIỆU BẢO MẬT X";
            host.Size = new Size(1100, 760);
            host.MinimumSize = new Size(1040, 700);
            host.StartPosition = FormStartPosition.CenterScreen;
            host.BackColor = Color.FromArgb(12, 17, 28);
            host.ForeColor = Color.White;

            Panel pnlLogin = new Panel { Dock = DockStyle.Top, Height = 118 };
            pnlLogin.Paint += DrawLoginGradient;

            Label lblTitle = new Label
            {
                Text = "TRUNG TÂM BẢO MẬT DỮ LIỆU",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(235, 248, 255),
                Location = new Point(24, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlLogin.Controls.Add(lblTitle);

            Label lblSubTitle = new Label
            {
                Text = "Masking Data | XOR kênh truyền | Trích xuất an toàn cho DEV/TEST",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(197, 234, 246),
                Location = new Point(26, 56),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlLogin.Controls.Add(lblSubTitle);

            Label lblRole = new Label
            {
                Text = "Vai trò hiện tại: Chưa đăng nhập",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 214, 107),
                Location = new Point(26, 83),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            pnlLogin.Controls.Add(lblRole);

            pnlLogin.Controls.Add(new Label
            {
                Text = "User",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(660, 20),
                Width = 45
            });

            TextBox txtUser = new TextBox
            {
                Location = new Point(710, 18),
                Width = 130,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(238, 247, 250)
            };
            pnlLogin.Controls.Add(txtUser);

            pnlLogin.Controls.Add(new Label
            {
                Text = "Pass",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(660, 56),
                Width = 45
            });

            TextBox txtPass = new TextBox
            {
                Location = new Point(710, 54),
                Width = 130,
                PasswordChar = '*',
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(238, 247, 250)
            };
            pnlLogin.Controls.Add(txtPass);

            Button btnLogin = new Button
            {
                Text = "Đăng Nhập",
                Location = new Point(855, 24),
                Width = 210,
                Height = 58,
                BackColor = Color.FromArgb(255, 132, 66),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += onLoginClick;
            pnlLogin.Controls.Add(btnLogin);

            Panel pnlCSKH = new Panel
            {
                Location = new Point(24, 142),
                Size = new Size(516, 264),
                BackColor = Color.FromArgb(21, 36, 51),
                Visible = false
            };
            pnlCSKH.Paint += DrawCardBorder;
            pnlCSKH.Controls.Add(new Label
            {
                Text = "PHÂN HỆ A | TRA CỨU CSKH",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(107, 220, 255),
                Location = new Point(18, 16),
                AutoSize = true
            });
            pnlCSKH.Controls.Add(new Label
            {
                Text = "Hiển thị dữ liệu đã che một phần, giải mã XOR trước khi render UI",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(173, 205, 221),
                Location = new Point(18, 44),
                AutoSize = true
            });
            pnlCSKH.Controls.Add(new Label
            {
                Text = "Mã khách hàng",
                Location = new Point(18, 86),
                Width = 110,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White
            });

            TextBox txtSearchID = new TextBox
            {
                Location = new Point(132, 83),
                Width = 220,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(236, 244, 248)
            };
            pnlCSKH.Controls.Add(txtSearchID);

            Button btnSearch = new Button
            {
                Text = "Tra Cứu",
                Location = new Point(366, 80),
                Width = 132,
                Height = 34,
                BackColor = Color.FromArgb(18, 110, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += onSearchClick;
            pnlCSKH.Controls.Add(btnSearch);

            Label lblResultCSKH = new Label
            {
                Location = new Point(18, 130),
                Size = new Size(480, 116),
                Font = new Font("Consolas", 11),
                ForeColor = Color.FromArgb(179, 255, 211),
                BackColor = Color.FromArgb(11, 23, 33),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Kết quả sau khi tra cứu sẽ hiển thị tại đây"
            };
            pnlCSKH.Controls.Add(lblResultCSKH);

            Panel pnlDEV = new Panel
            {
                Location = new Point(556, 142),
                Size = new Size(516, 264),
                BackColor = Color.FromArgb(31, 31, 45),
                Visible = false
            };
            pnlDEV.Paint += DrawCardBorder;
            pnlDEV.Controls.Add(new Label
            {
                Text = "PHÂN HỆ B | DEV / TESTER",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 178, 121),
                Location = new Point(18, 16),
                AutoSize = true
            });
            pnlDEV.Controls.Add(new Label
            {
                Text = "Masking tĩnh + mô phỏng mã hóa trước khi đưa vào môi trường test",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(234, 196, 176),
                Location = new Point(18, 44),
                AutoSize = true
            });

            Button btnExport = new Button
            {
                Text = "Trích Xuất CSDL An Toàn (RSA + AES + Mask)",
                Location = new Point(18, 78),
                Width = 480,
                Height = 36,
                BackColor = Color.FromArgb(255, 132, 66),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += onExportClick;
            pnlDEV.Controls.Add(btnExport);

            DataGridView dgvDev = new DataGridView
            {
                Location = new Point(18, 126),
                Size = new Size(480, 120),
                BackgroundColor = Color.FromArgb(247, 247, 252),
                BorderStyle = BorderStyle.None,
                ForeColor = Color.Black,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(37, 49, 77),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false
            };
            pnlDEV.Controls.Add(dgvDev);

            Label lblLogTitle = new Label
            {
                Text = "BẢNG GIÁM SÁT ĐƯỜNG TRUYỀN CÔNG KHAI",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(141, 174, 201),
                Location = new Point(24, 420),
                AutoSize = true
            };
            host.Controls.Add(lblLogTitle);

            RichTextBox txtConsole = new RichTextBox
            {
                Location = new Point(24, 448),
                Size = new Size(1048, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(5, 10, 16),
                ForeColor = Color.FromArgb(95, 255, 139),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            host.Controls.Add(pnlLogin);
            host.Controls.Add(pnlCSKH);
            host.Controls.Add(pnlDEV);
            host.Controls.Add(txtConsole);

            return new MainFormUiComponents
            {
                PnlLogin = pnlLogin,
                PnlCSKH = pnlCSKH,
                PnlDEV = pnlDEV,
                TxtUser = txtUser,
                TxtPass = txtPass,
                TxtSearchID = txtSearchID,
                BtnLogin = btnLogin,
                BtnSearch = btnSearch,
                BtnExport = btnExport,
                LblResultCSKH = lblResultCSKH,
                LblRole = lblRole,
                TxtConsole = txtConsole,
                DgvDev = dgvDev
            };
        }

        private static void DrawLoginGradient(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel == null) return;

            Rectangle rect = new Rectangle(0, 0, panel.Width, panel.Height);
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.FromArgb(20, 48, 86), Color.FromArgb(18, 101, 133), 10f))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
            using (Pen borderPen = new Pen(Color.FromArgb(72, 214, 255)))
            {
                e.Graphics.DrawLine(borderPen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            }
        }

        private static void DrawCardBorder(object sender, PaintEventArgs e)
        {
            Control card = sender as Control;
            if (card == null) return;

            using (Pen borderPen = new Pen(Color.FromArgb(66, 89, 118)))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
            }
        }
    }
}
