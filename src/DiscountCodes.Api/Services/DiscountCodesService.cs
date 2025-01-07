using DiscountCodes.Api.Messages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace DiscountCodes.Api.Services;

public class DiscountCodesService
{
    private readonly AppDbContext _dbContext;

    public DiscountCodesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GenerateResponse> GenerateCodes(GenerateRequest request)
    {
        if (request.Count > 2000 || request.Length < 7 || request.Length > 8)
        {
            return new GenerateResponse { Result = false };
        }

        var codes = new HashSet<string>();
        while (codes.Count < request.Count)
        {
            var code = GenerateRandomCode(request.Length);
            codes.Add(code);
        }

        var discountCodes = codes.Select(code => new DiscountCode { Code = code, IsUsed = false }).ToList();

        try
        {
            await _dbContext.DiscountCodes.AddRangeAsync(discountCodes);
            await _dbContext.SaveChangesAsync();
            return new GenerateResponse { Result = true };
        }
        catch (DbUpdateException)
        {
            var existingCodes = _dbContext.DiscountCodes
                .Where(dc => codes.Contains(dc.Code))
                .Select(dc => dc.Code)
                .ToHashSet();

            codes.ExceptWith(existingCodes);

            while(codes.Count < request.Count)
            {
                var newCode = GenerateRandomCode(request.Length);
                if (!existingCodes.Contains(newCode))
                {
                    codes.Add(newCode);
                }
            }

            discountCodes = codes.Select(code => new DiscountCode { Code = code, IsUsed = false }).ToList();
            await _dbContext.DiscountCodes.AddRangeAsync(discountCodes);
            await _dbContext.SaveChangesAsync();
        }

        return new GenerateResponse { Result = true };
    }

    public async Task<UseCodeResponse> UseCodeWithBlocking(
        UseCodeRequest request,
        Task? awaitAfterFetch = null)
    {
        return await UseCodeInternal(request, awaitAfterFetch);
    }

    public async Task<UseCodeResponse> UseCode(UseCodeRequest request)
    {
        return await UseCodeInternal(request);
    }

    private async Task<UseCodeResponse> UseCodeInternal(
        UseCodeRequest request,
        Task? afterFetch = null)
    {
        var discountCode = await _dbContext.DiscountCodes.FindAsync(request.Code);
        if (discountCode == null || discountCode.IsUsed)
        {
            return new UseCodeResponse { Result = 0 };
        }

        // This is necessary to artificially simulate racing condition.
        if (afterFetch is not null)
        {
            await afterFetch;
        }

        discountCode.IsUsed = true;
        await _dbContext.SaveChangesAsync();

        return new UseCodeResponse { Result = 1 };
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var buffer = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buffer);

        return new string(buffer.Select(b => chars[b % chars.Length]).ToArray());
    }
}