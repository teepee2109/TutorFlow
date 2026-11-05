using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class Facility
{
    public int FacilityId { get; set; }

    public string FacilityName { get; set; } = null!;

    public string? Address { get; set; }

    public int? ManagerId { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual User? Manager { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
