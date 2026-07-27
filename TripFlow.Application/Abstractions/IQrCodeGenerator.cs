namespace TripFlow.Application.Abstractions;

public interface IQrCodeGenerator
{
    byte[] GeneratePng(string content);
}
