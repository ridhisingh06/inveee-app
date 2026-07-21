using System;
using System.Collections.Generic;

namespace invmgmt.web.TempModels;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
