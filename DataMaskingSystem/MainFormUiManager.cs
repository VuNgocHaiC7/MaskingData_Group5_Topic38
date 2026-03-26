using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DataMaskingSystem
{
    public class MainFormUiComponents
    {
        public Panel PnlCSKH { get; set; }
        public Panel PnlDEV { get; set; }
        public TextBox TxtSearchID { get; set; }
        public Button BtnSearch { get; set; }
        public Button BtnExport { get; set; }
        public Button BtnLogout { get; set; }
        public Label LblRole { get; set; }
        public RichTextBox TxtConsole { get; set; }
        public DataGridView DgvDev { get; set; }
        public DataGridView DgvCustomerDetails { get; set; }
        public PictureBox PicPortrait { get; set; }
        public Label LblMaskedCard { get; set; }
        public Label LblBalance { get; set; }
        public ComboBox CboSearchType { get; set; }
        public Label LblProfileName { get; set; }
        public Label LblProfileDob { get; set; }
        public Label LblProfileGender { get; set; }
        public Label LblProfileCccd { get; set; }
        public Label LblProfilePhone { get; set; }
        public Label LblProfileEmail { get; set; }
    }

    public static class MainFormUiManager
    {
        public static MainFormUiComponents Build(Form host, EventHandler onSearchClick, EventHandler onExportClick, EventHandler onLogoutClick)
        {
            host.Text = "The Financial Atelier | Masking Console";
            host.Size = new Size(1360, 860);
            host.MinimumSize = new Size(1220, 760);
            host.StartPosition = FormStartPosition.CenterScreen;
            host.BackColor = Color.FromArgb(242, 244, 248);
            host.ForeColor = Color.FromArgb(21, 32, 60);

            Panel sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(248, 250, 253)
            };

            Label brand = new Label
            {
                Text = "The Financial Atelier",
                Font = new Font("Bahnschrift SemiBold", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 49, 126),
                AutoSize = true,
                Location = new Point(18, 22)
            };
            sidebar.Controls.Add(brand);

            Label navContext = new Label
            {
                Text = "Client Desk",
                Font = new Font("Bahnschrift", 10, FontStyle.Underline),
                ForeColor = Color.FromArgb(34, 84, 197),
                AutoSize = true,
                Location = new Point(18, 58)
            };
            sidebar.Controls.Add(navContext);

            sidebar.Controls.Add(CreateSidebarItem("Dashboard", 120, false));
            sidebar.Controls.Add(CreateSidebarItem("Customers", 154, true));
            sidebar.Controls.Add(CreateSidebarItem("Accounts", 188, false));
            sidebar.Controls.Add(CreateSidebarItem("Transactions", 222, false));
            sidebar.Controls.Add(CreateSidebarItem("Compliance", 256, false));

            Label support = new Label
            {
                Text = "Support",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(104, 117, 146),
                AutoSize = true,
                Location = new Point(18, host.ClientSize.Height - 48),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            sidebar.Controls.Add(support);

            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(242, 244, 248)
            };

            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.FromArgb(242, 244, 248)
            };

            Label lblRole = new Label
            {
                Text = "Role: Not signed in",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(89, 102, 131),
                Location = new Point(18, 14),
                AutoSize = true
            };
            topBar.Controls.Add(lblRole);

            Label pageTitle = new Label
            {
                Text = "Private Banking | Customer Context",
                Font = new Font("Bahnschrift SemiBold", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 40, 72),
                Location = new Point(16, 34),
                AutoSize = true
            };
            topBar.Controls.Add(pageTitle);

            TextBox topSearch = new TextBox
            {
                Text = "Search accounts...",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                Location = new Point(560, 34),
                Width = 220,
                Height = 34,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(138, 149, 174),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            topBar.Controls.Add(topSearch);

            Button btnLogout = new Button
            {
                Text = "Logout",
                Width = 126,
                Height = 38,
                BackColor = Color.FromArgb(16, 70, 163),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                Location = new Point(800, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += onLogoutClick;
            topBar.Controls.Add(btnLogout);

            Panel canvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(242, 244, 248),
                Padding = new Padding(16, 0, 16, 14)
            };

            Panel pnlCSKH = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(242, 244, 248),
                Visible = false
            };

            Panel pnlDEV = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(242, 244, 248),
                Visible = false
            };

            Panel profileCard = CreateCardPanel(new Rectangle(0, 0, 300, 552), Color.White);
            profileCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;

            PictureBox picPortrait = new PictureBox
            {
                Location = new Point(20, 22),
                Size = new Size(260, 250),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(236, 241, 248),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            profileCard.Controls.Add(picPortrait);

            Label profileName = new Label
            {
                Text = "Customer Snapshot",
                Font = new Font("Bahnschrift SemiBold", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 40, 68),
                Location = new Point(20, 286),
                AutoSize = true
            };
            profileCard.Controls.Add(profileName);

            Label profileDesc = new Label
            {
                Text = "Masked profile + verified context",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(93, 106, 133),
                Location = new Point(20, 318),
                AutoSize = true
            };
            profileCard.Controls.Add(profileDesc);

            int detailY = 352;
            int labelX = 20;
            int valueX = 100;
            int stepY = 24; 

            Font titleFont = new Font("Segoe UI", 9, FontStyle.Regular);
            Font valFont = new Font("Segoe UI", 9, FontStyle.Bold);
            Color titleColor = Color.FromArgb(108, 122, 137);
            Color valColor = Color.FromArgb(31, 43, 74);

            Label CreateTitle(string t, int y) => new Label { Text = t, Font = titleFont, ForeColor = titleColor, Location = new Point(labelX, y), AutoSize = true };
            Label CreateVal(int y) => new Label { Text = "---", Font = valFont, ForeColor = valColor, Location = new Point(valueX, y), AutoSize = true };

            profileCard.Controls.Add(CreateTitle("Họ tên:", detailY));
            Label lblName = CreateVal(detailY); profileCard.Controls.Add(lblName); detailY += stepY;

            profileCard.Controls.Add(CreateTitle("Ngày sinh:", detailY));
            Label lblDob = CreateVal(detailY); profileCard.Controls.Add(lblDob); detailY += stepY;

            profileCard.Controls.Add(CreateTitle("Giới tính:", detailY));
            Label lblGender = CreateVal(detailY); profileCard.Controls.Add(lblGender); detailY += stepY;

            profileCard.Controls.Add(CreateTitle("CCCD:", detailY));
            Label lblCccd = CreateVal(detailY); profileCard.Controls.Add(lblCccd); detailY += stepY;

            profileCard.Controls.Add(CreateTitle("SĐT:", detailY));
            Label lblPhone = CreateVal(detailY); profileCard.Controls.Add(lblPhone); detailY += stepY;

            profileCard.Controls.Add(CreateTitle("Email:", detailY));
            Label lblEmail = CreateVal(detailY); profileCard.Controls.Add(lblEmail);

            Panel contentCard = CreateCardPanel(new Rectangle(320, 0, 780, 552), Color.White);
            contentCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Panel metricCard = new Panel
            {
                Location = new Point(22, 20),
                Size = new Size(486, 102),
                BackColor = Color.FromArgb(16, 70, 163)
            };
            metricCard.Paint += DrawCardBorder;
            metricCard.Controls.Add(new Label
            {
                Text = "Số thẻ (PCI-DSS Masked)", // Đổi tiêu đề
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(174, 214, 255),
                Location = new Point(16, 14),
                AutoSize = true
            });
            Label lblMaskedCard = new Label
            {
                Text = "**** **** **** ****", // Giá trị mặc định
                Font = new Font("Bahnschrift SemiBold", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 40),
                AutoSize = true
            };
            metricCard.Controls.Add(lblMaskedCard);
            contentCard.Controls.Add(metricCard);

            Panel statusCard = CreateCardPanel(new Rectangle(524, 20, 232, 102), Color.FromArgb(246, 248, 252));
            statusCard.Controls.Add(new Label
            {
                Text = "Số dư hiện tại", // Đổi tiêu đề
                Font = new Font("Bahnschrift", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(99, 110, 136),
                Location = new Point(12, 13),
                AutoSize = true
            });
            Label lblBalance = new Label
            {
                Text = "0 VNĐ", // Giá trị mặc định
                Font = new Font("Bahnschrift SemiBold", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 43, 74),
                Location = new Point(12, 40),
                AutoSize = true
            };
            statusCard.Controls.Add(lblBalance);
            contentCard.Controls.Add(statusCard);

            Label searchLabel = new Label
            {
                Text = "Chọn tiêu chí & Nhập thông tin tìm kiếm",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(95, 108, 137),
                Location = new Point(22, 148),
                AutoSize = true
            };
            contentCard.Controls.Add(searchLabel);

            ComboBox cboSearchType = new ComboBox
            {
                Location = new Point(22, 172),
                Width = 160,
                Font = new Font("Bahnschrift", 11, FontStyle.Regular),
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Color.FromArgb(250, 251, 254),
                ForeColor = Color.FromArgb(31, 43, 74)
            };
            cboSearchType.Items.AddRange(new string[] { "Số điện thoại", "Số CMND/CCCD", "Số TK / Số thẻ" });
            cboSearchType.SelectedIndex = 0; 
            contentCard.Controls.Add(cboSearchType);

            TextBox txtSearchID = new TextBox
            {
                Location = new Point(190, 172),
                Width = 240,
                Height = 34,
                Font = new Font("Bahnschrift", 11, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 251, 254)
            };
            contentCard.Controls.Add(txtSearchID);

            Button btnSearch = new Button
            {
                Text = "Search Customer",
                Location = new Point(440, 170),
                Width = 150,
                Height = 34,
                BackColor = Color.FromArgb(20, 123, 197),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += onSearchClick;
            contentCard.Controls.Add(btnSearch);
            Label lblPropertyTitle = new Label
            {
                Text = "Database Properties",
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(58, 72, 101),
                Location = new Point(22, 220),
                AutoSize = true
            };
            contentCard.Controls.Add(lblPropertyTitle);

            DataGridView dgvCustomerDetails = BuildGrid(new Rectangle(22, 244, 734, 290));
            dgvCustomerDetails.DefaultCellStyle.Font = new Font("Bahnschrift", 9, FontStyle.Regular);
            contentCard.Controls.Add(dgvCustomerDetails);

            Panel devHeaderCard = CreateCardPanel(new Rectangle(0, 0, 1100, 122), Color.White);
            devHeaderCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            devHeaderCard.Controls.Add(new Label
            {
                Text = "DEV / TEST DATA DELIVERY",
                Font = new Font("Bahnschrift SemiBold", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 43, 74),
                Location = new Point(20, 20),
                AutoSize = true
            });
            devHeaderCard.Controls.Add(new Label
            {
                Text = "Static masking + encryption pipeline preview for test environments",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(92, 106, 136),
                Location = new Point(22, 56),
                AutoSize = true
            });

            Button btnExport = new Button
            {
                Text = "Export Secure Dataset",
                Location = new Point(878, 34),
                Width = 190,
                Height = 42,
                BackColor = Color.FromArgb(232, 101, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += onExportClick;
            devHeaderCard.Controls.Add(btnExport);

            Panel devTableCard = CreateCardPanel(new Rectangle(0, 138, 1100, 414), Color.White);
            devTableCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            devTableCard.Controls.Add(new Label
            {
                Text = "Account Portfolio & Transactions",
                Font = new Font("Bahnschrift SemiBold", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(36, 51, 85),
                Location = new Point(20, 18),
                AutoSize = true
            });

            DataGridView dgvDev = BuildGrid(new Rectangle(20, 52, 1060, 342));
            dgvDev.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            devTableCard.Controls.Add(dgvDev);

            pnlCSKH.Controls.Add(profileCard);
            pnlCSKH.Controls.Add(contentCard);

            pnlDEV.Controls.Add(devHeaderCard);
            pnlDEV.Controls.Add(devTableCard);

            Panel consoleCard = CreateCardPanel(new Rectangle(16, 566, 1100, 136), Color.FromArgb(6, 16, 35));
            consoleCard.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            consoleCard.Controls.Add(new Label
            {
                Text = "Public Channel Monitor",
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(158, 186, 234),
                Location = new Point(14, 10),
                AutoSize = true
            });

            RichTextBox txtConsole = new RichTextBox
            {
                Location = new Point(14, 34),
                Size = new Size(1072, 88),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(6, 16, 35),
                ForeColor = Color.FromArgb(115, 249, 182),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            consoleCard.Controls.Add(txtConsole);

            canvas.Controls.Add(consoleCard);
            canvas.Controls.Add(pnlCSKH);
            canvas.Controls.Add(pnlDEV);

            content.Controls.Add(canvas);
            content.Controls.Add(topBar);

            host.Controls.Add(content);
            host.Controls.Add(sidebar);

            return new MainFormUiComponents
            {
                PnlCSKH = pnlCSKH,
                PnlDEV = pnlDEV,
                TxtSearchID = txtSearchID,
                BtnSearch = btnSearch,
                BtnExport = btnExport,
                BtnLogout = btnLogout,
                LblRole = lblRole,
                TxtConsole = txtConsole,
                DgvDev = dgvDev,
                DgvCustomerDetails = dgvCustomerDetails,
                PicPortrait = picPortrait,
                LblMaskedCard = lblMaskedCard,
                LblBalance = lblBalance,
                CboSearchType = cboSearchType,
                LblProfileName = lblName,
                LblProfileDob = lblDob,
                LblProfileGender = lblGender,
                LblProfileCccd = lblCccd,
                LblProfilePhone = lblPhone,
                LblProfileEmail = lblEmail,
            };
        }

        private static Panel CreateSidebarItem(string text, int y, bool active)
        {
            Panel item = new Panel
            {
                Location = new Point(8, y),
                Size = new Size(202, 30),
                BackColor = active ? Color.FromArgb(234, 241, 255) : Color.Transparent
            };

            Label label = new Label
            {
                Text = text,
                Font = new Font("Bahnschrift", 10, active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active ? Color.FromArgb(19, 72, 180) : Color.FromArgb(104, 117, 146),
                Location = new Point(18, 6),
                AutoSize = true
            };
            item.Controls.Add(label);
            return item;
        }

        private static Panel CreateCardPanel(Rectangle bounds, Color backColor)
        {
            Panel panel = new Panel
            {
                Location = bounds.Location,
                Size = bounds.Size,
                BackColor = backColor
            };
            panel.Paint += DrawCardBorder;
            return panel;
        }

        private static DataGridView BuildGrid(Rectangle bounds)
        {
            DataGridView grid = new DataGridView
            {
                Location = bounds.Location,
                Size = bounds.Size,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(238, 242, 246),

                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,

                RowTemplate = new DataGridViewRow { Height = 48 },
                EnableHeadersVisualStyles = false,
            };

            grid.ColumnHeadersHeight = 44;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(136, 148, 160), 
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(12, 0, 0, 0)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(33, 43, 54),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(244, 246, 248),
                SelectionForeColor = Color.FromArgb(33, 43, 54),
                Padding = new Padding(12, 0, 0, 0)
            };

            return grid;
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

        private static void DrawTagBorder(object sender, PaintEventArgs e)
        {
            Control tag = sender as Control;
            if (tag == null) return;

            using (Pen borderPen = new Pen(Color.FromArgb(184, 212, 252)))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, tag.Width - 1, tag.Height - 1);
            }
        }
    }
}