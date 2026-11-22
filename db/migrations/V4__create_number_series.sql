CREATE TABLE number_series (
    series_code TEXT    NOT NULL,
    year        INTEGER NOT NULL,
    last_number INTEGER NOT NULL,
    PRIMARY KEY (series_code, year)
);
