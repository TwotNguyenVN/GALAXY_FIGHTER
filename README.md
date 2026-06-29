# GALAXY_FIGHTER (Chiến Cơ Huyền Thoại)

A classic 2D space shooter game built with C#, Windows Forms, and Entity Framework.

## Features
- Player profile management and leaderboards
- Multiple types of enemies and item drops
- Interactive game loop with collision detection
- Persistent game sessions using a database

## Project Architecture
- **GUI:** The presentation layer using Windows Forms. Contains the main game loop, graphics drawing, and UI forms.
- **BUS (Business Logic Layer):** Contains services handling game logic and data processing.
- **DAL (Data Access Layer):** Manages database connections and models using Entity Framework.

## Setup Instructions
1. Open `CHIEN_CO_HUYEN_THOAI.sln` in Visual Studio.
2. Ensure you have a compatible SQL Server instance installed (e.g., LocalDB or SQL Server Express).
3. The project uses Entity Framework. The database should automatically be created on the first run, or you can run `Update-Database` in the Package Manager Console.
4. Run the application from Visual Studio.
