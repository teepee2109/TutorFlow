using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data.Entities;

public partial class SystemSetting
{
    [Key]
    [StringLength(50)]
    public string SettingKey { get; set; } = null!;

    [StringLength(255)]
    public string SettingValue { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }
}
