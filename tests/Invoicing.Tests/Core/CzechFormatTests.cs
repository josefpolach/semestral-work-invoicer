using Invoicing.Core.Formatting;

namespace Invoicing.Tests.Core;

public class CzechFormatTests
{
    [Fact]
    public void FormatMoney_PositiveAndNegativeAmounts_UsesCzechSeparators()
    {
        Assert.Equal("12 100,00 Kč", CzechFormat.FormatMoney(12100m));
        Assert.Equal("-1 210,00 Kč", CzechFormat.FormatMoney(-1210m));
    }

    [Fact]
    public void FormatDate_CalendarDate_UsesDayMonthYear() =>
        Assert.Equal("6. 12. 2026", CzechFormat.FormatDate(new(2026, 12, 6)));
}
