using Invoicing.Core.Documents;
using Invoicing.Core.Email;
using Invoicing.Core.Repositories;
using Invoicing.Core.Services;
using Invoicing.Tests.TestHelpers;

namespace Invoicing.Tests.Email;

public class DocumentEmailServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendAsync_RecordsOnlySuccessfulAcceptance(bool successful)
    {
        var sender = new FakeSender(successful);
        var repository = new EmailRepository();
        var document = TestData.Issued(DocumentKind.TaxInvoice);
        var message = LettrEmailSenderTests.CreateMessage();
        var sentAt = new DateTimeOffset(2026, 11, 22, 10, 0, 0, TimeSpan.FromHours(1));
        var result = await new DocumentEmailService(sender, repository).SendAsync(document, message,
            "request-key", sentAt, CancellationToken.None);
        Assert.Equal(successful, result.IsSuccess);
        Assert.Equal(successful ? new DocumentEmailRecord(message.ToAddress, sentAt, "provider-id") : null,
            await repository.GetLatestAsync(document.Id));
    }

    private class FakeSender(bool successful) : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(EmailMessage message, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(successful ? EmailSendResult.Success("provider-id") : EmailSendResult.Failure(EmailSendError.NoConnection));
    }

    private class EmailRepository : IDocumentEmailRepository
    {
        private readonly Dictionary<int, DocumentEmailRecord> _records = new();

        public Task AddAsync(int documentId, DocumentEmailRecord record)
        {
            _records.Add(documentId, record);
            return Task.CompletedTask;
        }

        public Task<DocumentEmailRecord?> GetLatestAsync(int documentId) =>
            Task.FromResult(_records.GetValueOrDefault(documentId));
    }
}
