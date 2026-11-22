using Invoicing.Core.Documents;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Core;

public class DocumentLifecycleTests
{
    [Fact]
    public void Issue_Draft_AssignsNumberAndIssuedStatus()
    {
        var draft = TestData.Invoice();
        draft.Issue("2026-0001", TestData.Supplier());
        Assert.Equal("2026-0001", draft.Number);
        Assert.Equal(DocumentStatus.Issued, draft.Status);
    }

    [Fact]
    public void Issue_Draft_CopiesPartyDetails()
    {
        var customer = TestData.Customer();
        var supplier = TestData.Supplier();
        var invoice = TestData.Invoice(customer);
        invoice.Issue("2026-0001", supplier);
        var expected = (customer.Name, supplier.Name);

        customer.Name = "New customer";
        supplier.Name = "New supplier";

        Assert.Equal(expected, (invoice.Customer?.Name, invoice.Supplier?.Name));
    }

    [Fact]
    public void Issue_AlreadyIssued_Throws() =>
        Assert.Throws<InvalidOperationException>(() =>
            TestData.Issued(DocumentKind.TaxInvoice).Issue("2026-0002", TestData.Supplier()));

    [Theory]
    [InlineData(nameof(TaxInvoice.IssueDate))]
    [InlineData(nameof(TaxInvoice.DueDate))]
    [InlineData(nameof(TaxInvoice.TaxableSupplyDate))]
    [InlineData(nameof(TaxInvoice.Customer))]
    [InlineData(nameof(TaxInvoice.ReplaceLines))]
    public void Edit_IssuedInvoice_Throws(string member)
    {
        var invoice = Assert.IsType<TaxInvoice>(TestData.Issued(DocumentKind.TaxInvoice));
        Assert.Throws<InvalidOperationException>(() => Edit(invoice, member));
    }

    [Fact]
    public void SetCorrectionReason_IssuedCreditNote_Throws()
    {
        var credit = Assert.IsType<CreditNote>(TestData.Issued(DocumentKind.CreditNote));
        Assert.Throws<InvalidOperationException>(() => credit.CorrectionReason = "Other reason");
    }

    [Theory]
    [InlineData(DocumentKind.TaxInvoice)]
    [InlineData(DocumentKind.NonVatInvoice)]
    public void MarkAsPaid_IssuedInvoice_SetsPaidStatus(DocumentKind kind)
    {
        var invoice = TestData.Issued(kind);
        invoice.MarkAsPaid();
        Assert.Equal(DocumentStatus.Paid, invoice.Status);
    }

    [Fact]
    public void MarkAsPaid_Draft_Throws() =>
        Assert.Throws<InvalidOperationException>(TestData.Invoice().MarkAsPaid);

    [Fact]
    public void MarkAsPaid_CreditNote_Throws() =>
        Assert.Throws<InvalidOperationException>(TestData.Issued(DocumentKind.CreditNote).MarkAsPaid);

    [Fact]
    public void Cancel_IssuedNonVatInvoice_SetsCancelledStatus()
    {
        var invoice = TestData.Issued(DocumentKind.NonVatInvoice);
        invoice.Cancel();
        Assert.Equal(DocumentStatus.Cancelled, invoice.Status);
    }

    [Fact]
    public void Cancel_IssuedTaxInvoice_Throws() =>
        Assert.Throws<InvalidOperationException>(TestData.Issued(DocumentKind.TaxInvoice).Cancel);

    [Fact]
    public void Cancel_DraftNonVatInvoice_Throws() =>
        Assert.Throws<InvalidOperationException>(new NonVatInvoice(TestData.Header(), TestData.Lines).Cancel);

    [Theory]
    [InlineData(DocumentKind.TaxInvoice, DocumentStatus.Issued, 15, true)]
    [InlineData(DocumentKind.TaxInvoice, DocumentStatus.Issued, 14, false)]
    [InlineData(DocumentKind.TaxInvoice, DocumentStatus.Paid, 15, false)]
    [InlineData(DocumentKind.CreditNote, DocumentStatus.Issued, 15, false)]
    public void IsOverdue_StatusKindAndDueDate_ReturnsExpectedResult(DocumentKind kind, DocumentStatus status,
        int daysAfterIssue, bool expected)
    {
        var document = TestData.Issued(kind);
        if (status == DocumentStatus.Paid)
        {
            document.MarkAsPaid();
        }

        Assert.Equal(expected, document.IsOverdue(TestData.Today.AddDays(daysAfterIssue)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateCreditNote_IssuedOrPaidInvoice_PopulatesCorrectionDraft(bool paid)
    {
        var invoice = Assert.IsType<TaxInvoice>(TestData.Issued(DocumentKind.TaxInvoice));
        if (paid)
        {
            invoice.MarkAsPaid();
        }

        var correction = invoice.CreateCreditNote(TestData.Today, 14);

        Assert.Equal(new DocumentReference(invoice.Id, "2026-0001"), correction.OriginalInvoice);
        Assert.Equal(DocumentStatus.Draft, correction.Status);
        Assert.Equal(TestData.Today, correction.IssueDate);
        Assert.Equal(TestData.Today, correction.TaxableSupplyDate);
        Assert.Equal(TestData.Today.AddDays(14), correction.DueDate);
        Assert.Equal(invoice.Lines.Select(line => -line.Quantity), correction.Lines.Select(line => line.Quantity));
        Assert.Equal(invoice.Customer?.Name, correction.Customer?.Name);
    }

    [Fact]
    public void CreateCreditNote_DraftInvoice_Throws() =>
        Assert.Throws<InvalidOperationException>(() => TestData.Invoice().CreateCreditNote(TestData.Today, 14));

    private static void Edit(TaxInvoice invoice, string member)
    {
        switch (member)
        {
            case nameof(TaxInvoice.IssueDate): invoice.IssueDate = TestData.Today; break;
            case nameof(TaxInvoice.DueDate): invoice.DueDate = TestData.Today; break;
            case nameof(TaxInvoice.TaxableSupplyDate): invoice.TaxableSupplyDate = TestData.Today; break;
            case nameof(TaxInvoice.Customer): invoice.Customer = TestData.Customer(); break;
            case nameof(TaxInvoice.ReplaceLines): invoice.ReplaceLines([]); break;
            default: throw new ArgumentOutOfRangeException(nameof(member));
        }
    }
}
