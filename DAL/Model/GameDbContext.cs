using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace DAL.Model
{
    public partial class GameDbContext : DbContext
    {
        public GameDbContext()
            : base("name=GameDbContext")
        {
        }

        public virtual DbSet<EnemyType> EnemyTypes { get; set; }
        public virtual DbSet<GameSession> GameSessions { get; set; }
        public virtual DbSet<ItemType> ItemTypes { get; set; }
        public virtual DbSet<Player> Players { get; set; }
        public virtual DbSet<Setting> Settings { get; set; }
        public virtual DbSet<v_PlayerSummary> v_PlayerSummary { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EnemyType>()
                .Property(e => e.Code)
                .IsUnicode(false);

            modelBuilder.Entity<GameSession>()
                .Property(e => e.StartedAt)
                .HasPrecision(3);

            modelBuilder.Entity<GameSession>()
                .Property(e => e.EndedAt)
                .HasPrecision(3);

            modelBuilder.Entity<ItemType>()
                .Property(e => e.Code)
                .IsUnicode(false);

            modelBuilder.Entity<Player>()
                .Property(e => e.CreatedAt)
                .HasPrecision(3);

            modelBuilder.Entity<Player>()
                .Property(e => e.UpdatedAt)
                .HasPrecision(3);

            modelBuilder.Entity<Setting>()
                .Property(e => e.Key)
                .IsUnicode(false);

            modelBuilder.Entity<v_PlayerSummary>()
                .Property(e => e.LastPlayedAt)
                .HasPrecision(3);
        }
    }
}
