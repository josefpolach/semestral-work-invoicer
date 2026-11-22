using Invoicing.Pdf;

namespace Invoicing.Tests.Pdf;

public class QrCodeImageGeneratorTests
{
    [Fact]
    public void CreatePng_PaymentText_ProducesPng()
    {
        var bytes = new QrCodeImageGenerator().CreatePng("SPD*1.0*AM:100.00*");
        Assert.Equal((byte)0x89, bytes[0]);
        Assert.True(bytes.AsSpan(1).StartsWith("PNG"u8));
    }
}
