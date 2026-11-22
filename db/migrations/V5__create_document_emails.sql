-- One row per successfully sent e-mail
CREATE TABLE document_emails (
    id                  INTEGER PRIMARY KEY,
    document_id         INTEGER NOT NULL REFERENCES documents (id),
    sent_to             TEXT    NOT NULL,
    sent_at             TEXT    NOT NULL,
    provider_request_id TEXT    NOT NULL
);
