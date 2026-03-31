using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
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
        public string Id { get; set; } = "";
        public string MaKh { get; set; } = "";
        public string MaskedName { get; set; } = "";
        public string MaskedEmail { get; set; } = "";
        public string CipherCccd { get; set; } = "";
    }

    public class DevExportResponse
    {
        public List<DevExportRowDto> Rows { get; set; } = new();
        public string? ConsoleMessage { get; set; }
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
            "Tài khoản chính (Mask)",
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
                        // Adjust column headers if needed
                        if (ui.DgvDev.Columns.Count > 0)
                        {
                            ui.DgvDev.Columns["Id"].HeaderText = "ID";
                            ui.DgvDev.Columns["MaKh"].HeaderText = "Mã KH";
                            ui.DgvDev.Columns["MaskedName"].HeaderText = "Tên (Masked)";
                            ui.DgvDev.Columns["MaskedEmail"].HeaderText = "Email (Masked)";
                            ui.DgvDev.Columns["CipherCccd"].HeaderText = "CCCD (AES Encrypted)";
                        }
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

            // Update details grid
            ui.DgvCustomerDetails.DataSource = null;
            ui.DgvCustomerDetails.Rows.Clear();
            ui.DgvCustomerDetails.Columns.Clear();
            ui.DgvCustomerDetails.Columns.Add("LeftCategory", "Hạng Mục");
            ui.DgvCustomerDetails.Columns.Add("LeftDetail", "Chi tiết");
            ui.DgvCustomerDetails.Columns.Add("RightCategory", "Hạng Mục (Tiếp)");
            ui.DgvCustomerDetails.Columns.Add("RightDetail", "Chi tiết (Tiếp)");

            ui.DgvCustomerDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            int categoryWidth = 200;
            int detailWidth = 220;

            ui.DgvCustomerDetails.Columns["LeftCategory"].Width = categoryWidth;
            ui.DgvCustomerDetails.Columns["LeftDetail"].Width = detailWidth;
            ui.DgvCustomerDetails.Columns["RightCategory"].Width = categoryWidth;
            ui.DgvCustomerDetails.Columns["RightDetail"].Width = detailWidth;

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
            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

        public static MainFormUiComponents Build(Form host, Func<Task> onSearchClick, Func<Task> onExportClick, EventHandler onLogoutClick)
        {
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
                Text = "Số thẻ (PCI-DSS Masked)",
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
                Text = "Tìm kiếm",
                Location = new Point(440, 170),
                Width = 150,
                Height = 34,
                BackColor = Color.FromArgb(20, 123, 197),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiBold", 10, FontStyle.Bold)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += async (s, e) => await onSearchClick();
            contentCard.Controls.Add(btnSearch);

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
            dgvDev.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
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

            return new MainFormUiComponents
            {
                PnlCSKH = pnlCSKH,
                PnlDEV = pnlDEV,
                TxtSearchID = txtSearchID,
                BtnSearch = btnSearch,
                BtnExport = btnExport,
                BtnSaveFile = btnSaveFile,
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