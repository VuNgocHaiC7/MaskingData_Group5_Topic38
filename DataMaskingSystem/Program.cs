using System;
using System.Data;
using System.Drawing;
using System.Text;
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

            while (true)
            {
                using (LoginForm loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    using (MainForm mainForm = new MainForm(loginForm.SelectedRole))
                    {
                        Application.Run(mainForm);

                        if (!mainForm.ShouldLogout)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }

    public class MainForm : Form
    {
        // ⚠️ THAY MẬT KHẨU CỦA BẠN VÀO ĐÂY (Mình đang để tạm 123456)
        string connectionString = "Server=127.0.0.1;Port=3306;Database=BankSystemMasking;Uid=root;Pwd=123456;";

        // Các thành phần giao diện
        Panel pnlCSKH, pnlDEV;
        TextBox txtSearchID;
        Button btnSearch, btnExport, btnLogout;
        RichTextBox txtResultCSKH;
        Label lblRole;
        RichTextBox txtConsole;
        DataGridView dgvDev;
        readonly string selectedRole;

        public bool ShouldLogout { get; private set; }

        public MainForm(string selectedRole)
        {
            this.selectedRole = selectedRole;
            SetupUI();
            ApplyRolePermissions();
        }

        // ==========================================
        // 1. TỰ ĐỘNG VẼ GIAO DIỆN KHÔNG CẦN KÉO THẢ
        // ==========================================
        private void SetupUI()
        {
            MainFormUiComponents ui = MainFormUiManager.Build(this, BtnSearch_Click, BtnExport_Click, BtnLogout_Click);
            pnlCSKH = ui.PnlCSKH;
            pnlDEV = ui.PnlDEV;
            txtSearchID = ui.TxtSearchID;
            btnSearch = ui.BtnSearch;
            btnExport = ui.BtnExport;
            btnLogout = ui.BtnLogout;
            txtResultCSKH = ui.TxtResultCSKH;
            lblRole = ui.LblRole;
            txtConsole = ui.TxtConsole;
            dgvDev = ui.DgvDev;

            LogToConsole("Hệ thống UI mới đã sẵn sàng.");
        }

        private void ApplyRolePermissions()
        {
            pnlCSKH.Visible = (selectedRole == "cskh");
            pnlDEV.Visible = (selectedRole == "dev");

            if (selectedRole == "cskh")
            {
                lblRole.Text = "Vai trò hiện tại: CSKH | Dynamic Data Masking";
                LogToConsole("Đăng nhập thành công. Quyền CSKH đã được cấp.");
            }
            else if (selectedRole == "dev")
            {
                lblRole.Text = "Vai trò hiện tại: DEV/TESTER | Static Data Masking";
                LogToConsole("Đăng nhập thành công. Quyền DEV/TESTER đã được cấp.");
            }
            else
            {
                lblRole.Text = "Vai trò hiện tại: Không hợp lệ";
                LogToConsole("Cảnh báo: vai trò không hợp lệ.");
            }
        }

        private void LogToConsole(string message)
        {
            txtConsole.SelectionStart = 0;
            txtConsole.SelectedText = "> " + message + "\n";
        }

        // ==========================================
        // 2. LUỒNG NGHIỆP VỤ & SỰ KIỆN NÚT BẤM
        // ==========================================
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = @"SELECT HoTen, HoTen_EN, NgaySinh, GioiTinh, SoCCCD, MaKhachHang,
                                            SoDienThoai, SoDienThoai2, Email, DiaChiNha, Tinh_ThanhPho,
                                            SoTaiKhoan, SoTaiKhoan_Phu, SoDu, HanMucTinDung, LoaiTaiKhoan,
                                            TrangThaiKYC, TenCongTy, MaGiaoDich, NgayGioGD
                                     FROM Customers
                                     WHERE CustomerID = @customerId
                                     LIMIT 1";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerId", txtSearchID.Text.Trim());

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hoTenRaw = reader["HoTen"]?.ToString() ?? string.Empty;
                            string soDienThoaiRaw = reader["SoDienThoai"]?.ToString() ?? string.Empty;
                            string soTaiKhoanRaw = reader["SoTaiKhoan"]?.ToString() ?? string.Empty;
                            string soCccdRaw = reader["SoCCCD"]?.ToString() ?? string.Empty;
                            string emailRaw = reader["Email"]?.ToString() ?? string.Empty;

                            char[] name = CustomStringHelper.ToCharArray(hoTenRaw);
                            char[] phone = CustomStringHelper.ToCharArray(soDienThoaiRaw);
                            char[] acc = CustomStringHelper.ToCharArray(soTaiKhoanRaw);
                            char[] cccd = CustomStringHelper.ToCharArray(soCccdRaw);
                            char[] email = CustomStringHelper.ToCharArray(emailRaw);

                            // 1. Dynamic Data Masking
                            char[] maskedName = CustomDataMasker.MaskFullName(name);
                            char[] maskedPhone = CustomDataMasker.MaskPhoneNumber(phone);
                            char[] maskedAcc = CustomDataMasker.MaskBankAccount(acc);
                            char[] maskedCccd = CustomDataMasker.MaskBankAccount(cccd);
                            char[] maskedEmail = CustomDataMasker.MaskEmail(email);

                            // 2. Mã hóa đường truyền XOR
                            char[] key = CustomStringHelper.ToCharArray("KEY123");
                            char[] encPhone = CustomCryptography.XorEncryptDecrypt(maskedPhone, key);
                            char[] encAcc = CustomCryptography.XorEncryptDecrypt(maskedAcc, key);

                            LogToConsole($"[KÊNH TRUYỀN] Bắt được gói tin mã hóa XOR:\nSĐT: {new string(encPhone)} | STK: {new string(encAcc)}");

                            // 3. Giải mã hiển thị lên UI
                            char[] decPhone = CustomCryptography.XorEncryptDecrypt(encPhone, key);
                            char[] decAcc = CustomCryptography.XorEncryptDecrypt(encAcc, key);

                            string gioiTinh = (reader["GioiTinh"]?.ToString() ?? string.Empty) switch
                            {
                                "0" => "Nam",
                                "1" => "Nữ",
                                _ => "Không rõ"
                            };

                            string loaiTaiKhoan = (reader["LoaiTaiKhoan"]?.ToString() ?? string.Empty) switch
                            {
                                "1" => "Thường",
                                "2" => "VIP",
                                "3" => "Premium",
                                _ => "Không rõ"
                            };

                            string trangThaiKyc = (reader["TrangThaiKYC"]?.ToString() ?? string.Empty) switch
                            {
                                "0" => "Chưa KYC",
                                "1" => "Đang xét duyệt",
                                "2" => "Đạt",
                                _ => "Không rõ"
                            };

                            StringBuilder resultBuilder = new StringBuilder();
                            resultBuilder.AppendLine("KẾT QUẢ TRA CỨU CHI TIẾT (CSKH)");
                            resultBuilder.AppendLine();
                            resultBuilder.AppendLine($"Mã KH         : {reader["MaKhachHang"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Họ tên (Mask) : {new string(maskedName)}");
                            resultBuilder.AppendLine($"Họ tên EN     : {reader["HoTen_EN"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Ngày sinh     : {reader["NgaySinh"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Giới tính     : {gioiTinh}");
                            resultBuilder.AppendLine($"CCCD (Mask)   : {new string(maskedCccd)}");
                            resultBuilder.AppendLine($"SĐT chính     : {new string(decPhone)}");
                            resultBuilder.AppendLine($"SĐT phụ       : {reader["SoDienThoai2"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Email (Mask)  : {new string(maskedEmail)}");
                            resultBuilder.AppendLine($"Địa chỉ       : {reader["DiaChiNha"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Tỉnh/TP       : {reader["Tinh_ThanhPho"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"TK chính      : {new string(decAcc)}");
                            resultBuilder.AppendLine($"TK phụ        : {reader["SoTaiKhoan_Phu"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Số dư         : {reader["SoDu"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Hạn mức TD    : {reader["HanMucTinDung"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Loại TK       : {loaiTaiKhoan}");
                            resultBuilder.AppendLine($"Trạng thái KYC: {trangThaiKyc}");
                            resultBuilder.AppendLine($"Công ty       : {reader["TenCongTy"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Mã giao dịch  : {reader["MaGiaoDich"]?.ToString() ?? string.Empty}");
                            resultBuilder.AppendLine($"Lần GD gần nhất: {reader["NgayGioGD"]?.ToString() ?? string.Empty}");

                            txtResultCSKH.Text = resultBuilder.ToString();
                        }
                        else
                        {
                            txtResultCSKH.Text = "Không tìm thấy khách hàng!";
                        }
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

        private void BtnLogout_Click(object sender, EventArgs e)
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

    // =================================================================================
    // CÁC MODULE THUẬT TOÁN BẢO MẬT & XỬ LÝ CHUỖI TỰ CHẾ 
    // =================================================================================
    public static class CustomStringHelper
    {
        public static int GetLength(string input)
        {
            if (input == null) return 0;
            int len = 0;
            foreach (char c in input) len++;
            return len;
        }

        public static char[] ToCharArray(string input)
        {
            if (input == null) return new char[0];
            int len = GetLength(input);
            char[] result = new char[len];
            int i = 0;
            foreach (char c in input)
            {
                result[i] = c;
                i++;
            }
            return result;
        }
    }

    public static class CustomHash
    {
        public static char[] ComputeHash(char[] input)
        {
            if (input == null) return new char[0];
            int len = 0;
            foreach (char c in input) len++;
            
            char[] hash = new char[16];
            for (int i = 0; i < 16; i++) hash[i] = '0';
            for (int i = 0; i < len; i++)
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
            if (input == null || key == null) return new char[0];
            int lenInput = 0; foreach (char c in input) lenInput++;
            int lenKey = 0; foreach (char c in key) lenKey++;
            if (lenKey == 0) return input;
            
            char[] result = new char[lenInput];
            for (int i = 0; i < lenInput; i++) result[i] = (char)(input[i] ^ key[i % lenKey]);
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
            if (aesKey == null) return new int[0];
            int len = 0; foreach (char c in aesKey) len++;
            
            int[] encryptedKey = new int[len];
            for (int i = 0; i < len; i++)
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
            if (encryptedKey == null) return new char[0];
            int len = 0; foreach (int k in encryptedKey) len++;
            
            char[] decryptedKey = new char[len];
            for (int i = 0; i < len; i++)
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
            if (input == null || key == null) return new char[0];
            int lenInput = 0; foreach (char c in input) lenInput++;
            int lenKey = 0; foreach (char c in key) lenKey++;
            if (lenKey == 0) return input;
            
            char[] result = new char[lenInput];
            for (int i = 0; i < lenInput; i++) result[i] = (char)(input[i] + key[i % lenKey]);
            return result;
        }

        public static char[] DecryptData(char[] input, char[] key)
        {
            if (input == null || key == null) return new char[0];
            int lenInput = 0; foreach (char c in input) lenInput++;
            int lenKey = 0; foreach (char c in key) lenKey++;
            if (lenKey == 0) return input;
            
            char[] result = new char[lenInput];
            for (int i = 0; i < lenInput; i++) result[i] = (char)(input[i] - key[i % lenKey]);
            return result;
        }
    }

    public static class CustomDataMasker
    {
        public static char[] MaskPhoneNumber(char[] input)
        {
            if (input == null) return new char[0];
            int len = 0;
            foreach (char c in input) len++;
            
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
            if (input == null) return new char[0];
            int len = 0;
            foreach (char c in input) len++;
            
            char[] result = new char[len];
            int firstSpace = 0;
            while (firstSpace < len && input[firstSpace] != ' ') firstSpace++;
            int lastSpace = len - 1;
            while (lastSpace >= 0 && input[lastSpace] != ' ') lastSpace--;
            
            for (int i = 0; i < len; i++)
            {
                if (i < firstSpace || i > lastSpace || input[i] == ' ') 
                    result[i] = input[i];
                else 
                    result[i] = '*';
            }
            return result;
        }

        public static char[] MaskBankAccount(char[] input)
        {
            if (input == null) return new char[0];
            int len = 0;
            foreach (char c in input) len++;
            
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
            if (input == null) return new char[0];
            int len = 0;
            foreach (char c in input) len++;
            
            char[] result = new char[len];
            for (int i = 0; i < len; i++)
            {
                if (input[i] >= '0' && input[i] <= '9')
                {
                    int val = input[i] - '0';
                    val = (val + shiftVal) % 10;
                    result[i] = (char)(val + '0');
                }
                else
                {
                    result[i] = input[i];
                }
            }
            return result;
        }

        public static char[] MaskEmail(char[] input)
        {
            if (input == null) return new char[0];
            int len = 0; foreach (char c in input) len++;
            
            char[] result = new char[len];
            int atIndex = -1;
            for (int i = 0; i < len; i++)
            {
                if (input[i] == '@') { atIndex = i; break; }
            }

            if (atIndex == -1) // Không phải định dạng email
            {
                for (int i = 0; i < len; i++) result[i] = '*';
                return result;
            }

            for (int i = 0; i < len; i++)
            {
                if (i == 0 || i >= atIndex - 1) 
                    result[i] = input[i];
                else 
                    result[i] = '*';
            }
            return result;
        }

        public static void ShuffleBalances(decimal[] balances)
        {
            if (balances == null) return;
            int len = 0; foreach (decimal d in balances) len++;
            if (len <= 1) return;

            // Thuật toán hoán vị chéo đơn giản
            for (int i = 0; i < len - 1; i += 2)
            {
                decimal temp = balances[i];
                balances[i] = balances[i + 1];
                balances[i + 1] = temp;
            }
        }
    }
}