namespace DAL.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ItemType")]
    public partial class ItemType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        public int CooldownMs { get; set; }
    }
}
