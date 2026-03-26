using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Forms;

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
        static readonly HttpClient ApiClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5059/"),
            Timeout = TimeSpan.FromSeconds(60)
        };

        static readonly JsonSerializerOptions JsonReadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        Panel pnlCSKH, pnlDEV;
        TextBox txtSearchID;
        Button btnSearch, btnExport, btnLogout;
        Label lblRole;
        RichTextBox txtConsole;
        DataGridView dgvDev;
        DataGridView dgvCustomerDetails;
        PictureBox picPortrait;
        Label lblMaskedCard;
        Label lblBalance;
        ComboBox cboSearchType;
        Label lblProfileName, lblProfileDob, lblProfileGender, lblProfileCccd, lblProfilePhone, lblProfileEmail;
        readonly string selectedRole;

        public bool ShouldLogout { get; private set; }

        public MainForm(string selectedRole)
        {
            this.selectedRole = selectedRole;
            SetupUI();
            ApplyRolePermissions();
        }

        private void SetupUI()
        {
            MainFormUiComponents ui = MainFormUiManager.Build(this, BtnSearch_Click, BtnExport_Click, BtnLogout_Click);
            pnlCSKH = ui.PnlCSKH;
            pnlDEV = ui.PnlDEV;
            txtSearchID = ui.TxtSearchID;
            btnSearch = ui.BtnSearch;
            btnExport = ui.BtnExport;
            btnLogout = ui.BtnLogout;
            lblRole = ui.LblRole;
            txtConsole = ui.TxtConsole;
            dgvDev = ui.DgvDev;
            dgvCustomerDetails = ui.DgvCustomerDetails;
            picPortrait = ui.PicPortrait;
            lblMaskedCard = ui.LblMaskedCard;
            lblBalance = ui.LblBalance;
            cboSearchType = ui.CboSearchType;
            lblProfileName = ui.LblProfileName;
            lblProfileDob = ui.LblProfileDob;
            lblProfileGender = ui.LblProfileGender;
            lblProfileCccd = ui.LblProfileCccd;
            lblProfilePhone = ui.LblProfilePhone;
            lblProfileEmail = ui.LblProfileEmail;

            dgvCustomerDetails.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0)
                {
                    e.CellStyle.ForeColor = Color.FromArgb(108, 122, 137);
                    e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                }
                else if (e.RowIndex >= 0 && e.ColumnIndex == 1)
                {
                    e.CellStyle.ForeColor = Color.FromArgb(15, 23, 42);
                    e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }
            };

            LogToConsole("Hệ thống UI đã sẵn sàng.");
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

        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string keyword = txtSearchID.Text.Trim();
                if (string.IsNullOrEmpty(keyword))
                {
                    MessageBox.Show("Vui lòng nhập thông tin cần tìm!");
                    return;
                }
                int searchType = cboSearchType.SelectedIndex;
                string url = $"api/customer/cskh/search?type={searchType}&keyword={Uri.EscapeDataString(keyword)}";
                using HttpResponseMessage response = await ApiClient.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    ApiErrorResponse? notFound = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonReadOptions);
                    LogToConsole(notFound?.Message ?? "Không tìm thấy khách hàng!");
                    BindCustomerDetails(null);
                    UpdatePortraitImage(null);
                    return;
                }

                response.EnsureSuccessStatusCode();
                CskhSearchResponse? result = await response.Content.ReadFromJsonAsync<CskhSearchResponse>(JsonReadOptions);
                if (result == null)
                {
                    MessageBox.Show("Phản hồi không hợp lệ từ máy chủ.");
                    return;
                }

                if (!result.Found)
                {
                    LogToConsole(result.DisplayText ?? "Không tìm thấy khách hàng!");
                    BindCustomerDetails(null);
                    UpdatePortraitImage(null);
                    return;
                }

                if (!string.IsNullOrEmpty(result.ChannelLogMessage))
                {
                    LogToConsole(result.ChannelLogMessage);
                }

                if (!string.IsNullOrEmpty(result.DisplayText))
                {
                    LogToConsole(result.DisplayText.Replace("\r\n", " - "));
                }

                if (result.DbFields != null)
                {
                    lblProfileName.Text = ExtractAndRemove(result.DbFields, "Họ tên");
                    lblProfileDob.Text = ExtractAndRemove(result.DbFields, "Ngày sinh");
                    lblProfileGender.Text = ExtractAndRemove(result.DbFields, "Giới tính");
                    lblProfileCccd.Text = ExtractAndRemove(result.DbFields, "Cccd");
                    lblProfilePhone.Text = ExtractAndRemove(result.DbFields, "Sđt");
                    lblProfileEmail.Text = ExtractAndRemove(result.DbFields, "Email");

                    var cardKey = result.DbFields.Keys.FirstOrDefault(k => k.ToLower().Contains("số thẻ"));
                    if (cardKey != null)
                    {
                        lblMaskedCard.Text = result.DbFields[cardKey] ?? "**** **** **** ****";
                        result.DbFields.Remove(cardKey);
                    }
                    else
                    {
                        lblMaskedCard.Text = "**** **** **** ****";
                    }

                    var balanceKey = result.DbFields.Keys.FirstOrDefault(k => k.ToLower().Contains("số dư"));
                    if (balanceKey != null)
                    {
                        lblBalance.Text = result.DbFields[balanceKey] ?? "0 VNĐ";
                        result.DbFields.Remove(balanceKey);
                    }
                    else
                    {
                        lblBalance.Text = "0 VNĐ";
                    }

                    BindCustomerDetails(result.DbFields);
                    UpdatePortraitImage(result.PortraitImage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi API: " + ex.Message);
            }
        }

        private void BindCustomerDetails(Dictionary<string, string?>? fields)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Thuộc tính");
            dt.Columns.Add("Giá trị");

            if (fields != null)
            {
                foreach (KeyValuePair<string, string?> item in fields.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                {
                    dt.Rows.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            dgvCustomerDetails.DataSource = dt;
        }

        private void UpdatePortraitImage(string? portraitImage)
        {
            if (picPortrait.Image != null)
            {
                picPortrait.Image.Dispose();
                picPortrait.Image = null;
            }

            if (string.IsNullOrWhiteSpace(portraitImage))
            {
                return;
            }

            string? resolvedPath = ResolvePortraitPath(portraitImage);
            if (resolvedPath == null)
            {
                LogToConsole("Không tìm thấy ảnh chân dung trên máy local. DB path: " + portraitImage);
                return;
            }

            try
            {
                using (FileStream stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read))
                using (Image rawImage = Image.FromStream(stream))
                {
                    picPortrait.Image = new Bitmap(rawImage);
                }

                LogToConsole("Đã tải ảnh chân dung: " + resolvedPath);
            }
            catch (Exception)
            {
                LogToConsole("Không thể tải ảnh chân dung từ đường dẫn đã nhận.");
            }
        }

        private static string? ResolvePortraitPath(string portraitImage)
        {
            string normalizedInput = NormalizePortraitPath(portraitImage);

            string? directResolved = ResolveFileFromPathOrDirectory(normalizedInput);
            if (directResolved != null)
            {
                return directResolved;
            }

            string[] roots =
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory,
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                @"e:\MaskingDataC#"
            };

            foreach (string root in roots)
            {
                string candidate = Path.Combine(root, normalizedInput);
                string? resolved = ResolveFileFromPathOrDirectory(candidate);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            string? recovered = RecoverMalformedPortraitPath(normalizedInput, roots);
            if (recovered != null)
            {
                return recovered;
            }

            return null;
        }

        private static string NormalizePortraitPath(string portraitImage)
        {
            string path = portraitImage.Trim().Trim('"', '\'');
            while (path.Contains("\\\\", StringComparison.Ordinal))
            {
                path = path.Replace("\\\\", "\\", StringComparison.Ordinal);
            }
            return path;
        }

        private static string? RecoverMalformedPortraitPath(string normalizedInput, string[] roots)
        {
            string inputToken = CanonicalPathToken(normalizedInput);
            if (inputToken.Length == 0)
            {
                return null;
            }

            foreach (string root in roots)
            {
                string portraitRoot = Path.Combine(root, "PortraitImage");
                if (!Directory.Exists(portraitRoot))
                {
                    continue;
                }

                foreach (string dir in Directory.GetDirectories(portraitRoot))
                {
                    string dirToken = CanonicalPathToken(dir);
                    string nameToken = CanonicalPathToken(Path.GetFileName(dir));

                    if (!inputToken.Contains(dirToken, StringComparison.Ordinal) &&
                        !inputToken.EndsWith(nameToken, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string? resolved = ResolveFileFromPathOrDirectory(dir);
                    if (resolved != null)
                    {
                        return resolved;
                    }
                }
            }

            return null;
        }

        private static string CanonicalPathToken(string path)
        {
            char[] buffer = new char[path.Length];
            int index = 0;
            foreach (char c in path)
            {
                if (char.IsLetterOrDigit(c))
                {
                    buffer[index++] = char.ToLowerInvariant(c);
                }
            }
            return new string(buffer, 0, index);
        }

        private static string? ResolveFileFromPathOrDirectory(string path)
        {
            if (File.Exists(path))
            {
                return path;
            }

            if (!Directory.Exists(path))
            {
                return null;
            }

            string[] extensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.webp", "*.gif" };
            foreach (string extension in extensions)
            {
                string[] files = Directory.GetFiles(path, extension, SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }

            return null;
        }

        private async void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using HttpResponseMessage response = await ApiClient.GetAsync("api/customer/dev/export");
                response.EnsureSuccessStatusCode();
                DevExportResponse? result = await response.Content.ReadFromJsonAsync<DevExportResponse>(JsonReadOptions);
                if (result == null)
                {
                    MessageBox.Show("Phản hồi không hợp lệ từ máy chủ.");
                    return;
                }

                DataTable dt = new DataTable();
                dt.Columns.Add("ID Hệ thống");
                dt.Columns.Add("Mã KH");
                dt.Columns.Add("Họ tên (Masked)");
                dt.Columns.Add("Email (Masked)");
                dt.Columns.Add("CCCD (Mã hóa AES-256)");

                foreach (DevExportRowDto row in result.Rows)
                {
                    dt.Rows.Add(row.Id, row.MaKh, row.MaskedName, row.MaskedEmail, row.CipherCccd);
                }

                dgvDev.DataSource = dt;

                if (!string.IsNullOrEmpty(result.ConsoleMessage))
                {
                    foreach (string line in result.ConsoleMessage.Split('\n'))
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            LogToConsole(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi API: " + ex.Message);
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

        private string ExtractAndRemove(Dictionary<string, string?> dict, string partialKey)
        {
            var key = dict.Keys.FirstOrDefault(k => k.ToLower().Contains(partialKey.ToLower()));
            if (key != null)
            {
                string val = dict[key] ?? "---";
                dict.Remove(key);
                return val;
            }
            return "---";
        }
    }

    internal sealed class CskhSearchResponse
    {
        public bool Found { get; set; }
        public string? DisplayText { get; set; }
        public string? ChannelLogMessage { get; set; }
        public string? PortraitImage { get; set; }
        public Dictionary<string, string?> DbFields { get; set; } = new Dictionary<string, string?>();
    }

    internal sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }

    internal sealed class DevExportRowDto
    {
        public string Id { get; set; } = "";
        public string MaKh { get; set; } = "";
        public string MaskedName { get; set; } = "";
        public string MaskedEmail { get; set; } = "";
        public string CipherCccd { get; set; } = "";
    }

    internal sealed class DevExportResponse
    {
        public List<DevExportRowDto> Rows { get; set; } = new List<DevExportRowDto>();
        public string? ConsoleMessage { get; set; }
    }

}
