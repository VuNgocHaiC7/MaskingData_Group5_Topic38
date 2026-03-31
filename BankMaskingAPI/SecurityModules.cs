using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace BankMaskingAPI
{
    public sealed class CskhSearchResponse
    {
        public bool Found { get; set; }
        public string DisplayText { get; set; } = "";
        public string? ChannelLogMessage { get; set; }

        public string? PortraitImage { get; set; }
        public string MaskedCard { get; set; } = "**** **** **** ****";
        public string CurrentBalance { get; set; } = "0 VNĐ";
        public Dictionary<string, string?> DbFields { get; set; } = new();
    }

    public sealed class DevExportRowDto
    {
        public string Id { get; set; } = "";
        public string MaKh { get; set; } = "";
        public string MaskedName { get; set; } = "";
        public string MaskedEmail { get; set; } = "";
        public string CipherCccd { get; set; } = "";
    }

    public sealed class DevExportResponse
    {
        public List<DevExportRowDto> Rows { get; set; } = new();
        public string? ConsoleMessage { get; set; }
    }

    public sealed class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Role { get; set; } = "";
        public string Token { get; set; } = "";
        public DateTime? ExpiresAtUtc { get; set; }
    }

    public sealed class CustomerManageDto
    {
        public long CustomerId { get; set; }
        public string MaKhachHang { get; set; } = "";
        public string MaskedName { get; set; } = "";
        public string MaskedPhone { get; set; } = "";
        public string MaskedEmail { get; set; } = "";
        public string MaskedCccd { get; set; } = "";
        public int RoleId { get; set; }
    }

    public sealed class CustomerManageListResponse
    {
        public List<CustomerManageDto> Rows { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }


    // TODO: create (but FrontEnd don't call)
    public sealed class CustomerManageUpsertRequest
    {
        public string MaKhachHang { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string Email { get; set; } = "";
        public string SoCCCD { get; set; } = "";
        public string SoTaiKhoan { get; set; } = "";
        public string SoThe { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int RoleId { get; set; } = 1;
    }

    // TODO: update respone (but FrontEnd don't call)
    public sealed class OperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public sealed class SecurityModules
    {
        private readonly string _connectionString;
        private readonly string _dataEncryptionSecret;
        private readonly string _searchIndexSecret;
        private readonly string? _configurationError;

        public SecurityModules(IConfiguration configuration)
        {
            string host = configuration["DbSettings:Host"] ?? "127.0.0.1";
            string port = configuration["DbSettings:Port"] ?? "3306";
            string database = configuration["DbSettings:Database"] ?? "BankSystemMasking";
            string userEnv = configuration["DbSettings:UserEnv"] ?? "MASK_DB_USER";
            string passwordEnv = configuration["DbSettings:PasswordEnv"] ?? "MASK_DB_PASSWORD";
            string dataKeyEnv = configuration["DbSettings:DataKeyEnv"] ?? "MASK_DATA_KEY";
            string searchIndexKeyEnv = configuration["DbSettings:SearchIndexKeyEnv"] ?? "MASK_SEARCH_INDEX_KEY";
            string sslMode = configuration["DbSettings:SslMode"] ?? "Preferred";
            string allowPublicKeyRetrieval = configuration["DbSettings:AllowPublicKeyRetrieval"] ?? "true";

            string? dbUser = Environment.GetEnvironmentVariable(userEnv);
            string? dbPassword = Environment.GetEnvironmentVariable(passwordEnv);

            if (string.IsNullOrWhiteSpace(dbUser))
            {
                dbUser = configuration["DbSettings:User"];
            }

            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                dbPassword = configuration["DbSettings:Password"];
            }

            _dataEncryptionSecret = Environment.GetEnvironmentVariable(dataKeyEnv) ?? "MASKING_DEMO_KEY_CHANGE_ME";
            _searchIndexSecret = Environment.GetEnvironmentVariable(searchIndexKeyEnv) ?? (_dataEncryptionSecret + "_IDX");

            if (string.IsNullOrWhiteSpace(dbUser) || string.IsNullOrWhiteSpace(dbPassword))
            {
                _configurationError =
                    $"Missing database credentials. Set env vars '{userEnv}'/'{passwordEnv}' or DbSettings:User/DbSettings:Password.";
                _connectionString = string.Empty;
                return;
            }

            _connectionString =
                $"Server={host};Port={port};Database={database};Uid={dbUser};Pwd={dbPassword};" +
                $"SslMode={sslMode};AllowPublicKeyRetrieval={allowPublicKeyRetrieval};";
        }

        // 
        public void EnsureSensitiveDataEncryptedAtRest()
        {
            EnsureDatabaseConfigured();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            var updates = new List<(long Id, string Phone, string Email, string Cccd, string Account, string Card, string PhoneIdx, string CccdIdx, string AccountIdx, string CardIdx)>();

            const string scanSql = @"SELECT CustomerID, SoDienThoai, Email, SoCCCD, SoTaiKhoan, SoThe,
                                            SoDienThoaiIdx, SoCCCDIdx, SoTaiKhoanIdx, SoTheIdx
                                     FROM Customers";
            using (var cmd = new MySqlCommand(scanSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    long id = reader["CustomerID"] == DBNull.Value ? 0L : Convert.ToInt64(reader["CustomerID"]);
                    if (id <= 0)
                    {
                        continue;
                    }

                    bool changed = false;

                    string phone = NormalizeEncryptedField(reader["SoDienThoai"]?.ToString(), SensitiveFieldKind.Phone, ref changed);
                    string email = NormalizeEncryptedField(reader["Email"]?.ToString(), SensitiveFieldKind.Email, ref changed);
                    string cccd = NormalizeEncryptedField(reader["SoCCCD"]?.ToString(), SensitiveFieldKind.Cccd, ref changed);
                    string account = NormalizeEncryptedField(reader["SoTaiKhoan"]?.ToString(), SensitiveFieldKind.Account, ref changed);
                    string card = NormalizeEncryptedField(reader["SoThe"]?.ToString(), SensitiveFieldKind.Card, ref changed);

                    string phoneIdx = ComputeSearchIndex(DecryptIfNeeded(phone));
                    string cccdIdx = ComputeSearchIndex(DecryptIfNeeded(cccd));
                    string accountIdx = ComputeSearchIndex(DecryptIfNeeded(account));
                    string cardIdx = ComputeSearchIndex(DecryptIfNeeded(card));

                    string currentPhoneIdx = reader["SoDienThoaiIdx"]?.ToString() ?? string.Empty;
                    string currentCccdIdx = reader["SoCCCDIdx"]?.ToString() ?? string.Empty;
                    string currentAccountIdx = reader["SoTaiKhoanIdx"]?.ToString() ?? string.Empty;
                    string currentCardIdx = reader["SoTheIdx"]?.ToString() ?? string.Empty;

                    changed = changed
                        || !string.Equals(phoneIdx, currentPhoneIdx, StringComparison.Ordinal)
                        || !string.Equals(cccdIdx, currentCccdIdx, StringComparison.Ordinal)
                        || !string.Equals(accountIdx, currentAccountIdx, StringComparison.Ordinal)
                        || !string.Equals(cardIdx, currentCardIdx, StringComparison.Ordinal);

                    if (changed)
                    {
                        updates.Add((id, phone, email, cccd, account, card, phoneIdx, cccdIdx, accountIdx, cardIdx));
                    }
                }
            }

            if (updates.Count == 0)
            {
                return;
            }

            const string updateSql = @"UPDATE Customers
                                       SET SoDienThoai = @phone,
                                           Email = @email,
                                           SoCCCD = @cccd,
                                           SoTaiKhoan = @account,
                                           SoThe = @card,
                                           SoDienThoaiIdx = @phoneIdx,
                                           SoCCCDIdx = @cccdIdx,
                                           SoTaiKhoanIdx = @accountIdx,
                                           SoTheIdx = @cardIdx
                                       WHERE CustomerID = @id";

            foreach (var item in updates)
            {
                using var updateCmd = new MySqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(item.Phone) ? DBNull.Value : item.Phone);
                updateCmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(item.Email) ? DBNull.Value : item.Email);
                updateCmd.Parameters.AddWithValue("@cccd", string.IsNullOrWhiteSpace(item.Cccd) ? DBNull.Value : item.Cccd);
                updateCmd.Parameters.AddWithValue("@account", string.IsNullOrWhiteSpace(item.Account) ? DBNull.Value : item.Account);
                updateCmd.Parameters.AddWithValue("@card", string.IsNullOrWhiteSpace(item.Card) ? DBNull.Value : item.Card);
                updateCmd.Parameters.AddWithValue("@phoneIdx", string.IsNullOrWhiteSpace(item.PhoneIdx) ? DBNull.Value : item.PhoneIdx);
                updateCmd.Parameters.AddWithValue("@cccdIdx", string.IsNullOrWhiteSpace(item.CccdIdx) ? DBNull.Value : item.CccdIdx);
                updateCmd.Parameters.AddWithValue("@accountIdx", string.IsNullOrWhiteSpace(item.AccountIdx) ? DBNull.Value : item.AccountIdx);
                updateCmd.Parameters.AddWithValue("@cardIdx", string.IsNullOrWhiteSpace(item.CardIdx) ? DBNull.Value : item.CardIdx);
                updateCmd.Parameters.AddWithValue("@id", item.Id);
                updateCmd.ExecuteNonQuery();
            }
        }

        public CskhSearchResponse SearchCustomerCskh(int searchType, string keyword)
        {
            //Check if you have sufficient database connection information
            EnsureDatabaseConfigured();

            var response = new CskhSearchResponse();
            response.DbFields = new Dictionary<string, string?>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            char[] name = Array.Empty<char>();
            char[] phone = Array.Empty<char>();
            char[] acc = Array.Empty<char>();
            char[] cccd = Array.Empty<char>();
            char[] email = Array.Empty<char>();
            char[] maskedName = Array.Empty<char>();
            char[] maskedPhone = Array.Empty<char>();
            char[] maskedAcc = Array.Empty<char>();
            char[] maskedCccd = Array.Empty<char>();
            char[] maskedEmail = Array.Empty<char>();
            char[] channelSessionKey = Array.Empty<char>();
            char[] unwrappedSessionKey = Array.Empty<char>();
            char[] encPhone = Array.Empty<char>();
            char[] encAcc = Array.Empty<char>();
            char[] cardPlain = Array.Empty<char>();
            char[] maskedCard = Array.Empty<char>();

            try
            {

                string normalizedKeyword = (keyword ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normalizedKeyword))
                {
                    response.Found = false;
                    response.DisplayText = "Không tìm thấy khách hàng!";
                    return response;
                }

                string normalizedLookupValue = NormalizeSearchKeyword(normalizedKeyword);
                long customerId = TryFindCustomerIdBySearchIndex(conn, searchType, normalizedLookupValue);
                if (customerId <= 0)
                {
                    string encryptedKeyword = EncryptForStorage(normalizedKeyword);
                    customerId = TryFindCustomerIdByDirectMatch(conn, searchType, normalizedKeyword, encryptedKeyword);
                    if (customerId <= 0)
                    {
                        customerId = TryFindCustomerIdByDecryptedScan(conn, searchType, normalizedKeyword);
                    }
                }

                if (customerId <= 0)
                {
                    response.Found = false;
                    response.DisplayText = "Không tìm thấy khách hàng!";
                    return response;
                }

                const string selectedColumns = @"SELECT CustomerID, HinhAnhChanDung, HoTen, NgaySinh, GioiTinh, SoCCCD,
                                       MaKhachHang, QuocTich, SoDienThoai, Email, DiaChiNha,
                                       SoTaiKhoan, SoThe, SoDu, DuNoHienTai, LoaiTaiKhoan, TrangThaiThe, TenDangNhap
                                             FROM Customers";

                using var loadCmd = new MySqlCommand(selectedColumns + " WHERE CustomerID = @id LIMIT 1", conn);
                loadCmd.Parameters.AddWithValue("@id", customerId);
                using var reader = loadCmd.ExecuteReader();
                if (!reader.Read())
                {
                    response.Found = false;
                    response.DisplayText = "Không tìm thấy khách hàng!";
                    return response;
                }

                string hinhAnh = reader["HinhAnhChanDung"]?.ToString() ?? string.Empty;
                response.PortraitImage = hinhAnh;

                string hoTenRaw = reader["HoTen"]?.ToString() ?? string.Empty;
                string soDienThoaiRaw = DecryptIfNeeded(reader["SoDienThoai"]?.ToString());
                string soCccdRaw = DecryptIfNeeded(reader["SoCCCD"]?.ToString());
                string soTaiKhoanRaw = DecryptIfNeeded(reader["SoTaiKhoan"]?.ToString());
                string emailRaw = DecryptIfNeeded(reader["Email"]?.ToString());

                name = CustomStringHelper.ToCharArray(hoTenRaw);
                phone = CustomStringHelper.ToCharArray(soDienThoaiRaw);
                acc = CustomStringHelper.ToCharArray(soTaiKhoanRaw);
                cccd = CustomStringHelper.ToCharArray(soCccdRaw);
                email = CustomStringHelper.ToCharArray(emailRaw);

                maskedName = CustomDataMasker.MaskFullName(name);
                maskedPhone = CustomDataMasker.MaskPhoneNumber(phone);
                maskedAcc = CustomDataMasker.MaskBankAccount(acc);
                maskedCccd = CustomDataMasker.MaskBankAccount(cccd);
                maskedEmail = CustomDataMasker.MaskEmail(email);

                // 1. Initialize a 32-byte random session key for AES
                channelSessionKey = CustomRSA.GenerateRandomSessionKey(32);

                // 2. Wrap the session key using the RSA algorithm (Key encryption)
                BigInteger rsaWrappedKey = CustomRSA.EncryptAESKey(channelSessionKey);

                // (Simulation of the key de-encapsulation process at the receiving end)
                unwrappedSessionKey = CustomRSA.DecryptAESKey(rsaWrappedKey);

                // 3. Encrypt the actual data (payload) using AES-256 with the session key just decrypted.
                encPhone = CustomAES.EncryptData(maskedPhone, unwrappedSessionKey);
                encAcc = CustomAES.EncryptData(maskedAcc, unwrappedSessionKey);

                response.ChannelLogMessage = $"[KÊNH TRUYỀN] RSA bọc khóa phiên AES. Dữ liệu truyền đã mã hóa.\nSĐT: {new string(encPhone)} | STK: {new string(encAcc)}";

                string gioiTinh = (reader["GioiTinh"]?.ToString() ?? "") switch { "0" => "Nam", "1" => "Nữ", _ => "Không rõ" };
                string loaiTaiKhoan = (reader["LoaiTaiKhoan"]?.ToString() ?? "") switch { "1" => "Thường", "2" => "VIP", "3" => "Premium", _ => "Không rõ" };
                string trangThaiThe = (reader["TrangThaiThe"]?.ToString() ?? "") switch { "0" => "Khoá", "1" => "Hoạt động", "2" => "Hết hạn", _ => "Không rõ" };

                response.DbFields.Add("ID Hệ Thống", reader["CustomerID"]?.ToString() ?? "");
                response.DbFields.Add("Mã KH", reader["MaKhachHang"]?.ToString() ?? "");
                response.DbFields.Add("Họ tên (Mask)", new string(maskedName));
                response.DbFields.Add("Ngày sinh", reader["NgaySinh"] == DBNull.Value ? "" : Convert.ToDateTime(reader["NgaySinh"]).ToString("dd/MM/yyyy"));
                response.DbFields.Add("Giới tính", gioiTinh);
                response.DbFields.Add("CCCD (Mask)", new string(maskedCccd));
                response.DbFields.Add("Quốc tịch", reader["QuocTich"]?.ToString() ?? "");

                response.DbFields.Add("SĐT (Mask)", new string(maskedPhone));
                response.DbFields.Add("Email (Mask)", new string(maskedEmail));
                response.DbFields.Add("Địa chỉ nhà", reader["DiaChiNha"]?.ToString() ?? "");

                response.DbFields.Add("Tài khoản chính (Mask)", new string(maskedAcc));
                response.DbFields.Add("Dư nợ hiện tại", $"{reader["DuNoHienTai"]} VNĐ");
                response.DbFields.Add("Loại tài khoản", loaiTaiKhoan);

                response.DbFields.Add("Trạng thái thẻ", trangThaiThe);
                response.DbFields.Add("Tên đăng nhập", reader["TenDangNhap"]?.ToString() ?? "");

                if (reader["SoThe"] == DBNull.Value)
                {
                    response.MaskedCard = "**** **** **** ****";
                }
                else
                {
                    cardPlain = CustomStringHelper.ToCharArray(DecryptIfNeeded(reader["SoThe"]?.ToString()));
                    maskedCard = CustomDataMasker.MaskBankAccount(cardPlain);
                    response.MaskedCard = new string(maskedCard);
                }

                response.CurrentBalance = $"{reader["SoDu"]} VNĐ";

                var resultBuilder = new StringBuilder();
                resultBuilder.AppendLine($"Đã tìm thấy khách hàng: {new string(maskedName)}");
                resultBuilder.AppendLine("Trạng thái dữ liệu: [Đã áp dụng Masking & Encryption]");
                resultBuilder.AppendLine("Tất cả dữ liệu chi tiết đã được đẩy xuống bảng thuộc tính bên dưới.");

                response.Found = true;
                response.DisplayText = resultBuilder.ToString();

                return response;
            }
            finally
            {
                WipeCharArrays(name, phone, acc, cccd, email,
                    maskedName, maskedPhone, maskedAcc, maskedCccd, maskedEmail,
                    channelSessionKey, unwrappedSessionKey, encPhone, encAcc,
                    cardPlain, maskedCard);
            }
        }

        private long TryFindCustomerIdBySearchIndex(MySqlConnection conn, int searchType, string normalizedKeyword)
        {
            if (searchType < 0 || searchType > 2)
            {
                return 0;
            }

            string keywordIdx = ComputeSearchIndex(normalizedKeyword);
            if (string.IsNullOrWhiteSpace(keywordIdx))
            {
                return 0;
            }

            string sql = searchType switch
            {
                0 => "SELECT CustomerID FROM Customers WHERE SoDienThoaiIdx = @idx LIMIT 1",
                1 => "SELECT CustomerID FROM Customers WHERE SoCCCDIdx = @idx LIMIT 1",
                2 => "SELECT CustomerID FROM Customers WHERE SoTaiKhoanIdx = @idx OR SoTheIdx = @idx LIMIT 1",
                _ => "SELECT 0"
            };

            try
            {
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idx", keywordIdx);
                object? value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt64(value);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                // Unknown column: database chưa chạy migration index.
                return 0;
            }
        }

        private long TryFindCustomerIdByDirectMatch(MySqlConnection conn, int searchType, string plainKeyword, string encryptedKeyword)
        {
            using var cmd = BuildCustomerSearchCommand(conn, searchType, plainKeyword, encryptedKeyword);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return 0;
            }

            return reader["CustomerID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["CustomerID"]);
        }

        private long TryFindCustomerIdByDecryptedScan(MySqlConnection conn, int searchType, string keyword)
        {
            if (searchType < 0 || searchType > 2)
            {
                return 0;
            }

            const string sql = @"SELECT CustomerID, SoDienThoai, SoCCCD, SoTaiKhoan, SoThe FROM Customers";
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            string normalizedKeyword = NormalizeSearchKeyword(keyword);
            while (reader.Read())
            {
                bool matched = searchType switch
                {
                    0 => IsSearchMatch(reader["SoDienThoai"]?.ToString(), normalizedKeyword),
                    1 => IsSearchMatch(reader["SoCCCD"]?.ToString(), normalizedKeyword),
                    2 => IsSearchMatch(reader["SoTaiKhoan"]?.ToString(), normalizedKeyword)
                        || IsSearchMatch(reader["SoThe"]?.ToString(), normalizedKeyword),
                    _ => false
                };

                if (!matched)
                {
                    continue;
                }

                return reader["CustomerID"] == DBNull.Value ? 0 : Convert.ToInt64(reader["CustomerID"]);
            }

            return 0;
        }

        private bool IsSearchMatch(string? dbValue, string normalizedKeyword)
        {
            string plainValue = DecryptIfNeeded(dbValue);
            return string.Equals(NormalizeSearchKeyword(plainValue), normalizedKeyword, StringComparison.OrdinalIgnoreCase);
        }

        private static void WipeCharArrays(params char[][] buffers)
        {
            if (buffers == null)
            {
                return;
            }

            for (int i = 0; i < buffers.Length; i++)
            {
                char[]? buffer = buffers[i];
                if (buffer == null || buffer.Length == 0)
                {
                    continue;
                }

                Array.Clear(buffer, 0, buffer.Length);
            }
        }

        private static string NormalizeSearchKeyword(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(".", string.Empty)
                .ToLowerInvariant();
        }

        private string ComputeSearchIndex(string? plainValue)
        {
            string normalized = NormalizeSearchKeyword(plainValue);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            byte[] keyBytes = Encoding.UTF8.GetBytes(_searchIndexSecret);
            byte[] payload = Encoding.UTF8.GetBytes(normalized);
            using var hmac = new HMACSHA256(keyBytes);
            byte[] digest = hmac.ComputeHash(payload);

            var builder = new StringBuilder(digest.Length * 2);
            for (int i = 0; i < digest.Length; i++)
            {
                builder.Append(digest[i].ToString("X2"));
            }

            return builder.ToString();
        }

        private static MySqlCommand BuildCustomerSearchCommand(MySqlConnection conn, int searchType, string plainKeyword, string encryptedKeyword)
        {
                 const string selectedColumns = @"SELECT CustomerID, HinhAnhChanDung, HoTen, NgaySinh, GioiTinh, SoCCCD,
                                       MaKhachHang, QuocTich, SoDienThoai, Email, DiaChiNha,
                                       SoTaiKhoan, SoThe, SoDu, DuNoHienTai, LoaiTaiKhoan, TrangThaiThe, TenDangNhap
                                             FROM Customers";

            var cmd = new MySqlCommand { Connection = conn };

            switch (searchType)
            {
                case 0:
                    cmd.CommandText = selectedColumns + " WHERE SoDienThoai = @plain OR SoDienThoai = @enc LIMIT 1";
                    cmd.Parameters.AddWithValue("@plain", plainKeyword);
                    cmd.Parameters.AddWithValue("@enc", encryptedKeyword);
                    break;

                case 1:
                    cmd.CommandText = selectedColumns + " WHERE SoCCCD = @plain OR SoCCCD = @enc LIMIT 1";
                    cmd.Parameters.AddWithValue("@plain", plainKeyword);
                    cmd.Parameters.AddWithValue("@enc", encryptedKeyword);
                    break;

                case 2:
                    cmd.CommandText = selectedColumns + " WHERE SoTaiKhoan = @plain OR SoTaiKhoan = @enc OR SoThe = @plain OR SoThe = @enc LIMIT 1";
                    cmd.Parameters.AddWithValue("@plain", plainKeyword);
                    cmd.Parameters.AddWithValue("@enc", encryptedKeyword);
                    break;

                default:
                    if (long.TryParse(plainKeyword, out long idValue))
                    {
                        cmd.CommandText = selectedColumns + " WHERE CustomerID = @id LIMIT 1";
                        cmd.Parameters.AddWithValue("@id", idValue);
                    }
                    else
                    {
                        cmd.CommandText = selectedColumns + " WHERE 1 = 0";
                    }

                    break;
            }

            return cmd;
        }

        public DevExportResponse ExportDevSample()
        {
            EnsureDatabaseConfigured();

            var result = new DevExportResponse();
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT CustomerID, MaKhachHang, HoTen, Email, SoCCCD FROM Customers LIMIT 10", conn);
            using var reader = cmd.ExecuteReader();

            char[] aesSessionKey = CustomRSA.GenerateRandomSessionKey(32);
            BigInteger encryptedAesKey = CustomRSA.EncryptAESKey(aesSessionKey);
            char[] decryptedAesKey = CustomRSA.DecryptAESKey(encryptedAesKey);

            while (reader.Read())
            {
                char[] name = CustomStringHelper.ToCharArray(reader["HoTen"]?.ToString() ?? "");
                char[] maskedName = CustomDataMasker.MaskFullName(name);

                string plainEmail = DecryptIfNeeded(reader["Email"]?.ToString());
                char[] email = CustomStringHelper.ToCharArray(plainEmail);
                char[] maskedEmail = CustomDataMasker.MaskEmail(email);

                string plainCccd = DecryptIfNeeded(reader["SoCCCD"]?.ToString());
                char[] cccd = CustomStringHelper.ToCharArray(plainCccd);
                char[] cipherCccd = CustomAES.EncryptData(cccd, decryptedAesKey);

                result.Rows.Add(new DevExportRowDto
                {
                    Id = reader["CustomerID"]?.ToString() ?? "",
                    MaKh = reader["MaKhachHang"]?.ToString() ?? "",
                    MaskedName = new string(maskedName),
                    MaskedEmail = new string(maskedEmail),
                    CipherCccd = new string(cipherCccd)
                });
            }

            result.ConsoleMessage = "[RSA] Đã tạo và trao đổi khóa phiên AES 256-bit.\n[DEV] Áp dụng Masking tĩnh (Họ tên, Email) và Mã hóa AES (CCCD).\n[DEV] Trích xuất tập dữ liệu mẫu thành công cho môi trường TEST.";

            return result;
        }

        public LoginResult ValidateOperatorLogin(string username, string password)
        {
            EnsureDatabaseConfigured();

            string normalizedUser = (username ?? string.Empty).Trim();
            string inputPassword = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(inputPassword))
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Thiếu thông tin đăng nhập."
                };
            }

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            const string sql = @"SELECT TenDangNhap, MatKhauHash, RoleID
                                 FROM Customers
                                 WHERE LOWER(TenDangNhap) = LOWER(@username)
                                 LIMIT 1";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", normalizedUser);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Tài khoản hoặc mật khẩu sai."
                };
            }

            int roleId = reader["RoleID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["RoleID"]);
            if (roleId != 2 && roleId != 3)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Tài khoản hoặc mật khẩu sai."
                };
            }

            string storedPasswordValue = reader["MatKhauHash"]?.ToString() ?? "";

            // Chuẩn chính: SHA-256 (hex). Giữ tương thích dữ liệu cũ dùng CustomHash.
            string inputSha256 = ComputeSha256Hex(inputPassword);
            string inputLegacyHash = new string(CustomHash.ComputeHash(CustomStringHelper.ToCharArray(inputPassword)));

            bool ok = string.Equals(inputSha256, storedPasswordValue, StringComparison.OrdinalIgnoreCase)
                || string.Equals(inputLegacyHash, storedPasswordValue, StringComparison.OrdinalIgnoreCase);

            return new LoginResult
            {
                Success = ok,
                Role = roleId == 2 ? "cskh" : "dev",
                Message = ok ? "Đăng nhập hợp lệ." : "Tài khoản hoặc mật khẩu sai."
            };
        }

        public CustomerManageListResponse GetCustomersForManagement(int page, int pageSize)
        {
            EnsureDatabaseConfigured();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var response = new CustomerManageListResponse
            {
                Page = page,
                PageSize = pageSize
            };

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using (var countCmd = new MySqlCommand("SELECT COUNT(*) FROM Customers", conn))
            {
                response.Total = Convert.ToInt32(countCmd.ExecuteScalar());
            }

            int offset = (page - 1) * pageSize;
            const string query = @"SELECT CustomerID, MaKhachHang, HoTen, SoDienThoai, Email, SoCCCD, RoleID
                                   FROM Customers
                                   ORDER BY CustomerID DESC
                                   LIMIT @limit OFFSET @offset";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                char[] name = CustomStringHelper.ToCharArray(reader["HoTen"]?.ToString() ?? "");
                char[] phone = CustomStringHelper.ToCharArray(DecryptIfNeeded(reader["SoDienThoai"]?.ToString()));
                char[] email = CustomStringHelper.ToCharArray(DecryptIfNeeded(reader["Email"]?.ToString()));
                char[] cccd = CustomStringHelper.ToCharArray(DecryptIfNeeded(reader["SoCCCD"]?.ToString()));

                response.Rows.Add(new CustomerManageDto
                {
                    CustomerId = Convert.ToInt64(reader["CustomerID"]),
                    MaKhachHang = reader["MaKhachHang"]?.ToString() ?? "",
                    MaskedName = new string(CustomDataMasker.MaskFullName(name)),
                    MaskedPhone = new string(CustomDataMasker.MaskPhoneNumber(phone)),
                    MaskedEmail = new string(CustomDataMasker.MaskEmail(email)),
                    MaskedCccd = new string(CustomDataMasker.MaskBankAccount(cccd)),
                    RoleId = reader["RoleID"] == DBNull.Value ? 1 : Convert.ToInt32(reader["RoleID"])
                });
            }

            return response;
        }

        public OperationResponse CreateCustomer(CustomerManageUpsertRequest req)
        {
            EnsureDatabaseConfigured();

            if (!ValidateUpsertRequest(req, out string validateError))
            {
                return new OperationResponse { Success = false, Message = validateError };
            }

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string username = ResolveUsernameForCreate(req);
            string passwordHash = ResolvePasswordHashForCreate(req);

            string encryptedPhone = EncryptForStorage(req.SoDienThoai.Trim());
            string encryptedEmail = EncryptForStorage(req.Email.Trim());
            string encryptedCccd = EncryptForStorage(req.SoCCCD.Trim());
            string encryptedAccount = EncryptForStorage((req.SoTaiKhoan ?? string.Empty).Trim());
            string encryptedCard = EncryptForStorage((req.SoThe ?? string.Empty).Trim());
            string phoneIdx = ComputeSearchIndex(req.SoDienThoai);
            string cccdIdx = ComputeSearchIndex(req.SoCCCD);
            string accountIdx = ComputeSearchIndex(req.SoTaiKhoan);
            string cardIdx = ComputeSearchIndex(req.SoThe);

            const string sql = @"INSERT INTO Customers
                                 (MaKhachHang, HoTen, SoDienThoai, Email, SoCCCD, SoTaiKhoan, SoThe,
                                  SoDienThoaiIdx, SoCCCDIdx, SoTaiKhoanIdx, SoTheIdx,
                                  RoleID, TenDangNhap, MatKhauHash, PinGiaoDich)
                                 VALUES
                                 (@ma, @name, @phone, @email, @cccd, @stk, @sothe,
                                  @phoneIdx, @cccdIdx, @accountIdx, @cardIdx,
                                  @role, @username, @passwordHash, @pin)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ma", req.MaKhachHang.Trim());
            cmd.Parameters.AddWithValue("@name", req.HoTen.Trim());
            cmd.Parameters.AddWithValue("@phone", encryptedPhone);
            cmd.Parameters.AddWithValue("@email", encryptedEmail);
            cmd.Parameters.AddWithValue("@cccd", encryptedCccd);
            cmd.Parameters.AddWithValue("@stk", string.IsNullOrWhiteSpace(encryptedAccount) ? DBNull.Value : encryptedAccount);
            cmd.Parameters.AddWithValue("@sothe", string.IsNullOrWhiteSpace(encryptedCard) ? DBNull.Value : encryptedCard);
            cmd.Parameters.AddWithValue("@phoneIdx", string.IsNullOrWhiteSpace(phoneIdx) ? DBNull.Value : phoneIdx);
            cmd.Parameters.AddWithValue("@cccdIdx", string.IsNullOrWhiteSpace(cccdIdx) ? DBNull.Value : cccdIdx);
            cmd.Parameters.AddWithValue("@accountIdx", string.IsNullOrWhiteSpace(accountIdx) ? DBNull.Value : accountIdx);
            cmd.Parameters.AddWithValue("@cardIdx", string.IsNullOrWhiteSpace(cardIdx) ? DBNull.Value : cardIdx);
            cmd.Parameters.AddWithValue("@role", req.RoleId);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("@pin", DBNull.Value);

            int affected = cmd.ExecuteNonQuery();
            return new OperationResponse
            {
                Success = affected > 0,
                Message = affected > 0 ? "Tạo khách hàng thành công." : "Không thể tạo khách hàng."
            };
        }

        public OperationResponse UpdateCustomer(long customerId, CustomerManageUpsertRequest req)
        {
            EnsureDatabaseConfigured();

            if (customerId <= 0)
            {
                return new OperationResponse { Success = false, Message = "CustomerID không hợp lệ." };
            }

            if (!ValidateUpsertRequest(req, out string validateError))
            {
                return new OperationResponse { Success = false, Message = validateError };
            }

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string encryptedPhone = EncryptForStorage(req.SoDienThoai.Trim());
            string encryptedEmail = EncryptForStorage(req.Email.Trim());
            string encryptedCccd = EncryptForStorage(req.SoCCCD.Trim());
            string encryptedAccount = EncryptForStorage((req.SoTaiKhoan ?? string.Empty).Trim());
            string encryptedCard = EncryptForStorage((req.SoThe ?? string.Empty).Trim());
            string phoneIdx = ComputeSearchIndex(req.SoDienThoai);
            string cccdIdx = ComputeSearchIndex(req.SoCCCD);
            string accountIdx = ComputeSearchIndex(req.SoTaiKhoan);
            string cardIdx = ComputeSearchIndex(req.SoThe);

            const string sql = @"UPDATE Customers
                                 SET MaKhachHang=@ma,
                                     HoTen=@name,
                                     SoDienThoai=@phone,
                                     Email=@email,
                                     SoCCCD=@cccd,
                                     SoTaiKhoan=@stk,
                                     SoThe=@sothe,
                                     SoDienThoaiIdx=@phoneIdx,
                                     SoCCCDIdx=@cccdIdx,
                                     SoTaiKhoanIdx=@accountIdx,
                                     SoTheIdx=@cardIdx,
                                     RoleID=@role
                                 WHERE CustomerID=@id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ma", req.MaKhachHang.Trim());
            cmd.Parameters.AddWithValue("@name", req.HoTen.Trim());
            cmd.Parameters.AddWithValue("@phone", encryptedPhone);
            cmd.Parameters.AddWithValue("@email", encryptedEmail);
            cmd.Parameters.AddWithValue("@cccd", encryptedCccd);
            cmd.Parameters.AddWithValue("@stk", string.IsNullOrWhiteSpace(encryptedAccount) ? DBNull.Value : encryptedAccount);
            cmd.Parameters.AddWithValue("@sothe", string.IsNullOrWhiteSpace(encryptedCard) ? DBNull.Value : encryptedCard);
            cmd.Parameters.AddWithValue("@phoneIdx", string.IsNullOrWhiteSpace(phoneIdx) ? DBNull.Value : phoneIdx);
            cmd.Parameters.AddWithValue("@cccdIdx", string.IsNullOrWhiteSpace(cccdIdx) ? DBNull.Value : cccdIdx);
            cmd.Parameters.AddWithValue("@accountIdx", string.IsNullOrWhiteSpace(accountIdx) ? DBNull.Value : accountIdx);
            cmd.Parameters.AddWithValue("@cardIdx", string.IsNullOrWhiteSpace(cardIdx) ? DBNull.Value : cardIdx);
            cmd.Parameters.AddWithValue("@role", req.RoleId);
            cmd.Parameters.AddWithValue("@id", customerId);

            int affected = cmd.ExecuteNonQuery();
            return new OperationResponse
            {
                Success = affected > 0,
                Message = affected > 0 ? "Cập nhật khách hàng thành công." : "Không tìm thấy khách hàng để cập nhật."
            };
        }

        public OperationResponse DeleteCustomer(long customerId)
        {
            EnsureDatabaseConfigured();

            if (customerId <= 0)
            {
                return new OperationResponse { Success = false, Message = "CustomerID không hợp lệ." };
            }

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM Customers WHERE CustomerID = @id", conn);
            cmd.Parameters.AddWithValue("@id", customerId);

            int affected = cmd.ExecuteNonQuery();
            return new OperationResponse
            {
                Success = affected > 0,
                Message = affected > 0 ? "Xóa khách hàng thành công." : "Không tìm thấy khách hàng để xóa."
            };
        }

        private string EncryptForStorage(string plainValue)
        {
            if (string.IsNullOrWhiteSpace(plainValue))
            {
                return string.Empty;
            }

            char[] encrypted = CustomAES.EncryptData(
                CustomStringHelper.ToCharArray(plainValue),
                CustomStringHelper.ToCharArray(_dataEncryptionSecret));
            return "ENC:" + new string(encrypted);
        }

        private string DecryptIfNeeded(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string payload = value.StartsWith("ENC:", StringComparison.Ordinal)
                ? value[4..]
                : value;

            char[] decrypted = CustomAES.DecryptData(
                CustomStringHelper.ToCharArray(payload),
                CustomStringHelper.ToCharArray(_dataEncryptionSecret));

            if (decrypted.Length == 0)
            {
                return value;
            }

            return new string(decrypted);
        }

        private static string ComputeSha256Hex(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
            return CustomSha256.ComputeHex(bytes);
        }

        private static string MaskCredential(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            if (value.Length <= 4)
            {
                return "****";
            }

            return new string('*', value.Length - 4) + value[^4..];
        }

        private static string ResolveUsernameForCreate(CustomerManageUpsertRequest req)
        {
            if (!string.IsNullOrWhiteSpace(req.Username))
            {
                return req.Username.Trim().ToLowerInvariant();
            }

            return "kh_" + (req.MaKhachHang ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string ResolvePasswordHashForCreate(CustomerManageUpsertRequest req)
        {
            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                return ComputeSha256Hex(req.Password.Trim());
            }

            // Mặc định cho dữ liệu demo, cần đổi sau khi cấp tài khoản thật.
            return ComputeSha256Hex("123456");
        }

        private void EnsureDatabaseConfigured()
        {
            if (!string.IsNullOrWhiteSpace(_configurationError))
            {
                throw new InvalidOperationException(_configurationError);
            }
        }

        private string NormalizeEncryptedField(string? currentValue, SensitiveFieldKind kind, ref bool changed)
        {
            if (string.IsNullOrWhiteSpace(currentValue))
            {
                return string.Empty;
            }

            if (currentValue.StartsWith("ENC:", StringComparison.Ordinal))
            {
                return currentValue;
            }

            // If this is an older encrypted value (without ENC:), mark it to avoid re-encryption.
            string decrypted = TryDecryptLegacyCipher(currentValue);
            if (!string.IsNullOrWhiteSpace(decrypted) && LooksLikePlainSensitiveValue(decrypted, kind))
            {
                changed = true;
                return "ENC:" + currentValue;
            }

            changed = true;
            return EncryptForStorage(currentValue);
        }

        private string TryDecryptLegacyCipher(string value)
        {
            char[] decrypted = CustomAES.DecryptData(
                CustomStringHelper.ToCharArray(value),
                CustomStringHelper.ToCharArray(_dataEncryptionSecret));

            return decrypted.Length == 0 ? string.Empty : new string(decrypted);
        }

        private static bool LooksLikePlainSensitiveValue(string value, SensitiveFieldKind kind)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string text = value.Trim();
            switch (kind)
            {
                case SensitiveFieldKind.Email:
                    return text.Contains('@') && text.Contains('.');

                case SensitiveFieldKind.Phone:
                    return IsDigitsOnly(text, minLen: 8, maxLen: 15);

                case SensitiveFieldKind.Cccd:
                    return IsAlnumOnly(text, minLen: 8, maxLen: 20);

                case SensitiveFieldKind.Account:
                    return IsDigitsOnly(text, minLen: 8, maxLen: 25);

                case SensitiveFieldKind.Card:
                    if (string.Equals(text, "N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    return IsDigitsOnly(text, minLen: 12, maxLen: 25);

                default:
                    return false;
            }
        }

        private static bool IsDigitsOnly(string text, int minLen, int maxLen)
        {
            if (text.Length < minLen || text.Length > maxLen)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAlnumOnly(string text, int minLen, int maxLen)
        {
            if (text.Length < minLen || text.Length > maxLen)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool isDigit = c >= '0' && c <= '9';
                bool isUpper = c >= 'A' && c <= 'Z';
                bool isLower = c >= 'a' && c <= 'z';
                if (!isDigit && !isUpper && !isLower)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateUpsertRequest(CustomerManageUpsertRequest req, out string error)
        {
            error = "";
            if (req == null)
            {
                error = "Dữ liệu yêu cầu không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(req.MaKhachHang)
                || string.IsNullOrWhiteSpace(req.HoTen)
                || string.IsNullOrWhiteSpace(req.SoDienThoai)
                || string.IsNullOrWhiteSpace(req.Email)
                || string.IsNullOrWhiteSpace(req.SoCCCD))
            {
                error = "Thiếu dữ liệu bắt buộc (Mã KH, Họ tên, SĐT, Email, CCCD).";
                return false;
            }

            if (req.RoleId < 1 || req.RoleId > 3)
            {
                error = "RoleID chỉ hợp lệ trong khoảng 1..3.";
                return false;
            }

            return true;
        }
    }

    internal enum SensitiveFieldKind
    {
        Phone,
        Email,
        Cccd,
        Account,
        Card
    }

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

    public static class CustomSha256
    {
        private static readonly uint[] K =
        {
            0x428a2f98u, 0x71374491u, 0xb5c0fbcfu, 0xe9b5dba5u,
            0x3956c25bu, 0x59f111f1u, 0x923f82a4u, 0xab1c5ed5u,
            0xd807aa98u, 0x12835b01u, 0x243185beu, 0x550c7dc3u,
            0x72be5d74u, 0x80deb1feu, 0x9bdc06a7u, 0xc19bf174u,
            0xe49b69c1u, 0xefbe4786u, 0x0fc19dc6u, 0x240ca1ccu,
            0x2de92c6fu, 0x4a7484aau, 0x5cb0a9dcu, 0x76f988dau,
            0x983e5152u, 0xa831c66du, 0xb00327c8u, 0xbf597fc7u,
            0xc6e00bf3u, 0xd5a79147u, 0x06ca6351u, 0x14292967u,
            0x27b70a85u, 0x2e1b2138u, 0x4d2c6dfcu, 0x53380d13u,
            0x650a7354u, 0x766a0abbu, 0x81c2c92eu, 0x92722c85u,
            0xa2bfe8a1u, 0xa81a664bu, 0xc24b8b70u, 0xc76c51a3u,
            0xd192e819u, 0xd6990624u, 0xf40e3585u, 0x106aa070u,
            0x19a4c116u, 0x1e376c08u, 0x2748774cu, 0x34b0bcb5u,
            0x391c0cb3u, 0x4ed8aa4au, 0x5b9cca4fu, 0x682e6ff3u,
            0x748f82eeu, 0x78a5636fu, 0x84c87814u, 0x8cc70208u,
            0x90befffau, 0xa4506cebu, 0xbef9a3f7u, 0xc67178f2u
        };

        public static string ComputeHex(byte[] message)
        {
            byte[] padded = PadMessage(message ?? Array.Empty<byte>());

            uint h0 = 0x6a09e667u;
            uint h1 = 0xbb67ae85u;
            uint h2 = 0x3c6ef372u;
            uint h3 = 0xa54ff53au;
            uint h4 = 0x510e527fu;
            uint h5 = 0x9b05688cu;
            uint h6 = 0x1f83d9abu;
            uint h7 = 0x5be0cd19u;

            uint[] w = new uint[64];
            for (int offset = 0; offset < padded.Length; offset += 64)
            {
                for (int i = 0; i < 16; i++)
                {
                    int idx = offset + (i * 4);
                    w[i] = ((uint)padded[idx] << 24)
                        | ((uint)padded[idx + 1] << 16)
                        | ((uint)padded[idx + 2] << 8)
                        | padded[idx + 3];
                }

                for (int i = 16; i < 64; i++)
                {
                    uint s0 = SmallSigma0(w[i - 15]);
                    uint s1 = SmallSigma1(w[i - 2]);
                    w[i] = unchecked(w[i - 16] + s0 + w[i - 7] + s1);
                }

                uint a = h0;
                uint b = h1;
                uint c = h2;
                uint d = h3;
                uint e = h4;
                uint f = h5;
                uint g = h6;
                uint h = h7;

                for (int i = 0; i < 64; i++)
                {
                    uint t1 = unchecked(h + BigSigma1(e) + Ch(e, f, g) + K[i] + w[i]);
                    uint t2 = unchecked(BigSigma0(a) + Maj(a, b, c));

                    h = g;
                    g = f;
                    f = e;
                    e = unchecked(d + t1);
                    d = c;
                    c = b;
                    b = a;
                    a = unchecked(t1 + t2);
                }

                h0 = unchecked(h0 + a);
                h1 = unchecked(h1 + b);
                h2 = unchecked(h2 + c);
                h3 = unchecked(h3 + d);
                h4 = unchecked(h4 + e);
                h5 = unchecked(h5 + f);
                h6 = unchecked(h6 + g);
                h7 = unchecked(h7 + h);
            }

            return ToHex(h0) + ToHex(h1) + ToHex(h2) + ToHex(h3)
                + ToHex(h4) + ToHex(h5) + ToHex(h6) + ToHex(h7);
        }

        private static byte[] PadMessage(byte[] input)
        {
            ulong bitLength = (ulong)input.Length * 8UL;
            int totalLength = input.Length + 1 + 8;
            int paddedLength = ((totalLength + 63) / 64) * 64;

            byte[] padded = new byte[paddedLength];
            Buffer.BlockCopy(input, 0, padded, 0, input.Length);
            padded[input.Length] = 0x80;

            for (int i = 0; i < 8; i++)
            {
                padded[padded.Length - 1 - i] = (byte)((bitLength >> (8 * i)) & 0xffUL);
            }

            return padded;
        }

        private static uint RotateRight(uint x, int n) => (x >> n) | (x << (32 - n));
        private static uint Ch(uint x, uint y, uint z) => (x & y) ^ (~x & z);
        private static uint Maj(uint x, uint y, uint z) => (x & y) ^ (x & z) ^ (y & z);
        private static uint BigSigma0(uint x) => RotateRight(x, 2) ^ RotateRight(x, 13) ^ RotateRight(x, 22);
        private static uint BigSigma1(uint x) => RotateRight(x, 6) ^ RotateRight(x, 11) ^ RotateRight(x, 25);
        private static uint SmallSigma0(uint x) => RotateRight(x, 7) ^ RotateRight(x, 18) ^ (x >> 3);
        private static uint SmallSigma1(uint x) => RotateRight(x, 17) ^ RotateRight(x, 19) ^ (x >> 10);

        private static string ToHex(uint value)
        {
            const string digits = "0123456789ABCDEF";
            char[] chars = new char[8];
            for (int i = 7; i >= 0; i--)
            {
                chars[i] = digits[(int)(value & 0x0Fu)];
                value >>= 4;
            }
            return new string(chars);
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
        static readonly BigInteger n;
        static readonly BigInteger e;
        static readonly BigInteger d;
        static readonly CustomPseudoRandom Prng = new CustomPseudoRandom();

        static CustomRSA()
        {
            (n, e, d) = GenerateKeyPair(512);
        }

        static (BigInteger N, BigInteger E, BigInteger D) GenerateKeyPair(int primeBits)
        {
            BigInteger p = GenerateProbablePrime(primeBits);
            BigInteger q;
            do { q = GenerateProbablePrime(primeBits); } while (q == p);

            BigInteger modulus = p * q;
            BigInteger phi = (p - 1) * (q - 1);
            BigInteger publicExponent = 65537;

            while (BigInteger.GreatestCommonDivisor(publicExponent, phi) != 1)
            {
                publicExponent += 2;
            }

            BigInteger privateExponent = ModInverse(publicExponent, phi);
            return (modulus, publicExponent, privateExponent);
        }

        static BigInteger GenerateProbablePrime(int bits)
        {
            if (bits < 8) bits = 8;
            byte[] bytes = new byte[(bits + 7) / 8];

            while (true)
            {
                Prng.FillBytes(bytes);
                bytes[0] |= 0x01; 
                bytes[bytes.Length - 1] |= 0x40; 
                byte[] unsigned = new byte[bytes.Length + 1];
                Buffer.BlockCopy(bytes, 0, unsigned, 0, bytes.Length);
                BigInteger candidate = new BigInteger(unsigned);
                if (IsProbablePrime(candidate, 20)) return candidate;
            }
        }

        static bool IsProbablePrime(BigInteger value, int rounds)
        {
            if (value < 2) return false;
            if (value == 2 || value == 3) return true;
            if ((value & 1) == 0) return false;

            BigInteger dVal = value - 1;
            int s = 0;
            while ((dVal & 1) == 0)
            {
                dVal >>= 1;
                s++;
            }

            byte[] bytes = value.ToByteArray();
            for (int i = 0; i < rounds; i++)
            {
                BigInteger a = RandomBigIntegerInRange(2, value - 2, bytes.Length);
                BigInteger x = BigInteger.ModPow(a, dVal, value);
                if (x == 1 || x == value - 1) continue;

                bool witnessFound = true;
                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, value);
                    if (x == value - 1)
                    {
                        witnessFound = false;
                        break;
                    }
                }
                if (witnessFound) return false;
            }
            return true;
        }

        static BigInteger RandomBigIntegerInRange(BigInteger minInclusive, BigInteger maxInclusive, int byteLength)
        {
            BigInteger range = maxInclusive - minInclusive + 1;
            byte[] bytes = new byte[byteLength + 1];
            BigInteger sample;
            do
            {
                Prng.FillBytes(bytes);
                bytes[bytes.Length - 1] = 0;
                sample = new BigInteger(bytes);
            } while (sample >= range);
            return minInclusive + sample;
        }

        static BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            BigInteger oldR = a, r = m;
            BigInteger oldS = 1, s = 0;

            while (r != 0)
            {
                BigInteger q = oldR / r;
                (oldR, r) = (r, oldR - q * r);
                (oldS, s) = (s, oldS - q * s);
            }

            if (oldR != 1) throw new ArithmeticException("e and phi(n) are not coprime.");
            BigInteger inverse = oldS % m;
            if (inverse < 0) inverse += m;
            return inverse;
        }

        public static BigInteger EncryptAESKey(char[] aesKey)
        {
            if (aesKey == null || aesKey.Length == 0) return BigInteger.Zero;
            byte[] keyBytes = Encoding.UTF8.GetBytes(new string(aesKey));
            byte[] positiveBytes = new byte[keyBytes.Length + 1];
            Buffer.BlockCopy(keyBytes, 0, positiveBytes, 0, keyBytes.Length);
            BigInteger m = new BigInteger(positiveBytes);     
            if (m >= n) throw new ArgumentException("Khóa quá lớn so với Modulus n.");
            return BigInteger.ModPow(m, e, n);
        }

        public static char[] DecryptAESKey(BigInteger encryptedKey)
        {
            BigInteger m = BigInteger.ModPow(encryptedKey, d, n); 
            byte[] decryptedBytes = m.ToByteArray();
            int validLength = decryptedBytes.Length;
            if (decryptedBytes[validLength - 1] == 0) validLength--;
            return Encoding.UTF8.GetString(decryptedBytes, 0, validLength).ToCharArray();
        }

        public static char[] GenerateRandomSessionKey(int length = 32)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=";
            char[] key = new char[length];
            for (int i = 0; i < length; i++)
            {
                key[i] = chars[Prng.Next(chars.Length)];
            }
            return key;
        }
    }

    /// <summary>
    /// AES-256 (FIPS-197): 128-bit blocks, 256-bit key, 14 rounds, 15 round keys.
    /// ECB + PKCS#7; ciphertext returned as Base64 <c>char[]</c> for display.
    /// </summary>
    public static class CustomAES
    {
        const int Nb = 4;
        const int Nk = 8;
        const int Nr = 14;

        static readonly byte[] SBox =
        {
            0x63,0x7c,0x77,0x7b,0xf2,0x6b,0x6f,0xc5,0x30,0x01,0x67,0x2b,0xfe,0xd7,0xab,0x76,
            0xca,0x82,0xc9,0x7d,0xfa,0x59,0x47,0xf0,0xad,0xd4,0xa2,0xaf,0x9c,0xa4,0x72,0xc0,
            0xb7,0xfd,0x93,0x26,0x36,0x3f,0xf7,0xcc,0x34,0xa5,0xe5,0xf1,0x71,0xd8,0x31,0x15,
            0x04,0xc7,0x23,0xc3,0x18,0x96,0x05,0x9a,0x07,0x12,0x80,0xe2,0xeb,0x27,0xb2,0x75,
            0x09,0x83,0x2c,0x1a,0x1b,0x6e,0x5a,0xa0,0x52,0x3b,0xd6,0xb3,0x29,0xe3,0x2f,0x84,
            0x53,0xd1,0x00,0xed,0x20,0xfc,0xb1,0x5b,0x6a,0xcb,0xbe,0x39,0x4a,0x4c,0x58,0xcf,
            0xd0,0xef,0xaa,0xfb,0x43,0x4d,0x33,0x85,0x45,0xf9,0x02,0x7f,0x50,0x3c,0x9f,0xa8,
            0x51,0xa3,0x40,0x8f,0x92,0x9d,0x38,0xf5,0xbc,0xb6,0xda,0x21,0x10,0xff,0xf3,0xd2,
            0xcd,0x0c,0x13,0xec,0x5f,0x97,0x44,0x17,0xc4,0xa7,0x7e,0x3d,0x64,0x5d,0x19,0x73,
            0x60,0x81,0x4f,0xdc,0x22,0x2a,0x90,0x88,0x46,0xee,0xb8,0x14,0xde,0x5e,0x0b,0xdb,
            0xe0,0x32,0x3a,0x0a,0x49,0x06,0x24,0x5c,0xc2,0xd3,0xac,0x62,0x91,0x95,0xe4,0x79,
            0xe7,0xc8,0x37,0x6d,0x8d,0xd5,0x4e,0xa9,0x6c,0x56,0xf4,0xea,0x65,0x7a,0xae,0x08,
            0xba,0x78,0x25,0x2e,0x1c,0xa6,0xb4,0xc6,0xe8,0xdd,0x74,0x1f,0x4b,0xbd,0x8b,0x8a,
            0x70,0x3e,0xb5,0x66,0x48,0x03,0xf6,0x0e,0x61,0x35,0x57,0xb9,0x86,0xc1,0x1d,0x9e,
            0xe1,0xf8,0x98,0x11,0x69,0xd9,0x8e,0x94,0x9b,0x1e,0x87,0xe9,0xce,0x55,0x28,0xdf,
            0x8c,0xa1,0x89,0x0d,0xbf,0xe6,0x42,0x68,0x41,0x99,0x2d,0x0f,0xb0,0x54,0xbb,0x16
        };

        static readonly byte[] InvSBox =
        {
            0x52,0x09,0x6a,0xd5,0x30,0x36,0xa5,0x38,0xbf,0x40,0xa3,0x9e,0x81,0xf3,0xd7,0xfb,
            0x7c,0xe3,0x39,0x82,0x9b,0x2f,0xff,0x87,0x34,0x8e,0x43,0x44,0xc4,0xde,0xe9,0xcb,
            0x54,0x7b,0x94,0x32,0xa6,0xc2,0x23,0x3d,0xee,0x4c,0x95,0x0b,0x42,0xfa,0xc3,0x4e,
            0x08,0x2e,0xa1,0x66,0x28,0xd9,0x24,0xb2,0x76,0x5b,0xa2,0x49,0x6d,0x8b,0xd1,0x25,
            0x72,0xf8,0xf6,0x64,0x86,0x68,0x98,0x16,0xd4,0xa4,0x5c,0xcc,0x5d,0x65,0xb6,0x92,
            0x6c,0x70,0x48,0x50,0xfd,0xed,0xb9,0xda,0x5e,0x15,0x46,0x57,0xa7,0x8d,0x9d,0x84,
            0x90,0xd8,0xab,0x00,0x8c,0xbc,0xd3,0x0a,0xf7,0xe4,0x58,0x05,0xb8,0xb3,0x45,0x06,
            0xd0,0x2c,0x1e,0x8f,0xca,0x3f,0x0f,0x02,0xc1,0xaf,0xbd,0x03,0x01,0x13,0x8a,0x6b,
            0x3a,0x91,0x11,0x41,0x4f,0x67,0xdc,0xea,0x97,0xf2,0xcf,0xce,0xf0,0xb4,0xe6,0x73,
            0x96,0xac,0x74,0x22,0xe7,0xad,0x35,0x85,0xe2,0xf9,0x37,0xe8,0x1c,0x75,0xdf,0x6e,
            0x47,0xf1,0x1a,0x71,0x1d,0x29,0xc5,0x89,0x6f,0xb7,0x62,0x0e,0xaa,0x18,0xbe,0x1b,
            0xfc,0x56,0x3e,0x4b,0xc6,0xd2,0x79,0x20,0x9a,0xdb,0xc0,0xfe,0x78,0xcd,0x5a,0xf4,
            0x1f,0xdd,0xa8,0x33,0x88,0x07,0xc7,0x31,0xb1,0x12,0x10,0x59,0x27,0x80,0xec,0x5f,
            0x60,0x51,0x7f,0xa9,0x19,0xb5,0x4a,0x0d,0x2d,0xe5,0x7a,0x9f,0x93,0xc9,0x9c,0xef,
            0xa0,0xe0,0x3b,0x4d,0xae,0x2a,0xf5,0xb0,0xc8,0xeb,0xbb,0x3c,0x83,0x53,0x99,0x61,
            0x17,0x2b,0x04,0x7e,0xba,0x77,0xd6,0x26,0xe1,0x69,0x14,0x63,0x55,0x21,0x0c,0x7d
        };

        static readonly byte[] Rcon =
        {
            0x00, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36
        };

        static byte[] Derive256BitKey(byte[] keyMaterial)
        {
            if (keyMaterial.Length == 32)
                return (byte[])keyMaterial.Clone();

            byte[] output = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                output[i] = (byte)(i * 37 + 11);
            }

            for (int i = 0; i < keyMaterial.Length; i++)
            {
                int idx = i % 32;
                byte mixed = (byte)(keyMaterial[i] ^ (byte)(i * 29 + 7));
                output[idx] = (byte)((output[idx] + mixed + output[(idx + 7) % 32]) & 0xff);
                output[(idx + 13) % 32] = (byte)(output[(idx + 13) % 32] ^ (byte)(mixed << (i % 3)));
            }

            for (int round = 0; round < 16; round++)
            {
                for (int i = 0; i < 32; i++)
                {
                    byte left = output[(i + 31) % 32];
                    byte right = output[(i + 1) % 32];
                    output[i] = (byte)((output[i] ^ left) + right + round + i);
                }
            }

            return output;
        }

        static byte[] Pkcs7Pad(byte[] data, int blockSize)
        {
            int pad = blockSize - (data.Length % blockSize);
            if (pad == 0) pad = blockSize;
            byte[] outBuf = new byte[data.Length + pad];
            Buffer.BlockCopy(data, 0, outBuf, 0, data.Length);
            for (int i = data.Length; i < outBuf.Length; i++)
                outBuf[i] = (byte)pad;
            return outBuf;
        }

        static byte[] Pkcs7Unpad(byte[] data)
        {
            if (data.Length == 0) return data;
            int pad = data[data.Length - 1];
            if (pad < 1 || pad > 16 || pad > data.Length) throw new InvalidOperationException("Invalid padding.");
            for (int i = data.Length - pad; i < data.Length; i++)
            {
                if (data[i] != pad) throw new InvalidOperationException("Invalid padding.");
            }
            byte[] outBuf = new byte[data.Length - pad];
            Buffer.BlockCopy(data, 0, outBuf, 0, outBuf.Length);
            return outBuf;
        }

        static uint SubWord(uint w)
        {
            return (uint)(SBox[w & 0xff] | (SBox[(w >> 8) & 0xff] << 8) | (SBox[(w >> 16) & 0xff] << 16) | (SBox[(w >> 24) & 0xff] << 24));
        }

        static uint RotWord(uint w) => (w << 8) | (w >> 24);

        /// <summary>Produces 60 words (4*(Nr+1)) for AES-256.</summary>
        static uint[] KeyExpansion(byte[] key)
        {
            uint[] w = new uint[Nb * (Nr + 1)];
            for (int i = 0; i < Nk; i++)
                w[i] = (uint)(key[4 * i] | (key[4 * i + 1] << 8) | (key[4 * i + 2] << 16) | (key[4 * i + 3] << 24));

            for (int i = Nk; i < Nb * (Nr + 1); i++)
            {
                uint temp = w[i - 1];
                if (i % Nk == 0)
                    temp = SubWord(RotWord(temp)) ^ (uint)(Rcon[i / Nk] << 24);
                else if (Nk > 6 && (i % Nk == 4))
                    temp = SubWord(temp);
                w[i] = w[i - Nk] ^ temp;
            }
            return w;
        }

        static void AddRoundKey(byte[] state, uint[] w, int round)
        {
            for (int c = 0; c < Nb; c++)
            {
                uint rk = w[round * Nb + c];
                state[4 * c + 0] ^= (byte)(rk & 0xff);
                state[4 * c + 1] ^= (byte)((rk >> 8) & 0xff);
                state[4 * c + 2] ^= (byte)((rk >> 16) & 0xff);
                state[4 * c + 3] ^= (byte)((rk >> 24) & 0xff);
            }
        }

        static void SubBytes(byte[] state)
        {
            for (int i = 0; i < 16; i++) state[i] = SBox[state[i]];
        }

        static void InvSubBytes(byte[] state)
        {
            for (int i = 0; i < 16; i++) state[i] = InvSBox[state[i]];
        }

        static void ShiftRows(byte[] state)
        {
            byte t;
            t = state[1]; state[1] = state[5]; state[5] = state[9]; state[9] = state[13]; state[13] = t;
            t = state[2]; state[2] = state[10]; state[10] = t; t = state[6]; state[6] = state[14]; state[14] = t;
            t = state[3]; state[3] = state[7]; state[7] = state[11]; state[11] = state[15]; state[15] = t;
        }

        static void InvShiftRows(byte[] state)
        {
            byte t;
            t = state[13]; state[13] = state[9]; state[9] = state[5]; state[5] = state[1]; state[1] = t;
            t = state[2]; state[2] = state[10]; state[10] = t; t = state[14]; state[14] = state[6]; state[6] = t;
            t = state[15]; state[15] = state[11]; state[11] = state[7]; state[7] = state[3]; state[3] = t;
        }

        static byte Xtime(byte x) => (byte)((x << 1) ^ (((x >> 7) & 1) * 0x1b));

        static void MixColumns(byte[] state)
        {
            for (int c = 0; c < 4; c++)
            {
                int i = 4 * c;
                byte s0 = state[i], s1 = state[i + 1], s2 = state[i + 2], s3 = state[i + 3];
                byte t = (byte)(Xtime(s0) ^ Xtime(s1) ^ s1 ^ s2 ^ s3);
                byte u = (byte)(s0 ^ Xtime(s1) ^ Xtime(s2) ^ s2 ^ s3);
                byte v = (byte)(s0 ^ s1 ^ Xtime(s2) ^ Xtime(s3) ^ s3);
                byte w = (byte)(Xtime(s0) ^ s0 ^ s1 ^ s2 ^ Xtime(s3));
                state[i] = t; state[i + 1] = u; state[i + 2] = v; state[i + 3] = w;
            }
        }

        static byte Mul(byte a, byte b)
        {
            byte p = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((b & 1) != 0) p ^= a;
                byte hi = (byte)(a & 0x80);
                a <<= 1;
                if (hi != 0) a ^= 0x1b;
                b >>= 1;
            }
            return p;
        }

        static void InvMixColumns(byte[] state)
        {
            for (int c = 0; c < 4; c++)
            {
                int i = 4 * c;
                byte s0 = state[i], s1 = state[i + 1], s2 = state[i + 2], s3 = state[i + 3];
                state[i] = (byte)(Mul(s0, 0x0e) ^ Mul(s1, 0x0b) ^ Mul(s2, 0x0d) ^ Mul(s3, 0x09));
                state[i + 1] = (byte)(Mul(s0, 0x09) ^ Mul(s1, 0x0e) ^ Mul(s2, 0x0b) ^ Mul(s3, 0x0d));
                state[i + 2] = (byte)(Mul(s0, 0x0d) ^ Mul(s1, 0x09) ^ Mul(s2, 0x0e) ^ Mul(s3, 0x0b));
                state[i + 3] = (byte)(Mul(s0, 0x0b) ^ Mul(s1, 0x0d) ^ Mul(s2, 0x09) ^ Mul(s3, 0x0e));
            }
        }

        static void Cipher(byte[] input, byte[] output, uint[] w)
        {
            byte[] state = new byte[16];
            Buffer.BlockCopy(input, 0, state, 0, 16);
            AddRoundKey(state, w, 0);
            for (int round = 1; round <= Nr - 1; round++)
            {
                SubBytes(state);
                ShiftRows(state);
                MixColumns(state);
                AddRoundKey(state, w, round);
            }
            SubBytes(state);
            ShiftRows(state);
            AddRoundKey(state, w, Nr);
            Buffer.BlockCopy(state, 0, output, 0, 16);
        }

        static void InvCipher(byte[] input, byte[] output, uint[] w)
        {
            byte[] state = new byte[16];
            Buffer.BlockCopy(input, 0, state, 0, 16);
            AddRoundKey(state, w, Nr);
            for (int round = Nr - 1; round >= 1; round--)
            {
                InvShiftRows(state);
                InvSubBytes(state);
                AddRoundKey(state, w, round);
                InvMixColumns(state);
            }
            InvShiftRows(state);
            InvSubBytes(state);
            AddRoundKey(state, w, 0);
            Buffer.BlockCopy(state, 0, output, 0, 16);
        }

        static void EncryptBlock(byte[] blockIn, byte[] blockOut, byte[] key256)
        {
            uint[] w = KeyExpansion(key256);
            Cipher(blockIn, blockOut, w);
        }

        static void DecryptBlock(byte[] blockIn, byte[] blockOut, byte[] key256)
        {
            uint[] w = KeyExpansion(key256);
            InvCipher(blockIn, blockOut, w);
        }

        public static char[] EncryptData(char[] input, char[] key)
        {
            if (input == null || key == null) return new char[0];
            int lenKey = 0; foreach (char c in key) lenKey++;
            if (lenKey == 0) return input;

            string plainText = new string(input);
            byte[] pt = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(new string(key));
            byte[] aesKey = Derive256BitKey(keyBytes);
            byte[] padded = Pkcs7Pad(pt, 16);
            
            byte[] iv = new byte[16];
            CustomPseudoRandom.FillBytesStatic(iv);

            byte[] cipher = new byte[padded.Length];
            byte[] previousCipherBlock = new byte[16];
            Buffer.BlockCopy(iv, 0, previousCipherBlock, 0, 16);

            for (int offset = 0; offset < padded.Length; offset += 16)
            {
                byte[] currentPlainBlock = new byte[16];
                Buffer.BlockCopy(padded, offset, currentPlainBlock, 0, 16);

                for (int i = 0; i < 16; i++)
                {
                    currentPlainBlock[i] ^= previousCipherBlock[i];
                }

                byte[] currentCipherBlock = new byte[16];
                EncryptBlock(currentPlainBlock, currentCipherBlock, aesKey);
                
                Buffer.BlockCopy(currentCipherBlock, 0, cipher, offset, 16);
                Buffer.BlockCopy(currentCipherBlock, 0, previousCipherBlock, 0, 16);
            }
            byte[] finalResult = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, finalResult, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, finalResult, iv.Length, cipher.Length);

            return Convert.ToBase64String(finalResult).ToCharArray();
        }

        static void EncryptBlock(byte[] input, int inOff, byte[] output, int outOff, byte[] key256)
        {
            byte[] blockIn = new byte[16];
            Buffer.BlockCopy(input, inOff, blockIn, 0, 16);
            byte[] blockOut = new byte[16];
            EncryptBlock(blockIn, blockOut, key256);
            Buffer.BlockCopy(blockOut, 0, output, outOff, 16);
        }

        public static char[] DecryptData(char[] input, char[] key)
        {
            if (input == null || key == null) return new char[0];
            int lenKey = 0; foreach (char c in key) lenKey++;
            if (lenKey == 0) return input;

            string b64 = new string(input).Trim();
            byte[] fullCipher;
            try { fullCipher = Convert.FromBase64String(b64); }
            catch { return new char[0]; }

            if (fullCipher.Length < 16 || (fullCipher.Length % 16) != 0) return new char[0];

            byte[] keyBytes = Encoding.UTF8.GetBytes(new string(key));
            byte[] aesKey = Derive256BitKey(keyBytes);

            byte[] iv = new byte[16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);

            byte[] cipher = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

            byte[] plain = new byte[cipher.Length];
            byte[] previousCipherBlock = new byte[16];
            Buffer.BlockCopy(iv, 0, previousCipherBlock, 0, 16);

            for (int offset = 0; offset < cipher.Length; offset += 16)
            {
                byte[] currentCipherBlock = new byte[16];
                Buffer.BlockCopy(cipher, offset, currentCipherBlock, 0, 16);

                byte[] decryptedBlock = new byte[16];
                DecryptBlock(currentCipherBlock, decryptedBlock, aesKey);

                for (int i = 0; i < 16; i++)
                {
                    decryptedBlock[i] ^= previousCipherBlock[i];
                }

                Buffer.BlockCopy(decryptedBlock, 0, plain, offset, 16);
                Buffer.BlockCopy(currentCipherBlock, 0, previousCipherBlock, 0, 16);
            }

            byte[] unpadded;
            try { unpadded = Pkcs7Unpad(plain); }
            catch { return new char[0]; }

            return Encoding.UTF8.GetString(unpadded).ToCharArray();
        }

        static void DecryptBlock(byte[] input, int inOff, byte[] output, int outOff, byte[] key256)
        {
            byte[] blockIn = new byte[16];
            Buffer.BlockCopy(input, inOff, blockIn, 0, 16);
            byte[] blockOut = new byte[16];
            DecryptBlock(blockIn, blockOut, key256);
            Buffer.BlockCopy(blockOut, 0, output, outOff, 16);
        }
    }

    public sealed class CustomPseudoRandom
    {
        private ulong _state;
        private readonly object _sync = new object();

        public CustomPseudoRandom()
        {
            unchecked
            {
                _state = (ulong)DateTime.UtcNow.Ticks;
                _state ^= (ulong)Environment.TickCount << 17;
                _state ^= 0x9E3779B97F4A7C15UL;
                if (_state == 0)
                {
                    _state = 0xA5A5A5A5A5A5A5A5UL;
                }
            }
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                return 0;
            }

            ulong value = NextUInt64();
            return (int)(value % (ulong)maxExclusive);
        }

        public void FillBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(NextUInt64() & 0xff);
            }
        }

        public static void FillBytesStatic(byte[] buffer)
        {
            var prng = new CustomPseudoRandom();
            prng.FillBytes(buffer);
        }

        private ulong NextUInt64()
        {
            lock (_sync)
            {
                _state ^= _state << 13;
                _state ^= _state >> 7;
                _state ^= _state << 17;
                return _state;
            }
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

            if (atIndex == -1)
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
            for (int i = 0; i < len - 1; i += 2)
            {
                decimal temp = balances[i];
                balances[i] = balances[i + 1];
                balances[i + 1] = temp;
            }
        }
    }

}
