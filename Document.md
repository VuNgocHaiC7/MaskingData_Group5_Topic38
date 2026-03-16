# HƯỚNG DẪN TRIỂN KHAI HỆ THỐNG DATA MASKING (C#)

## 1. Tổng Quan Hệ Thống

Hệ thống quản lý khách hàng cho Ngân hàng/Ví điện tử "X" đóng vai trò là một "Middleware" viết bằng C# (Visual Studio), kết nối giữa người dùng cuối và cơ sở dữ liệu (SQL Server/Oracle/MySQL). Mục tiêu cốt lõi là bảo vệ hàng triệu hồ sơ khách hàng khỏi:

- **Rủi ro nội bộ:** Ngăn chặn nhân viên Chăm sóc khách hàng (CSKH) trục lợi từ dữ liệu thật và đảm bảo đội ngũ Kỹ thuật (Dev/Tester) có dữ liệu an toàn để kiểm thử.
- **Rủi ro đường truyền:** Chống tin tặc đánh chặn các gói tin dưới dạng văn bản rõ (clear-text) trên mạng công khai.

## 2. Ràng Buộc Kỹ Thuật Khắt Khe

Để đảm bảo an toàn tuyệt đối và tối ưu hóa ở mức cơ bản nhất, hệ thống phải tuân thủ nghiêm ngặt các nguyên tắc sau:

- **Thao tác thủ công:** BẮT BUỘC sử dụng mảng ký tự (`char[]`) và các vòng lặp (`for`/`while`) để tự xây dựng thuật toán che giấu.
- **Cấm sử dụng thư viện:** Tuyệt đối KHÔNG sử dụng các hàm có sẵn của .NET Framework như `string.Substring`, `string.Replace`, `Regex`, hoặc `LINQ`.
- **Khởi tạo CustomStringHelper:** Tự xây dựng một class tĩnh (`CustomStringHelper`) để xử lý các tiện ích chuỗi (kiểm tra độ dài, rỗng, nối chuỗi) nhằm đảm bảo Clean Code và tư duy OOP.
- **Tuân thủ pháp lý:** Thiết kế phải đáp ứng các tiêu chuẩn quốc tế và trong nước như PCI-DSS v4.0, GDPR Article 5, HIPAA Safe Harbor, và Nghị định 13/2023/NĐ-CP.

## 3. Kiến Trúc Luồng Xử Lý & Phân Hệ

Chương trình có Module Đăng nhập (yêu cầu mật khẩu băm SHA-256) để phân quyền thành hai phân hệ chuyên biệt:

### Phân hệ A: Tra cứu thông tin (Dành cho CSKH)

- **Mục đích:** Hỗ trợ giao dịch viên xác thực khách hàng bằng Dynamic Data Masking (DDM).
- **Nguyên tắc:** Chỉ nhìn thấy một phần dữ liệu (vd: 4 số cuối tài khoản) thông qua giao diện UI Data Masking, dữ liệu gốc trong CSDL được giữ nguyên.
- **Bảo vệ kênh truyền:** Dữ liệu sau khi che (vd: `098****321`) sẽ chạy qua một vòng lặp `for` để mã hóa bit trực tiếp bằng thuật toán XOR (XOR Cipher) với một "Khóa bí mật" tự định nghĩa. Đầu cuối giao diện sẽ chạy phép XOR ngược lại để giải mã hiển thị.

### Phân hệ B: Xuất dữ liệu môi trường Test (Dành cho Dev/Tester)

- **Mục đích:** Tạo bản sao Database hợp lệ cho môi trường phát triển (Static Data Masking - SDM).
- **Nguyên tắc:** Dữ liệu nhạy cảm bị biến đổi vĩnh viễn trên bản sao nhưng vẫn giữ được tính logic (vd: giữ nguyên cấu trúc bảng 69 thuộc tính của khách hàng).

## 4. Các Thuật Toán Mặt Nạ (Áp dụng cho 69 Thuộc tính)

Dữ liệu sẽ được xử lý qua class `CustomDataMasker` tự code với 6 loại mặt nạ chính:

1. **Redaction Masking:** Che ẩn hoàn toàn hoặc phần lớn dữ liệu, duyệt chuỗi và gán ký tự `*`. Áp dụng bắt buộc cho Số thẻ (chuẩn PCI-DSS), Tôn giáo, Dân tộc.
2. **Partial Masking:** Dùng `if/else` trong vòng lặp để giữ lại vị trí đầu và cuối. Áp dụng cho Số điện thoại (giữ 3 đầu, 3 cuối).
3. **Format-Preserving Masking:** Giữ nguyên định dạng của chuỗi. VD: Duyệt `while` tìm ký tự `@` để che Email nhưng vẫn giữ nguyên tên miền.
4. **Word-level Masking:** Duyệt mảng tìm vị trí khoảng trắng (space) đầu và cuối để che phần đệm. Áp dụng cho Họ và Tên.
5. **Shift Masking:** Ép kiểu số sang `int`, cộng hằng số, xoay vòng (`% 10`), và ép lại thành `char`. Áp dụng cho Số CCCD / Mã KH.
6. **Data Shuffling (Hoán vị):** Tự viết thuật toán hoán vị bằng mảng phụ để đổi chéo dữ liệu giữa các khách hàng, giữ nguyên tổng giá trị hệ thống. Áp dụng cho Số dư tài khoản ở Phân hệ B.

## 5. Bảo Mật Lưu Trữ (Trạng Thái Nghỉ)

- Các trường dữ liệu cực kỳ nhạy cảm phải được C# mã hóa bằng thuật toán đối xứng AES thành CipherText trước khi gửi lệnh INSERT/UPDATE xuống Database.
- Sử dụng kết hợp mã hóa bất đối xứng RSA để trao đổi khóa phiên an toàn và cấu hình SSL/TLS cho kết nối tới Database.
- Các dữ liệu như Hình ảnh eKYC, mật khẩu hash, OTP Secret được đánh dấu `[REDACTED]` (ẩn hoàn toàn).

## BẢNG THUỘC TÍNH KHÁCH HÀNG – BẢNG CUSTOMERS

_Hệ thống Ngân hàng / Ví điện tử · Đề tài 38 – Data Masking C# (Visual Studio)_
_📊 69 thuộc tính | 7 nhóm nghiệp vụ | 6 loại mặt nạ: Redaction · Partial · Word-level · Format-Preserving · Shift · Data Shuffle + XOR Cipher (kênh truyền)_

| STT     | Tên thuộc tính (Column)                                    | Kiểu dữ liệu  | Độ dài         | Loại mặt nạ áp dụng                    | Dữ liệu gốc (Ví dụ)   | Sau khi che (Masked)     |
| :------ | :--------------------------------------------------------- | :------------ | :------------- | :------------------------------------- | :-------------------- | :----------------------- |
| **I**   | **NHÓM 1 — THÔNG TIN ĐỊNH DANH CÁ NHÂN**                   |               |                |                                        |                       |                          |
| 1       | CustomerID                                                 | INT / BIGINT  | AUTO           | Không che (Khóa chính)                 | 10045                 | 10045                    |
| 2       | HoTen                                                      | NVARCHAR(100) | ≤100 ký tự     | Word-level Masking                     | Nguyễn Trần Lê Văn An | Nguyễn \* \* \* An       |
| 3       | HoTen_EN                                                   | VARCHAR(100)  | ≤100 ký tự     | Word-level Masking                     | Nguyen Tran Le Van An | Nguyen \* \* \* An       |
| 4       | NgaySinh                                                   | DATE          | YYYY-MM-DD     | Year Masking – giữ năm, che tháng/ngày | 1990-07-15            | 1990-**-**               |
| 5       | GioiTinh                                                   | TINYINT       | 0=Nam / 1=Nữ   | Không che                              | 1                     | 1                        |
| 6       | SoCCCD                                                     | VARCHAR(12)   | 12 số          | Shift Masking +3 (xoay vòng % 10)      | 079123456789          | 302456789012             |
| 7       | MaKhachHang                                                | VARCHAR(10)   | 10 ký tự       | Không che (mã nội bộ)                  | KH00010045            | KH00010045               |
| 8       | QuocTich                                                   | NVARCHAR(50)  | ≤50 ký tự      | Không che                              | Việt Nam              | Việt Nam                 |
| 9       | DanToc                                                     | NVARCHAR(50)  | ≤50 ký tự      | Redaction Masking                      | Kinh                  | **\***                   |
| 10      | TonGiao                                                    | NVARCHAR(50)  | ≤50 ký tự      | Redaction Masking                      | Phật giáo             | \***\*\*\*\***           |
| **II**  | **NHÓM 2 — THÔNG TIN LIÊN LẠC**                            |               |                |                                        |                       |                          |
| 11      | SoDienThoai                                                | VARCHAR(11)   | 10–11 số       | Partial Masking (giữ 3 đầu + 3 cuối)   | 0987654321            | 098\*\*\*\*321           |
| 12      | SoDienThoai2                                               | VARCHAR(11)   | 10–11 số       | Partial Masking                        | 0912000001            | 091\*\*\*\*001           |
| 13      | Email                                                      | VARCHAR(100)  | ≤100 ký tự     | Format-Preserving Masking              | nguyenvana@gmail.com  | n**\*\*\***a@gmail.com   |
| 14      | EmailCongTy                                                | VARCHAR(100)  | ≤100 ký tự     | Format-Preserving Masking              | vana@company.vn       | v\*\*a@company.vn        |
| 15      | DiaChiNha                                                  | NVARCHAR(255) | ≤255 ký tự     | Partial Address Masking – che số nhà   | 123 Nguyễn Huệ, Q1    | \*\*\* Nguyễn Huệ, Q1    |
| 16      | Phuong_Xa                                                  | NVARCHAR(100) | ≤100 ký tự     | Không che (địa danh công khai)         | Phường Bến Nghé       | Phường Bến Nghé          |
| 17      | Quan_Huyen                                                 | NVARCHAR(100) | ≤100 ký tự     | Không che                              | Quận 1                | Quận 1                   |
| 18      | Tinh_ThanhPho                                              | NVARCHAR(100) | ≤100 ký tự     | Không che                              | TP. Hồ Chí Minh       | TP. Hồ Chí Minh          |
| 19      | MaBuuChinh                                                 | VARCHAR(6)    | 6 số           | Partial Masking                        | 700000                | 70\*\*\*\*               |
| 20      | Facebook_Zalo_URL                                          | VARCHAR(200)  | ≤200 ký tự     | Redaction Masking                      | fb.com/vana.nguyen    | fb.com/\*\*\*            |
| **III** | **NHÓM 3 — THÔNG TIN TÀI CHÍNH**                           |               |                |                                        |                       |                          |
| 21      | SoTaiKhoan                                                 | VARCHAR(19)   | 16–19 số       | Redaction Masking (giữ 4 số cuối)      | 9704111122223333      | \***\*\*\*\*\*\*\***3333 |
| 22      | SoTaiKhoan_Phu                                             | VARCHAR(19)   | 16–19 số       | Redaction Masking                      | 9704999900001111      | \***\*\*\*\*\*\*\***1111 |
| 23      | SoDu                                                       | DECIMAL(18,2) | VND            | Data Shuffling giữa các khách hàng     | 125,000,000           | 89,000,000 (\*)          |
| 24      | HanMucTinDung                                              | DECIMAL(18,2) | VND            | Redaction Masking                      | 500,000,000           | **_,_**,\*\*\*           |
| 25      | DuNoHienTai                                                | DECIMAL(18,2) | VND            | Redaction Masking                      | 45,200,000            | **,\***,\*\*\*           |
| 26      | LaiSuatVay                                                 | DECIMAL(5,2)  | %              | Không che (thông tin sản phẩm)         | 8.50                  | 8.50                     |
| 27      | LoaiTaiKhoan                                               | TINYINT       | 1/2/3          | Không che                              | 2 (VIP)               | 2 (VIP)                  |
| 28      | NgayMoTaiKhoan                                             | DATE          | YYYY-MM-DD     | Year Masking                           | 2018-03-22            | 2018-**-**               |
| 29      | NgayHetHanTK                                               | DATE          | YYYY-MM-DD     | Không che                              | 2028-03-22            | 2028-03-22               |
| 30      | MaSoThue                                                   | VARCHAR(13)   | 10–13 số       | Shift Masking +3                       | 0123456789            | 3456789012               |
| 31      | ThuNhapHangThang                                           | DECIMAL(18,2) | VND            | Redaction / Data Shuffle               | 25,000,000            | **,\***,\*\*\*           |
| **IV**  | **NHÓM 4 — THÔNG TIN THẺ THANH TOÁN (Chuẩn PCI-DSS v4.0)** |               |                |                                        |                       |                          |
| 32      | SoThe (Card PAN)                                           | VARCHAR(19)   | 16–19 số       | Redaction BẮT BUỘC – PCI-DSS           | 4532015112830366      | \***\*\*\*\*\*\*\***0366 |
| 33      | TenChuThe                                                  | VARCHAR(100)  | ≤100 ký tự     | Word-level Masking                     | NGUYEN VAN AN         | NGUYEN \* AN             |
| 34      | NgayHetHanThe                                              | VARCHAR(5)    | MM/YY          | Partial Masking – che tháng            | 07/28                 | \*\*/28                  |
| 35      | CVV / CVC                                                  | VARCHAR(4)    | 3–4 số         | Redaction TOÀN BỘ – PCI-DSS cấm lưu    | 456                   | \*\*\*                   |
| 36      | LoaiThe                                                    | VARCHAR(20)   | VISA/MC/NAPAS  | Không che                              | VISA                  | VISA                     |
| 37      | TrangThaiThe                                               | TINYINT       | 0/1/2          | Không che                              | 1                     | 1                        |
| **V**   | **NHÓM 5 — TÀI KHOẢN HỆ THỐNG & BẢO MẬT**                  |               |                |                                        |                       |                          |
| 38      | TenDangNhap                                                | VARCHAR(50)   | ≤50 ký tự      | Partial Masking                        | vana.nguyen           | van\*.**\***             |
| 39      | MatKhauHash                                                | VARCHAR(128)  | BCrypt hash    | Redaction TOÀN BỘ                      | $2b$12$KIx...         | **_HASHED_**             |
| 40      | PinGiaoDich                                                | VARCHAR(128)  | 6 số (hashed)  | Redaction TOÀN BỘ                      | $2b$12$xxx            | **\*\***                 |
| 41      | CauHoiBiMat                                                | NVARCHAR(255) | ≤255 ký tự     | Redaction Masking                      | Tên thú cưng?         | **\***                   |
| 42      | TraLoiBiMat                                                | NVARCHAR(100) | ≤100 ký tự     | Redaction TOÀN BỘ                      | MiuMiu                | [REDACTED]               |
| 43      | OTPSecret                                                  | VARCHAR(32)   | 32 ký tự B32   | Redaction TOÀN BỘ                      | JBSWY3DPEBLW64TM      | [REDACTED]               |
| 44      | NgayDangKy                                                 | DATETIME      | YY-MM-DD HH:MM | Year Masking                           | 2021-05-10 14:32:00   | 2021-**-** **:**:\*\*    |
| 45      | NgayDangNhapCuoi                                           | DATETIME      | YY-MM-DD HH:MM | Partial Masking ngày                   | 2024-12-01 09:15:00   | 2024-**-** **:**:\*\*    |
| 46      | DiaChi_IP_DangNhap                                         | VARCHAR(45)   | IPv4 / IPv6    | Partial IP Masking (che octet cuối)    | 192.168.10.55         | 192.168._._              |
| 47      | DeviceID                                                   | VARCHAR(64)   | 64 ký tự       | Partial Masking                        | iPhone14-ABCD1234     | iPhone14-\*\*\*\*        |
| 48      | RoleID                                                     | TINYINT       | 1=KH/2=CSKH    | Không che                              | 1                     | 1                        |
| **VI**  | **NHÓM 6 — LỊCH SỬ GIAO DỊCH**                             |               |                |                                        |                       |                          |
| 49      | MaGiaoDich                                                 | VARCHAR(20)   | UUID/ULID      | Không che (mã nội bộ)                  | TXN20241201001234     | TXN20241201001234        |
| 50      | SoTienGD                                                   | DECIMAL(18,2) | VND            | Redaction / Data Shuffle               | 5,200,000             | \*,**_,_**               |
| 51      | SoTKNguon                                                  | VARCHAR(19)   | 16–19 số       | Redaction Masking                      | 9704111122223333      | \***\*\*\*\*\*\*\***3333 |
| 52      | SoTKDich                                                   | VARCHAR(19)   | 16–19 số       | Redaction Masking                      | 9704999900001111      | \***\*\*\*\*\*\*\***1111 |
| 53      | TenNguoiNhan                                               | NVARCHAR(100) | ≤100 ký tự     | Word-level Masking                     | Trần Thị Bích         | Trần \* Bích             |
| 54      | NoiDungCK                                                  | NVARCHAR(255) | ≤255 ký tự     | Redaction nếu chứa PII                 | CK cho Bích 0912      | CK cho **\* \*\***       |
| 55      | NgayGioGD                                                  | DATETIME      | YY-MM-DD HH:MM | Partial Time Masking                   | 2024-11-30 22:45:10   | 2024-11-30 **:**:\*\*    |
| 56      | KenhGiaoDich                                               | VARCHAR(10)   | ATM/APP/WEB    | Không che                              | APP                   | APP                      |
| 57      | TrangThaiGD                                                | TINYINT       | 0/1/2          | Không che                              | 1                     | 1                        |
| **VII** | **NHÓM 7 — KYC / AML / THÔNG TIN BỔ SUNG**                 |               |                |                                        |                       |                          |
| 58      | TrangThaiKYC                                               | TINYINT       | 0/1/2          | Không che                              | 2 (Đạt)               | 2 (Đạt)                  |
| 59      | HinhAnhCCCD_Truoc                                          | VARCHAR(255)  | URL / Base64   | Redaction TOÀN BỘ                      | https://s3/.../f.jpg  | [REDACTED]               |
| 60      | HinhAnhCCCD_Sau                                            | VARCHAR(255)  | URL / Base64   | Redaction TOÀN BỘ                      | https://s3/.../b.jpg  | [REDACTED]               |
| 61      | HinhAnhChanDung                                            | VARCHAR(255)  | URL / Base64   | Redaction TOÀN BỘ                      | https://s3/.../s.jpg  | [REDACTED]               |
| 62      | DiemTinDung                                                | SMALLINT      | 300–850        | Redaction Masking                      | 720                   | \*\*\*                   |
| 63      | MaGioiThieu                                                | VARCHAR(10)   | 10 ký tự       | Partial Masking                        | REF00102AB            | REF**\*02**              |
| 64      | NghiepVu                                                   | NVARCHAR(100) | ≤100 ký tự     | Không che                              | Kỹ sư phần mềm        | Kỹ sư phần mềm           |
| 65      | ThuNhapHangThang                                           | DECIMAL(18,2) | VND            | Redaction / Data Shuffle               | 25,000,000            | **,\***,\*\*\*           |
| 66      | TenCongTy                                                  | NVARCHAR(200) | ≤200 ký tự     | Partial Masking                        | TNHH ABC Tech         | TNHH **\* \*\***         |
| 67      | MaSoNhanVienCT                                             | VARCHAR(20)   | ≤20 ký tự      | Shift Masking +3                       | EMP-2024-00123        | EMP-2024-33456           |
| 68      | GhiChu                                                     | NVARCHAR(500) | ≤500 ký tự     | Redaction TOÀN BỘ nếu có PII           | Khách VIP             | [REDACTED]               |
| 69      | NgayCapNhat                                                | DATETIME      | YY-MM-DD HH:MM | Không che                              | 2024-12-01 10:00:00   | 2024-12-01 10:00:00      |

> **Chú thích:**
>
> - `(*)` **Data Shuffling:** Giá trị hoán vị với KH khác trong bản sao DB (Phân hệ B). Tổng số dư không đổi nhưng không thể tra ngược.
> - `(**)` **[REDACTED]:** Ẩn hoàn toàn – áp dụng cho ảnh eKYC, mật khẩu hash, OTP Secret, câu trả lời bí mật.
> - **Chuẩn áp dụng:** PCI-DSS v4.0 · GDPR Article 5 (Data Minimisation) · HIPAA Safe Harbor · Nghị định 13/2023/NĐ-CP (Việt Nam)
> - **Ràng buộc đề bài:** Mọi hàm xử lý mặt nạ PHẢI tự viết bằng `char[]` + vòng lặp `for`/`while` – KHÔNG dùng `string.Substring` / `string.Replace` / `Regex` / `LINQ`.
