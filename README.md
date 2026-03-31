# Data Masking Banking Demo (C# .NET 8)

Bộ source mô phỏng hệ thống ngân hàng với 2 thành phần:

- `BankMaskingAPI`: ASP.NET Core Web API cung cấp xác thực JWT, tra cứu CSKH, xuất dữ liệu DEV và CRUD quản trị khách hàng.
- `DataMaskingSystem`: WinForms client đăng nhập và gọi API để hiển thị dữ liệu đã masking.

Mục tiêu chính:

- Dynamic data masking cho người dùng CSKH.
- Static masking + mã hóa cho luồng DEV/TEST.
- Mã hóa dữ liệu nhạy cảm khi lưu trữ trong CSDL (at-rest encryption).

## 1) Công nghệ sử dụng

- .NET SDK 8.0
- ASP.NET Core 8 Web API
- WinForms (.NET 8 Windows)
- MySQL
- JWT Bearer Authentication
- Thư viện chính:
  - `MySql.Data`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `Swashbuckle.AspNetCore`
  - `Newtonsoft.Json`

## 2) Cấu trúc thư mục

- `BankMaskingAPI/`: backend API
- `DataMaskingSystem/`: desktop WinForms frontend
- `database.sql`: script tạo schema + dữ liệu mẫu
- `PortraitImage/`: ảnh chân dung mẫu

## 3) Yêu cầu môi trường

- Windows (khuyến nghị vì app WinForms)
- .NET SDK 8.0 trở lên
- MySQL Server (8.x khuyến nghị)
- Visual Studio 2022 hoặc VS Code + C# extension

Kiểm tra nhanh:

```bash
dotnet --version
```

## 4) Thiết lập database

1. Tạo DB và dữ liệu mẫu:

```sql
SOURCE e:/MaskingDataC#/database.sql;
```

Hoặc mở file `database.sql` và chạy toàn bộ script trong MySQL Workbench.

2. Cấu hình tài khoản DB cho API.

Mặc định trong `BankMaskingAPI/appsettings.json` đang dùng:

- User: `root`
- Password: `123456`
- Database: `BankSystemMasking`

Khuyến nghị dùng biến môi trường thay vì hardcode:

```powershell
setx MASK_DB_USER "root"
setx MASK_DB_PASSWORD "123456"
setx MASK_DATA_KEY "MASKING_DEMO_KEY_CHANGE_ME"
```

Sau khi set `setx`, mở terminal mới để nhận biến môi trường.

## 5) Chạy BankMaskingAPI

Từ thư mục gốc workspace:

```bash
cd BankMaskingAPI
dotnet restore
dotnet run
```

API mặc định chạy HTTPS tại:

- `https://localhost:7299`

Swagger UI (Development):

- `https://localhost:7299/swagger`

## 6) Chạy DataMaskingSystem (WinForms)

Mở terminal khác:

```bash
cd DataMaskingSystem
dotnet restore
dotnet run
```

Lưu ý:

- WinForms đang gọi API cố định tại `https://localhost:7299`.
- Hãy đảm bảo API đã chạy trước khi đăng nhập trên app desktop.

## 7) Tài khoản demo đăng nhập

Dữ liệu mẫu trong `database.sql` có các user chính:

- CSKH:
  - Username: `cskh`
  - Password: `123456`
- DEV:
  - Username: `dev`
  - Password: `123456`

`RoleID`:

- `2` = CSKH
- `3` = DEV

## 8) API chính

Base URL: `https://localhost:7299`

### Xác thực

- `POST /api/customer/auth/login`

Body mẫu:

```json
{
  "username": "cskh",
  "password": "123456"
}
```

### CSKH

- `GET /api/customer/cskh/search?type={0|1|2|...}&keyword=...`
- Yêu cầu Bearer token role `cskh`

`type`:

- `0`: tìm theo SĐT
- `1`: tìm theo CCCD
- `2`: tìm theo STK hoặc số thẻ
- khác: thử tìm theo CustomerID

### DEV

- `GET /api/customer/dev/export`
- Yêu cầu Bearer token role `dev`

### Quản trị khách hàng (role dev)

- `GET /api/customer/manage/list?page=1&pageSize=20`
- `POST /api/customer/manage/create`
- `PUT /api/customer/manage/{customerId}`
- `DELETE /api/customer/manage/{customerId}`

## 9) Luồng bảo mật chính trong dự án

- Mật khẩu đăng nhập được kiểm tra theo SHA-256 hex (giữ tương thích hash cũ).
- Dữ liệu nhạy cảm (SĐT, Email, CCCD, STK, số thẻ) được chuẩn hóa lưu dạng mã hóa có tiền tố `ENC:`.
- Khi trả dữ liệu cho UI:
  - CSKH nhận dữ liệu đã masking.
  - DEV nhận dữ liệu export đã masking và mã hóa theo luồng mô phỏng.
- API cấp JWT có claim role để phân quyền endpoint.

## 10) Một số lỗi thường gặp

1. App desktop báo không kết nối API.

- Kiểm tra API đã chạy chưa tại `https://localhost:7299/swagger`.
- Kiểm tra chứng chỉ HTTPS dev của .NET:

```bash
dotnet dev-certs https --trust
```

2. API báo thiếu cấu hình DB.

- Kiểm tra `MASK_DB_USER`, `MASK_DB_PASSWORD` hoặc thông số trong `appsettings.json`.

3. Đăng nhập thất bại.

- Kiểm tra dữ liệu trong bảng `Customers` đã được import từ `database.sql`.
- Đảm bảo user có `RoleID` = 2 hoặc 3 để đăng nhập hệ thống vận hành.

## 11) Gợi ý cải tiến tiếp theo

- Tách cấu hình theo môi trường `Development/Staging/Production`.
- Chuyển secret JWT và data key sang Secret Manager hoặc vault.
- Viết integration test cho các endpoint auth/search/manage.
- Triển khai logging + audit trail cho thao tác CRUD quản trị.
