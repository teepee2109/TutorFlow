using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

public partial class Facility
{
    [Key]
    public int FacilityId { get; set; }

    [StringLength(100)]
    public string FacilityName { get; set; } = null!;

    [StringLength(255)]
    public string? Address { get; set; }

    public int? ManagerId { get; set; }

    [InverseProperty("Facility")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    [ForeignKey("ManagerId")]
    [InverseProperty("Facility")]
    public virtual User? Manager { get; set; }

    [InverseProperty("Facility")]
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
