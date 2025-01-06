namespace DiscountCodes.Api.Messages;

public struct GenerateRequest
{
    public ushort Count { get; set; }
    public byte Length { get; set; }
}

public struct GenerateResponse
{
    public bool Result { get; set; }
}

public struct UseCodeRequest
{
    public string Code { get; set; }
}

public struct UseCodeResponse
{
    public byte Result { get; set; }
}