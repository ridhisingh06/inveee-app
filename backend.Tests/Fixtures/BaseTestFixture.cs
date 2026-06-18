using System;
using invmgmt.web.Data;
using Microsoft.EntityFrameworkCore;

namespace invmgmt.web.Tests.Fixtures
{
    public class BaseTestFixture : IDisposable
    {
        public AppDbContext DbContext { get; private set; }

        public BaseTestFixture()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            DbContext = new AppDbContext(options);
        }

        public void Dispose()
        {
            DbContext.Database.EnsureDeleted();
            DbContext.Dispose();
        }
    }
}
