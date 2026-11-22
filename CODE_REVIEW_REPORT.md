# Code review: full pre-submission review (2026-11-22)

Review done according to `CODE_REVIEW.md` (full review, sections 6–13) against `AGENTS.md` v1.2. No code was changed.

## Verdict

**Ready after fixes.** There are no Blockers. The build is clean, formatting is clean and all 137 tests pass. The money, VAT, numbering, IBAN, SPAYD and snapshot logic is correct. Three local Majors remain: one `catch` that hides programming errors, Czech text spread outside the `Texts/` classes, and one method over the hard length limit.

Findings: **0 Blocker, 3 Major, 14 Minor, 7 Level, 5 Questions.**

## Automated checks

| Check | Result | Notes |
|---|---|---|
| `dotnet build Invoicing.sln` (0 warnings) | ✅ | `0 Warning(s), 0 Error(s)` |
| `dotnet format --verify-no-changes` | ✅ | no changes reported |
| `dotnet test` | ✅ 137 passed, 0 failed, 0 skipped | |
| `dotnet list package --vulnerable` | not run | needs NuGet access (see "Not verified") |
| One Avalonia version | ✅ | all `Avalonia*` = 12.1.2 |
| No floating package versions | ✅ | |
| Flyway image pinned | ✅ | `flyway/flyway:13.13.0` in both services |
| `Directory.Build.props`, `.editorconfig` vs §8.3–8.4 | ✅ | identical in content |
| Secrets and git hygiene | ⚠️ not verifiable | `.git/` is an **empty folder**, so no `git` command works (Q1). `.gitignore` covers `.env`, `data/*.db`, `artifacts/`, `.idea/`. |
| Release zip | ✅ | `.env` in the zip has an empty `LETTR_API_KEY` (count = 0); `Invoicing.App.exe` and `data/invoicing.db` are both present |
| Grep checks (Appendix A) | ✅ | about 60 candidates. Confirmed: check 17 magic numbers (m1). Expected and accepted: `Kč` in `CzechFormat`, `Console.Error` in the global handler, `float` for a PDF size in cm, rounding `2`/`100`, HTTP status codes in the `switch` |
| Method metrics (Appendix B) | ⚠️ | one method over 25 lines (M3), about 15 methods with 16–22 lines (m2). Class-level hits come from primary constructors and are false positives. |
| Required test classes | ⚠️ | 18 of the 25 required class names are missing. The tests exist, but they are grouped into larger classes (m9). |
| Known values in tests | ✅ | all 15 known values are found |

## Course requirements

| # | Requirement | Status | Evidence / findings |
|---|---|---|---|
| 1 | aplikace má grafické rozhraní | ✅ | Avalonia app, three tabs, modal detail and send windows |
| 2 | konvence pojmenování v .NET | ✅ | the naming rules build clean; `Async` suffixes are correct. Some test names do not follow `Method_Condition_Expected` (m9). |
| 3 | srozumitelné pojmenování | ✅ | English names, domain abbreviations only. One misleading constant (m13). |
| 4 | přehledné formátování | ✅ | `dotnet format` is clean; one type per file; file-scoped namespaces |
| 5 | žádné dlouhé metody | ⚠️ | M3 (28 lines), m2 |
| 6 | žádný mrtvý kód | ✅ | no template leftovers, TODOs or commented-out code. Small leftovers: m5, m12. |
| 7 | metoda = jedna činnost | ⚠️ | `bool`/`null` parameters that switch behaviour (m6) |
| 8 | smysluplné komentáře | ⚠️ | § 28 and § 37 comments are present; "why" comments are missing at IBAN mod 97, SPAYD clean-up and a few others (m4) |
| 9 | žádné zastaralé komentáře | ✅ | every comment matches its code |
| 10 | žádné zbytečné komentáře | ✅ | |
| 11 | data a operace v jedné třídě | ✅ | `InvoiceLine.CalculateBase`, `Document.Validate`, `CzechBankAccount.ToIban`. Small exception: m7. |
| 12 | nekonstantní atributy privátní | ✅ | `Status`, `Number`, `Id` have private setters, and `Supplier` is a read-only copy |
| 13 | dědičnost = "je" | ✅ | only `Document` → 3 kinds and `Party` → 2 kinds (plus framework bases) |
| 14 | pokrytí jednotkovými testy | ✅ | every item of §15.1 is covered (some only indirectly); see m9 |
| 15 | všechny testy projdou | ✅ | 137 / 137 |
| 16 | rozdělení do vrstev | ✅ | references exactly as in §6.1; no SQL, HTTP or QuestPDF in ViewModels |
| 17 | žádné magické konstanty | ⚠️ | M2, m1 |
| 18 | žádné božské objekty | ⚠️ | `DocumentDetailViewModel` has ≈370 lines across two partial files (m3) |
| 19 | žádné skrývání chyb | ⚠️ | M1 |
| 20 | industry standards | ✅ | nullable on, warnings as errors, async I/O, parameterised SQL, one `HttpClient` per service, invariant culture in persistence, pinned versions |

## Error handling

| # | Failure | Status | Findings |
|---|---|---|---|
| E1 | `.env` malformed line | ✅ | The message text lives in `App.axaml.cs`, not in `Texts/` (M2) |
| E2 | Required setting missing or invalid | ✅ | Every key is listed; no fallback values |
| E3 | E-mail setting missing or invalid | ✅ | Status line and tooltip texts match §19.6 |
| E4 | DB missing, not migrated, or old schema | ✅ | `Mode = ReadWrite`; `SqliteException` at startup is also handled |
| E5 | SQL fails at run time | ⚠️ | The detail window reloads after a `SqliteException` ✅, but see M1 |
| E6 | Issue with validation errors | ✅ | Nothing is issued and the messages are shown; warnings are shown after issuing. There is no unit test that the repository is not called (optional test). |
| E7 | Forbidden status change or edit | ✅ | Guards throw and buttons are disabled |
| E8 | Invalid number in the lines grid | ✅ | `Neplatné číslo` in the cell, line message, totals show `—`, Save and Issue are disabled |
| E9 | Invalid bank account | ✅ | IBAN shows `Neplatné číslo účtu`; saving is still allowed |
| E10 | ARES failures | ✅ | Only the expected exceptions are caught; the token check is correct |
| E11 | Lettr failures | ✅ | The dialog stays open and the same idempotency key is reused; no DB row on failure |
| E12 | Recipient empty or invalid | ✅ | |
| E13 | Double-click on `Odeslat` | ✅ | `IsSending` and `AsyncRelayCommand` |
| E14 | Template placeholder or resource missing | ⚠️ | `TemplateRenderException` derives from `InvalidOperationException` and is shown as a "database" error (M1) |
| E15 | PDF of a draft | ✅ | |
| E16 | Total above the QR limit | ✅ | The note is shown; no test covers it |
| E17 | Save-file picker cancelled | ✅ | |
| E18 | PDF file cannot be written | ✅ | Dedicated Czech messages (see Q5) |
| E19 | No supplier, or no customer on a draft | ✅ | Tooltip `Nejdříve vyplňte údaje dodavatele.` plus guards |
| E20 | Any other exception | ⚠️ | The global handler is correct, but an error during startup leaves an invisible process running (m11) |

## Specification

| Area | Status | Findings |
|---|---|---|
| Money and VAT | ✅ | `decimal` everywhere; AwayFromZero; VAT from the sum per rate; rows from the highest rate |
| Documents and lifecycle | ✅ | Snapshots through `CreateSnapshot()`; the transition table is correct; credit notes are never overdue or paid |
| Validation | ✅ | Codes, targets and positions are correct; 15 days gives nothing, 16 days a warning; IČO, DIČ and e-mail rules are exact. Q4 |
| Numbering | ✅ | Numbering happens in one IMMEDIATE transaction; the year comes from the issue date; `OD-` series; yearly reset. A concurrency test exists. |
| IBAN and QR Platba | ✅ | All known IBANs and the SPAYD string match. The QR code is also hidden on cancelled invoices (Q3). |
| Configuration | ✅ | First `=`, one pair of quotes, line numbers, a defined environment variable overrides the file, `EmailSettings.ToString()` hides the key |
| Data | ✅ | Row classes and mappers use invariant culture; `AND status = 'Draft'` guards; version check in the right order. Credit-note draft customer: Q2 |
| ARES | ✅ | Absolute URL string, `Accept` header, `778/3a`, village fallback, `textovaAdresa` fallback |
| PDF | ✅ | Titles, Dodavatel and Odběratel, DIČ only on tax documents, register entry, `Vystavil`, `Strana x / y`, `Nejsem plátce DPH.`, credit-note section, `STORNOVÁNO`, ASCII file names |
| E-mail | ✅ | Template texts are exact; HTML values are encoded; Email does not reference Pdf; headers, snake_case JSON and base64 are correct; `sent_at` uses `"O"` |
| Culture | ✅ | `cs-CZ` is set at startup; no culture-dependent `Parse` or `ToString` on money or dates |
| Deliverables | ⚠️ | README is missing parts of §18 items 10–11 (m14); both diagrams exist and match the code; release script and demo data are OK |

---

## Findings

### M1 · Major · `src/Invoicing.App/ViewModels/DocumentDetailViewModel.Commands.cs:148`

**Rule:** AGENTS.md §3.5 (no hidden errors), CODE_REVIEW §8.1 ("catch only exceptions you expect at that point")

**Problem:** `RunDatabaseActionAsync` catches **every** `InvalidOperationException` (when `Id != 0`) and shows `Operaci se nepodařilo uložit do databáze.` It wraps Save, Issue, Paid, Delete, Cancel, Credit note **and Send e-mail**. Many exceptions of this type are programming-error guards, not database failures:
- `TemplateRenderException` (derives from `InvalidOperationException`) from the composer when the send dialog opens;
- `DocumentPdfGenerator` ("Draft documents cannot be exported"), `EnsureEditable`, `MarkAsPaid` and `Cancel` guards, and the composer's "Missing supplier snapshot".

**Why it matters:** A bug in a template or a lifecycle guard is shown to the user as "try again". The developer never sees it, because it never reaches the global handler or `Console.Error`. This is exactly the "hidden error" anti-pattern that is graded.

**Fix:** Catch only what the repository throws on a lost row. The smallest change is a dedicated exception type in Data, or simply removing the `InvalidOperationException` arm. With that arm removed, the "no longer has the expected status" guard reaches the global handler, which is acceptable under E5. Also stop sending the e-mail flow through the "database" wrapper:

```csharp
[RelayCommand(CanExecute = nameof(CanSendEmail))]
private async Task SendEmailAsync()
{
    IsBusy = true;
    try
    {
        await OpenEmailAsync();
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task RunDatabaseActionAsync(Func<Task> action)
{
    IsBusy = true;
    try
    {
        await action();
    }
    catch (SqliteException)
    {
        await HandleDatabaseFailureAsync();
    }
    finally
    {
        IsBusy = false;
    }
}
```

### M2 · Major · Czech literals outside `Texts/` (several files)

**Rule:** CODE_REVIEW §12 ("Czech literals in C# live only in the `Texts/` classes"), AGENTS.md §14.1 (`Texts/` structure), course requirement 17 (no magic constants)

**Problem:** About 20 user-facing Czech strings are written inline in ViewModels, services and `App`:
- `App.axaml.cs:61,65,69,73,112` (`.env` errors, `Nastala neočekávaná chyba: …`)
- `DocumentDetailViewModel.Commands.cs:46,51,55,68,82,91,133,162` (`PDF bylo uloženo.`, the three confirmations, `E-mail byl odeslán na …`, the DB-failure text)
- `DocumentDetailViewModel.cs:42,52,55,142` (`Cena za jednotku bez DPH`, `Odesláno … na …`, `Základ`/`DPH`, `Řádek {n}: …` duplicates the format in `ValidationTexts`)
- `DocumentsViewModel.cs:31` (filter names), `SendEmailViewModel.cs:31,40,104`, `SupplierViewModel.cs:37,78`, `DialogService.cs:21`

The `Texts/` folder also differs from §14.1 (`DocumentTexts` and `StartupTexts` instead of `UiTexts`, `StatusTexts`, `DocumentKindTexts`, `ConfigurationTexts` and `DatabaseTexts`). That part alone would be fine if all texts lived there.

**Why it matters:** The examiner checks "no magic constants" and "texts in one place". The §19 wording is harder to check and keep consistent when it is spread over ten files. `Řádek {n}:` is already duplicated.

**Fix:** Add a `UiTexts` class (constants plus small format methods) and replace the literals with references:

```csharp
internal static class UiTexts
{
    public const string PdfSaved = "PDF bylo uloženo.";
    public const string ConfirmIssue = "Vystavit doklad? Po vystavení už nepůjde upravit.";
    public static string EmailSent(string address) => $"E-mail byl odeslán na {address}.";
    // ...
}
```

### M3 · Major · `src/Invoicing.App/App.axaml.cs:45` (`StartAsync`, 28 lines)

**Rule:** AGENTS.md §2 (hard limit about 25 lines), course requirement 5

**Problem:** `StartAsync` has 28 lines: four `catch` blocks plus the success and failure branches. It is the only method over the hard limit.

**Fix:** Move the happy path into its own method, so `StartAsync` only maps exceptions to messages (and move the texts to `StartupTexts`, see M2):

```csharp
private static async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
{
    try
    {
        await LoadSettingsAndOpenAsync(desktop);
    }
    catch (EnvFileFormatException error)
    {
        ShowError(desktop, [StartupTexts.InvalidLine(error.LineNumber)], StartupTexts.ConfigurationHelp);
    }
    // ... the other three catches, one line each
}
```

---

### m1 · Minor · magic numbers and strings

**Rule:** AGENTS.md §3.6, course requirement 17 ("a repeated literal is always a finding")

- `CompositionRoot.cs:16-17`: `TimeSpan.FromSeconds(10)` and `(30)`. §11 and §13.4 explicitly ask for named constants (`AresTimeout`, `LettrTimeout`).
- `DocumentsViewModel.cs:86-89`: the filter index `1..4` matches the order of the `Filters` list only by convention. Name the indexes, or use a small `DocumentFilter` enum whose items map to `Filters`.
- PDF layout: `Padding(3)` is repeated about 15 times, plus `FontSize(22/16/14)`, `Spacing(16)`, `RelativeColumn(3)` and `BorderBottom(1)`. `DocumentPdf` already uses named constants (`Margin`, `FontSize`), so do the same in the components (`CellPadding`, `TitleFontSize`, …).
- `DocumentNumbering.cs:25` `"OD-"`, `PdfFileNames.cs:11` `"Opravny-doklad-"`, `"Faktura-"`, `".pdf"`: these are meaningful strings. Make them `const`.
- `DocumentEmailComposer.cs:13-16`: `"subject.txt"`, `"html"`, `"txt"`; and the placeholder name `"SupplierName"` appears twice (lines 13 and 28). Use constants.
- `CzechBankAccount.cs:39,42`: `97` and `98` are the mod-97 check constants. Name them (`IbanModulus`, `IbanCheckBase`) and add a comment (see m4).
- `LettrEmailSender.cs:42,63`: `"Bearer"` and `"[redacted]"`.

### m2 · Minor · methods with 16–22 lines

**Rule:** AGENTS.md §2 (aim ≤ 15 lines)

`DocumentDetailViewModel.LoadForm` (22), `UpdateDraft` (21), `ExportAsync` (21), `VatSummaryComponent.AddTable` (22), `AppSettings.Load` (20), `DocumentValidation.GetLineIssues` (20), `CompositionRoot.CreateMainAsync` (20), `PartiesComponent.AddParty` (20), `SendEmailViewModel.SendAsync` (19), `DocumentEmailComposer.GetValues` (19), `EnvFileParser.Parse` (18), `OpenMainAsync` (17), `QrPaymentComponent.Compose` (17), `DocumentMapper.Write` and `WriteParties` (17). None is hard to read. Where it is easy, extract one obvious step, for example `LoadForm` → `LoadLines()`, or `UpdateDraft` → `ApplySupplyDateAndReason()`.

### m3 · Minor · `src/Invoicing.App/ViewModels/DocumentDetailViewModel*.cs` (≈370 lines)

**Rule:** AGENTS.md §2 (classes ≤ 200 lines, split by responsibility), course requirement 18

**Problem:** The class is split into two `partial` files, but it is still one class. It holds form state, line editing, totals, validation, 10 commands, PDF file writing and the e-mail flow. A `partial` split does not count as a split by responsibility at the oral exam.

**Fix (no design change):** Move line handling (`Lines`, `AddLine`, `RemoveLine`, `AppendLine`, `HandleLineChanged`, `InputErrors`) into a small `DocumentLinesViewModel`, which the detail view model owns. That takes the class to about 250 lines; together with M1 and M2 it gets close to 200.

### m4 · Minor · missing "why" comments

**Rule:** course requirement 8; CODE_REVIEW §7 row 8 (IBAN mod 97, SPAYD clean-up)

- `CzechBankAccount.ToIban` (line 35): explain the ISO 13616 mod 97 step. `"123500"` = C=12, Z=35, then "00"; the remainder is computed digit by digit because the number does not fit into `long`.
- `SpaydPayment.NormalizeMessage` (line 35): explain that after `FormD` the diacritics become separate combining marks, which the ASCII filter drops.
- `DocumentMapper.cs:77`: `IsVatPayer = kind != NonVatInvoice`. Explain that the snapshot has no VAT-payer column, so the value comes from the document kind.
- `DocumentRepository.cs:34`: the comment says "immediate transaction", but the code calls `BeginTransaction()` with no arguments. Write `BeginTransaction(deferred: false)` so the code says what the comment says.
- `scripts/build-windows-release.sh`: explain why `--env-file .env.example` is passed (so that Compose never reads the developer's real `.env`).

### m5 · Minor · `src/Invoicing.Data/DocumentRepository.cs:85`

**Rule:** course requirement 6 (dead code), readability

**Problem:** `DocumentNumbering.ToVariableSymbol(number);` is called and its result is thrown away. It only acts as a "throw if invalid" check. A reader cannot tell why it is there, and for `YYYY-NNNN` it can never fail.

**Fix:** Delete the `if` block, or add a comment: `// Fail before saving if the number cannot be a variable symbol (QR Platba needs digits only).`

### m6 · Minor · `bool`/`null` parameters that switch behaviour

**Rule:** course requirement 7, CODE_REVIEW §7 row 7

- `Document.cs:124` `ValidateCommon(supplier, rates, bool creditNote)`.
- `Document.cs:127` `ValidateSupplier(messages, supplier, rates is not null)`: "rates are null" secretly means "this is a non-VAT invoice".
- `CreditNote.cs:45` `ValidateSupplyDate(messages, date, null)`: `null` secretly means "skip the § 28 warning".

**Fix:** Make the meaning visible, without a design change. Pass named arguments (`needsVatPayer: true`, `allowNegativeQuantity: true`, which is the name §9.4 uses) and split the supply-date check into `ValidateSupplyDateSet(...)` and `WarnIfIssuedLate(...)`. Only `TaxInvoice` calls the second one.

### m7 · Minor · `src/Invoicing.Core/Documents/Document.cs:72`

**Rule:** course requirement 13 and 11 (use inheritance properly; the base class should not know its subclasses)

**Problem:** `ReplaceLines` checks `Kind == DocumentKind.NonVatInvoice` to remove VAT. The base class asks which subclass it is.

**Fix:**

```csharp
// Document
protected virtual InvoiceLine PrepareLine(InvoiceLine line) => line;
public void ReplaceLines(IEnumerable<InvoiceLine> lines) { EnsureEditable(); ... lines.Select(PrepareLine) ... }

// NonVatInvoice
protected override InvoiceLine PrepareLine(InvoiceLine line) => line.WithoutVat();
```

### m8 · Minor · `src/Invoicing.App/Views/MessageWindow.axaml.cs:11-12`, `ErrorWindow.axaml.cs:11`

**Rule:** AGENTS.md §14.1 ("Views: XAML only + `InitializeComponent()`. No logic in code-behind.")

**Problem:** These two windows close themselves from `Click` handlers. `SendEmailWindow` already uses the agreed pattern (a command in the VM plus `CloseRequested`).

**Fix:** Give `MessageViewModel` and `ErrorViewModel` an `[RelayCommand] Close(bool)` that raises `CloseRequested`, and subscribe to it in `DialogService` and `App.ShowError`, as `ShowEmailAsync` already does. Alternatively, ask the examiner whether a one-line close handler is acceptable. Either way, be ready to explain it.

### m9 · Minor · test structure (`tests/Invoicing.Tests`)

**Rule:** AGENTS.md §15 ("one test class per tested class", `Method_Condition_Expected`), CODE_REVIEW §11 ("one behaviour per test")

- 18 required test classes do not exist under their names. Their tests live in `PaymentTests` (IBAN, IČO, numbering, SPAYD), `ConfigurationTests` (parser, settings, e-mail availability), `CzechFormattingTests` (format and parser), `DocumentValidationTests` (three document kinds plus supplier and customer), `EmailTemplateTests` (renderer, store and composer) and `AresCompanyRegistryTests` (also the mapper).
- Names without the three parts: `Parse_IgnoresCommentsAndUnquotesValues`, `IsOverdue_DependsOnKindDateAndStatus`, `CheckAsync_ComparesSchemaVersions`, `DocumentNumbering_FormatsBothSeriesAndVariableSymbol`.
- Several tests check many behaviours at once: `Issue_Draft_SnapshotsPartiesAndPreventsEdits` (11 asserts), `DocumentNumbering_…` (8), `Parse_…` (parsing **and** two error cases), `Evaluate_…` (five scenarios), `CheckAsync_…` (four statuses). When one assert fails, the others never run.
- Cases that are only covered indirectly: "negative quantity OK" and "no 15-day warning" on a credit note, "VAT rate not checked" on a NonVatInvoice, and "all six templates exist". It is better to give each one its own small test.

**Fix:** Split the classes by tested class, mostly by moving existing methods. Split the multi-scenario tests into `[Theory]` cases, or into one test per behaviour.

### m10 · Minor · `src/Invoicing.App/Services/DocumentDelivery.cs:12`, `DocumentWorkspace.cs`

**Rule:** AGENTS.md §2 (≤ 4 parameters), readability

**Problem:** `DocumentDelivery` takes 5 constructor parameters (not prescribed) and exposes two of them again (`Settings`, `Mail`). `DocumentWorkspace` is a bag of four dependencies, which leads to chains like `_editor.Workspace.Workflow.SaveDraftAsync(...)` (four dots).

**Fix:** `DocumentDelivery` can create `DocumentEmailService` from the sender and the repository it already has. In the detail VM, keep a direct `DocumentService` field (`_documents.SaveDraftAsync(...)`).

### m11 · Minor · `src/Invoicing.App/App.axaml.cs:31`

**Rule:** CODE_REVIEW §8.2 E20, industry practice (the app must always be able to close)

**Problem:** `ShutdownMode = OnExplicitShutdown`. If `StartAsync` fails with an exception that is not caught there (for example a `FormatException` while loading corrupted data in `CreateMainAsync`), the global handler shows the message window. After it is closed, no window is left and `Shutdown()` is never called, so the process keeps running invisibly.

**Fix:** In `ShowUnexpectedError`, when `desktop.MainWindow` is `null`, call `desktop.Shutdown(1)` from the message window's `Closed` event (the same pattern as `ShowError`).

### m12 · Minor · `.dockerignore` and empty tooling folders

**Rule:** course requirement 6 (no dead code or leftovers)

There is no Dockerfile (by design, §8.7), so `.dockerignore` is never used. It also lists `.aws`, `.codex` and `.agents`, which are empty folders in the repository root. Delete the file and the empty folders, or confirm that they are not committed (see Q1).

### m13 · Minor · `src/Invoicing.Core/Validation/IcoValidator.cs:8`

**Rule:** course requirement 3 (descriptive names)

`DigitCount = 10` is the modulus of the check digit, not a count of digits (the IČO has `Length = 8`). Rename it to `CheckDigitModulus`.

### m14 · Minor · `README.md` sections 10–11

**Rule:** AGENTS.md §18

- Section 10 (Dapper) does not say that **two migration systems would conflict** (Flyway and EF migrations), and it does not mention that money is stored as TEXT.
- Section 11 (Database) does not list the tables by name, does not explain **versioned vs repeatable** migrations, and does not explain **why the app only checks the version and does not migrate itself**.

Add two or three sentences each.

### Notes (not counted)

- `SupplierViewModel.cs:89`: `(int)(DueDays ?? 0)` silently cuts `14.5` to `14`. Set `FormatString="0"` / `Increment="1"` on the `NumericUpDown` so the user cannot enter decimals.

---

## Too advanced for the level

- **L1** · `src/Invoicing.Core/Documents/VatSummary.cs:13`: a chain of five operators plus a method group (`GroupBy → OrderByDescending → Select(CalculateRow) → ToList → AsReadOnly`). Use the `foreach` version from CODE_REVIEW §3.2. It is also where the § 37 comment reads best.
- **L2** · `src/Invoicing.Pdf/PdfSetup.cs:11`: `Lazy<bool>` plus `_ = _initialized.Value` (`Lazy<T>` is on the §3.2 list). The simpler version is a static constructor that registers the licence and fonts, with `Initialize()` documented as "forces the static constructor to run". Or keep `Lazy` and add a one-line comment you can explain.
- **L3** · `src/Invoicing.App/App.axaml.cs:38-39`: startup runs through `new AsyncRelayCommand(...)` + `Dispatcher.UIThread.Post(() => startup.Execute(null))`. This is a trick to route exceptions to the dispatcher handler. If you keep it, add a comment saying exactly that. Otherwise, call `StartAsync` from an `async` method and handle failure explicitly.
- **L4** · `src/Invoicing.App/Services/DialogService.cs:11,66`: a generic `ShowAsync<TResult>` (only ever used with `bool`), a `Stack<Window>` to track owners, and a local function in `ShowDocumentAsync`. Make it `Task<bool> ShowAsync(Window window)`; the stack can stay with a comment ("the newest open dialog owns the next one").
- **L5** · property patterns: `LettrResponse.cs:10,18` `json?.RootElement is { ValueKind: JsonValueKind.Object } root` and `DocumentDetailViewModel.Commands.cs:15` `Document is TaxInvoice { CanCreateCreditNote: true }`. Use `json is not null && json.RootElement.ValueKind == JsonValueKind.Object` and `Document is TaxInvoice invoice && invoice.CanCreateCreditNote`.
- **L6** · `src/Invoicing.App/ViewModels/DocumentDetailViewModel.cs:141`: a tuple projection `Select((line, index) => (line, index))`. A `for` loop that adds `UiTexts.InvalidLine(index + 1)` is easier to explain.
- **L7** · `src/Invoicing.Core/Email/EmailAvailability.cs:14` and `CreditNote.cs:15`: a ternary wrapped around a `switch` expression inside a constructor call. Write three early-return `if`s. `CreditNote.Restore` sets a private field through an object initializer on `new(...)`; a normal constructor parameter `correctionReason` would be simpler to explain.

## Questions for the author

- **Q1 · Git repository.** `.git/` is an empty directory, so this folder is not a working git repository. AGENTS.md §7 (commit per phase) and §21 (".env is git-ignored; `git status` shows no secrets") cannot be checked. Is this a copy without history? *Recommendation:* run the CODE_REVIEW §6 "Secrets and git hygiene" commands in the real repository before submission.
- **Q2 · Customer of a credit-note draft.** §10.3 says "customer of a draft = current row from `customers`". §14.8 says "a credit note keeps the customer of its original invoice". `DocumentQueries.ReadDocuments` uses the **original invoice's snapshot** for credit-note drafts. Options: (a) keep it (legally correct, matches §14.8), and note it in AGENTS.md Appendix B; (b) follow §10.3 literally. *Recommendation:* (a).
- **Q3 · QR code on cancelled invoices.** `DocumentPdf.cs:43` also hides QR Platba on a **cancelled** NonVatInvoice. §12.3 only says "TaxInvoice and NonVatInvoice". *Recommendation:* keep it, because a stornovaná faktura should not invite payment, and add it to Appendix B.
- **Q4 · Credit notes after a VAT rate change.** A credit note copies the original invoice's rates, but validation accepts only the **currently configured** rates (`VatRateNotAllowed`). After a rate change (as in 2024: 10/15 → 12 %), an older invoice could not be corrected. Options: (a) accept, since the spec says "other line rules as for TaxInvoice"; (b) for credit notes, also allow the rates that appear on the original invoice; (c) document it as a known limitation in the README. *Recommendation:* (c) now, (b) only if time allows. It is a design decision for you.
- **Q5 · Error texts beyond §19.** The app adds sensible Czech messages that are not in AGENTS.md §19: `PDF se nepodařilo uložit.`, `Do vybraného souboru nelze zapisovat.`, `Soubor .env se nepodařilo přečíst.`, `K souboru .env nebo k databázi chybí oprávnění.`, `Databázi se nepodařilo otevřít.`, `Operaci se nepodařilo uložit do databáze. Zkuste to znovu.`, `Potvrzení o odeslání se nepodařilo uložit…`, `Údaje dodavatele byly uloženy.` *Recommendation:* keep them, move them to `Texts/` (M2), and list them in §19 so that spec and code agree.

## Not verified

- **GUI behaviour.** The app was not started (no interactive display used). Please run the manual scenario in AGENTS.md §14.11 and tick each step:
  1. Migrations with demo data → app starts.
  2. Dodavatel: change "Vystavil" → Uložit → message shown.
  3. Adresář: Nový → IČO `27074358` → Načíst z ARES → Uložit.
  4. Doklady: Nový doklad → customer → 3 lines (21 %, 21 % −10 %, 12 %) → Uložit → Vystavit → `YYYY-NNNN`.
  5. Export PDF → Czech characters, VAT summary, QR code (scan it with a banking app).
  6. "Odeslat e-mailem": disabled with the tooltip when there is no key; with a sandbox key the e-mail arrives.
  7. Zaplaceno → Paid; Opravný doklad → reason → Vystavit → `OD-YYYY-NNNN`.
  8. Supplier set to non-payer → new "Faktura" → Vystavit → Stornovat → PDF shows `STORNOVÁNO`.
  Also check: the tooltip on the disabled "Nový doklad" and "Odeslat e-mailem" buttons is actually visible (`ToolTip.ShowOnDisabled`).
- **Git history and secrets** (Q1).
- **`dotnet list package --vulnerable`**: needs NuGet access.
- **The Windows release on Windows**: the zip contents were checked, but `Invoicing.App.exe` was not run on Windows. `docker compose run --rm flyway` on an empty `data/` folder was not re-run (the existing `data/invoicing.db` and the release DB show that it worked before).
- **Lettr and ARES**: not called (by rule); covered only by fake-handler tests.

## Done well

- **Issuing is atomic in memory too.** `DocumentRepository.IssueAsync` issues a *copy* inside the transaction and marks the original as issued only after the commit. A failed issue therefore never leaves a half-issued document in the UI. There is a rollback test and a concurrency test for this. It is better than the §10.4 baseline; explain it proudly at the exam.
- **Correct money core.** `decimal` everywhere, one rounding rule, VAT from the per-rate sums with a § 37 comment, and every known value from §15.1 asserted as a literal.
- **Careful secret handling.** `EmailSettings.ToString()` hides the key, the key is redacted from `Details`, the release script refuses a non-empty key, and the `.env` copy is Debug-only.
- **Thoughtful retry logic in the send dialog.** The idempotency key is reused, and the recipient is locked after an "uncertain" failure, so a retry can never send a different e-mail under the same key. The comment explains why.
- **Clean layering.** Core has no packages and no references; ViewModels only use Core interfaces and services; every view has `x:DataType`; the code-behind is practically empty.
