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

```sql
-- 1. Database
CREATE DATABASE IF NOT EXISTS BankSystemMasking
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE BankSystemMasking;

DROP TABLE IF EXISTS Customers;

-- 2. Bảng Customers
CREATE TABLE Customers (
	-- ═══ NHÓM 1: THÔNG TIN ĐỊNH DANH CÁ NHÂN ═══
	CustomerID BIGINT AUTO_INCREMENT PRIMARY KEY,
	HinhAnhChanDung VARCHAR(255),
	HoTen VARCHAR(100),
	NgaySinh DATE,
	GioiTinh TINYINT COMMENT '0=Nam / 1=Nữ',
	SoCCCD VARCHAR(12),
	MaKhachHang VARCHAR(10),
	QuocTich VARCHAR(50),
	DanToc VARCHAR(50),
	TonGiao VARCHAR(50),

	-- ═══ NHÓM 2: THÔNG TIN LIÊN LẠC ═══
	SoDienThoai VARCHAR(11),
	Email VARCHAR(100),
	DiaChiNha VARCHAR(255),
	Phuong_Xa VARCHAR(100),
	Tinh_ThanhPho VARCHAR(100),

	-- ═══ NHÓM 3: THÔNG TIN TÀI CHÍNH ═══
	SoTaiKhoan VARCHAR(19),
	SoDu DECIMAL(18,2),
	HanMucTinDung DECIMAL(18,2),
	DuNoHienTai DECIMAL(18,2),
	LaiSuatVay DECIMAL(5,2),
	LoaiTaiKhoan TINYINT COMMENT '1=Thường/2=VIP/3=Premium',
	NgayMoTaiKhoan DATE,
	NgayHetHanTK DATE,
	MaSoThue VARCHAR(13),
	ThuNhapHangThang DECIMAL(18,2),

	-- ═══ NHÓM 4: THÔNG TIN THẺ THANH TOÁN (Chuẩn PCI-DSS) ═══
	SoThe VARCHAR(19),
	TenChuThe VARCHAR(100),
	NgayHetHanThe VARCHAR(5),
	CVV_CVC VARCHAR(4),
	LoaiThe VARCHAR(20),
	TrangThaiThe TINYINT COMMENT '0=Khoá/1=HĐ/2=Hết hạn',

	-- ═══ NHÓM 5: TÀI KHOẢN HỆ THỐNG & BẢO MẬT ═══
	TenDangNhap VARCHAR(50),
	MatKhauHash VARCHAR(128),
	PinGiaoDich VARCHAR(128),
	CauHoiBiMat VARCHAR(255),
	TraLoiBiMat VARCHAR(100),
	OTPSecret VARCHAR(32),
	NgayDangKy DATETIME,
	NgayDangNhapCuoi DATETIME,
	DeviceID VARCHAR(64),
	RoleID TINYINT COMMENT '1=KH/2=CSKH/3=Admin',

	-- ═══ NHÓM 6: LỊCH SỬ GIAO DỊCH ═══
	MaGiaoDich VARCHAR(20),
	SoTienGD DECIMAL(18,2),
	SoTKNguon VARCHAR(19),
	SoTKDich VARCHAR(19),
	TenNguoiNhan VARCHAR(100),
	NoiDungCK VARCHAR(255),
	NgayGioGD DATETIME,
	KenhGiaoDich VARCHAR(10),
	TrangThaiGD TINYINT COMMENT '0=Lỗi/1=OK/2=Chờ'
);
```
