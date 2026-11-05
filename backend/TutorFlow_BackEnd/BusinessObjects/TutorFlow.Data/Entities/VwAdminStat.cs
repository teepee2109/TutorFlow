using System;
using System.Collections.Generic;

namespace TutorFlow.Data.Entities;

public partial class VwAdminStat
{
    public int? TotalRooms { get; set; }

    public int? OpenRooms { get; set; }

    public int? ClosedRooms { get; set; }

    public int? ActiveClasses { get; set; }

    public int? ActiveLecturers { get; set; }

    public int? ActiveStudents { get; set; }

    public int? PendingChangeRequests { get; set; }

    public decimal TotalUnpaidTuition { get; set; }
}
