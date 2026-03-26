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
        public Dictionary<string, string?> DbFields { get; set; } = new();
    }

    public sealed class DevExportRowDto
    {
        public string Id { get; set; } = "";
        public string MaskedCccd { get; set; } = "";
        public string CipherText { get; set; } = "";
    }

    public sealed class DevExportResponse
    {
        public List<DevExportRowDto> Rows { get; set; } = new();
        public string? ConsoleMessage { get; set; }
    }

    public sealed class SecurityModules
    {
        private readonly string _connectionString;

        public SecurityModules(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BankDatabase")
                ?? throw new InvalidOperationException("Connection string 'BankDatabase' is not configured.");
        }

        public CskhSearchResponse SearchCustomerCskh(int searchType, string keyword)
        {
            var response = new CskhSearchResponse();
            response.DbFields = new Dictionary<string, string?>();

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string whereClause = searchType switch
            {
                0 => "SoDienThoai = @keyword",
                1 => "SoCCCD = @keyword",
                2 => "(SoTaiKhoan = @keyword OR SoThe = @keyword)",
                _ => "CustomerID = @keyword" 
            };

            string query = $"SELECT * FROM Customers WHERE {whereClause} LIMIT 1";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@keyword", keyword.Trim());
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                response.Found = false;
                response.DisplayText = "Không tìm thấy khách hàng!";
                return response;
            }

            string hinhAnh = reader["HinhAnhChanDung"]?.ToString() ?? string.Empty;
            response.PortraitImage = hinhAnh;

            string hoTenRaw = reader["HoTen"]?.ToString() ?? string.Empty;
            string soDienThoaiRaw = reader["SoDienThoai"]?.ToString() ?? string.Empty;
            string soTaiKhoanRaw = reader["SoTaiKhoan"]?.ToString() ?? string.Empty;
            string soCccdRaw = reader["SoCCCD"]?.ToString() ?? string.Empty;
            string emailRaw = reader["Email"]?.ToString() ?? string.Empty;
            string soTheRaw = reader["SoThe"]?.ToString() ?? string.Empty;

            char[] name = CustomStringHelper.ToCharArray(hoTenRaw);
            char[] phone = CustomStringHelper.ToCharArray(soDienThoaiRaw);
            char[] acc = CustomStringHelper.ToCharArray(soTaiKhoanRaw);
            char[] cccd = CustomStringHelper.ToCharArray(soCccdRaw);
            char[] email = CustomStringHelper.ToCharArray(emailRaw);
            char[] card = CustomStringHelper.ToCharArray(soTheRaw);

            char[] maskedName = CustomDataMasker.MaskFullName(name);
            char[] maskedPhone = CustomDataMasker.MaskPhoneNumber(phone);
            char[] maskedAcc = CustomDataMasker.MaskBankAccount(acc);
            char[] maskedCccd = CustomDataMasker.MaskBankAccount(cccd);
            char[] maskedEmail = CustomDataMasker.MaskEmail(email);
            char[] maskedCard = CustomDataMasker.MaskBankAccount(card);

            char[] key = CustomStringHelper.ToCharArray("KEY123");
            char[] encPhone = CustomCryptography.XorEncryptDecrypt(maskedPhone, key);
            char[] encAcc = CustomCryptography.XorEncryptDecrypt(maskedAcc, key);

            response.ChannelLogMessage = $"[KÊNH TRUYỀN] Bắt được gói tin mã hóa XOR:\nSĐT: {new string(encPhone)} | STK: {new string(encAcc)}";

            char[] decPhone = CustomCryptography.XorEncryptDecrypt(encPhone, key);
            char[] decAcc = CustomCryptography.XorEncryptDecrypt(encAcc, key);

            string gioiTinh = (reader["GioiTinh"]?.ToString() ?? "") switch { "0" => "Nam", "1" => "Nữ", _ => "Không rõ" };
            string loaiTaiKhoan = (reader["LoaiTaiKhoan"]?.ToString() ?? "") switch { "1" => "Thường", "2" => "VIP", "3" => "Premium", _ => "Không rõ" };
            string trangThaiThe = (reader["TrangThaiThe"]?.ToString() ?? "") switch { "0" => "Khoá", "1" => "Hoạt động", "2" => "Hết hạn", _ => "Không rõ" };
            string roleID = (reader["RoleID"]?.ToString() ?? "") switch { "1" => "Khách hàng", "2" => "CSKH", "3" => "Admin", _ => "Không rõ" };
            string trangThaiGD = (reader["TrangThaiGD"]?.ToString() ?? "") switch { "0" => "Lỗi", "1" => "Thành công", "2" => "Đang chờ", _ => "Không rõ" };

            response.DbFields.Add("ID Hệ Thống", reader["CustomerID"]?.ToString());
            response.DbFields.Add("Mã KH", reader["MaKhachHang"]?.ToString());
            response.DbFields.Add("Họ tên (Mask)", new string(maskedName));
            response.DbFields.Add("Ngày sinh", reader["NgaySinh"]?.ToString());
            response.DbFields.Add("Giới tính", gioiTinh);
            response.DbFields.Add("CCCD (Mask)", new string(maskedCccd));
            response.DbFields.Add("Quốc tịch", reader["QuocTich"]?.ToString());

            response.DbFields.Add("SĐT (Decrypted)", new string(decPhone));
            response.DbFields.Add("Email (Mask)", new string(maskedEmail));
            response.DbFields.Add("Địa chỉ nhà", reader["DiaChiNha"]?.ToString());

            response.DbFields.Add("Tài khoản chính", new string(decAcc));
            response.DbFields.Add("Số dư hiện tại", $"{reader["SoDu"]} VNĐ");
            response.DbFields.Add("Dư nợ hiện tại", $"{reader["DuNoHienTai"]} VNĐ");
            response.DbFields.Add("Loaị tài khoản", loaiTaiKhoan);

            response.DbFields.Add("Số thẻ (Mask)", new string(maskedCard));
            response.DbFields.Add("Trạng thái thẻ", trangThaiThe);
            response.DbFields.Add("Tên đăng nhập", reader["TenDangNhap"]?.ToString());
            response.DbFields.Add("Mật khẩu/PIN", "[ĐÃ MÃ HÓA HASH]");

            var resultBuilder = new StringBuilder();
            resultBuilder.AppendLine($"Đã tìm thấy khách hàng: {new string(maskedName)}");
            resultBuilder.AppendLine("Trạng thái dữ liệu: [Đã áp dụng Masking & Encryption]");
            resultBuilder.AppendLine("Tất cả dữ liệu chi tiết đã được đẩy xuống bảng thuộc tính bên dưới.");

            response.Found = true;
            response.DisplayText = resultBuilder.ToString();

            return response;
        }

        public DevExportResponse ExportDevSample()
        {
            var result = new DevExportResponse();
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("SELECT CustomerID, SoCCCD FROM Customers LIMIT 5", conn);
            using var reader = cmd.ExecuteReader();

            char[] aesSessionKey = CustomRSA.GenerateRandomSessionKey(32);
            BigInteger encryptedAesKey = CustomRSA.EncryptAESKey(aesSessionKey);
            char[] decryptedAesKey = CustomRSA.DecryptAESKey(encryptedAesKey);

            while (reader.Read())
            {
                char[] cccd = CustomStringHelper.ToCharArray(reader["SoCCCD"].ToString() ?? string.Empty);
                char[] maskedCccd = CustomDataMasker.ShiftMask(cccd, 3);
                char[] cipherCccd = CustomAES.EncryptData(maskedCccd, decryptedAesKey);

                result.Rows.Add(new DevExportRowDto
                {
                    Id = reader["CustomerID"].ToString() ?? "",
                    MaskedCccd = new string(maskedCccd),
                    CipherText = new string(cipherCccd)
                });
            }

            result.ConsoleMessage = "[RSA] Đang trao đổi khóa phiên AES an toàn...\n[DEV] Áp dụng Mô hình lai ghép RSA-AES và trích xuất DB thành công.";

            return result;
        }
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
                RandomNumberGenerator.Fill(bytes);
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
                RandomNumberGenerator.Fill(bytes);
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
            Random rnd = new Random(); 
            for (int i = 0; i < length; i++)
            {
                key[i] = chars[rnd.Next(chars.Length)];
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
            using (SHA256 sha = SHA256.Create())
                return sha.ComputeHash(keyMaterial);
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
            if (pad < 1 || pad > 16 || pad > data.Length) throw new CryptographicException("Invalid padding.");
            for (int i = data.Length - pad; i < data.Length; i++)
            {
                if (data[i] != pad) throw new CryptographicException("Invalid padding.");
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
            RandomNumberGenerator.Fill(iv);

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
