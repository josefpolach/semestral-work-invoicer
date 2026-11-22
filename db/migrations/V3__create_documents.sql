CREATE TABLE documents (
    id                      INTEGER PRIMARY KEY,
    document_type           TEXT    NOT NULL CHECK (document_type IN ('TaxInvoice', 'NonVatInvoice', 'CreditNote')),
    number                  TEXT    UNIQUE,
    status                  TEXT    NOT NULL CHECK (status IN ('Draft', 'Issued', 'Paid', 'Cancelled')),
    issue_date              TEXT,
    taxable_supply_date     TEXT,
    due_date                TEXT,
    customer_id             INTEGER NOT NULL REFERENCES customers (id),
    -- Snapshot of the customer, filled when the document is issued
    customer_name           TEXT,
    customer_street         TEXT,
    customer_city           TEXT,
    customer_postal_code    TEXT,
    customer_ico            TEXT,
    customer_dic            TEXT,
    customer_email          TEXT,
    -- Snapshot of the supplier, filled when the document is issued
    supplier_name           TEXT,
    supplier_street         TEXT,
    supplier_city           TEXT,
    supplier_postal_code    TEXT,
    supplier_ico            TEXT,
    supplier_dic            TEXT,
    supplier_register_entry TEXT,
    supplier_bank_account   TEXT,
    supplier_iban           TEXT,
    supplier_issued_by      TEXT,
    original_document_id    INTEGER REFERENCES documents (id),
    correction_reason       TEXT
);

CREATE TABLE document_lines (
    id               INTEGER PRIMARY KEY,
    document_id      INTEGER NOT NULL REFERENCES documents (id) ON DELETE CASCADE,
    position         INTEGER NOT NULL,
    description      TEXT    NOT NULL,
    quantity         TEXT    NOT NULL,
    unit             TEXT    NOT NULL,
    unit_price       TEXT    NOT NULL,
    vat_rate         TEXT    NOT NULL,
    discount_percent TEXT    NOT NULL,
    UNIQUE (document_id, position)
);
