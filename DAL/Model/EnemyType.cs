namespace DAL.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("EnemyType")]
    public partial class EnemyType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public int Hp { get; set; }

        public int Speed { get; set; }

        public int ScoreOnKill { get; set; }
    }
}
