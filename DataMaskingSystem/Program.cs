using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

// 2 DÒNG NÀY SẼ ĐÁNH BAY DỨT ĐIỂM LỖI CS0104
using Application = System.Windows.Forms.Application;
using Font = System.Drawing.Font;

namespace DataMaskingSystem
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Khởi chạy Form giao diện chính
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // ⚠️ THAY MẬT KHẨU CỦA BẠN VÀO ĐÂY (Mình đang để tạm 123456)
        string connectionString = "Server=127.0.0.1;Port=3306;Database=BankSystemMasking;Uid=root;Pwd=123456;";

        // Các thành phần giao diện
        Panel pnlLogin, pnlCSKH, pnlDEV;
        TextBox txtUser, txtPass, txtSearchID;
        Button btnLogin, btnSearch, btnExport;
        Label lblResultCSKH, lblConsole;
        DataGridView dgvDev;

        public MainForm()
        {
            SetupUI();
        }

        // ==========================================
        // 1. TỰ ĐỘNG VẼ GIAO DIỆN KHÔNG CẦN KÉO THẢ
        // ==========================================
        private void SetupUI()
        {
            // --- CẤU HÌNH FORM CHÍNH ---
            this.Text = "HỆ THỐNG QUẢN LÝ DỮ LIỆU BẢO MẬT X";
            this.Size = new Size(820, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 30, 54); // Nền xanh đen Dark Mode
            this.ForeColor = Color.White; // Chữ trắng toàn cục

            // --- PANEL ĐĂNG NHẬP ---
            pnlLogin = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(46, 51, 73) };

            Label lblTitle = new Label { Text = "🔒 BẢO MẬT NGÂN HÀNG", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(0, 126, 249), Location = new Point(20, 25), AutoSize = true };
            pnlLogin.Controls.Add(lblTitle);

            pnlLogin.Controls.Add(new Label { Text = "User:", Font = new Font("Segoe UI", 10), Location = new Point(320, 32), Width = 40 });
            txtUser = new TextBox { Location = new Point(365, 30), Width = 120, Font = new Font("Segoe UI", 10) };
            pnlLogin.Controls.Add(txtUser);

            pnlLogin.Controls.Add(new Label { Text = "Pass:", Font = new Font("Segoe UI", 10), Location = new Point(500, 32), Width = 40 });
            txtPass = new TextBox { Location = new Point(545, 30), Width = 120, PasswordChar = '●', Font = new Font("Segoe UI", 10) };
            pnlLogin.Controls.Add(txtPass);

            btnLogin = new Button { Text = "Đăng Nhập", Location = new Point(680, 28), Width = 100, Height = 30, BackColor = Color.FromArgb(0, 126, 249), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;
            pnlLogin.Controls.Add(btnLogin);

            // --- PANEL CSKH (Phân hệ A) ---
            pnlCSKH = new Panel { Location = new Point(20, 100), Size = new Size(360, 250), BackColor = Color.FromArgb(37, 42, 64), Visible = false };
            pnlCSKH.Controls.Add(new Label { Text = "PHÂN HỆ A: CSKH", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(0, 126, 249), Location = new Point(15, 15), AutoSize = true });

            pnlCSKH.Controls.Add(new Label { Text = "Nhập mã KH:", Location = new Point(15, 55), Width = 90, Font = new Font("Segoe UI", 10) });
            txtSearchID = new TextBox { Location = new Point(105, 52), Width = 130, Font = new Font("Segoe UI", 10) };
            pnlCSKH.Controls.Add(txtSearchID);

            btnSearch = new Button { Text = "Tra cứu", Location = new Point(245, 51), Width = 90, Height = 28, BackColor = Color.FromArgb(24, 30, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSearch.Click += BtnSearch_Click;
            pnlCSKH.Controls.Add(btnSearch);

            lblResultCSKH = new Label { Location = new Point(15, 95), Size = new Size(330, 140), Font = new Font("Consolas", 11), ForeColor = Color.LightGreen };
            pnlCSKH.Controls.Add(lblResultCSKH);

            // --- PANEL DEV (Phân hệ B) ---
            pnlDEV = new Panel { Location = new Point(410, 100), Size = new Size(370, 250), BackColor = Color.FromArgb(37, 42, 64), Visible = false };
            pnlDEV.Controls.Add(new Label { Text = "PHÂN HỆ B: DEV / TESTER", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(0, 126, 249), Location = new Point(15, 15), AutoSize = true });

            btnExport = new Button { Text = "Trích xuất CSDL (AES + Masking)", Location = new Point(15, 50), Width = 340, Height = 30, BackColor = Color.FromArgb(24, 30, 54), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnExport.Click += BtnExport_Click;
            pnlDEV.Controls.Add(btnExport);

            dgvDev = new DataGridView { Location = new Point(15, 95), Size = new Size(340, 140), BackColor = Color.White, ForeColor = Color.Black, AllowUserToAddRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            pnlDEV.Controls.Add(dgvDev);

            // --- MÀN HÌNH CONSOLE (Log) ---
            Label lblLogTitle = new Label { Text = "MÔ PHỎNG ĐƯỜNG TRUYỀN (NETWORK TRAFFIC):", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(20, 365), AutoSize = true };
            this.Controls.Add(lblLogTitle);

            lblConsole = new Label { Location = new Point(20, 390), Size = new Size(760, 170), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 10), BackColor = Color.Black, ForeColor = Color.Lime };

            // --- THÊM VÀO FORM ---
            this.Controls.Add(pnlLogin);
            this.Controls.Add(pnlCSKH);
            this.Controls.Add(pnlDEV);
            this.Controls.Add(lblConsole);
            LogToConsole("Hệ thống UI đã sẵn sàng. Đăng nhập để tiếp tục (cskh / dev | Pass: 123).");
        }

        private void LogToConsole(string message)
        {
            lblConsole.Text = "> " + message + "\n" + lblConsole.Text;
        }

        // ==========================================
        // 2. LUỒNG NGHIỆP VỤ & SỰ KIỆN NÚT BẤM
        // ==========================================
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text;
            char[] passChars = CustomStringHelper.ToCharArray(txtPass.Text);

            // Hàm băm mật khẩu tự chế (Hashing 1 chiều)
            string hashedInput = new string(CustomHash.ComputeHash(passChars));
            string expectedHash = new string(CustomHash.ComputeHash(CustomStringHelper.ToCharArray("123")));

            if (hashedInput == expectedHash)
            {
                LogToConsole($"Đăng nhập thành công! Cấp quyền: {user.ToUpper()}");
                pnlCSKH.Visible = (user == "cskh");
                pnlDEV.Visible = (user == "dev");
            }
            else
            {
                MessageBox.Show("Sai mật khẩu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = $"SELECT HoTen, SoDienThoai, SoTaiKhoan FROM Customers WHERE CustomerID = '{txtSearchID.Text}'";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            char[] name = CustomStringHelper.ToCharArray(reader["HoTen"].ToString());
                            char[] phone = CustomStringHelper.ToCharArray(reader["SoDienThoai"].ToString());
                            char[] acc = CustomStringHelper.ToCharArray(reader["SoTaiKhoan"].ToString());

                            // 1. Dynamic Data Masking
                            char[] maskedName = CustomDataMasker.MaskFullName(name);
                            char[] maskedPhone = CustomDataMasker.MaskPhoneNumber(phone);
                            char[] maskedAcc = CustomDataMasker.MaskBankAccount(acc);

                            // 2. Mã hóa đường truyền XOR
                            char[] key = CustomStringHelper.ToCharArray("KEY123");
                            char[] encPhone = CustomCryptography.XorEncryptDecrypt(maskedPhone, key);
                            char[] encAcc = CustomCryptography.XorEncryptDecrypt(maskedAcc, key);

                            LogToConsole($"[KÊNH TRUYỀN] Bắt được gói tin mã hóa XOR:\nSĐT: {new string(encPhone)} | STK: {new string(encAcc)}");

                            // 3. Giải mã hiển thị lên UI
                            char[] decPhone = CustomCryptography.XorEncryptDecrypt(encPhone, key);
                            char[] decAcc = CustomCryptography.XorEncryptDecrypt(encAcc, key);

                            lblResultCSKH.Text = $"KẾT QUẢ TRA CỨU:\n\n" +
                                                 $"Họ Tên: {new string(maskedName)}\n" +
                                                 $"SĐT   : {new string(decPhone)}\n" +
                                                 $"Số TK : {new string(decAcc)}";
                        }
                        else
                        {
                            lblResultCSKH.Text = "Không tìm thấy khách hàng!";
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi CSDL: " + ex.Message); }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ID");
            dt.Columns.Add("CCCD (Masked)");
            dt.Columns.Add("CipherText (AES)");

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT CustomerID, SoCCCD FROM Customers LIMIT 5", conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        char[] aesSessionKey = CustomStringHelper.ToCharArray("KHOAAES");

                        LogToConsole("[RSA] Đang trao đổi khóa phiên AES an toàn...");
                        int[] encryptedAesKey = CustomRSA.EncryptAESKey(aesSessionKey);
                        char[] decryptedAesKey = CustomRSA.DecryptAESKey(encryptedAesKey);

                        while (reader.Read())
                        {
                            char[] cccd = CustomStringHelper.ToCharArray(reader["SoCCCD"].ToString());

                            // 1. Static Data Masking (Shift Masking)
                            char[] maskedCccd = CustomDataMasker.ShiftMask(cccd, 3);
                            // 2. Mã hóa AES
                            char[] cipherCccd = CustomAES.EncryptData(maskedCccd, decryptedAesKey);

                            dt.Rows.Add(reader["CustomerID"].ToString(), new string(maskedCccd), new string(cipherCccd));
                        }
                        dgvDev.DataSource = dt;
                        LogToConsole("[DEV] Áp dụng Mô hình lai ghép RSA-AES và trích xuất DB thành công.");
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lỗi CSDL: " + ex.Message); }
            }
        }
    }

    // =================================================================================
    // CÁC MODULE THUẬT TOÁN BẢO MẬT & XỬ LÝ CHUỖI TỰ CHẾ 
    // =================================================================================
    public static class CustomStringHelper
    {
        public static char[] ToCharArray(string input)
        {
            if (input == null) return new char[0];
            char[] result = new char[input.Length];
            for (int i = 0; i < input.Length; i++) result[i] = input[i];
            return result;
        }
    }

    public static class CustomHash
    {
        public static char[] ComputeHash(char[] input)
        {
            char[] hash = new char[16];
            for (int i = 0; i < 16; i++) hash[i] = '0';
            for (int i = 0; i < input.Length; i++)
            {
                int val = input[i];
                val = ((val << 3) | (val >> 5)) ^ (i * 17);
                int index = i % 16;
                hash[index] = (char)((hash[index] + val) % 26 + 'A');
            }
            return hash;
        }
    }

    public static class CustomCryptography
    {
        public static char[] XorEncryptDecrypt(char[] input, char[] key)
        {
            char[] result = new char[input.Length];
            for (int i = 0; i < input.Length; i++) result[i] = (char)(input[i] ^ key[i % key.Length]);
            return result;
        }
    }

    public static class CustomRSA
    {
        public static int n = 143;
        public static int e = 7;
        public static int d = 103;

        public static int[] EncryptAESKey(char[] aesKey)
        {
            int[] encryptedKey = new int[aesKey.Length];
            for (int i = 0; i < aesKey.Length; i++)
            {
                int m = aesKey[i] % n;
                int c = 1;
                for (int j = 0; j < e; j++) c = (c * m) % n;
                encryptedKey[i] = c;
            }
            return encryptedKey;
        }

        public static char[] DecryptAESKey(int[] encryptedKey)
        {
            char[] decryptedKey = new char[encryptedKey.Length];
            for (int i = 0; i < encryptedKey.Length; i++)
            {
                int c = encryptedKey[i];
                int m = 1;
                for (int j = 0; j < d; j++) m = (m * c) % n;
                decryptedKey[i] = (char)m;
            }
            return decryptedKey;
        }
    }

    public static class CustomAES
    {
        public static char[] EncryptData(char[] input, char[] key)
        {
            char[] result = new char[input.Length];
            for (int i = 0; i < input.Length; i++) result[i] = (char)(input[i] + key[i % key.Length]);
            return result;
        }

        public static char[] DecryptData(char[] input, char[] key)
        {
            char[] result = new char[input.Length];
            for (int i = 0; i < input.Length; i++) result[i] = (char)(input[i] - key[i % key.Length]);
            return result;
        }
    }

    public static class CustomDataMasker
    {
        public static char[] MaskPhoneNumber(char[] input)
        {
            int len = input.Length;
            char[] result = new char[len];
            for (int i = 0; i < len; i++)
            {
                if (i < 3 || i >= len - 3) result[i] = input[i];
                else result[i] = '*';
            }
            return result;
        }

        public static char[] MaskFullName(char[] input)
        {
            int len = input.Length;
            char[] result = new char[len];
            int firstSpace = 0;
            while (firstSpace < len && input[firstSpace] != ' ') firstSpace++;
            int lastSpace = len - 1;
            while (lastSpace >= 0 && input[lastSpace] != ' ') lastSpace--;
            for (int i = 0; i < len; i++)
            {
                if (input[i] == ' ') result[i] = ' ';
                else if (i < firstSpace || i > lastSpace) result[i] = input[i];
                else result[i] = '*';
            }
            return result;
        }

        public static char[] MaskBankAccount(char[] input)
        {
            int len = input.Length;
            char[] result = new char[len];
            for (int i = 0; i < len; i++)
            {
                if (i >= len - 4) result[i] = input[i];
                else result[i] = '*';
            }
            return result;
        }

        public static char[] ShiftMask(char[] input, int shiftVal)
        {
            int len = input.Length;
            char[] result = new char[len];
            for (int i = 0; i < len; i++)
            {
                int val = input[i] - '0';
                val = (val + shiftVal) % 10;
                result[i] = (char)(val + '0');
            }
            return result;
        }
    }
}