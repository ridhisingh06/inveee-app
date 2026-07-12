using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using invmgmt.web.Data;
using invmgmt.web.Models;
using invmgmt.web.Models.Enums;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("d:\\inveee-app\\backend\\appsettings.Development.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));

        using var context = new AppDbContext(optionsBuilder.Options);

        Console.WriteLine("=== REQUESTS IN DATABASE ===");
        var requests = context.Requests
            .Include(r => r.RequestItems)
            .ToList();

        foreach (var r in requests)
        {
            Console.WriteLine($"Request ID: {r.Id}, Status: {r.Status}, Items Count: {r.RequestItems.Count}");
            foreach (var ri in r.RequestItems)
            {
                Console.WriteLine($"  -> ItemId: {ri.ItemId}, Status: {ri.Status}, QtyRequested: {ri.QuantityRequested}");
            }
        }
    }
}
