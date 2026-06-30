# 🚀 GALAXY FIGHTER (Chiến Cơ Huyền Thoại)

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET Framework](https://img.shields.io/badge/.NET_Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-68217A?style=for-the-badge&logo=nuget&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC292B?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)

A classic 2D space shooter game built with **C# Windows Forms** and **Entity Framework**.

---

## 🌟 Features / Tính năng chính
- **Player Profiles & Leaderboards:** Quản lý hồ sơ người chơi, lưu trữ lịch sử các ván chơi và bảng xếp hạng trực tuyến.
- **Dynamic Gameplay:** Hệ thống kẻ địch đa dạng (Small, Medium, Large, Boss) và vật phẩm hỗ trợ (Máu, Khiên, Tên lửa, Nâng cấp đạn).
- **Interactive Game Loop:** Vòng lặp game mượt mà tích hợp thuật toán xử lý va chạm (collision detection).
- **Database Persistence:** Dữ liệu được lưu trữ an toàn qua SQL Server.
- **Security First:** Mã hóa và tách biệt cấu hình bảo mật (`connections.config`) để tránh rò rỉ dữ liệu lên Git.

## 🏗️ Project Architecture / Kiến trúc dự án
Dự án được cấu trúc theo mô hình **3 Lớp (3-Tier Architecture)**:
- 🎨 **GUI (Presentation Layer):** Lớp giao diện sử dụng Windows Forms. Xử lý vòng lặp game, đồ họa 2D và tương tác người dùng.
- ⚙️ **BUS (Business Logic Layer):** Lớp nghiệp vụ, đóng vai trò trung gian xử lý logic game.
- 🗄️ **DAL (Data Access Layer):** Lớp dữ liệu, quản lý kết nối và truy vấn CSDL thông qua Entity Framework.

## 🚀 Setup Instructions / Hướng dẫn cài đặt

1. **Mở dự án:**
   Mở file `CHIEN_CO_HUYEN_THOAI.sln` bằng Visual Studio.

2. **Cấu hình Cơ sở dữ liệu:**
   - Copy file `connections.config.example` ra một bản sao và đổi tên thành `connections.config`.
   - Mở file `connections.config` và nhập IP, Username, Password của máy chủ SQL Server của bạn vào chuỗi kết nối.
   *(Lưu ý: File này đã được cấu hình `.gitignore` để không push lên GitHub).*

3. **Khởi tạo Database (Entity Framework):**
   - Mở **Package Manager Console** trong Visual Studio.
   - Chọn Default Project là `DAL` và chạy lệnh: `Update-Database`.
   *(Nếu bạn đã trỏ tới Database có sẵn dữ liệu, có thể bỏ qua bước này).*

4. **Chạy Game:**
   - Đặt project `GUI` làm **Startup Project** (Chuột phải vào GUI -> Set as Startup Project).
   - Nhấn **F5** để bắt đầu chơi! 🎮

---
*Đồ án học phần Lập trình WinForms.*
