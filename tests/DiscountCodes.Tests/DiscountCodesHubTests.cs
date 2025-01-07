using DiscountCodes.Api.Messages;
using DiscountCodes.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace DiscountCodes.Tests;

public class DiscountCodesHubTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private WebApplication? _app;
    private HubConnection? _connection;

    public DiscountCodesHubTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithPassword(Guid.NewGuid().ToString())
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSignalR();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(_sqlContainer.GetConnectionString()));
        builder.Services.AddScoped<DiscountCodesService>();

        _app = builder.Build();

        using var scope = _app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        _app.MapHub<DiscountCodesHub>("/discountcodeshub");

        await _app.StartAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_app.Urls.First()}/discountcodeshub")
            .Build();

        await _connection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection!.DisposeAsync();
        }

        if (_app != null)
        {
            await _app.DisposeAsync();
        }

        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task GenerateCodes_ShouldCreateValidCodes()
    {
        // Arrange
        var request = new GenerateRequest { Count = 10, Length = 8 };

        // Act
        var response = await _connection!.InvokeAsync<GenerateResponse>(
            "GenerateCodes",
            request);

        // Assert
        Assert.True(response.Result);

        using var scope = _app!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var codesCount = await dbContext.DiscountCodes.CountAsync();
        Assert.Equal(request.Count, codesCount);
    }

    [Fact]
    public async Task UseCode_ShouldMarkCodeAsUsed()
    {
        // Arrange
        var generateRequest = new GenerateRequest { Count = 1, Length = 8 };
        await _connection!.InvokeAsync<GenerateResponse>("GenerateCodes", generateRequest);

        using var scope = _app!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var code = await dbContext.DiscountCodes.FirstAsync();

        var useRequest = new UseCodeRequest { Code = code.Code };

        // Act
        var response = await _connection!.InvokeAsync<UseCodeResponse>("UseCode", useRequest);

        // Assert
        Assert.Equal(1, response.Result);

        await dbContext.Entry(code).ReloadAsync();
        Assert.True(code.IsUsed);
    }

    [Fact]
    public async Task UseCode_IfCodeIsAlreadyUsed_ShouldReturnZero()
    {
        // Arrange
        var generateRequest = new GenerateRequest { Count = 1, Length = 8 };
        await _connection!.InvokeAsync<GenerateResponse>("GenerateCodes", generateRequest);

        using var scope = _app!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var code = await dbContext.DiscountCodes.FirstAsync();

        var useRequest = new UseCodeRequest { Code = code.Code };
        var useCodeResponse = await _connection!.InvokeAsync<UseCodeResponse>("UseCode", useRequest);
        Assert.Equal(1, useCodeResponse.Result);
        await dbContext.Entry(code).ReloadAsync();
        Assert.True(code.IsUsed);

        // Act
        useCodeResponse = await _connection!.InvokeAsync<UseCodeResponse>("UseCode", useRequest);

        // Assert
        Assert.Equal(0, useCodeResponse.Result);
    }

    [Fact]
    public async Task UseCode_WithInvalidCode_ShouldReturnZero()
    {
        // Arrange
        var useRequest = new UseCodeRequest { Code = "INVALID" };

        // Act
        var response = await _connection!.InvokeAsync<UseCodeResponse>("UseCode", useRequest);

        // Assert
        Assert.Equal(0, response.Result);
    }
}