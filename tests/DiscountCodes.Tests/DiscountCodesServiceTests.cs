using DiscountCodes.Api.Messages;
using DiscountCodes.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace DiscountCodes.Tests;

public class DiscountCodesServiceTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private ServiceProvider _serviceProvider = null!;

    public DiscountCodesServiceTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithPassword(Guid.NewGuid().ToString())
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(_sqlContainer.GetConnectionString()));

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public async Task UseCodeWithBlocker_IfMultipleInstancesUseTheSameCode_OnlyOneWillSucceed()
    {
        // Arrange
        using var codeGenerationScope = _serviceProvider.CreateScope();
        var dbContext = codeGenerationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new DiscountCodesService(dbContext);

        // Seed discount code
        var generateRequest = new GenerateRequest { Count = 1, Length = 8 };
        await service.GenerateCodes(generateRequest);
        var generatedCode = await dbContext.DiscountCodes.SingleAsync();

        // Create separate discount 
        using var useCodeScope1 = _serviceProvider.CreateScope();
        var useScopeDbContext1 = useCodeScope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var service1 = new DiscountCodesService(useScopeDbContext1);

        using var useCodeScope2 = _serviceProvider.CreateScope();
        var useScopeDbContext2 = useCodeScope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var service2 = new DiscountCodesService(useScopeDbContext2);

        var request = new UseCodeRequest { Code = generatedCode.Code };
        var waitTwoSeconds = Task.Delay(2000);

        // Act
        var task1 = Task.Run(() => service1.UseCodeWithBlocking(request, waitTwoSeconds));
        var task2 = Task.Run(() => service2.UseCodeWithBlocking(request, waitTwoSeconds));

        // Assert
        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await Task.WhenAll([task1, task2]));

        Assert.Contains(
            "The database operation was expected to affect 1 row(s), but actually affected 0 row(s); " +
            "data may have been modified or deleted since entities were loaded.",
            exception.Message);
        await dbContext.Entry(generatedCode).ReloadAsync();
        Assert.True(generatedCode.IsUsed);
    }
}
