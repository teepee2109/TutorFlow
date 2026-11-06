using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

[Keyless]
public partial class VwAdminStat
{
    public int? TotalRooms { get; set; }

    public int? OpenRooms { get; set; }

    public int? ClosedRooms { get; set; }

    public int? ActiveClasses { get; set; }

    public int? ActiveLecturers { get; set; }

    public int? ActiveStudents { get; set; }

    public int? PendingChangeRequests { get; set; }

    [Column(TypeName = "decimal(38, 0)")]
    public decimal TotalUnpaidTuition { get; set; }
}
