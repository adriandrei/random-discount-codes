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
        return await _service.GenerateCodes(request);
    }

    public async Task<UseCodeResponse> UseCode(UseCodeRequest request)
    {
        return await _service.UseCode(request);
    }
}