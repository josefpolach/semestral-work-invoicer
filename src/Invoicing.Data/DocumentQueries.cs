namespace Invoicing.Data;

/// <summary>Parameterized statements for document persistence.</summary>
internal static class DocumentQueries
{
    public const string ReadDocuments = """
        SELECT d.id, d.document_type, d.number, d.status, d.issue_date, d.taxable_supply_date,
            d.due_date, d.customer_id, d.original_document_id, d.correction_reason, o.number AS original_number,
            CASE WHEN d.status <> 'Draft' THEN d.customer_name
                 WHEN o.id IS NOT NULL THEN o.customer_name ELSE c.name END AS customer_name,
            CASE WHEN d.status <> 'Draft' THEN d.customer_street
                 WHEN o.id IS NOT NULL THEN o.customer_street ELSE c.street END AS customer_street,
            CASE WHEN d.status <> 'Draft' THEN d.customer_city
                 WHEN o.id IS NOT NULL THEN o.customer_city ELSE c.city END AS customer_city,
            CASE WHEN d.status <> 'Draft' THEN d.customer_postal_code
                 WHEN o.id IS NOT NULL THEN o.customer_postal_code ELSE c.postal_code END AS customer_postal_code,
            CASE WHEN d.status <> 'Draft' THEN d.customer_ico
                 WHEN o.id IS NOT NULL THEN o.customer_ico ELSE c.ico END AS customer_ico,
            CASE WHEN d.status <> 'Draft' THEN d.customer_dic
                 WHEN o.id IS NOT NULL THEN o.customer_dic ELSE c.dic END AS customer_dic,
            CASE WHEN d.status <> 'Draft' THEN d.customer_email
                 WHEN o.id IS NOT NULL THEN o.customer_email ELSE c.email END AS customer_email,
            d.supplier_name, d.supplier_street, d.supplier_city, d.supplier_postal_code,
            d.supplier_ico, d.supplier_dic, d.supplier_register_entry, d.supplier_bank_account,
            d.supplier_iban, d.supplier_issued_by
        FROM documents d
        LEFT JOIN customers c ON d.customer_id = c.id
        LEFT JOIN documents o ON d.original_document_id = o.id
        WHERE @Id IS NULL OR d.id = @Id
        ORDER BY d.issue_date DESC, d.id DESC
        """;

    public const string ReadLines = """
        SELECT * FROM document_lines WHERE @Id IS NULL OR document_id = @Id ORDER BY document_id, position
        """;

    public const string Insert = """
        INSERT INTO documents (
            document_type, number, status, issue_date,
            taxable_supply_date, due_date, customer_id, customer_name,
            customer_street, customer_city, customer_postal_code, customer_ico,
            customer_dic, customer_email, supplier_name, supplier_street,
            supplier_city, supplier_postal_code, supplier_ico, supplier_dic,
            supplier_register_entry, supplier_bank_account, supplier_iban, supplier_issued_by,
            original_document_id, correction_reason)
        VALUES (
            @DocumentType, @Number, @Status, @IssueDate,
            @TaxableSupplyDate, @DueDate, @CustomerId, @CustomerName,
            @CustomerStreet, @CustomerCity, @CustomerPostalCode, @CustomerIco,
            @CustomerDic, @CustomerEmail, @SupplierName, @SupplierStreet,
            @SupplierCity, @SupplierPostalCode, @SupplierIco, @SupplierDic,
            @SupplierRegisterEntry, @SupplierBankAccount, @SupplierIban, @SupplierIssuedBy,
            @OriginalDocumentId, @CorrectionReason)
        RETURNING id
        """;

    public const string Update = """
        UPDATE documents SET
            number = @Number, status = @Status, issue_date = @IssueDate, taxable_supply_date = @TaxableSupplyDate,
            due_date = @DueDate, customer_id = @CustomerId, customer_name = @CustomerName, customer_street = @CustomerStreet,
            customer_city = @CustomerCity, customer_postal_code = @CustomerPostalCode, customer_ico = @CustomerIco, customer_dic = @CustomerDic,
            customer_email = @CustomerEmail, supplier_name = @SupplierName, supplier_street = @SupplierStreet, supplier_city = @SupplierCity,
            supplier_postal_code = @SupplierPostalCode, supplier_ico = @SupplierIco, supplier_dic = @SupplierDic, supplier_register_entry = @SupplierRegisterEntry,
            supplier_bank_account = @SupplierBankAccount, supplier_iban = @SupplierIban, supplier_issued_by = @SupplierIssuedBy, original_document_id = @OriginalDocumentId,
            correction_reason = @CorrectionReason
        WHERE id = @Id AND document_type = @DocumentType AND status = 'Draft'
        """;

    public const string InsertLines = """
        INSERT INTO document_lines (document_id, position, description, quantity, unit, unit_price, vat_rate, discount_percent)
        VALUES (@DocumentId, @Position, @Description, @Quantity, @Unit, @UnitPrice, @VatRate, @DiscountPercent)
        """;
    public const string UpdateStatus = "UPDATE documents SET status = @Status WHERE id = @Id AND status = 'Issued'";
    public const string DeleteDraft = "DELETE FROM documents WHERE id = @Id AND status = 'Draft'";
    public const string DeleteLines = "DELETE FROM document_lines WHERE document_id = @Id";
    public const string ReadSequence = "SELECT last_number FROM number_series WHERE series_code = @Series AND year = @Year";
    public const string WriteSequence = """
        INSERT INTO number_series (series_code, year, last_number) VALUES (@Series, @Year, @LastNumber)
        ON CONFLICT (series_code, year) DO UPDATE SET last_number = excluded.last_number
        """;
}
