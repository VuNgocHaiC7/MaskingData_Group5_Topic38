CREATE DATABASE IF NOT EXISTS BankSystemMasking
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE BankSystemMasking;

DROP TABLE IF EXISTS Customers;

CREATE TABLE Customers (
    CustomerID BIGINT AUTO_INCREMENT PRIMARY KEY,

    -- NHÓM 1: Định danh
    HinhAnhChanDung VARCHAR(255),
    HoTen VARCHAR(100) NOT NULL,
    NgaySinh DATE,
    GioiTinh TINYINT COMMENT '0=Nam / 1=Nữ',
    SoCCCD VARCHAR(255) NOT NULL, 
    MaKhachHang VARCHAR(20) NOT NULL,
    QuocTich VARCHAR(50),

    -- NHÓM 2: Liên lạc
    SoDienThoai VARCHAR(255) NOT NULL, 
    SoDienThoaiIdx VARCHAR(64),
    Email VARCHAR(255) NOT NULL,       
    DiaChiNha VARCHAR(255),

    -- NHÓM 3: Tài chính (mức tối thiểu phục vụ màn hình hiện tại)
    SoTaiKhoan VARCHAR(255),           
    SoTaiKhoanIdx VARCHAR(64),
    SoDu DECIMAL(18,2) DEFAULT 0,
    DuNoHienTai DECIMAL(18,2) DEFAULT 0,
    LoaiTaiKhoan TINYINT COMMENT '1=Thường/2=VIP/3=Premium',

    -- NHÓM 4: Thẻ
    SoThe VARCHAR(255),                
    SoTheIdx VARCHAR(64),
    TrangThaiThe TINYINT COMMENT '0=Khoá/1=Hoạt động/2=Hết hạn',

    -- NHÓM 5: Tài khoản hệ thống & bảo mật
    SoCCCDIdx VARCHAR(64),
    TenDangNhap VARCHAR(50) NOT NULL,
    MatKhauHash VARCHAR(128) NOT NULL,
    PinGiaoDich VARCHAR(128),
    RoleID TINYINT NOT NULL COMMENT '1=KH/2=CSKH/3=DEV'
);

ALTER TABLE Customers
    ADD CONSTRAINT uq_customers_makh UNIQUE (MaKhachHang),
    ADD CONSTRAINT uq_customers_username UNIQUE (TenDangNhap),
    ADD INDEX idx_customers_phone (SoDienThoai),
    ADD INDEX idx_customers_phone_idx (SoDienThoaiIdx),
    ADD INDEX idx_customers_cccd (SoCCCD),
    ADD INDEX idx_customers_cccd_idx (SoCCCDIdx),
    ADD INDEX idx_customers_account (SoTaiKhoan),
    ADD INDEX idx_customers_account_idx (SoTaiKhoanIdx),
    ADD INDEX idx_customers_card (SoThe),
    ADD INDEX idx_customers_card_idx (SoTheIdx),
    ADD INDEX idx_customers_role (RoleID);

INSERT INTO Customers (
    HinhAnhChanDung, HoTen, NgaySinh, GioiTinh, SoCCCD, MaKhachHang, QuocTich,
    SoDienThoai, Email, DiaChiNha,
    SoTaiKhoan, SoDu, DuNoHienTai, LoaiTaiKhoan,
    SoThe, TrangThaiThe,
    TenDangNhap, MatKhauHash, PinGiaoDich, RoleID
) VALUES
(
    'E:/MaskingDataC#/PortraitImage/NguyenVanHai/1.jpg', 'Nguyen Van Hai', '1995-05-12', 0, '031095001234', 'KH0001', 'Viet Nam',
    '0901234567', 'nguyenvanhai.demo@gmail.com', '123 Duong Le Loi, TP.HCM',
    '190312345678901', 50000000.00, 0.00, 1,
    '4221123456789012', 1,
    'kh_hai', '8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', '123456', 1
),
(
    'E:/MaskingDataC#/PortraitImage/TranMaiPhuong/1.jpg', 'Tran Mai Phuong', '1988-11-20', 1, '001188009876', 'KH0002', 'Viet Nam',
    '0988765432', 'phuong.tran88@company.vn', 'Landmark 81, TP.HCM',
    '190399998888777', 1500500000.00, 25000000.00, 2,
    '5123987654321098', 1,
    'kh_phuong', '2DC0269FA54D269A87536810EC453CB095B4B92F45E63826A21DFF1C2E76F169', '654321', 1
),
(
    'E:/MaskingDataC#/PortraitImage/VuHoangQuan/1.jpg', 'Vu Hoang Quan', '1990-12-25', 0, '022090001122', 'NV0001', 'Viet Nam',
    '0988112233', 'quan.vu@bankadmin.vn', 'Ha Noi',
    '190300001111222', 75000000.00, 0.00, 1,
    '9704000011112222', 1,
    'cskh', '8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', '246802', 2
),
(
    'E:/MaskingDataC#/PortraitImage/MichaelJohnson/1.jpg', 'Michael Johnson', '1980-10-15', 0, 'N12345678', 'NV0002', 'USA',
    '0900112233', 'michael.j@international.com', 'Thao Dien, TP.HCM',
    '190322223333444', 1200000000.00, 50000000.00, 3,
    '3782222233334444', 1,
    'dev', '8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', '135790', 3
);