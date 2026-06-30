using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Tests.Admin;

// Like AdminWebApplicationFactory (test auth + Development) but also swaps the
// SQL Server ApplicationDbContext for an isolated in-memory one, so admin
// integration tests exercise the REAL controller + service + routing end-to-end
// without ever connecting to the real database.
public class AdminInMemoryWebApplicationFactory : AdminWebApplicationFactory
{
    private readonly string _db = "admin-inmem-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
    }
}
