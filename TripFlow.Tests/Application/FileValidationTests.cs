using FluentAssertions;
using TripFlow.Application.Documents;
using Xunit;

namespace TripFlow.Tests.Application;

public class FileValidationTests
{
    [Theory]
    [InlineData("application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 })] // %PDF-1.4
    [InlineData("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    public void IsAllowed_HeaderBateComOContentType_RetornaTrue(string contentType, byte[] header)
    {
        FileValidation.IsAllowed(contentType, header).Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_Webp_PrecisaDoMarcadorWebpDepoisDoRiff()
    {
        var validWebp = "RIFF????WEBP"u8.ToArray();
        var justRiff = "RIFF????XXXX"u8.ToArray();

        FileValidation.IsAllowed("image/webp", validWebp).Should().BeTrue();
        FileValidation.IsAllowed("image/webp", justRiff).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_ArquivoDisfarcado_TextoDeclaradoComoPdf_RetornaFalse()
    {
        var textHeader = "so um texto qualquer"u8.ToArray();

        FileValidation.IsAllowed("application/pdf", textHeader).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_ContentTypeNaoSuportado_RetornaFalse()
    {
        var header = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // zip

        FileValidation.IsAllowed("application/zip", header).Should().BeFalse();
    }

    [Fact]
    public void IsAllowed_HeaderMenorQueAAssinatura_RetornaFalseSemLancarExcecao()
    {
        FileValidation.IsAllowed("application/pdf", new byte[] { 0x25 }).Should().BeFalse();
    }
}
