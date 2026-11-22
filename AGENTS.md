# AGENTS.md: Invoicing app (Komponentová tvorba, ČZU)

Instructions for the coding agent that builds this project. Read the **whole file** before you write any code. It contains the full agreed specification (v1.2, 22 Nov 2026), the implementation decisions, the order of work, and the checks to run before you report that something is done.

---

## 0. How to work with this file

1. **This file is the source of truth.** If something here contradicts your habits or "best practice", follow this file. If something here contradicts itself or is impossible, **stop and ask the user**. Do not guess.
2. **Do not change the agreed design** (libraries, architecture, database, project structure, scope) on your own. If you think a change is needed, explain the problem, propose 1–3 alternatives, and wait for the user's decision.
3. **Work in phases** (section 7). At the end of every phase: build with zero warnings, run all tests (all must pass), commit, then give the user a short report (what was done, what to try, anything unclear) and **wait for the go-ahead** unless the user told you to continue without stopping.
4. **Library APIs change.** Version numbers and API names in this file were checked in November 2026. If the installed version of a library behaves differently, follow the installed version's documentation, keep the intent of this file, and mention the difference in your report.
5. **Never print, log or commit secrets.** Do not open or print the user's `.env` file. Use `.env.example` to learn the keys.
6. **Do not call real external services from tests** (ARES, Lettr). Do not send real e-mails yourself.

---

## 1. What we are building

A desktop application for issuing invoices that comply with Czech law. It is a semester project for the course *Komponentová tvorba* at ČZU. The student develops on **CachyOS Linux (KDE) in JetBrains Rider**. The teacher may run the app on **Windows**.

The app can:

- keep the supplier's details (Dodavatel), with a lookup in the ARES company register;
- keep an address book of customers (Adresář), with ARES lookup;
- create, edit and issue three document types:
  - **TaxInvoice**: *faktura – daňový doklad* (supplier is a VAT payer, § 29 of the VAT act)
  - **NonVatInvoice**: *faktura* of a non-payer ("Nejsem plátce DPH.")
  - **CreditNote**: *opravný daňový doklad* (§ 45), created from an issued tax invoice
- calculate VAT correctly, assign document numbers without gaps, export a PDF with a **QR Platba** code, and send the PDF by **e-mail** through the Lettr API;
- store everything in **SQLite**; the schema is created by **Flyway** running in **Docker Compose**.

---

## 2. Who the code is written for (very important)

The author is a **second-year university student**. The code must be **correct, clean and easy to read**, but it must look like the work of a good student, **not a senior architect**.

Do:
- Use plain classes, properties, constructors, `List<T>`, `foreach`, simple LINQ (`Where`, `Select`, `Sum`, `GroupBy`, `OrderBy`), `switch` expressions, `async`/`await`.
- Use small `record` types only for small immutable data (e.g. `ValidationMessage`, `DocumentReference`).
- Wire objects by hand in one composition root (no DI container).
- Prefer obvious code over clever code. If a student could not explain a line at the oral exam, rewrite it.

Do not use:
- DI containers, MediatR, AutoMapper, FluentValidation, Polly, Serilog or any logging framework, ReactiveUI, EF Core, generic repositories, `Result<T>` monads, functional pipelines, reflection, expression trees, custom source generators, `dynamic`.
- Static mutable global state (no static "Database" class holding data).
- Abstractions "for the future" that nothing uses yet.

Size guidelines:
- Methods: aim for **≤ 15 lines**, hard limit about 25. One method = one job.
- Classes: aim for **≤ 200 lines**. Split by responsibility if larger.
- Parameters: **≤ 4**. Use a small parameter object otherwise.
- Nesting: **≤ 2 levels**. Use early returns.

---

## 3. Hard rules (non-negotiable)

1. **Language**
   - All code is in **English**: identifiers, comments, commit messages, exception messages meant for developers, README.
   - All UI, PDF and e-mail text is in **Czech** with correct diacritics.
   - Czech strings live **only** in `Invoicing.App`, `Invoicing.Pdf` and `Invoicing.Email` (templates). `Invoicing.Core` returns **codes (enums)**, which the UI turns into Czech messages.
2. **Money is always `decimal`.** Never `double` or `float`. Rounding is always `Math.Round(value, 2, MidpointRounding.AwayFromZero)`.
3. **Dates are `DateOnly`** in the domain. Store them in SQLite as `TEXT` in `yyyy-MM-dd`.
4. **Secrets:** the Lettr API key is never logged, never shown in the UI, never committed, never put into a release. Do not make a `record` out of any class that holds the key (a record's generated `ToString()` prints every property).
5. **No hidden errors.** No empty `catch`. No `catch (Exception)` except in the single global UI error handler (section 14.10). Catch only exceptions you expect and turn them into a code or a Czech message the user sees.
6. **No magic constants.** Every number or string with a meaning gets a named constant (`private const int MaxDaysBetweenSupplyAndIssue = 15;`). Obvious values such as `0`, `1`, `100` in a percentage formula are fine.
7. **No dead code.** No commented-out code, no unused classes or methods, no template leftovers, no `TODO` comments in the final result.
8. **Private fields only.** All non-constant fields are `private`. Expose data with properties (auto-properties are fine) or methods.
9. **Inheritance only for real "is-a" relationships** (e.g. `TaxInvoice` is a `Document`, `Customer` is a `Party`).
10. **Layers:** UI (App) → Core ← infrastructure (Data, Ares, Pdf, Email). Core depends on nothing. ViewModels contain no SQL, no HTTP and no PDF code.
11. **Async all the way.** No `.Result`, `.Wait()` or `async void` (except Avalonia event handlers, which we avoid by using commands).
12. **Warnings are errors.** Fix warnings; do not suppress them with `#pragma` or the `!` operator unless the value truly cannot be null (and then add a short comment why).
13. **Do not add features from the out-of-scope list** (section 20).

---

## 4. Course requirements and what they mean in code

The course (in Czech) requires the following. Every item must be true at the end.

| Requirement (CZ) | What you do |
|---|---|
| aplikace má grafické rozhraní | Avalonia desktop app (section 5.1 explains why not WinForms/WPF). |
| konvence pro pojmenovávání v .NET | PascalCase types/methods/properties, `_camelCase` private fields, `camelCase` locals/parameters, `I` prefix for interfaces, `Async` suffix for async methods. Enforced by `.editorconfig`. |
| srozumitelné a popisné pojmenování | `CalculateVatSummary`, not `Calc`; `customerRepository`, not `repo2`. No abbreviations except domain ones (IČO → `Ico`, DIČ → `Dic`, DUZP → `TaxableSupplyDate`). |
| kód je přehledně formátován | `.editorconfig` + `dotnet format`. File-scoped namespaces, one type per file, 4 spaces. |
| kód neobsahuje dlouhé metody | See size guidelines in section 2. |
| projekt neobsahuje mrtvý kód | Remove template samples (e.g. the "Welcome to Avalonia" greeting), unused usings, unused members. |
| každá metoda provádí jen jednu činnost | Split "load + validate + save" into three methods. |
| kód je dobře a smysluplně komentován | XML `/// <summary>` on public classes and on public members whose purpose is not obvious from the name. Inline comments explain **why** (e.g. a legal rule: `// § 37 VAT act: VAT is calculated from the sum of bases per rate`). |
| neobsahuje zastaralé/zbytečné komentáře | No comments that repeat the code (`// increment i`). Update or delete comments when code changes. |
| data a operace v jedné třídě | `InvoiceLine` calculates its own base; `Document` validates itself; `CzechBankAccount` converts itself to IBAN. |
| nekonstantní atributy privátní | See rule 8. |
| dědičnost = vztah "je" | `Document` → `TaxInvoice`, `NonVatInvoice`, `CreditNote`; `Party` → `Customer`, `Supplier`. Nothing else. |
| kód pokryt jednotkovými testy, všechny projdou | xUnit tests in section 15; `dotnet test` must be green. |
| aplikace rozdělena do vrstev | Separate projects (section 6), MVVM in the UI. |
| bez magických konstant, božských objektů, skrývání chyb | Rules 5 and 6; no class that "does everything" (e.g. no 1000-line MainViewModel). |
| + industry standards | Nullable reference types on, warnings as errors, async I/O, parameterised SQL (never string-concatenated SQL), `HttpClient` reused, no secrets in git. |

---

## 5. Tech stack

| Area | Choice | Notes |
|---|---|---|
| Runtime | .NET 10, C# | `net10.0` everywhere |
| UI | **Avalonia 12.x**, MVVM with **CommunityToolkit.Mvvm 8.x** | Fluent theme. Compiled bindings are on by default in Avalonia 12, so every view needs `x:DataType`. |
| Grid | **Avalonia.Controls.DataGrid** (same version as Avalonia) | Needs a `StyleInclude` (section 14.2). |
| Database | **SQLite** via **Microsoft.Data.Sqlite** + **Dapper** | Plain SQL. EF Core is **not** used (README explains why). |
| Migrations | **Flyway** in Docker Compose (official `flyway/flyway` image, pinned version) | No Dockerfile. |
| PDF | **QuestPDF** (Community licence) | Bundled font with Czech characters. |
| QR code | **QRCoder** (`PngByteQRCode`) | The SPAYD text is built by our own code. |
| Company lookup | ARES REST API via `HttpClient` | No API key. |
| E-mail | **Lettr** transactional API via `HttpClient` | Templates live in our app, not in Lettr. |
| Tests | **xUnit** (use what `dotnet new xunit` gives you) | No mocking library is needed; write small fakes by hand. |
| Config | `.env` file; real environment variables override it | Own small parser, no package. |
| Diagrams | Mermaid in Markdown | |

Use the **latest stable** version of each NuGet package when you create the project and pin the exact versions in the `.csproj` files. All `Avalonia*` packages must have the **same** version. Known current lines (Nov 2026): Avalonia 12.1.x, QuestPDF 2026.x.

### 5.1 Why Avalonia (for the README, keep it short)

- WinForms and WPF run only on Windows; the app is developed on Linux, and Rider's WinForms designer works only on Windows.
- Avalonia runs on Linux, Windows and macOS from the same code. The teacher gets a self-contained Windows `.exe` that runs without installing .NET.
- Avalonia is very close to WPF: the same XAML, data binding and MVVM. This matches "WPF, ..." in the course requirements, and MVVM separates the UI from the logic.
- Alternatives considered: WinForms/WPF (Windows only), .NET MAUI (no Linux desktop), Qt Quick via Qt Bridge for C# (QML instead of XAML, needs a C++ toolchain, less mature), Quickshell (a Linux desktop-shell toolkit, not for applications).

---

## 6. Repository layout

```
<repo root>/
├── AGENTS.md                      ← this file
├── README.md
├── Invoicing.sln                  ← classic .sln (best compatibility with Rider and Visual Studio)
├── Directory.Build.props
├── .editorconfig
├── .gitignore
├── .gitattributes
├── .env.example                   ← committed; .env is git-ignored
├── docker-compose.yml
├── data/
│   └── .gitkeep                   ← the SQLite file is created here by Flyway; *.db is git-ignored
├── db/
│   ├── migrations/
│   │   ├── V1__create_supplier.sql
│   │   ├── V2__create_customers.sql
│   │   ├── V3__create_documents.sql
│   │   ├── V4__create_number_series.sql
│   │   └── V5__create_document_emails.sql
│   └── demo/
│       └── R__demo_data.sql
├── docs/
│   ├── class-diagram.md
│   └── activity-diagram.md
├── scripts/
│   └── build-windows-release.sh
├── src/
│   ├── Invoicing.Core/
│   ├── Invoicing.Data/
│   ├── Invoicing.Ares/
│   ├── Invoicing.Pdf/
│   ├── Invoicing.Email/
│   └── Invoicing.App/
└── tests/
    └── Invoicing.Tests/
```

### 6.1 Projects (components) and references

| Project | Type | Contains | References |
|---|---|---|---|
| `Invoicing.Core` | class library | Domain model, VAT calculation, validation, numbering rules, SPAYD, IBAN, IČO validator, configuration parsing, e-mail availability rules, formatting helpers, interfaces (`I…Repository`, `ICompanyRegistry`, `IEmailSender`), application services | **nothing** |
| `Invoicing.Data` | class library | SQLite repositories (Dapper), connection factory, database version check | Core |
| `Invoicing.Ares` | class library | ARES client implementing `ICompanyRegistry` | Core |
| `Invoicing.Pdf` | class library | QuestPDF documents, QR image, bundled font, PDF file names | Core |
| `Invoicing.Email` | class library | Lettr client implementing `IEmailSender`, Czech templates (embedded resources), renderer, composer | Core |
| `Invoicing.App` | Avalonia app (`WinExe`) | Views, ViewModels, Czech texts, dialogs, composition root | all of the above |
| `Invoicing.Tests` | xUnit | Unit tests (+ a few SQLite tests) | Core, Data, Ares, Pdf, Email (**not** App) |

NuGet packages per project:

| Project | Packages |
|---|---|
| Core | none |
| Data | `Microsoft.Data.Sqlite`, `Dapper` |
| Ares | none (`System.Net.Http.Json` is part of .NET) |
| Pdf | `QuestPDF`, `QRCoder` |
| Email | none |
| App | `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, `Avalonia.Controls.DataGrid`, `CommunityToolkit.Mvvm` (+ whatever the Avalonia template adds for dev tools, Debug only) |
| Tests | the packages from the `xunit` template |

---

## 7. Phases (order of work)

| Phase | Content | Done when |
|---|---|---|
| 0 | Tooling check, solution skeleton, config files, Docker Compose, migrations | `dotnet build` is clean; `docker compose run --rm flyway` creates `data/invoicing.db` |
| 1 | Core: parties, lines, VAT, documents, lifecycle, validation, numbering, IČO, bank account/IBAN, SPAYD, configuration, formatting, e-mail availability, interfaces, services + tests | All Core tests pass |
| 2 | Data: connection factory, repositories, version check + tests | Tests pass against a temporary SQLite file |
| 3 | ARES client + tests | Tests pass with a fake HTTP handler |
| 4 | PDF + QR + font + smoke tests | A PDF of each document type is generated in tests |
| 5 | E-mail: templates, renderer, composer, Lettr client + tests | Tests pass with a fake HTTP handler |
| 6 | App part 1: startup checks, composition root, Dodavatel, Adresář, Doklady, Detail dokladu, PDF export | Manual scenario in section 14.11 works up to "Export PDF" |
| 7 | App part 2: e-mail send dialog, status line, "Odesláno …" | Sandbox e-mail can be sent by the user |
| 8 | Demo data, Windows release script, README, Mermaid diagrams | Release zip builds; README complete |
| 9 | Final quality pass | Every item of the checklist in section 21 is true |

Commit at least once per phase. Commit message style: imperative English, e.g. `Add VAT summary calculation`. Add any attribution lines the user's environment requires.

---

## 8. Phase 0: setup

### 8.1 Check tools

```bash
dotnet --version          # must be 10.x
docker compose version    # Compose v2
git --version
```

If something is missing, tell the user. On CachyOS: `paru -S dotnet-sdk docker docker-compose`, then `sudo systemctl enable --now docker` and add the user to the `docker` group (re-login needed). Do not run these yourself without the user's permission.

### 8.2 Create the solution

```bash
dotnet new sln -n Invoicing --format sln
dotnet new classlib -n Invoicing.Core  -o src/Invoicing.Core
dotnet new classlib -n Invoicing.Data  -o src/Invoicing.Data
dotnet new classlib -n Invoicing.Ares  -o src/Invoicing.Ares
dotnet new classlib -n Invoicing.Pdf   -o src/Invoicing.Pdf
dotnet new classlib -n Invoicing.Email -o src/Invoicing.Email
dotnet new install Avalonia.Templates
dotnet new avalonia.mvvm -n Invoicing.App -o src/Invoicing.App -m CommunityToolkit --remove-view-locator
dotnet new xunit -n Invoicing.Tests -o tests/Invoicing.Tests
# add all projects to the solution, add project references as in section 6.1
```

- If an Avalonia template option is not accepted, create the project with defaults and adjust by hand (target `net10.0`, CommunityToolkit.Mvvm, no ViewLocator).
- Delete the template's `Class1.cs`, `UnitTest1.cs`, the greeting property and any sample content.
- Remove `<TargetFramework>`, `<Nullable>` and `<ImplicitUsings>` from individual `.csproj` files; they come from `Directory.Build.props`.

### 8.3 `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <!-- Required so that IDE0005 (unused usings) is reported during build -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <!-- XML docs are not required on every public member (see section 4) -->
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
```

### 8.4 `.editorconfig`

Create a standard .NET `.editorconfig`. It must at least contain:

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.{json,yml,yaml,xml,csproj,props,axaml}]
indent_size = 2

[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
csharp_prefer_braces = true:warning
csharp_style_var_when_type_is_apparent = true:suggestion
dotnet_sort_system_directives_first = true
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_readonly_field = true:warning
dotnet_diagnostic.IDE0005.severity = warning   # unused usings
dotnet_diagnostic.IDE0051.severity = warning   # unused private members
dotnet_diagnostic.IDE0052.severity = warning   # unread private members

# Naming
dotnet_naming_symbols.interfaces.applicable_kinds = interface
dotnet_naming_style.prefix_i.required_prefix = I
dotnet_naming_style.prefix_i.capitalization = pascal_case
dotnet_naming_rule.interfaces_start_with_i.symbols = interfaces
dotnet_naming_rule.interfaces_start_with_i.style = prefix_i
dotnet_naming_rule.interfaces_start_with_i.severity = warning

dotnet_naming_symbols.types_and_members.applicable_kinds = class, struct, enum, property, method, event, delegate
dotnet_naming_style.pascal.capitalization = pascal_case
dotnet_naming_rule.types_and_members_pascal.symbols = types_and_members
dotnet_naming_rule.types_and_members_pascal.style = pascal
dotnet_naming_rule.types_and_members_pascal.severity = warning

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_camel.required_prefix = _
dotnet_naming_style.underscore_camel.capitalization = camel_case
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_camel
dotnet_naming_rule.private_fields_underscore.severity = warning

dotnet_naming_symbols.constants.applicable_kinds = field
dotnet_naming_symbols.constants.required_modifiers = const
dotnet_naming_rule.constants_pascal.symbols = constants
dotnet_naming_rule.constants_pascal.style = pascal
dotnet_naming_rule.constants_pascal.severity = warning
```

Note: naming rules are matched in order of specificity; make sure `const` fields are PascalCase and other private fields are `_camelCase`. Check with `dotnet format --verify-no-changes`.

### 8.5 `.gitignore` and `.gitattributes`

`.gitignore`: start from `dotnet new gitignore`, then add:

```
.env
data/*.db
data/*.db-*
artifacts/
.idea/
```

`.gitattributes`:

```
* text=auto eol=lf
*.png binary
*.ttf binary
```

### 8.6 `.env.example` (committed)

```
# Copy this file to .env and fill it in. The .env file is never committed.

# Required
VAT_RATE_STANDARD=21
VAT_RATE_REDUCED=12
# A relative path is resolved against the application folder (the folder that contains .env).
# For development, use an absolute path to <repo>/data/invoicing.db.
DATABASE_PATH=data/invoicing.db
ARES_BASE_URL=https://ares.gov.cz/ekonomicke-subjekty-v-be/rest
LETTR_BASE_URL=https://app.lettr.com/api

# Optional: e-mail sending is enabled only when both values are set and valid
LETTR_API_KEY=
EMAIL_FROM_ADDRESS=
```

The App project copies the repo's `.env` into the build output **only in Debug** and **never** into a publish output (so a real key can never get into a release):

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <None Include="..\..\.env" Link=".env"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="Never"
        Condition="Exists('..\..\.env')" />
</ItemGroup>
```

### 8.7 `docker-compose.yml`

Only Flyway runs in Docker. The app runs on the host. Pin the image to the newest **exact** stable tag (look it up on Docker Hub `flyway/flyway`; Flyway is at 13.x in Nov 2026). Never use `latest`.

```yaml
# Database migrations only. The application itself runs on the host.
services:
  flyway:
    image: flyway/flyway:<PINNED_VERSION>
    user: "${UID:-1000}:${GID:-1000}"     # the created .db file must belong to the host user
    volumes:
      - ./db/migrations:/flyway/sql:ro
      - ${DATA_DIR:-./data}:/flyway/db
    command: >
      -url=jdbc:sqlite:/flyway/db/invoicing.db
      -locations=filesystem:/flyway/sql
      -ignoreMigrationPatterns=repeatable:missing
      migrate

  flyway-demo:
    image: flyway/flyway:<PINNED_VERSION>
    profiles: ["demo"]
    user: "${UID:-1000}:${GID:-1000}"
    volumes:
      - ./db/migrations:/flyway/sql:ro
      - ./db/demo:/flyway/demo:ro
      - ${DATA_DIR:-./data}:/flyway/db
    command: >
      -url=jdbc:sqlite:/flyway/db/invoicing.db
      -locations=filesystem:/flyway/sql,filesystem:/flyway/demo
      migrate
```

Usage:

```bash
docker compose run --rm flyway                       # create or upgrade the schema
docker compose --profile demo run --rm flyway-demo   # schema + demo data
```

Why `ignoreMigrationPatterns=repeatable:missing`: after the demo data was applied once, the normal `flyway` service does not see `R__demo_data.sql` and would otherwise fail validation.

After the first run, check that `data/invoicing.db` is owned by the user (`ls -l data`). If the user's id is not 1000, they run `export UID; export GID=$(id -g)` first (document this in the README). If Flyway fails because of file permissions under `user:`, stop and tell the user. Do not switch to running as root.

### 8.8 Migrations (exact SQL)

Tables use `snake_case`. Money is `TEXT` (invariant culture, e.g. `1234.50`) because SQLite has no exact decimal type. Enums are stored as `TEXT` with the enum member name.

`db/migrations/V1__create_supplier.sql`

```sql
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
```

`db/migrations/V2__create_customers.sql`

```sql
CREATE TABLE customers (
    id          INTEGER PRIMARY KEY,
    name        TEXT NOT NULL,
    street      TEXT NOT NULL,
    city        TEXT NOT NULL,
    postal_code TEXT NOT NULL,
    ico         TEXT,
    dic         TEXT,
    email       TEXT
);
```

`db/migrations/V3__create_documents.sql`

```sql
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
```

`db/migrations/V4__create_number_series.sql`

```sql
CREATE TABLE number_series (
    series_code TEXT    NOT NULL,
    year        INTEGER NOT NULL,
    last_number INTEGER NOT NULL,
    PRIMARY KEY (series_code, year)
);
```

`db/migrations/V5__create_document_emails.sql`

```sql
-- One row per successfully sent e-mail
CREATE TABLE document_emails (
    id                  INTEGER PRIMARY KEY,
    document_id         INTEGER NOT NULL REFERENCES documents (id),
    sent_to             TEXT    NOT NULL,
    sent_at             TEXT    NOT NULL,
    provider_request_id TEXT    NOT NULL
);
```

`sent_at` is stored as ISO 8601 with offset (`DateTimeOffset.ToString("O")`). Demo data: section 16.

---

## 9. Phase 1: `Invoicing.Core`

Suggested folders and namespaces:

```
Invoicing.Core/
  Parties/        Party, Customer, Supplier
  Documents/      Document, TaxInvoice, NonVatInvoice, CreditNote, DocumentHeader, DocumentKind,
                  DocumentStatus, DocumentReference, InvoiceLine, VatSummary, VatSummaryRow,
                  DocumentNumbering, NumberSeries
  Validation/     ValidationMessage, ValidationCode, ValidationTarget, ValidationSeverity,
                  IcoValidator, DicValidator, EmailAddressValidator
  Payments/       CzechBankAccount, SpaydPayment
  Configuration/  SettingKeys, EnvFileParser, EnvFileFormatException, AppSettings, SettingsLoadResult,
                  ConfigurationError, ConfigurationProblem, VatRates, EmailSettings
  Companies/      ICompanyRegistry, CompanyInfo, CompanyLookupResult, CompanyLookupError
  Email/          IEmailSender, EmailMessage, EmailAttachment, EmailSendResult, EmailSendError,
                  EmailAvailability, EmailUnavailableReason, DocumentEmailRecord
  Repositories/   ISupplierRepository, ICustomerRepository, IDocumentRepository, IDocumentEmailRepository
  Services/       DocumentService, DocumentEmailService
  Formatting/     CzechFormat, CzechNumberParser
```

The code sketches below show **names and signatures**. Write the bodies yourself, following the rules.

### 9.1 Parties

```csharp
/// <summary>Common data of a business party (supplier or customer).</summary>
public abstract class Party
{
    public string Name { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Ico { get; set; }
    public string? Dic { get; set; }

    public bool HasCompleteAddress { get; }      // street, city and postal code are not blank
    protected void ValidateNameAndAddress(List<ValidationMessage> messages, ValidationTarget target);
    protected void ValidateDicFormatIfPresent(List<ValidationMessage> messages, ValidationTarget target);
}

public class Customer : Party
{
    public int Id { get; set; }                  // 0 = not saved yet
    public string? Email { get; set; }           // default recipient for e-mails
    public IReadOnlyList<ValidationMessage> Validate();   // name, address, IČO valid if filled, DIČ format if filled, e-mail valid if filled
    public Customer CreateSnapshot();            // a copy with the same values
}

public class Supplier : Party
{
    public bool IsVatPayer { get; set; }
    public string RegisterEntry { get; set; } = string.Empty;  // e.g. "zapsaná v OR vedeném Městským soudem v Praze, oddíl C, vložka 12345"
    public string BankAccount { get; set; } = string.Empty;    // "prefix-number/bankCode", prefix optional
    public string Iban { get; set; } = string.Empty;           // calculated from BankAccount (section 9.7)
    public string IssuedBy { get; set; } = string.Empty;       // "Vystavil"
    public int DefaultDueDays { get; set; }
    public IReadOnlyList<ValidationMessage> Validate();        // rules in section 9.5
    public Supplier CreateSnapshot();
}
```

Customers are always Czech. There is exactly one supplier. The supplier may not exist yet (empty database); repositories return `null` then.

### 9.2 Invoice line and money

```csharp
/// <summary>One line of a document. Immutable; the UI replaces lines instead of changing them.</summary>
public class InvoiceLine
{
    public InvoiceLine(string description, decimal quantity, string unit,
                       decimal unitPriceWithoutVat, decimal vatRate, decimal discountPercent);
    public string Description { get; }
    public decimal Quantity { get; }
    public string Unit { get; }
    public decimal UnitPriceWithoutVat { get; }
    public decimal VatRate { get; }          // percent, e.g. 21; 0 on NonVatInvoice lines (not shown anywhere)
    public decimal DiscountPercent { get; }  // 0 = no discount

    public decimal CalculateBase();          // round(quantity × unitPrice × (1 − discount/100), 2), AwayFromZero
    public InvoiceLine WithNegatedQuantity(); // used when a credit note is created
}
```

The constructor does **not** throw for invalid values (the user is still typing); `Document.Validate()` reports them.

### 9.3 VAT summary (§ 37 "zdola" method)

```csharp
public record VatSummaryRow(decimal VatRate, decimal TaxBase, decimal VatAmount)
{
    public decimal Total => TaxBase + VatAmount;
}

public class VatSummary
{
    public static VatSummary Calculate(IEnumerable<InvoiceLine> lines);
    public IReadOnlyList<VatSummaryRow> Rows { get; }   // one row per rate, highest rate first
    public decimal TotalBase { get; }
    public decimal TotalVat { get; }
    public decimal Total { get; }                       // TotalBase + TotalVat; no rounding line (bank transfer only)
}
```

- Group lines by `VatRate`. Base per rate = sum of line bases. VAT per rate = `round(base × rate / 100, 2)`, AwayFromZero.
- Works for negative amounts (credit notes): `round(-0.525) = -0.53`.

### 9.4 Documents

```
Document (abstract)
├── TaxInvoice     faktura – daňový doklad (§ 29)       has TaxableSupplyDate, VAT summary
├── NonVatInvoice  faktura neplátce                     no VAT, no DUZP
└── CreditNote     opravný daňový doklad (§ 45)         has TaxableSupplyDate, OriginalInvoice, CorrectionReason, VAT summary
```

Supporting types:

```csharp
public enum DocumentKind { TaxInvoice, NonVatInvoice, CreditNote }
public enum DocumentStatus { Draft, Issued, Paid, Cancelled }
public record DocumentReference(int Id, string Number);   // the original invoice of a credit note

/// <summary>Common persisted state of a document; keeps constructors short.</summary>
public record DocumentHeader
{
    public int Id { get; init; }
    public string? Number { get; init; }
    public DocumentStatus Status { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public Customer? Customer { get; init; }
    public Supplier? Supplier { get; init; }       // snapshot; null until issued
    public static DocumentHeader NewDraft(Customer? customer, DateOnly issueDate, DateOnly dueDate);
}
```

`Document` (base class) responsibilities:

```csharp
public abstract class Document
{
    protected Document(DocumentHeader header, IEnumerable<InvoiceLine> lines);

    public abstract DocumentKind Kind { get; }
    public int Id { get; private set; }
    public string? Number { get; private set; }
    public DocumentStatus Status { get; private set; }
    public DateOnly? IssueDate { get; set; }      // setter calls EnsureEditable()
    public DateOnly? DueDate { get; set; }        // setter calls EnsureEditable()
    public Customer? Customer { get; set; }       // setter calls EnsureEditable()
    public Supplier? Supplier { get; private set; }
    public IReadOnlyList<InvoiceLine> Lines { get; }

    public bool IsEditable { get; }               // Status == Draft
    public void AssignId(int id);                 // called once by the repository after INSERT; throws if Id is already set
    public void ReplaceLines(IEnumerable<InvoiceLine> lines);   // EnsureEditable()
    public decimal CalculateTotalBase();          // sum of line bases
    public abstract decimal CalculateTotal();

    public abstract IReadOnlyList<ValidationMessage> Validate(Supplier? supplier, VatRates vatRates);

    public void Issue(string number, Supplier supplier);   // Draft only; sets Number, snapshots (Supplier.CreateSnapshot(), Customer.CreateSnapshot()), Status = Issued
    public virtual bool CanMarkAsPaid { get; }    // false here
    public void MarkAsPaid();                     // throws InvalidOperationException if !CanMarkAsPaid
    public virtual bool CanCancel { get; }        // false here
    public void Cancel();                         // throws InvalidOperationException if !CanCancel
    public virtual bool IsOverdue(DateOnly today);// Status == Issued && DueDate < today; CreditNote overrides → false

    protected void EnsureEditable();              // throws InvalidOperationException("Only draft documents can be changed.")
    // protected validation helpers shared by subclasses, e.g.:
    protected void ValidateCustomer(List<ValidationMessage> messages);
    protected void ValidateDates(List<ValidationMessage> messages);
    protected void ValidateLines(List<ValidationMessage> messages, VatRates? vatRates, bool allowNegativeQuantity);
}
```

Subclass specifics:

| | TaxInvoice | NonVatInvoice | CreditNote |
|---|---|---|---|
| Extra properties | `DateOnly? TaxableSupplyDate` (setter → EnsureEditable) | – | `DateOnly? TaxableSupplyDate`, `DocumentReference? OriginalInvoice`, `string CorrectionReason` (setter → EnsureEditable) |
| `CalculateTotal()` | `CalculateVatSummary().Total` | `CalculateTotalBase()` | `CalculateVatSummary().Total` (usually negative) |
| `CalculateVatSummary()` | yes | – | yes |
| `CanMarkAsPaid` | Status == Issued | Status == Issued | false |
| `CanCancel` | false | Status == Issued | false |
| `IsOverdue` | base | base | always false (a credit note cannot become "Paid", so it would stay overdue forever) |
| Other | `bool CanCreateCreditNote` (Issued or Paid); `CreditNote CreateCreditNote(DateOnly today, int dueDays)` | – | – |

`TaxInvoice.CreateCreditNote(today, dueDays)`:
- throws `InvalidOperationException` if `!CanCreateCreditNote`;
- returns a **new Draft** `CreditNote` with: the invoice's customer (snapshot copy), lines copied with **negated quantities**, `OriginalInvoice = new DocumentReference(Id, Number!)`, `IssueDate = today`, `TaxableSupplyDate = today`, `DueDate = today + dueDays`, empty `CorrectionReason`. The user then edits it.

Which type is created for a **new document**: supplier is a VAT payer → `TaxInvoice`, otherwise → `NonVatInvoice`. A `CreditNote` can only be created from an issued or paid `TaxInvoice`.

Defaults for a new document: issue date = today, DUZP = today (tax documents), due date = issue date + `Supplier.DefaultDueDays`. Changing the issue date later does not move the due date.

### 9.4.1 Lifecycle

| Status | Meaning | Allowed for |
|---|---|---|
| Draft | Editable, can be deleted, has no number | all |
| Issued | `Validate()` returned no **errors**; number assigned; snapshot taken; read-only | all |
| Paid | Set manually ("Zaplaceno") | TaxInvoice, NonVatInvoice |
| Cancelled | Set manually ("Stornovat"), irreversible | NonVatInvoice only (a non-payer has no credit note) |

Allowed transitions: Draft → Issued; Issued → Paid (invoices); Issued → Cancelled (NonVatInvoice). Everything else throws `InvalidOperationException` (the UI disables those buttons, so this is a programming-error guard).

**Snapshot on issue:** when a document is issued, supplier and customer details are copied onto it. Later edits in the address book or supplier settings never change issued documents.

**Overdue:** status is Issued and the due date is before today (invoices only).

### 9.5 Validation

```csharp
public enum ValidationSeverity { Error, Warning }
public enum ValidationTarget { Document, Supplier, Customer, Line }
public record ValidationMessage(ValidationCode Code, ValidationTarget Target,
                                ValidationSeverity Severity = ValidationSeverity.Error,
                                int? LinePosition = null);     // 1-based, only for Target == Line
```

`ValidationCode` values (generic codes + a target, so the enum stays small):

```
Missing, NameMissing, AddressIncomplete, IcoMissing, IcoInvalid, DicMissing, DicInvalid,
RegisterEntryMissing, BankAccountInvalid, IssuedByMissing, EmailInvalid,
MustBeVatPayer, MustNotBeVatPayer,
NoLines, DescriptionMissing, QuantityNotPositive, QuantityZero, UnitPriceNegative,
VatRateNotAllowed, DiscountOutOfRange,
IssueDateMissing, DueDateMissing, TaxableSupplyDateMissing, DueDateBeforeIssueDate,
IssuedLongAfterTaxableSupply, CorrectionReasonMissing, OriginalInvoiceMissing
```

Rules:

**Supplier (`Supplier.Validate()`, target Supplier)**
- name not blank; complete address (street, city, postal code);
- IČO present (`IcoMissing`) and valid (`IcoInvalid`);
- if `IsVatPayer`: DIČ present (`DicMissing`); DIČ format valid if present (`DicInvalid`);
- register entry not blank;
- bank account parses as a Czech account (`BankAccountInvalid`), because invoices are paid by bank transfer and the QR code needs the IBAN;
- "Vystavil" not blank (`IssuedByMissing`), because every PDF shows it.

**Customer (`Customer.Validate()`, target Customer)**
- name not blank; complete address;
- IČO valid if filled; DIČ format valid if filled (DIČ is optional for private persons); e-mail valid if filled.

**TaxInvoice** (§ 29 VAT act, § 435 civil code)
- supplier: `Missing` if null; otherwise `Supplier.Validate()` and `MustBeVatPayer` if `!IsVatPayer`;
- customer: `Missing` if null; otherwise `Customer.Validate()`;
- lines: at least one (`NoLines`, target Document); each line: description not blank, quantity > 0, unit price ≥ 0, VAT rate is one of the configured rates, discount between 0 and 100;
- dates: issue date, DUZP and due date set; due date not before issue date;
- **warning** (not an error): issue date is more than 15 days after DUZP (`IssuedLongAfterTaxableSupply`, § 28). Use a named constant for 15.

**NonVatInvoice**
- supplier: as above but `MustNotBeVatPayer` if `IsVatPayer` (DIČ is then not required);
- customer and lines: as for TaxInvoice, but the VAT rate is not checked (VAT appears nowhere; store 0);
- dates: issue date and due date set; due date not before issue date; there is no DUZP.

**CreditNote** (§ 45)
- supplier and customer: as for TaxInvoice;
- `OriginalInvoice` set (`OriginalInvoiceMissing`); correction reason not blank (`CorrectionReasonMissing`);
- lines: at least one; quantity **≠ 0** (`QuantityZero`, negative is normal); other line rules as for TaxInvoice;
- dates: issue date, DUZP, due date set; due date not before issue date. The 15-day deadline is **not** tracked (the app does not know when the facts for the correction became known).

Helpers:

- `IcoValidator.IsValid(string?)`: exactly 8 digits; weights 8,7,6,5,4,3,2 on the first 7 digits; `check = (11 − sum % 11) % 10`; must equal the 8th digit.
- `DicValidator.IsValid(string?)`: `^CZ\d{8,10}$`.
- `EmailAddressValidator.IsValid(string?)`: `MailAddress.TryCreate(value, out var address)` and `address.Address == value.Trim()` (rejects forms like `Name <a@b.cz>`).

"Issue" is allowed only when `Validate()` returns **no messages with severity Error**. Warnings are shown but do not block.

### 9.6 Numbering

```csharp
public enum NumberSeries { Invoice, CreditNote }      // stored in number_series.series_code as 'Invoice' / 'CreditNote'

public static class DocumentNumbering
{
    public static NumberSeries GetSeries(DocumentKind kind);          // both invoice kinds → Invoice
    public static int NextSequenceNumber(int? lastNumberInYear);      // null (first in the year) → 1, otherwise last + 1
    public static string Format(NumberSeries series, int year, int sequence);  // "2026-0001" / "OD-2026-0001"
    public static string ToVariableSymbol(string invoiceNumber);      // "2026-0001" → "20260001"; must be digits only, ≤ 10 digits
}
```

- Invoices (both types) share one series: `2026-0001`. Credit notes have their own: `OD-2026-0001`.
- The year is taken from the document's **issue date**. Each series restarts every year (a new `(series_code, year)` row starts at 1).
- A number is assigned **only when the document is issued**, inside a database transaction (section 10.4), so there are no gaps.

### 9.7 Czech bank account and IBAN

```csharp
public class CzechBankAccount
{
    public static bool TryParse(string? text, out CzechBankAccount? account);  // "19-2000145399/0800" or "2000145399/0800"
    public string Prefix { get; }        // up to 6 digits, may be empty
    public string Number { get; }        // 2–10 digits
    public string BankCode { get; }      // exactly 4 digits
    public string ToIban();
}
```

- Format: `^(?:(\d{1,6})-)?(\d{2,10})/(\d{4})$`.
- IBAN: `CZ` + 2 check digits + bank code (4) + prefix (6, zero-padded) + number (10, zero-padded).
- Check digits (mod 97): take BBAN = bankCode + prefix(6) + number(10); append `"123500"` (C = 12, Z = 35, then "00"); `check = 98 − (that big number mod 97)`; pad to 2 digits. Compute the mod 97 piece by piece (the number does not fit into `long`), or use `System.Numerics.BigInteger`.

### 9.8 QR Platba (SPAYD)

```csharp
public class SpaydPayment
{
    public const decimal MaxAmount = 9_999_999.99m;
    public SpaydPayment(string iban, decimal amount, string variableSymbol, DateOnly dueDate, string message);
    public string ToText();   // throws ArgumentException for values that break the limits
}
```

Text format (no spaces except inside MSG):

```
SPD*1.0*ACC:{IBAN}*AM:{0.00}*CC:CZK*X-VS:{vs}*DT:{yyyyMMdd}*MSG:{text}*
```

Limits and rules:
- ACC ≤ 46 characters.
- AM: invariant culture, decimal point, always 2 decimals (`"0.00"`), ≤ 10 characters, 0 ≤ amount ≤ 9 999 999.99.
- X-VS: digits only, ≤ 10.
- DT: due date, exactly 8 digits.
- MSG: remove diacritics (`Normalize(FormD)`, drop `NonSpacingMark` chars), upper-case (`ToUpperInvariant`), drop `*` and any non-ASCII character, trim, cut to ≤ 60 characters. The message text itself (e.g. `"Faktura 2026-0001"`) is supplied by `Invoicing.Pdf`, because Core contains no Czech text.
- No `*` may appear inside any value.
- Only on **TaxInvoice and NonVatInvoice** PDFs. If the total is above `MaxAmount`, the PDF shows a short Czech note instead of the QR code.

### 9.9 Configuration

```csharp
public static class SettingKeys
{
    public const string VatRateStandard = "VAT_RATE_STANDARD";
    public const string VatRateReduced = "VAT_RATE_REDUCED";
    public const string DatabasePath = "DATABASE_PATH";
    public const string AresBaseUrl = "ARES_BASE_URL";
    public const string LettrBaseUrl = "LETTR_BASE_URL";
    public const string LettrApiKey = "LETTR_API_KEY";
    public const string EmailFromAddress = "EMAIL_FROM_ADDRESS";
    public static IReadOnlyList<string> All { get; }
}
```

- `EnvFileParser.Parse(IEnumerable<string> lines) → Dictionary<string, string>`
  - ignore blank lines and lines starting with `#`;
  - split at the **first** `=`; trim key and value; remove one pair of surrounding `"` or `'`;
  - a non-empty line without `=` or with an empty key throws `EnvFileFormatException(lineNumber)`.
- The App reads `.env` from `AppContext.BaseDirectory` (a missing file = no values), then for every key in `SettingKeys.All` a **real environment variable that is defined (not null) overrides** the file value.
- `AppSettings.Load(IReadOnlyDictionary<string, string> values) → SettingsLoadResult` (either `AppSettings` or a list of `ConfigurationError(string Key, ConfigurationProblem Problem)` with `ConfigurationProblem { Missing, Invalid }`).
  - **Required:** `VAT_RATE_STANDARD`, `VAT_RATE_REDUCED` (decimal, invariant culture, `0 < rate < 100`), `DATABASE_PATH` (not blank), `ARES_BASE_URL`, `LETTR_BASE_URL` (absolute `http`/`https` URI). Missing or invalid → error; the app shows them and exits. **No fallback values in code.**
  - **Optional:** `LETTR_API_KEY` (not blank) and `EMAIL_FROM_ADDRESS` (valid e-mail). They go into `EmailSettings`; problems only disable e-mail.
- `VatRates`: `Standard`, `Reduced`, `All` (highest first), `IsAllowed(decimal rate)`.
- `EmailSettings` (a **class**, not a record): `ApiKey`, `FromAddress`, `IsEnabled`, `Problems` (list of `ConfigurationError` for the two e-mail keys, so the UI can say which one is missing or invalid). Override `ToString()` so that it never contains the key.
- Relative `DATABASE_PATH` is resolved against `AppContext.BaseDirectory` (the App does this, not Core).

### 9.10 E-mail types and availability

```csharp
public enum EmailUnavailableReason { NotConfigured, DocumentIsDraft, DocumentIsCancelled }

public class EmailAvailability
{
    public static EmailAvailability Evaluate(EmailSettings settings, Document document);
    public bool CanSend { get; }
    public EmailUnavailableReason? Reason { get; }   // null when CanSend
}
```

Order of checks: not configured → draft → cancelled → available.

```csharp
public record EmailAttachment(string FileName, string ContentType, byte[] Content);
public record EmailMessage(string FromAddress, string FromName, string ToAddress,
                           string Subject, string HtmlBody, string TextBody, EmailAttachment Attachment);

public enum EmailSendError
{
    SenderDomainNotVerified,   // 400
    InvalidApiKey,             // 401
    NotAllowed,                // 403
    PreviousAttemptInProgress, // 409
    InvalidData,               // 422 (details text available)
    RateLimited,               // 429
    ServiceUnavailable,        // 5xx
    NoConnection,              // timeout or network error
    UnexpectedResponse         // anything else, e.g. 404 or a 200 without request_id
}

public class EmailSendResult   // static factories Success(requestId) / Failure(error, details?, statusCode?)
{
    public bool IsSuccess { get; }
    public string? ProviderRequestId { get; }
    public EmailSendError? Error { get; }
    public string? Details { get; }      // e.g. field errors from 422
    public int? StatusCode { get; }
}

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, string idempotencyKey, CancellationToken cancellationToken);
}

public record DocumentEmailRecord(string SentTo, DateTimeOffset SentAt, string ProviderRequestId);
```

### 9.11 Company registry (ARES) types

```csharp
public record CompanyInfo(string Ico, string Name, string Street, string City, string PostalCode, string? Dic);
public enum CompanyLookupError { InvalidIco, NotFound, Timeout, NoConnection, UnexpectedResponse }
public class CompanyLookupResult  // Success(CompanyInfo) / Failure(CompanyLookupError)
public interface ICompanyRegistry
{
    Task<CompanyLookupResult> FindByIcoAsync(string ico, CancellationToken cancellationToken);
}
```

### 9.12 Repository interfaces

```csharp
public interface ISupplierRepository
{
    Task<Supplier?> GetAsync();
    Task SaveAsync(Supplier supplier);                 // insert or update the single row (id = 1)
}

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task SaveAsync(Customer customer);                 // insert (sets Id) or update
}

public interface IDocumentRepository
{
    Task<IReadOnlyList<Document>> GetAllAsync();       // with lines; the app is small, so totals are calculated in memory
    Task<Document?> GetByIdAsync(int id);
    Task SaveDraftAsync(Document document);            // insert (AssignId) or update; replaces lines
    Task IssueAsync(Document document, Supplier supplier);  // one transaction: number + Issue() + full save
    Task UpdateStatusAsync(Document document);         // after MarkAsPaid / Cancel
    Task DeleteDraftAsync(Document document);          // only drafts; lines go by ON DELETE CASCADE
}

public interface IDocumentEmailRepository
{
    Task AddAsync(int documentId, DocumentEmailRecord record);
    Task<DocumentEmailRecord?> GetLatestAsync(int documentId);
}
```

### 9.13 Services (application logic used by the ViewModels)

`DocumentService(ISupplierRepository, IDocumentRepository, VatRates)`. Keep every method short:

| Method | What it does |
|---|---|
| `Task<Document> CreateDraftAsync(DateOnly today)` | Loads supplier (throws `InvalidOperationException` if missing; the UI disables the button then); creates `TaxInvoice` or `NonVatInvoice` with the defaults; **not saved yet** |
| `Task SaveDraftAsync(Document)` | Delegates to the repository |
| `Task<IReadOnlyList<ValidationMessage>> IssueAsync(Document)` | Loads supplier, validates; if there are errors returns them without issuing; otherwise calls `IDocumentRepository.IssueAsync` and returns the warnings |
| `Task MarkAsPaidAsync(Document)` | `document.MarkAsPaid()` + `UpdateStatusAsync` |
| `Task CancelAsync(Document)` | `document.Cancel()` + `UpdateStatusAsync` |
| `Task<CreditNote> CreateCreditNoteAsync(TaxInvoice, DateOnly today)` | Uses supplier `DefaultDueDays`; returns an unsaved draft |
| `Task DeleteDraftAsync(Document)` | Throws if not a draft; delegates |

`DocumentEmailService(IEmailSender, IDocumentEmailRepository)`: `SendAsync(Document, EmailMessage, string idempotencyKey, DateTimeOffset sentAt, CancellationToken) → EmailSendResult`. Sends; on success stores a `DocumentEmailRecord`; on failure stores nothing.

### 9.14 Formatting and parsing (culture helpers, no UI text)

`CzechFormat` (uses its own `NumberFormatInfo` so the result is the same on every machine):
- `FormatMoney(decimal)` → `12 100,00 Kč`: group separator and the space before `Kč` are **non-breaking spaces (U+00A0)**, decimal comma, 2 decimals, minus sign `-`.
- `FormatNumber(decimal)` → `12 100,00` (same, without currency).
- `FormatQuantity(decimal)` → up to 3 decimals, no trailing zeros (`1,5`, `2`).
- `FormatDate(DateOnly)` → `6. 12. 2026` (format `d. M. yyyy`, normal spaces).

`CzechNumberParser.TryParseDecimal(string?, out decimal)`: accepts `1,5`, `1.5`, `1 000,50` (normal or non-breaking spaces); used for user input in the lines grid and forms.

"Kč" is a currency symbol, so it may live in Core.

---

## 10. Phase 2: `Invoicing.Data`

### 10.1 Connection factory

```csharp
public class SqliteConnectionFactory
{
    public SqliteConnectionFactory(string databaseFilePath);
    public async Task<SqliteConnection> OpenConnectionAsync();
}
```

- Connection string built with `SqliteConnectionStringBuilder`: `DataSource = path`, `Mode = SqliteOpenMode.ReadWrite` (**never create** a missing database silently), `ForeignKeys = true`.
- Set `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true` once (static constructor of the factory), so `issue_date` maps to `IssueDate`.
- Open one connection per repository method (`await using`). Always use **parameters** (`@Name`), never string concatenation.

### 10.2 Row classes and mapping

Do not let Dapper convert money, dates or enums implicitly (culture problems). Use simple **row classes** in `Invoicing.Data` with `string` properties for money, dates and enums, and convert explicitly in small mapper classes:

- `decimal.Parse(text, CultureInfo.InvariantCulture)` / `value.ToString(CultureInfo.InvariantCulture)`
- `DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture)` / `ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)`
- `Enum.Parse<DocumentStatus>(text)` / `status.ToString()`
- `is_vat_payer` 0/1 ↔ `bool`

Suggested classes: `SupplierRow`, `CustomerRow`, `DocumentRow`, `DocumentLineRow`, `SupplierMapper`, `CustomerMapper`, `DocumentMapper`, `DocumentLineMapper`. Split `DocumentMapper` into small methods (`MapHeader`, `MapCustomer`, `MapSupplierSnapshot`, `MapLines`, `CreateDocument`).

### 10.3 Loading documents

- `GetAllAsync`: one query for documents (with `LEFT JOIN customers` for the current customer data and `LEFT JOIN documents o` for the original invoice number of a credit note), one query for all lines; group lines by `document_id` in memory.
- Customer of a **draft** = current row from `customers` (join columns). Customer of an **issued** document = snapshot columns. Supplier = snapshot columns (null for drafts).
- Build the right subclass from `document_type`.

### 10.4 Saving and issuing

- `SaveDraftAsync`: in a transaction: `INSERT` (then `document.AssignId(newId)`, use `RETURNING id` or `last_insert_rowid()`) or `UPDATE`; then `DELETE` the document's lines and `INSERT` them again with `position` 1..n.
- Draft rows store `customer_id`; snapshot columns stay `NULL`. A draft without a customer cannot be saved (the UI disables "Uložit"); the repository throws `InvalidOperationException` as a guard.
- `IssueAsync(document, supplier)`, **one transaction**:
  1. `series = DocumentNumbering.GetSeries(document.Kind)`, `year = document.IssueDate!.Value.Year`;
  2. `SELECT last_number FROM number_series WHERE series_code = @Series AND year = @Year`;
  3. `next = DocumentNumbering.NextSequenceNumber(last)`;
  4. `INSERT INTO number_series … ON CONFLICT (series_code, year) DO UPDATE SET last_number = excluded.last_number`;
  5. `document.Issue(DocumentNumbering.Format(series, year, next), supplier)`;
  6. save the full document (insert or update + lines + all snapshot columns);
  7. commit.
  
  If anything fails, the transaction rolls back and the exception goes up. The in-memory document may already say "Issued", so the UI **reloads the document from the database** after any error.
- `UpdateStatusAsync`: `UPDATE documents SET status = @Status WHERE id = @Id`.
- `DeleteDraftAsync`: `DELETE FROM documents WHERE id = @Id AND status = 'Draft'`.

### 10.5 Database version check

`DatabaseVersionChecker(string databaseFilePath)` with `const string RequiredSchemaVersion = "5"` and `Task<DatabaseStatus> CheckAsync()`:

```csharp
public enum DatabaseStatus { Ready, FileMissing, NotMigrated, OutdatedSchema }
```

- File does not exist → `FileMissing` (check before opening).
- `SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'flyway_schema_history'` is 0 → `NotMigrated`.
- `SELECT COUNT(*) FROM flyway_schema_history WHERE version = @Version AND success = 1` is 0 → `OutdatedSchema`.
- Otherwise `Ready`.

---

## 11. Phase 3: `Invoicing.Ares`

`AresCompanyRegistry(HttpClient httpClient, string baseUrl) : ICompanyRegistry`

1. If `!IcoValidator.IsValid(ico)` → `Failure(InvalidIco)` **without** calling the network.
2. `GET {baseUrl without trailing '/'}/ekonomicke-subjekty/{ico}` with `Accept: application/json`. Build the **absolute** URL as a string. (Do not rely on `HttpClient.BaseAddress` + relative path: a base address without a trailing slash silently drops its last segment.)
3. 200 → parse → `Success(CompanyInfo)`; 404 → `NotFound`; other status → `UnexpectedResponse`.
4. `TaskCanceledException` when the caller did not cancel (= `HttpClient.Timeout`) → `Timeout`; `HttpRequestException` → `NoConnection`; `JsonException` → `UnexpectedResponse`. Catch only these.

Timeout: `private static readonly TimeSpan` / constant in the composition root, 10 seconds. One `HttpClient` instance for the whole app run.

### 11.1 Response mapping

Real response (shortened) for IČO 27074358. Save it as `tests/Invoicing.Tests/TestData/ares-27074358.json`:

```json
{
  "ico": "27074358",
  "obchodniJmeno": "Asseco Central Europe, a.s.",
  "dic": "CZ27074358",
  "sidlo": {
    "kodStatu": "CZ",
    "nazevStatu": "Česká republika",
    "nazevObce": "Praha",
    "nazevMestskehoObvodu": "Praha 4",
    "nazevCastiObce": "Michle",
    "nazevUlice": "Budějovická",
    "cisloDomovni": 778,
    "cisloOrientacni": 3,
    "cisloOrientacniPismeno": "a",
    "psc": 14000,
    "textovaAdresa": "Budějovická 778/3a, Michle, 14000 Praha 4"
  }
}
```

Note the JSON types: `psc`, `cisloDomovni` and `cisloOrientacni` are **numbers**; `cisloOrientacniPismeno` is a string. Every field may be missing. Use DTO classes with `[JsonPropertyName]` and nullable properties.

Mapping (`AresResponseMapper.Map(AresSubjectDto) → CompanyInfo`, public so it can be tested):

| ARES | CompanyInfo |
|---|---|
| `ico` | Ico |
| `obchodniJmeno` | Name |
| `dic` (may be missing) | Dic |
| `sidlo.nazevUlice` + ` ` + `cisloDomovni` [+ `/` + `cisloOrientacni` + `cisloOrientacniPismeno`] | Street → `Budějovická 778/3a` |
| `sidlo.nazevObce` | City → `Praha` |
| `sidlo.psc` (number → string) | PostalCode → `14000` |

Missing parts:
- If only `nazevUlice` is missing (small villages have no streets), use `nazevCastiObce` (or `nazevObce`) as the street name: `Lhota 15`.
- If `nazevObce`, `psc` or `cisloDomovni` is missing, put the whole `textovaAdresa` into Street and leave City and PostalCode empty, so the user completes them.

ARES is used in the address book and on the supplier screen ("Načíst z ARES"). It does not return the register entry text or VAT-payer status; the user fills those in.

---

## 12. Phase 4: `Invoicing.Pdf`

### 12.1 Setup

- QuestPDF licence: `QuestPDF.Settings.License = LicenseType.Community;` (once, at startup, in a `PdfSetup.Initialize()` method that the App and the tests call).
- **Bundled font:** download the static TTF files **Noto Sans Regular** and **Noto Sans Bold** (Google Fonts, "Noto Sans", the `static/` folder of the family download) and its `OFL.txt` licence. Put them in `src/Invoicing.Pdf/Resources/Fonts/` as `EmbeddedResource`. If you cannot download files, stop and ask the user to put them there.
- Register them once in `PdfSetup.Initialize()` with `FontManager.RegisterFontFromEmbeddedResource(assembly, resourceName)` (check the method name in the installed QuestPDF version) and use `page.DefaultTextStyle(x => x.FontFamily(PdfFonts.FamilyName))`.
- Keep QuestPDF's default `ThrowOnMissingTextGlyphs` on, so a missing Czech glyph fails loudly (and in tests).

### 12.2 Classes

| Class | Responsibility |
|---|---|
| `PdfSetup` | licence + font registration (idempotent) |
| `DocumentPdfGenerator` | `byte[] Generate(Document document)`; throws `InvalidOperationException` for drafts |
| `DocumentPdf : IDocument` | page setup; calls the components |
| Components (`IComponent`) | `HeaderComponent`, `PartiesComponent`, `DatesAndPaymentComponent`, `LinesTableComponent`, `VatSummaryComponent`, `TotalsComponent`, `QrPaymentComponent`, `FooterComponent` (split further if a component gets long) |
| `QrCodeImageGenerator` | `byte[] CreatePng(string text)`: QRCoder, ECC level **M**, `PngByteQRCode(data).GetGraphic(pixelsPerModule)` |
| `PdfTexts` | all Czech labels as constants |
| `PdfFileNames` | `Faktura-2026-0001.pdf`, `Opravny-doklad-OD-2026-0001.pdf` (ASCII only) |

### 12.3 Content (A4, Czech)

Every PDF:
- Title: `Faktura – daňový doklad` / `Faktura` / `Opravný daňový doklad`, followed by `č. {number}`.
- **Dodavatel** block: name, street, postal code + city, `IČO: …`, `DIČ: …` (TaxInvoice and CreditNote only), register entry.
- **Odběratel** block: name, street, postal code + city, `IČO: …` and `DIČ: …` if present.
- `Vystavil: {IssuedBy}` line.
- Page numbers in the footer (`Strana 1 / 1`).

Dates and payment:
- `Datum vystavení`, `Datum uskutečnění zdanitelného plnění` (tax documents), `Datum splatnosti`, `Forma úhrady: bankovním převodem`.
- Invoices: `Číslo účtu`, `IBAN`, `Variabilní symbol`.

Lines table:
- Tax documents: `Popis`, `Množství`, `Jednotka`, `Cena za jednotku bez DPH`, `Sleva %`, `Sazba DPH`, `Základ`.
- NonVatInvoice: `Popis`, `Množství`, `Jednotka`, `Cena za jednotku`, `Sleva %`, `Celkem`.

Summary:
- TaxInvoice: `Rekapitulace DPH` table (`Sazba`, `Základ daně`, `DPH`, `Celkem`), then `Celkem k úhradě`.
- CreditNote: `Opravovaný doklad: {original number}`, `Důvod opravy: {reason}`, table `Rozdíl základu daně a DPH podle sazeb` (same columns; values are the credit note's own, usually negative), then `Celková částka opravy`.
- NonVatInvoice: `Celkem k úhradě` and the sentence **`Nejsem plátce DPH.`**

QR Platba (TaxInvoice and NonVatInvoice only): label `QR Platba`, image about 3.5 cm wide; SPAYD from section 9.8 with IBAN from the supplier snapshot, total, variable symbol, due date and message `Faktura {number}`. If the total is above `SpaydPayment.MaxAmount`, show `QR Platbu nelze vytvořit, částka je příliš vysoká.` instead.

Cancelled document: a clearly visible `STORNOVÁNO` under the title.

All amounts use `CzechFormat`.

---

## 13. Phase 5: `Invoicing.Email`

### 13.1 Templates (embedded resources, Czech)

Files in `src/Invoicing.Email/Templates/` (build action `EmbeddedResource`):

```
invoice.subject.txt       invoice.html       invoice.txt
credit-note.subject.txt   credit-note.html   credit-note.txt
```

`invoice` is used for TaxInvoice and NonVatInvoice; `credit-note` for CreditNote. Both the HTML and the plain-text body are always sent.

Placeholders: `{{DocumentNumber}}`, `{{SupplierName}}`, `{{CustomerName}}`, `{{TotalAmount}}`, `{{DueDate}}`, `{{VariableSymbol}}`, `{{BankAccount}}`, `{{IssuedBy}}`.

Template texts:

`invoice.subject.txt`
```
Faktura {{DocumentNumber}} – {{SupplierName}}
```

`invoice.txt`
```
Dobrý den,

v příloze Vám zasíláme fakturu č. {{DocumentNumber}} pro {{CustomerName}}.

Částka k úhradě: {{TotalAmount}}
Datum splatnosti: {{DueDate}}
Číslo účtu: {{BankAccount}}
Variabilní symbol: {{VariableSymbol}}

Děkujeme za včasnou úhradu.

S pozdravem
{{IssuedBy}}
{{SupplierName}}
```

`credit-note.subject.txt`
```
Opravný daňový doklad {{DocumentNumber}} – {{SupplierName}}
```

`credit-note.txt`
```
Dobrý den,

v příloze Vám zasíláme opravný daňový doklad č. {{DocumentNumber}} pro {{CustomerName}}.

Částka opravy: {{TotalAmount}}

S pozdravem
{{IssuedBy}}
{{SupplierName}}
```

The `.html` files have the same content as simple HTML (`<!DOCTYPE html>`, `<html lang="cs">`, `<meta charset="utf-8">`, `<p>` paragraphs, a small table for the payment details, no external images or CSS files).

### 13.2 Renderer

`EmailTemplateRenderer.Render(string template, IReadOnlyDictionary<string, string> values, bool htmlEncodeValues) → string`
- Finds every `{{Name}}` (regex `\{\{(\w+)\}\}`).
- **Every placeholder must be replaced.** A placeholder without a value throws `TemplateRenderException` naming it. Nothing is silently left in the text.
- Values unused by the template are fine.
- For the HTML body, values are encoded with `WebUtility.HtmlEncode`.

`EmailTemplateStore` loads the six embedded resources (throws `InvalidOperationException` if one is missing).

### 13.3 Composer

`DocumentEmailComposer(EmailTemplateStore, EmailTemplateRenderer)`:
`EmailMessage Compose(Document document, string recipient, string fromAddress, EmailAttachment attachment)`
- Template by kind; values from the **issued** document's snapshot:
  - `DocumentNumber` = number; `SupplierName`, `IssuedBy`, `BankAccount` from the supplier snapshot; `CustomerName` from the customer snapshot;
  - `TotalAmount` = `CzechFormat.FormatMoney(total)`; `DueDate` = `CzechFormat.FormatDate(dueDate)`;
  - `VariableSymbol` = `DocumentNumbering.ToVariableSymbol(number)` (**invoices only**; credit notes have no variable symbol and their template does not use it).
- `FromName` = supplier name. The App builds the attachment (Email must not reference Pdf): the same PDF bytes as "Export PDF", file name from `PdfFileNames`, content type `application/pdf`.

### 13.4 Lettr client

`LettrEmailSender(HttpClient httpClient, string baseUrl, string apiKey) : IEmailSender`

Request:
- `POST {baseUrl without trailing '/'}/emails` (absolute URL string).
- Headers: `Authorization: Bearer {apiKey}`, `Accept: application/json`, `Idempotency-Key: {key}`; body `Content-Type: application/json`.
- Body (System.Text.Json, `[JsonPropertyName]` for snake_case):

```json
{
  "from": "faktury@firma.cz",
  "from_name": "Firma s.r.o.",
  "to": ["jan@zakaznik.cz"],
  "subject": "Faktura 2026-0001 – Firma s.r.o.",
  "html": "<!DOCTYPE html>…",
  "text": "Dobrý den, …",
  "attachments": [
    { "name": "Faktura-2026-0001.pdf", "type": "application/pdf", "data": "<base64 without line breaks>" }
  ]
}
```

`Convert.ToBase64String(bytes)` produces base64 without line breaks.

Response:
- 200 → read `data.request_id` (`{"message": "...", "data": {"request_id": "...", "accepted": 1, "rejected": 0}}`) → `Success(requestId)`. Missing `request_id` → `UnexpectedResponse`.
- Errors:

| Status | `EmailSendError` |
|---|---|
| 400 | SenderDomainNotVerified |
| 401 | InvalidApiKey |
| 403 | NotAllowed |
| 409 | PreviousAttemptInProgress (same idempotency key still processing) |
| 422 | InvalidData; put the response's `message` and the flattened `errors` object into `Details` (if the body cannot be parsed, leave `Details` null) |
| 429 | RateLimited |
| 500–599 | ServiceUnavailable |
| other | UnexpectedResponse (with `StatusCode`) |
| `TaskCanceledException` (timeout), `HttpRequestException` | NoConnection |

- Timeout constant: 30 seconds. One `HttpClient` for the app run.
- **Never** put the API key into exception messages, `Details` or `ToString()`.
- Sandbox keys (`lttr_sandbox_…`) need no verified domain; Lettr redirects every e-mail to the key owner's own address. Use them for development and the presentation. Live keys need a verified sender domain.
- We do **not** use Lettr's stored templates.

Idempotency: the App creates **one new GUID per send dialog** (`Guid.NewGuid().ToString()`), and **reuses it for every retry** in that dialog, so a retry can never deliver the e-mail twice.

---

## 14. Phases 6–7: `Invoicing.App` (Avalonia, Czech UI)

### 14.1 Structure

```
Invoicing.App/
  Program.cs
  App.axaml / App.axaml.cs
  CompositionRoot.cs            builds all objects by hand (one small method per area)
  Startup/                      EnvironmentReader (reads .env + environment variables), StartupChecker
  Services/                     IDialogService, DialogService (message, confirm, save-file picker, open windows)
  Texts/                        Czech texts: UiTexts, ValidationTexts, StatusTexts, DocumentKindTexts,
                                CompanyLookupTexts, EmailTexts, ConfigurationTexts, DatabaseTexts
  Converters/                   MoneyConverter, DateConverter (use CzechFormat)
  ViewModels/                   MainWindowViewModel, SupplierViewModel, CustomersViewModel, CustomerEditViewModel,
                                DocumentsViewModel, DocumentListItemViewModel, DocumentDetailViewModel,
                                InvoiceLineViewModel, SendEmailViewModel, ErrorViewModel
  Views/                        MainWindow, SupplierView, CustomersView, DocumentsView,
                                DocumentDetailWindow, SendEmailWindow, MessageWindow, ErrorWindow
```

- ViewModels inherit `ObservableObject` (CommunityToolkit). Use `[ObservableProperty]` on `_camelCase` private fields and `[RelayCommand]` for commands (`CanExecute = nameof(...)`, call `XxxCommand.NotifyCanExecuteChanged()` when the condition changes). Use one style consistently.
- Views: XAML only + `InitializeComponent()`. No logic in code-behind.
- Every view and data template sets `x:DataType` (compiled bindings are the default in Avalonia 12).
- ViewModels never reference Views or Avalonia windows; dialogs go through `IDialogService`.
- At startup set `CultureInfo.DefaultThreadCurrentCulture` and `DefaultThreadCurrentUICulture` to `cs-CZ`, so date pickers show Czech formats.

### 14.2 Avalonia notes

- DataGrid: package `Avalonia.Controls.DataGrid` (same version as Avalonia) and in `App.axaml`:
  ```xml
  <Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml" />
  </Application.Styles>
  ```
- File save: `TopLevel.GetTopLevel(visual).StorageProvider.SaveFilePickerAsync(...)` (the old `SaveFileDialog` class no longer exists in Avalonia 12). Do this inside `DialogService`.
- Avalonia has no built-in message box: create a small `MessageWindow` (text + OK, or Ano/Ne for confirmations).
- Dates: `CalendarDatePicker` (`SelectedDate` is `DateTime?`); the ViewModel converts to and from `DateOnly?`.
- Tooltip on a disabled button: `ToolTip.Tip="{Binding SendEmailToolTip}"` and `ToolTip.ShowOnDisabled="True"` (check that the property exists in the installed version).
- Envelope icon: draw it yourself, e.g. `<Path Data="M3,6 H21 V18 H3 Z M3,6 L12,13 L21,6" Stroke="{DynamicResource ...}" StrokeThickness="1.5" Width="16" Height="16" Stretch="Uniform" />` next to the text. Do not copy icons from icon packs.

### 14.3 Startup sequence (`App.OnFrameworkInitializationCompleted`)

1. Read configuration (`EnvironmentReader` → `AppSettings.Load`). `EnvFileFormatException` or load errors → `ErrorWindow` with Czech messages → the app ends when it is closed.
2. Resolve `DATABASE_PATH` (relative → against `AppContext.BaseDirectory`); `DatabaseVersionChecker.CheckAsync()`. Not `Ready` → `ErrorWindow` with the reason and the instruction to run migrations.
3. `PdfSetup.Initialize()`.
4. `CompositionRoot` creates everything; register the global error handler (14.10); show `MainWindow`.

### 14.4 Main window

A `TabControl` with three tabs: **Dodavatel**, **Adresář**, **Doklady**. `MainWindowViewModel` only holds the three tab ViewModels (no god object). Document detail and the send dialog open as **modal windows**.

### 14.5 Dodavatel (supplier)

- Form fields: Název, Ulice, Obec, PSČ, IČO, DIČ, Plátce DPH (checkbox), Zápis v obchodním rejstříku, Číslo účtu, IBAN (read-only, calculated live from Číslo účtu; shows `Neplatné číslo účtu` if it cannot be parsed), Vystavil, Výchozí splatnost (dní) (`NumericUpDown`, minimum 0).
- Buttons: **Načíst z ARES** (fills name, address, IČO, DIČ from the typed IČO; shows Czech errors), **Uložit**.
- Live list of `Supplier.Validate()` messages (informative; saving is still allowed, so the user can fill the data step by step; documents cannot be issued until the supplier is complete).
- Read-only e-mail status line (section 19.6).
- A new (empty) supplier form starts with `Výchozí splatnost = 14` (named constant).

### 14.6 Adresář (customers)

- Left: list of customers (DataGrid: Název, IČO, Obec, E-mail) with a search box (filters in memory by name, IČO or city, case-insensitive).
- Right / below: edit form (Název, Ulice, Obec, PSČ, IČO, DIČ, E-mail) with **Načíst z ARES**, **Nový**, **Uložit**.
- "Uložit" is enabled only when `Customer.Validate()` returns no errors (messages shown under the form).
- Customers cannot be deleted (out of scope; they may be used by documents).

### 14.7 Doklady (document list)

- Filter (ComboBox): Vše, Koncepty, Vystavené, Zaplacené, Po splatnosti. ("Vystavené" = status Issued, overdue included; "Po splatnosti" = `IsOverdue(today)`.)
- DataGrid columns: Číslo (`koncept` for drafts), Typ, Odběratel, Vystaveno, Splatnost, Částka, Stav (overdue shown as `Po splatnosti`).
- Buttons: **Nový doklad** (disabled with tooltip `Nejdříve vyplňte údaje dodavatele.` when no supplier exists), **Otevřít** (or double-click).
- After a detail window closes, reload the list.

### 14.8 Detail dokladu (document detail window)

Header: kind title and number (or `Koncept`), status.

Fields:
- Odběratel: ComboBox of customers for invoice drafts. A credit note keeps the customer of its original invoice (read-only). Issued documents show the snapshot.
- Datum vystavení, DUZP (tax documents only), Datum splatnosti.
- Credit note: Opravovaný doklad (read-only number), Důvod opravy (TextBox).
- Lines grid (editable only for drafts): Popis, Množství, Jednotka, Cena za jednotku bez DPH (or `Cena za jednotku` for non-payers), Sleva %, Sazba DPH (ComboBox, hidden for NonVatInvoice), Základ (read-only). Buttons **Přidat řádek** (new line: quantity 1, unit `ks`, standard VAT rate) and **Odebrat řádek**.
  - `InvoiceLineViewModel` keeps the typed **text** of numbers, parses it with `CzechNumberParser`, and shows `Neplatné číslo` for that cell if parsing fails. Each line ViewModel exposes the list of VAT rates for its ComboBox (simpler than binding to the parent).
  - After every change the detail ViewModel calls `document.ReplaceLines(...)` (drafts) and refreshes totals.
- Live totals: Základ celkem, VAT summary rows (tax documents), Celkem.
- Live validation messages (errors and warnings, Czech, section 19.3).
- `Odesláno {datum} na {adresa}` when the document has been e-mailed (latest `document_emails` row).

Toolbar buttons and when they are enabled:

| Button | Enabled when | Action |
|---|---|---|
| Uložit | draft and a customer is selected | `SaveDraftAsync` |
| Vystavit | draft and a customer is selected | confirm `Vystavit doklad? Po vystavení už nepůjde upravit.` → `IssueAsync`; show errors (nothing issued) or warnings (issued) |
| Smazat | draft that was saved | confirm `Opravdu chcete smazat tento koncept?` → delete → close window |
| Zaplaceno | `CanMarkAsPaid` | `MarkAsPaidAsync` |
| Stornovat | `CanCancel` (NonVatInvoice) | confirm `Opravdu chcete doklad stornovat? Tuto akci nelze vrátit.` → `CancelAsync` |
| Opravný doklad | `TaxInvoice.CanCreateCreditNote` | `CreateCreditNoteAsync` → open a new detail window with the credit-note draft |
| Export PDF | not a draft | save-file picker (default name from `PdfFileNames`) → write `DocumentPdfGenerator.Generate` bytes → confirm `PDF bylo uloženo.` |
| Odeslat e-mailem | **always visible**; enabled when `EmailAvailability.CanSend` | open the send dialog |

When disabled, "Odeslat e-mailem" shows a tooltip with the reason (section 19.6).

After any failed database operation, reload the document from the database before continuing.

### 14.9 Send e-mail dialog (`SendEmailWindow`)

- **Komu:** recipient, pre-filled from the document customer's e-mail, editable. If empty or invalid, an inline message `Zadejte platnou e-mailovou adresu.` appears and "Odeslat" is disabled.
- **Předmět** and **Text:** read-only preview of the rendered template (plain-text body).
- **Příloha:** the attachment file name, e.g. `Faktura-2026-0001.pdf`.
- If the document was sent before: `Tento doklad už byl odeslán 22. 11. 2026 na jan@firma.cz.`
- One idempotency key (GUID) is created when the dialog opens and reused on every retry.
- While sending: a progress indicator (`ProgressBar IsIndeterminate`) is shown and "Odeslat" is disabled (no double sending by double-clicking).
- Success: the dialog closes, a confirmation `E-mail byl odeslán na {adresa}.` appears, and the detail shows `Odesláno {datum} na {adresa}`.
- Failure: a Czech error message (section 19.6) is shown **in the dialog**, which stays open with the data filled in, so the user can try again.

### 14.10 Global error handler

Register `Dispatcher.UIThread.UnhandledException` (and `AppDomain.CurrentDomain.UnhandledException` for logging only). Show `Nastala neočekávaná chyba: {exception.Message}` in a `MessageWindow`, write the full exception to `Console.Error` (visible in Rider), set `Handled = true`. This is the only place where `Exception` is caught.

### 14.11 Manual test scenario (run it at the end of phases 6 and 7)

1. Run migrations with demo data; start the app from Rider.
2. Dodavatel: check the data, change "Vystavil", save.
3. Adresář: Nový → type IČO `27074358` → Načíst z ARES → data filled → save.
4. Doklady: Nový doklad → choose that customer → add three lines (21 %, 21 % with 10 % discount, 12 %) → Uložit → Vystavit → number `YYYY-NNNN` assigned.
5. Export PDF → open the PDF: Czech characters, VAT summary, QR code (scan it with a banking app if possible).
6. Odeslat e-mailem → with no Lettr key the button is disabled with the tooltip; with a sandbox key the e-mail arrives at the key owner's address.
7. Zaplaceno → status Paid. Opravný doklad → edit reason → Vystavit → number `OD-YYYY-NNNN`.
8. Switch the supplier to non-payer → new document is a "Faktura" → issue → Stornovat.

---

## 15. Tests (`Invoicing.Tests`, xUnit)

General:
- Name tests `MethodOrBehavior_Condition_ExpectedResult` (e.g. `CalculateBase_WithDiscount_AppliesDiscountBeforeRounding`). Arrange / Act / Assert.
- One test class per tested class, in folders `Core/`, `Data/`, `Ares/`, `Pdf/`, `Email/`.
- Small helpers in `TestHelpers/`: `TestData` (a valid supplier, customer and documents), `FakeHttpMessageHandler`, simple in-memory fakes of repositories where needed. Keep helpers simple.
- `[Theory]` + `[InlineData]` for value tables.
- No network, no real e-mails, no dependence on the machine's culture (the code itself must be culture-independent). A test that checks culture independence may set `CultureInfo.CurrentCulture` (it is per thread) and must restore it in `finally`.
- Copy test data and migrations to the output folder:
  ```xml
  <ItemGroup>
    <None Include="TestData\**\*" CopyToOutputDirectory="PreserveNewest" />
    <None Include="..\..\db\migrations\*.sql" Link="Migrations\%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  ```

### 15.1 Required tests and known values

**Line base and rounding (`InvoiceLineTests`)**
- `2 × 1000, discount 15 %` → `1700.00`
- `1 × 99.99, discount 10 %` → `89.99` (89.991)
- `3 × 33.335` → `100.01` (midpoint 100.005 rounds away from zero)
- `-2.5 × 10.01` → `-25.03` (negative midpoint away from zero)
- `WithNegatedQuantity` keeps everything except the sign of the quantity.

**VAT summary (`VatSummaryTests`)**: lines `2 × 1000 @21 %`, `1 × 500.50 @21 % −10 %`, `3 × 120 @12 %`:

| Rate | Base | VAT |
|---|---|---|
| 21 % | 2450.45 | 514.59 |
| 12 % | 360.00 | 43.20 |

Total base `2810.45`, total VAT `557.79`, total `3368.24`. Rows ordered 21 then 12.
- Midpoints: base `0.50` @21 % → VAT `0.11`; base `2.50` @21 % → `0.53`; base `-2.50` @21 % → `-0.53`.
- VAT is calculated from the **sum** per rate, not per line (make a case where per-line rounding would give a different result: e.g. three lines of base `0.50` @21 %: sum 1.50 → VAT 0.32; per-line would be 3 × 0.11 = 0.33).

**Validation (`TaxInvoiceValidationTests`, `NonVatInvoiceValidationTests`, `CreditNoteValidationTests`, `SupplierTests`, `CustomerTests`)**
- A fully valid document of each type → no errors.
- One test (or `[Theory]` case) **per rule** in section 9.5, each producing exactly the expected code and target (and line position for line rules).
- 15-day rule: issue date = DUZP + 15 → no warning; DUZP + 16 → warning `IssuedLongAfterTaxableSupply` with severity Warning; a document with only this warning can be issued.
- NonVatInvoice: no DUZP required; VAT rate not checked; supplier VAT payer → `MustNotBeVatPayer`.
- CreditNote: negative quantity OK; zero quantity → `QuantityZero`; missing reason; missing original invoice; no 15-day warning.

**Lifecycle (`DocumentLifecycleTests`)**
- After `Issue`: `Number` set, status Issued, snapshots are **copies** (changing the original customer object afterwards does not change the document).
- Issued document: setting `IssueDate`, `DueDate`, `Customer`, `TaxableSupplyDate`, `CorrectionReason` or calling `ReplaceLines` throws `InvalidOperationException`.
- `MarkAsPaid` works on issued invoices; throws on drafts and on credit notes.
- `Cancel` works on an issued NonVatInvoice; throws on TaxInvoice and on drafts.
- `Issue` on a non-draft throws.
- `IsOverdue`: issued + due yesterday → true; due today → false; paid → false; credit note → false.
- `CreateCreditNote`: from issued and paid TaxInvoice works (negated quantities, reference to original, dates = today, due = today + days); from a draft throws.

**Numbering (`DocumentNumberingTests`)**
- `Format(Invoice, 2026, 1)` → `2026-0001`; `Format(CreditNote, 2026, 12)` → `OD-2026-0012`.
- `NextSequenceNumber(null)` → 1 (first in a new year = yearly reset); `NextSequenceNumber(41)` → 42.
- `ToVariableSymbol("2026-0001")` → `20260001`.
- `GetSeries` for both invoice kinds → Invoice; credit note → CreditNote.

**SPAYD (`SpaydPaymentTests`)**
- IBAN `CZ6508000000192000145399`, amount `3368.24`, VS `20260001`, due date 2026-12-06, message `Faktura 2026-0001` →
  `SPD*1.0*ACC:CZ6508000000192000145399*AM:3368.24*CC:CZK*X-VS:20260001*DT:20261206*MSG:FAKTURA 2026-0001*`
- Amount `100` → `AM:100.00` (invariant culture even if the current culture is cs-CZ).
- Message with diacritics: `Faktura č. 2026-0001` → `FAKTURA C. 2026-0001`.
- Message with `*` → `*` removed; message of 70 characters → cut to 60.
- Amount above `MaxAmount`, negative amount, VS with letters or 11 digits, IBAN longer than 46 → `ArgumentException`.
- The produced text never contains `*` inside a value (check by splitting).

**Bank account / IBAN (`CzechBankAccountTests`)**

| Account | IBAN |
|---|---|
| `19-2000145399/0800` | `CZ6508000000192000145399` |
| `2000145399/0800` | `CZ7908000000002000145399` |
| `123456789/0100` | `CZ1801000000000123456789` |
| `2400123456/2010` | `CZ6020100000002400123456` |

Invalid inputs: `""`, `1/0800` (number needs at least 2 digits), `2000145399/800`, `12345678901/0800`, `1234567-2000145399/0800`, `abc/0800` → `TryParse` returns false.

**IČO (`IcoValidatorTests`)**: valid `27074358`, `00006947`, `26168685`, `27082440`; invalid `27074359`, `12345678`, `1234567`, `123456789`, `2707435A`, `""`, `null`.

**Configuration (`EnvFileParserTests`, `AppSettingsTests`)**
- Comments, blank lines, quotes, `=` inside a value (`A=b=c` → `b=c`), malformed line → exception with line number.
- All required present → settings loaded; each required key missing → `Missing`; invalid VAT rate (`abc`, `0`, `100`, `21,5` with a comma is invalid in invariant culture) → `Invalid`; invalid URL → `Invalid`.
- E-mail: both set and valid → enabled; key missing → disabled with problem `LETTR_API_KEY Missing`; from-address invalid → disabled with `EMAIL_FROM_ADDRESS Invalid`; app still loads.
- `EmailSettings.ToString()` does not contain the key.

**E-mail availability (`EmailAvailabilityTests`)**: not configured, draft, cancelled, available (issued and paid). Not configured wins over draft.

**Formatting (`CzechFormatTests`, `CzechNumberParserTests`)**
- `FormatMoney(12100m)` → `"12 100,00 Kč"`; `FormatMoney(-1210m)` → `"-1 210,00 Kč"`.
- `FormatDate(new DateOnly(2026, 12, 6))` → `"6. 12. 2026"`.
- Parser: `1,5`, `1.5`, `1 000,50`, `1 000,50` → OK; `abc`, `""` → false.

**ARES (`AresResponseMapperTests`, `AresCompanyRegistryTests`)**
- Sample file → `Asseco Central Europe, a.s.`, `Budějovická 778/3a`, `Praha`, `14000`, `CZ27074358`.
- Without `dic` → `Dic == null`. Without `nazevUlice` → street from `nazevCastiObce`. Without `psc` → street = `textovaAdresa`, city and postal code empty.
- Registry with fake handler: invalid IČO → `InvalidIco` and **no request sent**; 404 → `NotFound`; handler throws `HttpRequestException` → `NoConnection`; handler throws `TaskCanceledException` → `Timeout`; request URL is exactly `{base}/ekonomicke-subjekty/27074358`.

**E-mail (`EmailTemplateRendererTests`, `EmailTemplateStoreTests`, `DocumentEmailComposerTests`, `LettrEmailSenderTests`, `DocumentEmailServiceTests`)**
- Renderer: all placeholders replaced; unknown placeholder → `TemplateRenderException`; HTML encoding (`<b>` → `&lt;b&gt;`, `&` → `&amp;`); plain text not encoded.
- All six templates exist; each template renders with the values the composer provides for that kind (no exception) and the result contains no `{{`.
- Composer: invoice subject `Faktura 2026-0001 – …`; amount `3 368,24 Kč` (with U+00A0); credit-note values do not need `VariableSymbol`.
- Lettr: URL `{base}/emails`; method POST; headers `Authorization: Bearer …`, `Accept`, `Idempotency-Key` equal to the given key; JSON fields `from`, `from_name`, `to` (array), `subject`, `html`, `text`, `attachments[0].name/type/data`; `data` decodes back to the original bytes and contains no line breaks; 200 → request id; status mapping for 400, 401, 403, 409, 422 (details), 429, 500, 503, 404; timeout and network error → NoConnection; 200 without `request_id` → UnexpectedResponse.
- `FakeHttpMessageHandler` must read and store the request body **inside** `SendAsync` (the content is disposed afterwards).
- `DocumentEmailService`: success stores one record; failure stores none.

**Data (`Data/` tests against a temporary SQLite file)**
- Test fixture: create a temp file path, create the schema by executing the copied `V*.sql` files in version order (Flyway is not needed in tests), delete the file afterwards (`SqliteConnection.ClearAllPools()` first).
- Money round-trip: a line with `0.1`, `1234.56`, `-25.03` loads back exactly.
- Draft save + load: same lines and order; update replaces lines.
- Issue: two invoices in 2026 → `2026-0001`, `2026-0002`; a credit note → `OD-2026-0001`; an invoice with issue date in 2027 → `2027-0001`; snapshot columns filled; later customer change does not change the loaded issued document.
- `DatabaseVersionChecker`: missing file, no history table, version 4 only, version 5 → the four statuses.

**PDF (`DocumentPdfGeneratorTests`, smoke tests)**
- For an issued TaxInvoice, NonVatInvoice, CreditNote and a cancelled NonVatInvoice with Czech text everywhere (`Žluťoučký kůň`): bytes are not empty and start with `%PDF`.
- Draft → `InvalidOperationException`.
- `PdfFileNames` for invoice and credit note.
- `QrCodeImageGenerator` returns PNG bytes (start with `0x89 'P' 'N' 'G'`).

Optional (nice, small): `DocumentServiceTests` with in-memory fakes: issuing with validation errors does not call the repository; issuing a valid document calls it once.

Run `dotnet test` after every phase. All tests must pass. Optional coverage: `dotnet test --collect:"XPlat Code Coverage"`.

---

## 16. Demo data (`db/demo/R__demo_data.sql`)

A Flyway **repeatable** migration with idempotent `INSERT OR IGNORE` statements and fixed ids. Applied only through the Compose `demo` profile. Meant for an empty database (say so in a comment at the top and in the README).

Content (fictional companies, `@example.com` e-mails, IČOs with a valid check digit):
- Supplier (id 1): a fictional s.r.o., VAT payer, complete data (register entry, bank account `19-2000145399/0800`, IBAN `CZ6508000000192000145399`, issued by a name, default due days 14).
- 3 customers: two companies (with IČO, DIČ, e-mail) and one private person (no IČO/DIČ).
- 1 **draft** TaxInvoice with the three lines from the VAT-summary test (so the presenter can issue it live).
- 1 **issued** TaxInvoice `2026-0001` (snapshot columns filled) with a due date in the past (shows "Po splatnosti"), plus `number_series ('Invoice', 2026, 1)`.

Use real dates in 2026, money as invariant-culture text, enum names as text.

---

## 17. Windows release

`scripts/build-windows-release.sh` (bash, `set -euo pipefail`, runs from the repo root):

1. Clean `artifacts/release/`.
2. `dotnet publish src/Invoicing.App/Invoicing.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/release/Invoicing-win-x64`
3. Create a migrated database for the release: `DATA_DIR=./artifacts/release/Invoicing-win-x64/data docker compose run --rm flyway` (create the folder first).
4. Copy `.env.example` to `artifacts/release/Invoicing-win-x64/.env` (it has an empty `LETTR_API_KEY`, so e-mail is disabled and no secret is shipped). Then **check** it: fail the script if the release `.env` has a non-empty `LETTR_API_KEY`.
5. Zip the folder to `artifacts/release/Invoicing-win-x64.zip` (`zip -r`; on CachyOS the `zip` package may need installing: `paru -S zip`).

The zip contains the self-contained publish output, `.env` and `data/invoicing.db`. The teacher unzips it and runs `Invoicing.App.exe`; no .NET installation is needed.

---

## 18. README and diagrams

`README.md` (English), sections:
1. What the app does (short, with the three document types).
2. Requirements (.NET 10 SDK, Docker with Compose, Rider or any IDE; CachyOS example: `paru -S dotnet-sdk docker docker-compose`).
3. Setup: copy `.env.example` to `.env` (development: absolute `DATABASE_PATH`); run migrations `docker compose run --rm flyway`; optional demo data `docker compose --profile demo run --rm flyway-demo`; note about `UID`/`GID` if the user id is not 1000.
4. E-mail setup: create a Lettr account; a **sandbox key** (`lttr_sandbox_…`) for testing (e-mails go to the key owner's address, no domain needed); a verified sender domain for real sending; put the key into `.env` and restart; the key is never committed.
5. Running from Rider (open `Invoicing.sln`, run `Invoicing.App`; Avalonia previewer via Rider's Avalonia support / AvaloniaRider plugin) and from the command line (`dotnet run --project src/Invoicing.App`).
6. Running the tests (`dotnet test`).
7. Building for Windows (`scripts/build-windows-release.sh`).
8. Architecture and components (the table from 6.1, layer diagram, MVVM).
9. Why Avalonia (section 5.1, short).
10. Why Dapper, and what EF Core is: EF Core is Microsoft's ORM; it maps classes to tables, generates SQL from LINQ and has its own migrations. We use Dapper because the SQL stays visible and simple to explain, Flyway already owns the schema (two migration systems would conflict), money is stored as TEXT and converted explicitly, and the app has only a few tables.
11. Database and Flyway (tables, versioned vs repeatable migrations, why the app only checks the version and does not migrate itself).
12. How PostgreSQL could be added later (not built): add a `postgres` service to Compose, a separate Postgres migrations folder, and Npgsql-based repository classes implementing the same Core interfaces; the UI and Core do not change.
13. Diagrams (embed the Mermaid code).
14. Licences: QuestPDF Community licence, Noto Sans (SIL OFL), other packages MIT.

`docs/class-diagram.md`: Mermaid `classDiagram` with `Party`, `Customer`, `Supplier`, `Document`, `TaxInvoice`, `NonVatInvoice`, `CreditNote`, `InvoiceLine`, `VatSummary`, `DocumentService`, `DocumentEmailService`, the repository interfaces, `ICompanyRegistry`, `IEmailSender` and their implementations. Show inheritance and the main associations.

`docs/activity-diagram.md`: Mermaid `flowchart TD`: user adds a customer from ARES → creates an invoice → adds lines → issues it (decision: validation errors? → fix and retry) → exports PDF **or** sends by e-mail, with the decision **"E-mail nastaven?"** (no → button disabled with tooltip → export PDF; yes → send dialog → success / error → retry).

---

## 19. Czech texts reference

Put these into the `Texts/` classes (App), `PdfTexts` (Pdf) and templates (Email). Keep the wording.

### 19.1 Screens and buttons

Tabs: `Dodavatel`, `Adresář`, `Doklady`. Buttons: `Načíst z ARES`, `Uložit`, `Nový`, `Nový doklad`, `Otevřít`, `Přidat řádek`, `Odebrat řádek`, `Vystavit`, `Smazat`, `Zaplaceno`, `Stornovat`, `Opravný doklad`, `Export PDF`, `Odeslat e-mailem`, `Odeslat`, `Zrušit`, `OK`, `Ano`, `Ne`. Window title: `Fakturace`.

### 19.2 Document kinds and statuses

| Code | Czech |
|---|---|
| TaxInvoice | Faktura – daňový doklad |
| NonVatInvoice | Faktura |
| CreditNote | Opravný daňový doklad |
| Draft | Koncept |
| Issued | Vystaveno |
| Paid | Zaplaceno |
| Cancelled | Stornováno |
| (overdue) | Po splatnosti |

### 19.3 Validation messages

Format: for targets Supplier / Customer / Line the text is `{prefix}: {message}.`, prefixes `Dodavatel`, `Odběratel`, `Řádek {n}`. Target Document messages are full sentences.

| Code | Message |
|---|---|
| Missing | není vyplněn |
| NameMissing | chybí název nebo jméno |
| AddressIncomplete | adresa není úplná (ulice, obec, PSČ) |
| IcoMissing | chybí IČO |
| IcoInvalid | IČO není platné |
| DicMissing | chybí DIČ |
| DicInvalid | DIČ má neplatný formát (např. CZ12345678) |
| RegisterEntryMissing | chybí zápis v obchodním rejstříku |
| BankAccountInvalid | chybí číslo účtu nebo je neplatné |
| IssuedByMissing | chybí údaj Vystavil |
| EmailInvalid | e-mail není platný |
| MustBeVatPayer | musí být plátce DPH |
| MustNotBeVatPayer | nesmí být plátce DPH |
| DescriptionMissing | chybí popis |
| QuantityNotPositive | množství musí být větší než nula |
| QuantityZero | množství nesmí být nula |
| UnitPriceNegative | cena za jednotku nesmí být záporná |
| VatRateNotAllowed | sazba DPH není povolená |
| DiscountOutOfRange | sleva musí být od 0 do 100 % |
| NoLines | Doklad musí mít alespoň jeden řádek. |
| IssueDateMissing | Chybí datum vystavení. |
| DueDateMissing | Chybí datum splatnosti. |
| TaxableSupplyDateMissing | Chybí datum uskutečnění zdanitelného plnění (DUZP). |
| DueDateBeforeIssueDate | Datum splatnosti nesmí být dříve než datum vystavení. |
| IssuedLongAfterTaxableSupply | Upozornění: doklad je vystaven více než 15 dní po DUZP (§ 28 zákona o DPH). |
| CorrectionReasonMissing | Chybí důvod opravy. |
| OriginalInvoiceMissing | Chybí opravovaná faktura. |

Use an exhaustive `switch` expression; the default arm throws `ArgumentOutOfRangeException` (so a new code without text is found immediately).

### 19.4 Startup errors

- Window title: `Aplikaci nelze spustit`
- Configuration: `Chybí povinná hodnota {KEY} v souboru .env.` / `Hodnota {KEY} v souboru .env je neplatná.` / `Soubor .env obsahuje neplatný řádek {n}.` + `Upravte soubor .env ve složce aplikace a spusťte aplikaci znovu.`
- Database: `Databáze nebyla nalezena: {path}` / `Databáze není připravena (chybí tabulka flyway_schema_history).` / `Databáze má starou verzi schématu. Je potřeba verze {version}.` + `Spusťte migrace v kořenové složce projektu: docker compose run --rm flyway`

### 19.5 ARES

| Error | Czech |
|---|---|
| InvalidIco | Neplatné IČO. IČO musí mít 8 číslic a správnou kontrolní číslici. |
| NotFound | Subjekt s tímto IČO nebyl v ARES nalezen. |
| Timeout | ARES neodpověděl včas. Zkuste to znovu. |
| NoConnection | Nepodařilo se spojit s ARES. Zkontrolujte připojení k internetu. |
| UnexpectedResponse | ARES vrátil neočekávanou odpověď. |

### 19.6 E-mail

Status line on the Dodavatel screen:
- `Odesílání e-mailů: zapnuto (odesílatel {from})`
- `Odesílání e-mailů: vypnuto – chybí LETTR_API_KEY` (or `chybí EMAIL_FROM_ADDRESS`, `neplatná hodnota EMAIL_FROM_ADDRESS`; join several with `, `)

Tooltip of the disabled "Odeslat e-mailem" button:
- not configured: `Odesílání e-mailů není nastaveno. Doplňte LETTR_API_KEY a EMAIL_FROM_ADDRESS do souboru .env a restartujte aplikaci.`
- draft: `Doklad nejdříve vystavte.`
- cancelled: `Stornovaný doklad nelze odeslat.`

Send dialog: labels `Komu:`, `Předmět:`, `Text:`, `Příloha:`; `Tento doklad už byl odeslán {datum} na {adresa}.`; `Zadejte platnou e-mailovou adresu.`; success `E-mail byl odeslán na {adresa}.`; detail info `Odesláno {datum} na {adresa}`.

Send errors:

| Error | Czech |
|---|---|
| SenderDomainNotVerified | Doména odesílatele není v Lettr ověřená. |
| InvalidApiKey | Neplatný API klíč Lettr. |
| NotAllowed | Pro tento klíč nebo doménu není odesílání povoleno. |
| PreviousAttemptInProgress | Předchozí pokus o odeslání se ještě zpracovává. Zkuste to za chvíli znovu. |
| InvalidData | Lettr odmítl data e-mailu: {details} |
| RateLimited | Byl dosažen limit odesílání. Zkuste to později. |
| ServiceUnavailable | Služba Lettr je nedostupná. Zkuste to později. |
| NoConnection | Nepodařilo se spojit se službou Lettr. Zkontrolujte připojení k internetu. |
| UnexpectedResponse | Neočekávaná odpověď služby Lettr (HTTP {status}). |

### 19.7 Other

- `Nastala neočekávaná chyba: {message}`
- `Nejdříve vyplňte údaje dodavatele.`
- `PDF bylo uloženo.`
- `Neplatné číslo účtu`, `Neplatné číslo`
- `PDF se nepodařilo uložit.`
- `Do vybraného souboru nelze zapisovat.`
- `Soubor .env se nepodařilo přečíst.`
- `K souboru .env nebo k databázi chybí oprávnění.`
- `Databázi se nepodařilo otevřít.`
- `Operaci se nepodařilo uložit do databáze. Zkuste to znovu.`
- `Doklad již v databázi není.`
- `Potvrzení o odeslání se nepodařilo uložit. Zkuste odeslání znovu v tomto okně.`
- `Údaje dodavatele byly uloženy.`
- PDF texts: section 12.3.

---

## 20. Out of scope (do not build)

Foreign currency; foreign or EU customers; reverse charge; VAT-exempt supplies; advance invoices (zálohy); simplified invoices; cash payments and rounding; multiple suppliers; users or logins; running the GUI inside Docker; deleting customers; for e-mail: editing templates in the UI, multiple recipients or CC, scheduled sending, delivery tracking or webhooks, Lettr stored templates. PostgreSQL is only described in the README.

---

## 21. Final checklist (phase 9: all must be true)

Build and tests
- [ ] `dotnet build` with **zero warnings**; `dotnet format --verify-no-changes` passes.
- [ ] `dotnet test`: all tests pass; every item of section 15.1 is covered.
- [ ] `docker compose run --rm flyway` works on an empty `data/` folder; the demo profile works.
- [ ] The Windows release zip builds and contains `.env` with an empty `LETTR_API_KEY`.

Course requirements
- [ ] GUI app (Avalonia), layered (projects + MVVM), no SQL/HTTP/PDF code in ViewModels.
- [ ] .NET naming everywhere; descriptive names; no Czech identifiers.
- [ ] No method longer than ~25 lines; each method does one thing; no class is a "god object".
- [ ] No dead code, no commented-out code, no TODOs, no template leftovers.
- [ ] Comments are meaningful (XML docs where useful, "why" comments with legal references); no redundant or outdated comments.
- [ ] Data and operations together (lines calculate bases, documents validate themselves, etc.).
- [ ] All non-constant fields private.
- [ ] Inheritance only `Document` → 3 kinds and `Party` → `Customer`/`Supplier`.
- [ ] No magic constants; no empty or broad `catch` (except the global handler); errors reach the user in Czech.

Specification
- [ ] Money `decimal`, rounding AwayFromZero, VAT "zdola" per rate.
- [ ] Numbering without gaps, yearly reset, `OD-` series for credit notes.
- [ ] Snapshot on issue; issued documents read-only.
- [ ] PDFs: correct titles, register entry, IČO, "Vystavil", "Nejsem plátce DPH.", QR only on invoices, Czech characters.
- [ ] E-mail button always visible; disabled with the correct tooltip; idempotency key reused on retry; key never logged, shown or committed.
- [ ] Missing required config stops the app with a Czech message; missing e-mail config only disables e-mail.
- [ ] README has every section of section 18; both Mermaid diagrams exist.
- [ ] `.env` is git-ignored; `.env.example` is committed; `git status` shows no secrets.

When everything is true, give the user a final report: what was built, how to run it, the test count, and any open questions.

---

## Appendix A: glossary (Czech → English in code)

| Czech | Code |
|---|---|
| faktura – daňový doklad | `TaxInvoice` |
| faktura (neplátce) | `NonVatInvoice` |
| opravný daňový doklad | `CreditNote` |
| doklad | `Document` |
| koncept / vystaveno / zaplaceno / stornováno | `Draft` / `Issued` / `Paid` / `Cancelled` |
| dodavatel / odběratel | `Supplier` / `Customer` |
| IČO / DIČ | `Ico` / `Dic` |
| plátce DPH | `IsVatPayer` |
| zápis v obchodním rejstříku | `RegisterEntry` |
| vystavil | `IssuedBy` |
| datum vystavení | `IssueDate` |
| DUZP (datum uskutečnění zdanitelného plnění) | `TaxableSupplyDate` |
| datum splatnosti | `DueDate` |
| po splatnosti | overdue (`IsOverdue`) |
| variabilní symbol | `VariableSymbol` |
| číslo účtu | `BankAccount` |
| základ daně | `TaxBase` |
| sazba DPH | `VatRate` |
| rekapitulace DPH | `VatSummary` |
| sleva | `Discount` |
| důvod opravy | `CorrectionReason` |
| číselná řada | `NumberSeries` |
| adresář | address book (`Customers…`) |

## Appendix B: decisions that fill gaps in the agreed spec

These were not spelled out in the spec and were decided for this file. If the user disagrees with any of them, the user's decision wins.

1. `.env` is read from the app folder, and a relative `DATABASE_PATH` is resolved against that folder. For development, the `.env` uses an absolute path; the Debug build copies the repo `.env` next to the app, and publish never copies it.
2. Compose has a second Flyway service `flyway-demo` (profile `demo`) and passes `ignoreMigrationPatterns=repeatable:missing` to the normal one; both run as the host user.
3. Lettr status **409** (same idempotency key still processing) is mapped to its own Czech message.
4. ARES: the house-number letter (`cisloOrientacniPismeno`) is included in the street; villages without streets use the part-of-town name.
5. Extra validation: supplier bank account must be valid (needed for the QR code), "Vystavil" required, DIČ format `CZ` + 8–10 digits, supplier VAT status must match the document type, line discount 0–100 %, credit-note line quantity ≠ 0.
6. Credit notes never become "overdue".
7. Drafts can be deleted with a "Smazat" button in the detail window (the spec allows deleting drafts but listed no button).
8. Export PDF is available only for non-draft documents; a cancelled document's PDF shows "STORNOVÁNO".
9. The supplier form can be saved incomplete; the customer form only when valid.
10. Customers cannot be deleted.
11. `CreditNote.OriginalInvoice` is a small `DocumentReference(Id, Number)` instead of a full invoice object.
12. Validation results use `ValidationMessage` (code + target + severity) so that warnings and errors share one type.

13. A credit-note draft uses the original invoice's customer snapshot when reopened. Ordinary invoice drafts use current address-book details.
14. A cancelled invoice PDF omits QR Platba so it does not invite payment.
