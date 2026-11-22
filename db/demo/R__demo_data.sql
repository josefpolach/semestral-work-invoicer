-- Optional sample records for a freshly migrated, empty database.
INSERT OR IGNORE INTO supplier
    (id, name, street, city, postal_code, ico, dic, is_vat_payer, register_entry,
     bank_account, iban, issued_by, default_due_days)
VALUES (1, 'Modrý papír s.r.o.', 'Lipová 19', 'Praha', '14000', '71234560', 'CZ71234560', 1,
        'Zapsaná v obchodním rejstříku vedeném Městským soudem v Praze, oddíl C, vložka 12345',
        '19-2000145399/0800', 'CZ6508000000192000145399', 'Tereza Nováková', 14);

INSERT OR IGNORE INTO customers (id, name, street, city, postal_code, ico, dic, email)
VALUES
    (1, 'Červená dílna s.r.o.', 'Javorová 15', 'Brno', '60200', '72345675', 'CZ72345675', 'dilna@example.com'),
    (2, 'Lesní ateliér s.r.o.', 'Jedlová 16', 'Plzeň', '30100', '73456781', 'CZ73456781', 'atelier@example.com'),
    (3, 'Jan Malý', 'Říční 10', 'Olomouc', '77900', NULL, NULL, 'jan@example.com');

INSERT OR IGNORE INTO documents (id, document_type, status, issue_date, taxable_supply_date, due_date, customer_id)
VALUES (1, 'TaxInvoice', 'Draft', '2026-11-22', '2026-11-22', '2026-12-06', 1);

INSERT OR IGNORE INTO documents (
    id, document_type, number, status, issue_date, taxable_supply_date, due_date, customer_id,
    customer_name, customer_street, customer_city, customer_postal_code, customer_ico, customer_dic, customer_email,
    supplier_name, supplier_street, supplier_city, supplier_postal_code, supplier_ico, supplier_dic,
    supplier_register_entry, supplier_bank_account, supplier_iban, supplier_issued_by
)
SELECT 2, 'TaxInvoice', '2026-0001', 'Issued', '2026-10-01', '2026-10-01', '2026-10-15', c.id,
    c.name, c.street, c.city, c.postal_code, c.ico, c.dic, c.email,
    s.name, s.street, s.city, s.postal_code, s.ico, s.dic, s.register_entry, s.bank_account, s.iban, s.issued_by
FROM supplier s CROSS JOIN customers c WHERE s.id = 1 AND c.id = 2;

INSERT OR IGNORE INTO document_lines
    (id, document_id, position, description, quantity, unit, unit_price, vat_rate, discount_percent)
VALUES
    (1, 1, 1, 'Grafické práce', '2', 'hod', '1000.00', '21', '0'),
    (2, 1, 2, 'Příprava podkladů', '1', 'ks', '500.50', '21', '10'),
    (3, 1, 3, 'Tištěná publikace', '3', 'ks', '120.00', '12', '0'),
    (4, 2, 1, 'Grafické práce', '2', 'hod', '1000.00', '21', '0');

INSERT OR IGNORE INTO number_series (series_code, year, last_number)
VALUES ('Invoice', 2026, 1);
