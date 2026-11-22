CREATE TABLE supplier (
    id               INTEGER PRIMARY KEY CHECK (id = 1),
    name             TEXT    NOT NULL,
    street           TEXT    NOT NULL,
    city             TEXT    NOT NULL,
    postal_code      TEXT    NOT NULL,
    ico              TEXT    NOT NULL,
    dic              TEXT,
    is_vat_payer     INTEGER NOT NULL CHECK (is_vat_payer IN (0, 1)),
    register_entry   TEXT    NOT NULL,
    bank_account     TEXT    NOT NULL,
    iban             TEXT    NOT NULL,
    issued_by        TEXT    NOT NULL,
    default_due_days INTEGER NOT NULL CHECK (default_due_days >= 0)
);
