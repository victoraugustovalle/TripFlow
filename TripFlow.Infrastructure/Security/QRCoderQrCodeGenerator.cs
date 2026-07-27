using QRCoder;
using TripFlow.Application.Abstractions;

namespace TripFlow.Infrastructure.Security;

public class QRCoderQrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(data);
        return pngQr.GetGraphic(10);
    }
}
