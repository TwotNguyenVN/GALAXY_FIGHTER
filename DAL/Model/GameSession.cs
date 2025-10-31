namespace DAL.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("GameSession")]
    public partial class GameSession
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime StartedAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? EndedAt { get; set; }

        public int Score { get; set; }

        public int CoinsEarned { get; set; }

        public bool BossKilled { get; set; }

        public bool NoHitBossKill { get; set; }

        public virtual Player Player { get; set; }
    }
}
