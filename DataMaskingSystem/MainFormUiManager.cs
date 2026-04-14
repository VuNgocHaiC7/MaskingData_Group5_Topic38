using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace DataMaskingSystem
{
    // --- DATA TRANSFER OBJECTS (DTOs) ---
    // These classes MUST match the JSON structure returned by the backend API.

    public class CskhSearchResponse
    {
        public bool Found { get; set; }
        public string DisplayText { get; set; } = "";
        public string? ChannelLogMessage { get; set; }
        public string? PortraitImage { get; set; }
        public string MaskedCard { get; set; } = "**** **** **** ****";
        public string CurrentBalance { get; set; } = "0 VNĐ";
        public Dictionary<string, string?> DbFields { get; set; } = new();
    }

    public class DevExportRowDto
    {
        public long CustomerID { get; set; }
        public string HinhAnhChanDung { get; set; } = "";
        public string HoTenMasked { get; set; } = "";
        public string NgaySinh { get; set; } = "";
        public int GioiTinh { get; set; }
        public string MaKhachHang { get; set; } = "";
        public string QuocTich { get; set; } = "";
        public string DiaChiNhaMasked { get; set; } = "";
        public decimal SoDu { get; set; }
        public decimal DuNoHienTai { get; set; }
        public int LoaiTaiKhoan { get; set; }
        public int TrangThaiThe { get; set; }
        public string TenDangNhapMasked { get; set; } = "";
        public int RoleID { get; set; }

        public string MaskedSoDienThoai { get; set; } = "";
        public string MaskedEmail { get; set; } = "";
        public string MaskedSoCCCD { get; set; } = "";
        public string MaskedSoTaiKhoan { get; set; } = "";
        public string MaskedSoThe { get; set; } = "";

        public string CipherSoDienThoai { get; set; } = "";
        public string CipherEmail { get; set; } = "";
        public string CipherSoCCCD { get; set; } = "";
        public string CipherSoTaiKhoan { get; set; } = "";
        public string CipherSoThe { get; set; } = "";
    }

    public class DevExportResponse
    {
        public List<DevExportRowDto> Rows { get; set; } = new();
        public string? ConsoleMessage { get; set; }
    }

    public class CustomerSelfProfileResponse
    {
        public bool Found { get; set; }
        public string Message { get; set; } = "";
        public string? PortraitImage { get; set; }
        public long CustomerId { get; set; }
        public string MaKhachHang { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string NgaySinh { get; set; } = "";
        public int GioiTinh { get; set; }
        public string SoCCCD { get; set; } = "";
        public string QuocTich { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string Email { get; set; } = "";
        public string DiaChiNha { get; set; } = "";
        public string SoTaiKhoan { get; set; } = "";
        public decimal SoDu { get; set; }
        public decimal DuNoHienTai { get; set; }
        public int LoaiTaiKhoan { get; set; }
        public string SoThe { get; set; } = "";
        public int TrangThaiThe { get; set; }
        public string TenDangNhap { get; set; } = "";
        public int RoleId { get; set; }
    }

    public class CskhUpdateRequestApiModel
    {
        public long CustomerId { get; set; }
        public string RequestReason { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string NgaySinh { get; set; } = "";
        public int? GioiTinh { get; set; }
        public string QuocTich { get; set; } = "";
        public string DiaChiNha { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string Email { get; set; } = "";
        public string SoCCCD { get; set; } = "";
    }

    public class CskhUpdateRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public long RequestId { get; set; }
    }

    public static class MainFormUiManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "https://localhost:7299"; // Matches BankMaskingAPI HTTPS profile in launchSettings.json
        private static readonly HashSet<string> HiddenDetailKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "Họ tên (Mask)",
            "Ngày sinh",
            "Giới tính",
            "CCCD (Mask)",
            "SĐT (Mask)",
            "Email (Mask)"
        };
        private static readonly string[] PreferredDetailOrder =
        {
            "Dư nợ hiện tại",
            "ID Hệ Thống",
            "Loại tài khoản",
            "Loaị tài khoản",
            "Mã KH",
            "Quốc tịch",
            "Trạng thái thẻ",
            "Tài khoản chính",
            "Tên đăng nhập",
            "Địa chỉ nhà"
        };

        public static async Task SearchAndDisplay(MainFormUiComponents ui, int searchType, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                MessageBox.Show("Vui lòng nhập từ khóa tìm kiếm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ui.TxtConsole.Text = "Đang gửi yêu cầu đến máy chủ...";
            ui.BtnSearch.Enabled = false;

            try
            {
                ApplyAuthHeader();
                string apiUrl = $"{ApiBaseUrl}/api/customer/cskh/search?type={searchType}&keyword={Uri.EscapeDataString(keyword)}";
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<CskhSearchResponse>(jsonString);

                    if (result != null && result.Found)
                    {
                        ui.TxtConsole.Text = result.ChannelLogMessage ?? "Không có log kênh truyền.";
                        UpdateCskhUI(ui, result);
                    }
                    else
                    {
                        ResetCskhUi(ui);
                        ui.TxtConsole.Text = result?.DisplayText ?? "Không tìm thấy khách hàng.";
                        MessageBox.Show(result?.DisplayText ?? "Không tìm thấy khách hàng.", "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    ResetCskhUi(ui);
                    string errorContent = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        string notFoundMessage = "Không tìm thấy khách hàng.";
                        try
                        {
                            var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(errorContent);
                            if (payload != null && payload.TryGetValue("message", out string? message) && !string.IsNullOrWhiteSpace(message))
                            {
                                notFoundMessage = message;
                            }
                        }
                        catch
                        {
                        }

                        ui.TxtConsole.Text = notFoundMessage;
                        MessageBox.Show(notFoundMessage, "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        ui.TxtConsole.Text = "Phiên đăng nhập không hợp lệ hoặc không đủ quyền truy cập.";
                        MessageBox.Show("Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.", "Xác thực thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ui.TxtConsole.Text = $"Lỗi API: {response.StatusCode}\n{errorContent}";
                    MessageBox.Show($"Không thể kết nối đến máy chủ hoặc có lỗi xảy ra.\nChi tiết: {response.ReasonPhrase}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                ResetCskhUi(ui);
                ui.TxtConsole.Text = $"Lỗi kết nối: {ex.Message}";
                MessageBox.Show($"Không thể kết nối đến máy chủ. Vui lòng đảm bảo API backend đang chạy.\nChi tiết: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (JsonException ex)
            {
                 ResetCskhUi(ui);
                ui.TxtConsole.Text = $"Lỗi phân tích dữ liệu (JSON): {ex.Message}";
                MessageBox.Show($"Dữ liệu trả về từ máy chủ không hợp lệ.\nChi tiết: {ex.Message}", "Lỗi Dữ Liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ui.BtnSearch.Enabled = true;
            }
        }

        public static async Task SubmitCskhUpdateRequest(MainFormUiComponents ui)
        {
            if (ui.PnlSearchArea.Tag == null || !long.TryParse(ui.PnlSearchArea.Tag.ToString(), out long customerId) || customerId <= 0)
            {
                MessageBox.Show("Vui lòng tìm và chọn khách hàng trước khi cập nhật thông tin.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new CskhUpdateRequestDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var payload = new CskhUpdateRequestApiModel
            {
                CustomerId = customerId,
                RequestReason = dialog.RequestReason,
                HoTen = dialog.HoTen,
                NgaySinh = dialog.NgaySinh,
                GioiTinh = dialog.GioiTinh,
                QuocTich = dialog.QuocTich,
                DiaChiNha = dialog.DiaChiNha,
                SoDienThoai = dialog.SoDienThoai,
                Email = dialog.Email,
                SoCCCD = dialog.SoCCCD
            };

            try
            {
                ApplyAuthHeader();
                string apiUrl = $"{ApiBaseUrl}/api/customer/cskh/update-request";
                string body = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);

                string responseText = await response.Content.ReadAsStringAsync();
                CskhUpdateRequestResult? result = null;
                try
                {
                    result = JsonConvert.DeserializeObject<CskhUpdateRequestResult>(responseText);
                }
                catch
                {
                }

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = result?.Message ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errorMsg = $"Cập nhật thất bại: {response.StatusCode}";
                    }

                    ui.TxtConsole.Text = errorMsg;
                    MessageBox.Show(errorMsg, "Cập nhật thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string successMsg = result?.Message ?? string.Empty;
                if (string.IsNullOrWhiteSpace(successMsg))
                {
                    successMsg = "Đã cập nhật thông tin khách hàng thành công.";
                }

                ui.TxtConsole.Text = successMsg;
                MessageBox.Show(successMsg, "Cập nhật thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ui.TxtConsole.Text = "Lỗi cập nhật thông tin: " + ex.Message;
                MessageBox.Show("Không thể cập nhật thông tin khách hàng.\nChi tiết: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task ExportAndDisplay(MainFormUiComponents ui)
        {
            ui.TxtConsole.Text = "Đang yêu cầu xuất dữ liệu DEV/TEST...";
            ui.BtnExport.Enabled = false;

            try
            {
                ApplyAuthHeader();
                string apiUrl = $"{ApiBaseUrl}/api/customer/dev/export";
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<DevExportResponse>(jsonString);

                    if (result != null)
                    {
                        ui.TxtConsole.Text = result.ConsoleMessage ?? "Xuất dữ liệu thành công.";
                        ui.DgvDev.DataSource = result.Rows;
                        ConfigureDevGridColumns(ui.DgvDev);
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        ui.TxtConsole.Text = "Phiên đăng nhập không hợp lệ hoặc không đủ quyền DEV.";
                        MessageBox.Show("Bạn không có quyền truy cập chức năng DEV hoặc token đã hết hạn.", "Xác thực thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ui.TxtConsole.Text = $"Lỗi API: {response.StatusCode}\n{errorContent}";
                }
            }
            catch (Exception ex)
            {
                ui.TxtConsole.Text = $"Lỗi: {ex.Message}";
            }
            finally
            {
                ui.BtnExport.Enabled = true;
            }
        }

        public static void SaveDevGridToCsv(MainFormUiComponents ui)
        {
            if (ui.DgvDev.Rows.Count == 0 || ui.DgvDev.Columns.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để lưu. Vui lòng tải dữ liệu trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Title = "Lưu dữ liệu CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = "csv",
                AddExtension = true,
                FileName = $"customers_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                var sb = new StringBuilder();

                var visibleColumns = new List<DataGridViewColumn>();
                foreach (DataGridViewColumn column in ui.DgvDev.Columns)
                {
                    if (column.Visible)
                    {
                        visibleColumns.Add(column);
                    }
                }

                for (int i = 0; i < visibleColumns.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(EscapeCsv(visibleColumns[i].HeaderText));
                }
                sb.AppendLine();

                foreach (DataGridViewRow row in ui.DgvDev.Rows)
                {
                    if (row.IsNewRow)
                    {
                        continue;
                    }

                    for (int i = 0; i < visibleColumns.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        object? value = row.Cells[visibleColumns[i].Index].Value;
                        sb.Append(EscapeCsv(value?.ToString() ?? string.Empty));
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Đã lưu file CSV thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu file CSV thất bại: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string EscapeCsv(string input)
        {
            if (input.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return "\"" + input.Replace("\"", "\"\"") + "\"";
            }

            return input;
        }

        private static void ConfigureDevGridColumns(DataGridView grid)
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.ScrollBars = ScrollBars.Both;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.MinimumWidth = 120;
                column.Width = 170;
            }

            if (grid.Columns.Contains("CustomerID"))
            {
                grid.Columns["CustomerID"].Width = 90;
                grid.Columns["CustomerID"].Frozen = true;
            }

            if (grid.Columns.Contains("MaKhachHang"))
            {
                grid.Columns["MaKhachHang"].Width = 110;
            }

            if (grid.Columns.Contains("HoTenMasked"))
            {
                grid.Columns["HoTenMasked"].Width = 160;
            }

            if (grid.Columns.Contains("NgaySinh"))
            {
                grid.Columns["NgaySinh"].Width = 110;
            }

            if (grid.Columns.Contains("GioiTinh"))
            {
                grid.Columns["GioiTinh"].Width = 80;
            }
        }

        public static async Task LoadAndDisplayMyProfile(MainFormUiComponents ui)
        {
            ui.TxtConsole.Text = "Đang tải hồ sơ cá nhân...";
            ui.BtnSearch.Enabled = false;
            ui.TxtSearchID.Enabled = false;
            ui.CboSearchType.Enabled = false;

            try
            {
                ApplyAuthHeader();
                string apiUrl = $"{ApiBaseUrl}/api/customer/me/profile";
                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ResetCskhUi(ui);
                    string errorContent = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        ui.TxtConsole.Text = "Token không hợp lệ hoặc đã hết hạn.";
                        MessageBox.Show("Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.", "Xác thực thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ui.TxtConsole.Text = $"Lỗi API: {response.StatusCode}\n{errorContent}";
                    MessageBox.Show("Không thể tải hồ sơ cá nhân từ máy chủ.", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<CustomerSelfProfileResponse>(jsonString);
                if (result == null || !result.Found)
                {
                    ResetCskhUi(ui);
                    ui.TxtConsole.Text = result?.Message ?? "Không tìm thấy hồ sơ cá nhân.";
                    MessageBox.Show(result?.Message ?? "Không tìm thấy hồ sơ cá nhân.", "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                UpdateCustomerSelfUi(ui, result);
                ui.TxtConsole.Text = "Đã tải hồ sơ cá nhân thành công qua kết nối xác thực JWT.";
            }
            catch (Exception ex)
            {
                ResetCskhUi(ui);
                ui.TxtConsole.Text = $"Lỗi tải hồ sơ: {ex.Message}";
                MessageBox.Show($"Không thể tải hồ sơ cá nhân.\nChi tiết: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ConfigureCustomerSelfLayout(MainFormUiComponents ui)
        {
            ui.PnlSearchArea.Visible = false;
            ui.BtnCskhUpdateRequest.Visible = false;
            ui.LblDetailsTitle.Location = new Point(ui.LblDetailsTitle.Location.X, 148);
            ui.DgvCustomerDetails.Location = new Point(ui.DgvCustomerDetails.Location.X, 172);
            ui.DgvCustomerDetails.Height = 362;
        }

        private static void UpdateCskhUI(MainFormUiComponents ui, CskhSearchResponse data)
        {
            // Update simple labels
            ui.LblMaskedCard.Text = string.IsNullOrWhiteSpace(data.MaskedCard)
                ? "**** **** **** ****"
                : data.MaskedCard;
            ui.LblBalance.Text = string.IsNullOrWhiteSpace(data.CurrentBalance)
                ? "0 VNĐ"
                : data.CurrentBalance;

            // Update profile card
            ui.LblProfileName.Text = data.DbFields.GetValueOrDefault("Họ tên (Mask)", "---");
            ui.LblProfileDob.Text = data.DbFields.GetValueOrDefault("Ngày sinh", "---");
            ui.LblProfileGender.Text = data.DbFields.GetValueOrDefault("Giới tính", "---");
            ui.LblProfileCccd.Text = data.DbFields.GetValueOrDefault("CCCD (Mask)", "---");
            ui.LblProfilePhone.Text = data.DbFields.GetValueOrDefault("SĐT (Mask)", "---");
            ui.LblProfileEmail.Text = data.DbFields.GetValueOrDefault("Email (Mask)", "---");

            if (data.DbFields.TryGetValue("ID Hệ Thống", out string? customerIdText)
                && long.TryParse(customerIdText, out long customerId)
                && customerId > 0)
            {
                ui.PnlSearchArea.Tag = customerId;
            }
            else
            {
                ui.PnlSearchArea.Tag = null;
            }

            // Update details grid
            ui.DgvCustomerDetails.DataSource = null;
            ui.DgvCustomerDetails.Rows.Clear();
            ui.DgvCustomerDetails.Columns.Clear();
            ui.DgvCustomerDetails.Columns.Add("LeftCategory", "Hạng Mục");
            ui.DgvCustomerDetails.Columns.Add("LeftDetail", "Chi tiết");
            ui.DgvCustomerDetails.Columns.Add("RightCategory", "Hạng Mục (Tiếp)");
            ui.DgvCustomerDetails.Columns.Add("RightDetail", "Chi tiết (Tiếp)");
            ConfigureDetailsGridColumns(ui.DgvCustomerDetails);

            var displayFields = BuildDisplayFields(data.DbFields);
            for (int i = 0; i < displayFields.Count; i += 2)
            {
                var left = displayFields[i];
                var right = i + 1 < displayFields.Count
                    ? displayFields[i + 1]
                    : new KeyValuePair<string, string>(string.Empty, string.Empty);

                ui.DgvCustomerDetails.Rows.Add(left.Key, left.Value, right.Key, right.Value);
            }

            ui.DgvCustomerDetails.Columns["LeftDetail"].DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            ui.DgvCustomerDetails.Columns["RightDetail"].DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            SetPortraitImage(ui.PicPortrait, data.PortraitImage);
        }

        private static void UpdateCustomerSelfUi(MainFormUiComponents ui, CustomerSelfProfileResponse data)
        {
            string genderText = data.GioiTinh == 0 ? "Nam" : "Nữ";
            ui.LblMaskedCard.Text = string.IsNullOrWhiteSpace(data.SoThe) ? "---" : data.SoThe;
            ui.LblBalance.Text = string.Format("{0:N0} VNĐ", data.SoDu);

            ui.LblProfileName.Text = string.IsNullOrWhiteSpace(data.HoTen) ? "---" : data.HoTen;
            ui.LblProfileDob.Text = string.IsNullOrWhiteSpace(data.NgaySinh) ? "---" : data.NgaySinh;
            ui.LblProfileGender.Text = genderText;
            ui.LblProfileCccd.Text = string.IsNullOrWhiteSpace(data.SoCCCD) ? "---" : data.SoCCCD;
            ui.LblProfilePhone.Text = string.IsNullOrWhiteSpace(data.SoDienThoai) ? "---" : data.SoDienThoai;
            ui.LblProfileEmail.Text = string.IsNullOrWhiteSpace(data.Email) ? "---" : data.Email;

            ui.DgvCustomerDetails.DataSource = null;
            ui.DgvCustomerDetails.Rows.Clear();
            ui.DgvCustomerDetails.Columns.Clear();
            ui.DgvCustomerDetails.Columns.Add("LeftCategory", "Hạng Mục");
            ui.DgvCustomerDetails.Columns.Add("LeftDetail", "Chi tiết");
            ui.DgvCustomerDetails.Columns.Add("RightCategory", "Hạng Mục (Tiếp)");
            ui.DgvCustomerDetails.Columns.Add("RightDetail", "Chi tiết (Tiếp)");
            ConfigureDetailsGridColumns(ui.DgvCustomerDetails);

            var details = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("ID Hệ Thống", data.CustomerId.ToString()),
                new KeyValuePair<string, string>("Mã KH", data.MaKhachHang),
                new KeyValuePair<string, string>("Quốc tịch", data.QuocTich),
                new KeyValuePair<string, string>("Địa chỉ", data.DiaChiNha),
                new KeyValuePair<string, string>("Số tài khoản", data.SoTaiKhoan),
                new KeyValuePair<string, string>("Số thẻ", data.SoThe),
                new KeyValuePair<string, string>("Dư nợ hiện tại", string.Format("{0:N0} VNĐ", data.DuNoHienTai)),
                new KeyValuePair<string, string>("Loại tài khoản", data.LoaiTaiKhoan.ToString()),
                new KeyValuePair<string, string>("Trạng thái thẻ", data.TrangThaiThe.ToString()),
                new KeyValuePair<string, string>("Tên đăng nhập", data.TenDangNhap)
            };

            for (int i = 0; i < details.Count; i += 2)
            {
                var left = details[i];
                var right = i + 1 < details.Count
                    ? details[i + 1]
                    : new KeyValuePair<string, string>(string.Empty, string.Empty);

                ui.DgvCustomerDetails.Rows.Add(left.Key, left.Value, right.Key, right.Value);
            }

            SetPortraitImage(ui.PicPortrait, data.PortraitImage);
        }

        private static void ConfigureDetailsGridColumns(DataGridView grid)
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            const int categoryWidth = 132;
            const int detailWidth = 170;

            grid.Columns["LeftCategory"].Width = categoryWidth;
            grid.Columns["LeftDetail"].Width = detailWidth;
            grid.Columns["RightCategory"].Width = categoryWidth;
            grid.Columns["RightDetail"].Width = detailWidth;

            grid.Columns["LeftDetail"].DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.Columns["RightDetail"].DefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }

        private static void ResetCskhUi(MainFormUiComponents ui)
        {
            ui.LblMaskedCard.Text = "**** **** **** ****";
            ui.LblBalance.Text = "0 VNĐ";
            ui.LblProfileName.Text = "---";
            ui.LblProfileDob.Text = "---";
            ui.LblProfileGender.Text = "---";
            ui.LblProfileCccd.Text = "---";
            ui.LblProfilePhone.Text = "---";
            ui.LblProfileEmail.Text = "---";
            ui.DgvCustomerDetails.DataSource = null;
            ui.DgvCustomerDetails.Rows.Clear();
            ui.DgvCustomerDetails.Columns.Clear();
            ui.PnlSearchArea.Tag = null;
            ClearPortraitImage(ui.PicPortrait);
            ui.PicPortrait.Image = null;
        }

        private static List<KeyValuePair<string, string>> BuildDisplayFields(Dictionary<string, string?> dbFields)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in dbFields)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                map[item.Key.Trim()] = item.Value?.Trim() ?? string.Empty;
            }

            if (map.ContainsKey("Loaị tài khoản") && !map.ContainsKey("Loại tài khoản"))
            {
                map["Loại tài khoản"] = map["Loaị tài khoản"];
                map.Remove("Loaị tài khoản");
            }

            var result = new List<KeyValuePair<string, string>>();
            var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in PreferredDetailOrder)
            {
                if (map.TryGetValue(key, out string? value) && !usedKeys.Contains(key))
                {
                    result.Add(new KeyValuePair<string, string>(key, value));
                    usedKeys.Add(key);
                }
            }

            foreach (var item in map)
            {
                if (!usedKeys.Contains(item.Key))
                {
                    result.Add(new KeyValuePair<string, string>(item.Key, item.Value));
                }
            }

            result.RemoveAll(item => HiddenDetailKeys.Contains(item.Key));

            return result;
        }

        private static void ApplyAuthHeader()
        {
            string token = AuthSession.AccessToken;
            string clientId = AuthSession.ClientId;
            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _httpClient.DefaultRequestHeaders.Remove("X-Client-Id");
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Remove("X-Client-Id");
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Client-Id", clientId);
            }
        }

        private static void SetPortraitImage(PictureBox portraitBox, string? portraitRelativePath)
        {
            ClearPortraitImage(portraitBox);

            if (string.IsNullOrWhiteSpace(portraitRelativePath))
            {
                portraitBox.Image = null;
                return;
            }

            string? imagePath = ResolvePortraitPath(portraitRelativePath);
            if (imagePath == null)
            {
                portraitBox.Image = null;
                return;
            }

            try
            {
                using var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sourceImage = Image.FromStream(imageStream);
                portraitBox.Image = new Bitmap(sourceImage);
            }
            catch
            {
                portraitBox.Image = null;
            }
        }

        private static string? ResolvePortraitPath(string portraitRelativePath)
        {
            string rawInput = portraitRelativePath.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return null;
            }

            string normalizedInput = rawInput.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (Path.IsPathFullyQualified(normalizedInput) && File.Exists(normalizedInput))
            {
                return normalizedInput;
            }

            string cleanRelativePath = normalizedInput.TrimStart(Path.DirectorySeparatorChar);
            string? portraitRoot = FindPortraitRootDirectory();

            var candidates = new List<string>
            {
                Path.Combine(Application.StartupPath, "..", "..", "..", "..", "PortraitImage", cleanRelativePath),
                Path.Combine(Application.StartupPath, "..", "..", "..", "PortraitImage", cleanRelativePath),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PortraitImage", cleanRelativePath),
                Path.Combine(Directory.GetCurrentDirectory(), "PortraitImage", cleanRelativePath),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "PortraitImage", cleanRelativePath)
            };

            if (!string.IsNullOrWhiteSpace(portraitRoot))
            {
                candidates.Add(Path.Combine(portraitRoot, cleanRelativePath));
            }

            string marker = "PortraitImage";
            int markerIndex = cleanRelativePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                string relativeFromPortrait = cleanRelativePath[(markerIndex + marker.Length)..].TrimStart(Path.DirectorySeparatorChar);
                if (!string.IsNullOrWhiteSpace(relativeFromPortrait))
                {
                    candidates.Add(Path.Combine(Application.StartupPath, "..", "..", "..", "..", marker, relativeFromPortrait));
                    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), marker, relativeFromPortrait));

                    if (!string.IsNullOrWhiteSpace(portraitRoot))
                    {
                        candidates.Add(Path.Combine(portraitRoot, relativeFromPortrait));
                    }
                }
            }

            foreach (string candidate in candidates)
            {
                string fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            string? fallbackMatch = ResolvePortraitByHints(rawInput, portraitRoot);
            if (!string.IsNullOrWhiteSpace(fallbackMatch))
            {
                return fallbackMatch;
            }

            return null;
        }

        private static string? ResolvePortraitByHints(string rawInput, string? portraitRoot)
        {
            if (string.IsNullOrWhiteSpace(portraitRoot) || !Directory.Exists(portraitRoot))
            {
                return null;
            }

            string input = rawInput.Trim();
            string? fileName = ExtractImageFileName(input);

            foreach (string personFolder in Directory.GetDirectories(portraitRoot))
            {
                string personName = Path.GetFileName(personFolder);
                if (string.IsNullOrWhiteSpace(personName))
                {
                    continue;
                }

                if (input.IndexOf(personName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    string directCandidate = Path.Combine(personFolder, fileName);
                    if (File.Exists(directCandidate))
                    {
                        return directCandidate;
                    }
                }

                string[] jpgCandidates = Directory.GetFiles(personFolder, "*.jpg", SearchOption.TopDirectoryOnly);
                if (jpgCandidates.Length > 0)
                {
                    return jpgCandidates[0];
                }

                string[] pngCandidates = Directory.GetFiles(personFolder, "*.png", SearchOption.TopDirectoryOnly);
                if (pngCandidates.Length > 0)
                {
                    return pngCandidates[0];
                }
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string[] allMatches = Directory.GetFiles(portraitRoot, fileName, SearchOption.AllDirectories);
                if (allMatches.Length > 0)
                {
                    return allMatches[0];
                }
            }

            return null;
        }

        private static string? ExtractImageFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            string normalized = input.Replace('\\', '/');
            string fromPath = Path.GetFileName(normalized);
            if (!string.IsNullOrWhiteSpace(fromPath)
                && (fromPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || fromPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || fromPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || fromPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                    || fromPath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)))
            {
                return fromPath;
            }

            Match match = Regex.Match(normalized, @"([A-Za-z0-9_\-]+\.(jpg|jpeg|png|webp|bmp))$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private static string? FindPortraitRootDirectory()
        {
            string[] seedDirectories =
            {
                Application.StartupPath,
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory()
            };

            foreach (string seed in seedDirectories)
            {
                string? current = seed;
                for (int depth = 0; depth < 8 && !string.IsNullOrWhiteSpace(current); depth++)
                {
                    string candidate = Path.Combine(current, "PortraitImage");
                    if (Directory.Exists(candidate))
                    {
                        return candidate;
                    }

                    current = Directory.GetParent(current)?.FullName;
                }
            }

            return null;
        }

        private static void ClearPortraitImage(PictureBox portraitBox)
        {
            if (portraitBox.Image != null)
            {
                var oldImage = portraitBox.Image;
                portraitBox.Image = null;
                oldImage.Dispose();
            }
        }

        public static MainFormUiComponents Build(Form host, Func<Task> onSearchClick, Func<Task> onExportClick, EventHandler onSaveCsvClick, EventHandler onLogoutClick)
        {
            MainFormUiComponents? uiRef = null;

            host.Text = "The Financial Atelier | Masking Console";
            host.Size = new Size(1360, 860);
            host.MinimumSize = new Size(1220, 760);
            host.StartPosition = FormStartPosition.CenterScreen;
            host.BackColor = Color.FromArgb(242, 244, 248);
            host.ForeColor = Color.FromArgb(21, 32, 60);

            int fullWidth = host.ClientSize.Width;
            int canvasWidth = fullWidth - 32;

            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(242, 244, 248)
            };

            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Width = fullWidth,
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
                Text = "Kiểm soát thông tin khách hàng",
                Font = new Font("Bahnschrift SemiBold", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 40, 72),
                Location = new Point(16, 34),
                AutoSize = true
            };
            topBar.Controls.Add(pageTitle);

            Button btnLogout = new Button
            {
                Text = "Đăng xuất",
                Width = 126,
                Height = 38,
                BackColor = Color.FromArgb(16, 70, 163),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                Location = new Point(fullWidth - 160, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += onLogoutClick;
            topBar.Controls.Add(btnLogout);

            // --- ĐÃ TẮT AUTOSCROLL ĐỂ BẢNG TỰ QUẢN LÝ THANH CUỘN ---
            Panel canvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(242, 244, 248),
                Padding = new Padding(16, 0, 16, 14),
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
            // -------------------------------------------------------

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
                Text = "Ảnh chân dung",
                Font = new Font("Bahnschrift SemiBold", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 40, 68),
                Location = new Point(20, 280), 
                Width = 260,           
                Height = 40,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter
            };
            profileCard.Controls.Add(profileName);

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

            Panel contentCard = CreateCardPanel(new Rectangle(320, 0, canvasWidth - 320, 552), Color.White);
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
                Text = "Số thẻ",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(174, 214, 255),
                Location = new Point(16, 14),
                AutoSize = true
            });
            Label lblMaskedCard = new Label
            {
                Text = "**** **** **** ****",
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
                Text = "Số dư hiện tại",
                Font = new Font("Bahnschrift", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(99, 110, 136),
                Location = new Point(12, 13),
                AutoSize = true
            });
            Label lblBalance = new Label
            {
                Text = "0 VNĐ",
                Font = new Font("Bahnschrift SemiBold", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 43, 74),
                Location = new Point(12, 40),
                AutoSize = true
            };
            statusCard.Controls.Add(lblBalance);
            contentCard.Controls.Add(statusCard);

            Panel searchArea = new Panel
            {
                Location = new Point(22, 148),
                Size = new Size(570, 62),
                BackColor = Color.Transparent
            };

            Label searchLabel = new Label
            {
                Text = "Chọn tiêu chí & Nhập thông tin tìm kiếm",
                Font = new Font("Bahnschrift", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(95, 108, 137),
                Location = new Point(0, 0),
                AutoSize = true
            };
            searchArea.Controls.Add(searchLabel);

            ComboBox cboSearchType = new ComboBox
            {
                Location = new Point(0, 24),
                Width = 160,
                Font = new Font("Bahnschrift", 11, FontStyle.Regular),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(250, 251, 254),
                ForeColor = Color.FromArgb(31, 43, 74)
            };
            cboSearchType.Items.AddRange(new string[] { "Số điện thoại", "Số CMND/CCCD", "Số TK / Số thẻ" });
            cboSearchType.SelectedIndex = 0;
            searchArea.Controls.Add(cboSearchType);

            TextBox txtSearchID = new TextBox
            {
                Location = new Point(168, 24),
                Width = 240,
                Height = 34,
                Font = new Font("Bahnschrift", 11, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 251, 254)
            };
            searchArea.Controls.Add(txtSearchID);

            Button btnSearch = new Button
            {
                Text = "Tìm kiếm",
                Location = new Point(416, 22),
                Width = 150,
                Height = 34,
                BackColor = Color.FromArgb(20, 123, 197),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += async (s, e) => await onSearchClick();
            searchArea.Controls.Add(btnSearch);

            Button btnCskhUpdateRequest = new Button
            {
                Text = "Cập nhật thông tin",
                Location = new Point(606, 168),
                Width = 150,
                Height = 34,
                BackColor = Color.FromArgb(232, 101, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCskhUpdateRequest.FlatAppearance.BorderSize = 0;
            btnCskhUpdateRequest.Click += async (s, e) =>
            {
                if (uiRef != null)
                {
                    await SubmitCskhUpdateRequest(uiRef);
                }
            };
            contentCard.Controls.Add(btnCskhUpdateRequest);
            contentCard.Controls.Add(searchArea);

            Label lblPropertyTitle = new Label
            {
                Text = "Thông tin chi tiết khách hàng",
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(58, 72, 101),
                Location = new Point(22, 220),
                AutoSize = true
            };
            contentCard.Controls.Add(lblPropertyTitle);

            DataGridView dgvCustomerDetails = BuildGrid(new Rectangle(22, 244, canvasWidth - 364, 290));
            dgvCustomerDetails.DefaultCellStyle.Font = new Font("Bahnschrift", 9, FontStyle.Regular);
            dgvCustomerDetails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvCustomerDetails.ScrollBars = ScrollBars.Both; // Đảm bảo bật thanh cuộn
            contentCard.Controls.Add(dgvCustomerDetails);

            // =====================================
            // KHU VỰC GIAO DIỆN DEV / TEST
            // =====================================
            Panel devHeaderCard = CreateCardPanel(new Rectangle(0, 0, canvasWidth, 90), Color.White);
            devHeaderCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label devTitle = new Label { Text = "STATIC DATA MASKING (DEV / TEST)", Font = new Font("Bahnschrift SemiBold", 16, FontStyle.Bold), ForeColor = Color.FromArgb(31, 43, 74), Location = new Point(20, 20), AutoSize = true };
            devHeaderCard.Controls.Add(devTitle);

            Label devSub = new Label { Text = "Trích xuất dữ liệu mẫu đã được làm mờ và mã hóa cho môi trường kiểm thử.", Font = new Font("Bahnschrift", 10, FontStyle.Regular), ForeColor = Color.FromArgb(92, 106, 136), Location = new Point(22, 52), AutoSize = true };
            devHeaderCard.Controls.Add(devSub);

            Button btnExport = new Button
            {
                Text = "Tải Dữ liệu (Preview)",
                Location = new Point(canvasWidth - 380, 24),
                Width = 160,
                Height = 42,
                BackColor = Color.FromArgb(232, 101, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += async (s, e) => await onExportClick();
            devHeaderCard.Controls.Add(btnExport);

            Button btnSaveFile = new Button
            {
                Text = "Lưu file (.CSV)",
                Location = new Point(canvasWidth - 210, 24),
                Width = 150,
                Height = 42,
                BackColor = Color.FromArgb(34, 166, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveFile.FlatAppearance.BorderSize = 0;
            btnSaveFile.Click += onSaveCsvClick;
            devHeaderCard.Controls.Add(btnSaveFile);

            int cardW = (canvasWidth - 32) / 3;
            Panel infoCard1 = CreateCardPanel(new Rectangle(0, 106, cardW, 80), Color.FromArgb(246, 248, 252));
            infoCard1.Controls.Add(new Label { Text = "Mô hình bảo mật", Font = new Font("Bahnschrift", 9), ForeColor = Color.FromArgb(99, 110, 136), Location = new Point(16, 16), AutoSize = true });
            infoCard1.Controls.Add(new Label { Text = "Lai ghép RSA + AES-256", Font = new Font("Bahnschrift SemiBold", 12, FontStyle.Bold), ForeColor = Color.FromArgb(31, 43, 74), Location = new Point(16, 40), AutoSize = true });

            Panel infoCard2 = CreateCardPanel(new Rectangle(cardW + 16, 106, cardW, 80), Color.FromArgb(246, 248, 252));
            infoCard2.Controls.Add(new Label { Text = "Quy tắc làm mờ (Masking)", Font = new Font("Bahnschrift", 9), ForeColor = Color.FromArgb(99, 110, 136), Location = new Point(16, 16), AutoSize = true });
            infoCard2.Controls.Add(new Label { Text = "Data Redaction & Substitution", Font = new Font("Bahnschrift SemiBold", 12, FontStyle.Bold), ForeColor = Color.FromArgb(31, 43, 74), Location = new Point(16, 40), AutoSize = true });

            Panel infoCard3 = CreateCardPanel(new Rectangle((cardW + 16) * 2, 106, cardW, 80), Color.FromArgb(246, 248, 252));
            infoCard3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            infoCard3.Controls.Add(new Label { Text = "Môi trường đích", Font = new Font("Bahnschrift", 9), ForeColor = Color.FromArgb(99, 110, 136), Location = new Point(16, 16), AutoSize = true });
            infoCard3.Controls.Add(new Label { Text = "QA / Staging (Non-Prod)", Font = new Font("Bahnschrift SemiBold", 12, FontStyle.Bold), ForeColor = Color.FromArgb(31, 43, 74), Location = new Point(16, 40), AutoSize = true });

            Panel devTableCard = CreateCardPanel(new Rectangle(0, 202, canvasWidth, 350), Color.White);
            devTableCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            devTableCard.Controls.Add(new Label { Text = "Bản xem trước Dữ liệu mẫu (Preview)", Font = new Font("Bahnschrift SemiBold", 13, FontStyle.Bold), ForeColor = Color.FromArgb(36, 51, 85), Location = new Point(20, 18), AutoSize = true });

            DataGridView dgvDev = BuildGrid(new Rectangle(20, 52, canvasWidth - 40, 278));
            dgvDev.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvDev.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvDev.ScrollBars = ScrollBars.Both;
            devTableCard.Controls.Add(dgvDev);

            pnlCSKH.Controls.Add(profileCard);
            pnlCSKH.Controls.Add(contentCard);

            pnlDEV.Controls.Add(devHeaderCard);
            pnlDEV.Controls.Add(infoCard1);
            pnlDEV.Controls.Add(infoCard2);
            pnlDEV.Controls.Add(infoCard3);
            pnlDEV.Controls.Add(devTableCard);

            Panel consoleCard = CreateCardPanel(new Rectangle(0, 0, canvasWidth, 136), Color.FromArgb(6, 16, 35));
            consoleCard.Dock = DockStyle.Bottom;
            consoleCard.Visible = false;

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
                Size = new Size(canvasWidth - 28, 88),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(6, 16, 35),
                ForeColor = Color.FromArgb(115, 249, 182),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            consoleCard.Controls.Add(txtConsole);

            Panel spacer = new Panel { Dock = DockStyle.Bottom, Height = 16, BackColor = Color.Transparent, Visible = false };

            canvas.Controls.Add(consoleCard);
            canvas.Controls.Add(spacer);
            canvas.Controls.Add(pnlCSKH);
            canvas.Controls.Add(pnlDEV);

            content.Controls.Add(canvas);
            content.Controls.Add(topBar);

            host.Controls.Add(content);

            uiRef = new MainFormUiComponents
            {
                PnlCSKH = pnlCSKH,
                PnlDEV = pnlDEV,
                PnlSearchArea = searchArea,
                TxtSearchID = txtSearchID,
                BtnSearch = btnSearch,
                BtnExport = btnExport,
                BtnSaveFile = btnSaveFile,
                BtnLogout = btnLogout,
                LblRole = lblRole,
                TxtConsole = txtConsole,
                DgvDev = dgvDev,
                DgvCustomerDetails = dgvCustomerDetails,
                LblDetailsTitle = lblPropertyTitle,
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
                BtnCskhUpdateRequest = btnCskhUpdateRequest,
            };

            return uiRef;
        }

        private sealed class CskhUpdateRequestDialog : Form
        {
            private readonly TextBox _txtReason;
            private readonly TextBox _txtHoTen;
            private readonly TextBox _txtNgaySinh;
            private readonly ComboBox _cboGioiTinh;
            private readonly TextBox _txtQuocTich;
            private readonly TextBox _txtDiaChi;
            private readonly TextBox _txtPhone;
            private readonly TextBox _txtEmail;
            private readonly TextBox _txtCccd;

            public string RequestReason => _txtReason.Text.Trim();
            public string HoTen => _txtHoTen.Text.Trim();
            public string NgaySinh => _txtNgaySinh.Text.Trim();
            public int? GioiTinh => _cboGioiTinh.SelectedIndex switch { 1 => 0, 2 => 1, _ => null };
            public string QuocTich => _txtQuocTich.Text.Trim();
            public string DiaChiNha => _txtDiaChi.Text.Trim();
            public string SoDienThoai => _txtPhone.Text.Trim();
            public string Email => _txtEmail.Text.Trim();
            public string SoCCCD => _txtCccd.Text.Trim();

            public CskhUpdateRequestDialog()
            {
                Text = "CSKH - Cập nhật thông tin khách hàng";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                Width = 620;
                Height = 560;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 11,
                    Padding = new Padding(14)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                for (int i = 0; i < 10; i++)
                {
                    layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                }
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                _txtReason = NewTextBox();
                _txtHoTen = NewTextBox();
                _txtNgaySinh = NewTextBox("yyyy-MM-dd");
                _cboGioiTinh = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
                _cboGioiTinh.Items.AddRange(new[] { "(Không đổi)", "Nam", "Nữ" });
                _cboGioiTinh.SelectedIndex = 0;
                _txtQuocTich = NewTextBox();
                _txtDiaChi = NewTextBox();
                _txtPhone = NewTextBox();
                _txtEmail = NewTextBox();
                _txtCccd = NewTextBox();

                AddRow(layout, 0, "Ghi chú", _txtReason);
                AddRow(layout, 1, "Họ tên mới", _txtHoTen);
                AddRow(layout, 2, "Ngày sinh mới", _txtNgaySinh);
                AddRow(layout, 3, "Giới tính mới", _cboGioiTinh);
                AddRow(layout, 4, "Quốc tịch mới", _txtQuocTich);
                AddRow(layout, 5, "Địa chỉ mới", _txtDiaChi);
                AddRow(layout, 6, "SĐT mới", _txtPhone);
                AddRow(layout, 7, "Email mới", _txtEmail);
                AddRow(layout, 8, "CCCD mới", _txtCccd);

                var hint = new Label
                {
                    Text = "Điền các trường cần sửa, trường để trống sẽ được giữ nguyên.",
                    AutoSize = true,
                    ForeColor = Color.FromArgb(92, 106, 136)
                };
                layout.Controls.Add(hint, 1, 9);

                var pnlButtons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft
                };
                var btnSend = new Button { Text = "Cập nhật", Width = 120, Height = 32 };
                var btnCancel = new Button { Text = "Hủy", Width = 90, Height = 32 };
                btnSend.Click += (s, e) =>
                {
                    bool hasUpdate = !string.IsNullOrWhiteSpace(HoTen)
                        || !string.IsNullOrWhiteSpace(NgaySinh)
                        || GioiTinh.HasValue
                        || !string.IsNullOrWhiteSpace(QuocTich)
                        || !string.IsNullOrWhiteSpace(DiaChiNha)
                        || !string.IsNullOrWhiteSpace(SoDienThoai)
                        || !string.IsNullOrWhiteSpace(Email)
                        || !string.IsNullOrWhiteSpace(SoCCCD);

                    if (!hasUpdate)
                    {
                        MessageBox.Show("Bạn chưa nhập trường nào cần chỉnh sửa.", "Thiếu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DialogResult = DialogResult.OK;
                    Close();
                };
                btnCancel.Click += (s, e) =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                };

                pnlButtons.Controls.Add(btnSend);
                pnlButtons.Controls.Add(btnCancel);
                layout.Controls.Add(pnlButtons, 1, 10);

                Controls.Add(layout);
            }

            private static TextBox NewTextBox(string placeholder = "")
            {
                var textBox = new TextBox { Width = 360 };
                if (!string.IsNullOrWhiteSpace(placeholder))
                {
                    textBox.PlaceholderText = placeholder;
                }
                return textBox;
            }

            private static void AddRow(TableLayoutPanel layout, int row, string labelText, Control editor)
            {
                var label = new Label
                {
                    Text = labelText,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    ForeColor = Color.FromArgb(58, 72, 101)
                };

                layout.Controls.Add(label, 0, row);
                editor.Anchor = AnchorStyles.Left;
                layout.Controls.Add(editor, 1, row);
            }
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
                Padding = new Padding(6, 0, 0, 0)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(33, 43, 54),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                SelectionBackColor = Color.FromArgb(244, 246, 248),
                SelectionForeColor = Color.FromArgb(33, 43, 54),
                Padding = new Padding(6, 0, 0, 0)
            };

            return grid;
        }

        private static void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            Control? card = sender as Control;
            if (card == null) return;

            using (Pen borderPen = new Pen(Color.FromArgb(66, 89, 118)))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
            }
        }
    }
}