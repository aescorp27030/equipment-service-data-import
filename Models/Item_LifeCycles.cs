﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace DataNormalization.Models;

/// <summary>
/// Life Cycles for items
/// </summary>
public partial class Item_LifeCycles
{
    public int id { get; set; }

    public string lifecycle { get; set; }

    public virtual ICollection<EquipmentService_ItemInfo> EquipmentService_ItemInfo { get; set; } = new List<EquipmentService_ItemInfo>();
}