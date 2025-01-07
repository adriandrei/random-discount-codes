using DiscountCodes.Api.Messages;
using DiscountCodes.Api.Services;
using Microsoft.AspNetCore.SignalR;

public class DiscountCodesHub : Hub
{
    private readonly DiscountCodesService _service;

    public DiscountCodesHub(DiscountCodesService service)
    {
        _service = service;
    }

    public async Task<GenerateResponse> GenerateCodes(GenerateRequest request)
    {
        try
        {
            return await _service.GenerateCodes(request);
        }
        catch (Exception e)
        {
            // log details.
            return new GenerateResponse { Result = false };
        }
    }

    public async Task<UseCodeResponse> UseCode(UseCodeRequest request)
    {
        try
        {
            return await _service.UseCode(request);
        }
        catch (Exception e)
        {
            // log details.
            return new UseCodeResponse { Result = 0 };
        }
    }
}