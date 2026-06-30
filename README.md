<div align="center">
  <h1>🚀 GALAXY FIGHTER (Chiến Cơ Huyền Thoại)</h1>
  <p><i>A classic 2D space shooter game built with C# Windows Forms and Entity Framework.</i></p>

  ![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
  ![.NET Framework](https://img.shields.io/badge/.NET_Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
  ![Entity Framework](https://img.shields.io/badge/Entity_Framework-68217A?style=for-the-badge&logo=nuget&logoColor=white)
  ![SQL Server](https://img.shields.io/badge/SQL_Server-CC292B?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
</div>

---

## 📖 Mô Tả Dự Án (Project Description)
**Galaxy Fighter** (Chiến Cơ Huyền Thoại) là một tựa game bắn súng không gian 2D cổ điển được phát triển trên nền tảng **C# Windows Forms**. Người chơi sẽ điều khiển một chiến cơ, đối đầu với từng đợt kẻ địch và thu thập các vật phẩm nâng cấp để sinh tồn lâu nhất có thể.

Dự án không chỉ tập trung vào đồ họa và trải nghiệm mượt mà với thuật toán phát hiện va chạm (Collision Detection), mà còn được cấu trúc bài bản theo mô hình **3-Tier (3 Lớp)** kết hợp với **Entity Framework** và **SQL Server** để quản lý hồ sơ người chơi, lưu trữ điểm số và bảng xếp hạng trực tuyến một cách an toàn.

---

## 🎮 Cách Chơi Game (Gameplay)
- Trò chơi thuộc thể loại **Endless Survival** (Sinh tồn vô tận).
- Kẻ địch sẽ liên tục xuất hiện từ phía trên màn hình và di chuyển xuống dưới với tốc độ và số lượng tăng dần theo thời gian.
- Bạn có 3 mạng (Lives) khi bắt đầu. Mỗi lần va chạm với kẻ địch hoặc trúng đạn, bạn sẽ mất 1 mạng hoặc giảm thanh máu.
- Tiêu diệt kẻ địch sẽ cộng điểm vào điểm số hiện tại (Score) của bạn.
- Thu thập các vật phẩm (Items) rơi ra ngẫu nhiên để hồi máu, tạo khiên chắn hoặc nâng cấp hỏa lực.
- Trò chơi kết thúc (Game Over) khi thanh máu và số mạng của bạn đều trở về 0. Sau đó, điểm số của bạn sẽ được lưu lại để tranh tài trên **Leaderboard** (Bảng Xếp Hạng).

---

## ⌨️ Các Nút Chức Năng & Di Chuyển (Controls)
Hệ thống điều khiển được thiết kế tối giản để mang lại trải nghiệm nhập vai tốt nhất.

| Hành Động | Nút Bấm / Thao Tác | Mô Tả |
| :--- | :---: | :--- |
| **Di chuyển lên** | `W` hoặc `↑` | Tăng tốc tiến về phía trước |
| **Di chuyển xuống** | `S` hoặc `↓` | Lùi tàu chiến về phía sau |
| **Sang trái** | `A` hoặc `←` | Né tránh sang trái |
| **Sang phải** | `D` hoặc `→` | Né tránh sang phải |
| **Bắn đạn** | `Space` / `Chuột trái`| Tấn công kẻ địch (có chế độ Auto-fire) |
| **Tạm dừng game** | `Esc` hoặc `P` | Mở menu tạm dừng (Pause Menu) |

---

## 🎁 Các Vật Phẩm (Items)
Trong quá trình chiến đấu, bạn có thể nhặt các vật phẩm tiếp tế để gia tăng sức mạnh:

- ❤️ **Hồi Máu (Health Pack):** Hồi phục một lượng máu (HP) nhất định của chiến cơ.
- 🛡️ **Khiên Chắn (Shield):** Tạo một vòng bảo vệ giúp miễn nhiễm sát thương trong thời gian ngắn.
- 🚀 **Tên Lửa (Missile):** Phóng ra tên lửa tầm nhiệt hoặc gây sát thương diện rộng.
- ⚡ **Nâng Cấp Đạn (Firepower Upgrade):** Tăng số lượng tia đạn bắn ra (đạn đôi, đạn chùm) và gia tăng tốc độ bắn.

---

## 🏗️ Kiến Trúc Hệ Thống (Architecture)
Dự án tuân thủ nghiêm ngặt mô hình **3-Tier Architecture**:
1. 🎨 **GUI (Presentation Layer):** Giao diện WinForms. Đảm nhận việc vẽ đồ họa, xử lý vòng lặp game, bắt sự kiện bàn phím.
2. ⚙️ **BUS (Business Logic Layer):** Xử lý nghiệp vụ (tính điểm, quản lý level, xử lý logic trò chơi).
3. 🗄️ **DAL (Data Access Layer):** Giao tiếp với SQL Server bằng Entity Framework (Lưu/đọc người chơi, lịch sử trận đấu, xếp hạng).

---

## 🛠️ Chuẩn Bị Môi Trường & Cài Đặt (Setup & Installation)

Để chạy được source code dự án, bạn cần làm theo các bước sau:

### 1. Yêu Cầu Hệ Thống (Prerequisites)
- **IDE:** Visual Studio 2022 (hoặc cũ hơn có hỗ trợ C# WinForms).
- **Database:** SQL Server (Express hoặc Developer Edition).
- **Framework:** .NET Framework 4.7.2 trở lên.

### 2. Cài Đặt & Cấu Hình Database
1. Clone hoặc tải mã nguồn dự án về máy và mở file `CHIEN_CO_HUYEN_THOAI.sln` bằng Visual Studio.
2. Copy file `connections.config.example` ra một bản sao và đổi tên thành `connections.config`.
3. Mở file `connections.config` và cấu hình chuỗi kết nối tới SQL Server của bạn:
   - Điền IP/Tên máy chủ vào phần `Data Source`.
   - Điền `User ID` và `Password` (hoặc dùng Windows Authentication).
   > **Lưu ý:** File này đã được cấu hình trong `.gitignore` để đảm bảo bảo mật thông tin database của bạn.

### 3. Cập Nhật Database (Entity Framework)
1. Trong Visual Studio, mở **Package Manager Console** (`Tools` > `NuGet Package Manager`).
2. Ở phần **Default project** (thanh dropdown), chọn project `DAL`.
3. Chạy lệnh sau để tự động tạo Database theo cấu trúc Migration:
   ```powershell
   Update-Database
   ```
   *(Nếu bạn sử dụng file backup `CCHTB.bak` hoặc script `CCHT_backup_data.sql` ở thư mục `data` để restore CSDL, bạn có thể bỏ qua bước chạy lệnh này).*

### 4. Chạy Trò Chơi
1. Click chuột phải vào project `GUI` trong **Solution Explorer**.
2. Chọn **Set as Startup Project**.
3. Nhấn **F5** (hoặc nút Start) để biên dịch và chạy game.

<br/>
<div align="center">
  <b>Chúc bạn có những giờ phút giải trí vui vẻ cùng Galaxy Fighter! 🎮</b>
  <br/>
  <i>Đồ án học phần Lập trình WinForms.</i>
</div>
