# CODE_REVIEW.md: code review instructions (Invoicing app, Komponentová tvorba, ČZU)

Instructions for whoever reviews the code of this project: a coding agent or a person. Read the **whole file** before you start. A review answers three questions:

1. Does the code meet **all course requirements**?
2. Is **every error handled**? No hidden errors, and every expected failure reaches the user as a Czech message.
3. Is the code **correct** (against the agreed specification) and does it follow **good practice**, at the level of a **good second-year student**?

---

## 0. How to ask for a review

Example requests:

- `Review phase 2 according to CODE_REVIEW.md.`
- `Review the changes on this branch against main according to CODE_REVIEW.md.`
- `Do the full pre-submission review according to CODE_REVIEW.md.`
- `Fix the Blocker and Major findings from the last review.` (section 15)

A review produces a **report** (section 14). It does not change code unless the user asks for fixes.

---

## 1. Sources of truth

Read these before reviewing:

| Source | What it defines |
|---|---|
| Course requirements (`Komponentova tvorba - Semestral Project requirements.md`; also quoted in `AGENTS.md` §4) | What is graded. Every item must hold. |
| `AGENTS.md` | The agreed specification v1.2 in full, hard rules (§3), size guidelines (§2), tests (§15), final checklist (§21), Czech texts (§19), out of scope (§20). |
| `Invoicing app - agreed specification.md` | The specification agreed with the user (contained in `AGENTS.md`). |
| This file | How to review, rate and report. |

- If `AGENTS.md` and the course requirements disagree, report it as a **Question**. Do not choose yourself.
- The course's sample project (`josefpolach/komponentova-tvorba-ukazkovy-projekt`, "Videopujcovna") is **not** a style reference. It uses Czech identifiers, a static data class and several types per file, which `AGENTS.md` forbids. Never use it to justify a finding or to excuse one.

---

## 2. Reviewer rules

1. **Review, don't rewrite.** The output is a report. Change code only when the user asks (section 15).
2. **Verify every finding** by reading the code in context. A grep hit or a helper-script line is a *candidate*, not a finding.
3. **Name the rule** for every finding: a course requirement, `AGENTS.md §x`, or a named industry practice. Without a rule there is no finding (at most a Question).
4. **Suggest the smallest correct fix at student level**, with a short code sketch when it helps.
5. **Never propose a design change as a fix.** Libraries, architecture, database, project structure, scope and UI flow are agreed. If you think a change is needed, write a **Question**: the problem, 1–3 alternatives with trade-offs, and your recommendation. The user decides.
6. **Don't recommend anything from the out-of-scope list** (`AGENTS.md` §20), not even as "nice to have".
7. **Secrets and external services:** do not open or print `.env`; print no secret values (the commands in section 6 only count or list file names); do not call ARES or Lettr; do not send e-mails.
8. **Say what you could not verify** and why (for example, GUI behaviour without a display, the Windows release without Docker). Do not guess.
9. If an installed library behaves differently from what `AGENTS.md` says (`AGENTS.md` §0.4), report a note, not a defect.
10. **Write for a student** who will defend the code at an oral exam: explain *why* in one or two sentences, be specific and kind.

---

## 3. The level: a good second-year student

The code must be **correct and clean**, but it must look like the work of a good student, not of a senior architect. Review in both directions:

- **Too weak** (always report): wrong results, unhandled errors, broken course rules, unreadable code. "Student level" never excuses these.
- **Too advanced** (report as **Level**): code the author could not explain line by line at the oral exam. Suggest the simpler equivalent.

The test for every non-trivial line: *could a second-year student explain in two sentences what it does and why it is written this way?*

### 3.1 Accept (do not report)

- Plain classes, properties, constructors, `List<T>`, `foreach`, `switch` expressions, `async`/`await`, simple LINQ (`Where`, `Select`, `Sum`, `GroupBy`, `OrderBy`, `Any`, `FirstOrDefault`).
- Objects wired by hand in `CompositionRoot`. No DI container, no logging framework, no EF Core, no mocking library: all by design.
- Hand-written fakes in tests; some repetition in tests when it keeps each test readable; literal expected values.
- Signatures that `AGENTS.md` prescribes, even when they exceed the size guidelines: the `InvoiceLine` constructor (6 parameters), the `SpaydPayment` constructor (5), the `EmailMessage` record (7), `DocumentEmailService.SendAsync` (5).
- `catch (Exception)` and `Console.Error` **only** in the global UI error handler (`AGENTS.md` §14.10).
- `0`, `1` and `100` in a percentage formula, and the `2` in `Math.Round(value, 2, MidpointRounding.AwayFromZero)` (`AGENTS.md` §3).
- The `!` operator where `AGENTS.md` uses it and the value cannot be null (for example `Number!` when a credit note is created from an issued invoice), **with a short comment why**.
- Stateless static helpers (`IcoValidator`, `DocumentNumbering`, `CzechFormat`), `static readonly` values that never change (`TimeSpan`, `Regex`), and the one-time Dapper setting `DefaultTypeMap.MatchNamesWithUnderscores` (`AGENTS.md` §10.1).
- `decimal.Parse`, `DateOnly.ParseExact` and `Enum.Parse` with invariant culture for values read from our own database (the schema guarantees the format).
- Repository methods without a `CancellationToken` (`AGENTS.md` §9.12 signatures).
- `Kč` inside Core (`CzechFormat`): it is a currency symbol (`AGENTS.md` §9.14).
- Either `[ObservableProperty]` style (private field or partial property), as long as one style is used throughout.
- No XML docs on members whose name says everything (warning CS1591 is off on purpose).
- No `ConfigureAwait(false)`, `sealed`, primary constructors, `Span<T>` or other optimisations.

### 3.2 Report as Level (too advanced)

- Anything on the "do not use" list in `AGENTS.md` §2: DI containers, MediatR, AutoMapper, FluentValidation, Polly, Serilog or any logging framework, ReactiveUI, EF Core, generic repositories, `Result<T>` monads, functional pipelines, reflection, expression trees, custom source generators, `dynamic`.
- Patterns where an `if` or a `switch` is enough: strategy, specification, unit of work, CQRS, factories of factories.
- LINQ that needs a whiteboard: `Aggregate`, `Zip`, nested `SelectMany`, chains of more than about four operators, query syntax mixed with method syntax.
- Generic types or methods "for reuse"; interfaces or base classes with one implementation that `AGENTS.md` does not list (the allowed interfaces are the four repositories, `ICompanyRegistry`, `IEmailSender` and `IDialogService`).
- Clever pattern matching (nested property or list patterns), tuples used as data structures, extension methods on framework types, events or delegates where a method call works, `Lazy<T>`, concurrent collections, locks, `IAsyncEnumerable`, `unsafe`, custom attributes.
- Abstractions "for the future" that nothing uses yet.

Example:

```csharp
// Level: correct, but hard to explain at the exam
var rows = lines.GroupBy(l => l.VatRate).OrderByDescending(g => g.Key)
    .Aggregate(new List<VatSummaryRow>(), (acc, g) => { acc.Add(new(g.Key, g.Sum(x => x.CalculateBase()), CalculateVat(g.Sum(x => x.CalculateBase()), g.Key))); return acc; });

// Student level: same result, easy to read and explain
var rows = new List<VatSummaryRow>();
foreach (var group in lines.GroupBy(line => line.VatRate).OrderByDescending(group => group.Key))
{
    decimal taxBase = group.Sum(line => line.CalculateBase());
    rows.Add(new VatSummaryRow(group.Key, taxBase, CalculateVat(taxBase, group.Key)));
}
```

---

## 4. Severity and verdict

| Severity | IDs | Meaning | Examples |
|---|---|---|---|
| **Blocker** | B1, B2, … | Clearly fails a course requirement or a hard rule (`AGENTS.md` §3); wrong money, VAT, number or legal content; a secret leak; a hidden error; a build warning; a failing test | empty `catch`; `double` for money; VAT rounded per line; API key in an exception message; `dotnet test` red |
| **Major** | M1, … | Deviates from the agreed spec; an expected failure without handling (section 8.2); a required test is missing (`AGENTS.md` §15.1); a layer violation; one method clearly over the hard limit (~25 lines); a god object | ARES timeout not mapped; SQL in a ViewModel; numbering outside the issue transaction |
| **Minor** | m1, … | Readability, naming, comments, small duplication, a method of 16–25 lines, nesting deeper than 2, more than 4 parameters (where not prescribed) | `Calc()`; a comment that repeats the code; the same format string in two places |
| **Level** | L1, … | Correct but too advanced (section 3.2) | `Aggregate` in `VatSummary` |
| **Question** | Q1, … | Ambiguous, or would need a design change. The user decides | a gap in the spec; a contradiction between `AGENTS.md` and the course rules |

An isolated, borderline violation of a course requirement (one 27-line method) is Major; a clear or repeated one is Blocker.

**Verdict**

- **Ready to submit:** no Blocker, no Major, every Level finding fixed or consciously accepted by the author, all automated checks green.
- **Ready after fixes:** no Blocker; a few local Majors.
- **Not ready:** at least one Blocker.

---

## 5. Review modes and scope

| Mode | When | Scope |
|---|---|---|
| Phase review | at the end of every phase (`AGENTS.md` §7) | files added or changed in the phase, the phase's "Done when" condition and the focus sections below; automated checks (section 6) on the whole solution |
| Change review | a branch or pull request | `git diff --stat main...HEAD`, then every changed file **in full** (not only the changed lines) and the callers of changed public members |
| Full review | phase 9, before submission | sections 6–13 completely and every item of `AGENTS.md` §21 |

Focus per phase:

| Phase | Focus |
|---|---|
| 0 Setup | 6 (build, format, git hygiene, secrets), 10 (project references, packages), Compose and migrations against `AGENTS.md` §8.7–8.8 |
| 1 Core | 7, 8, 9 (money, VAT, documents, validation, numbering, IBAN, SPAYD, configuration, formatting), 11 |
| 2 Data | 8 (database failures, transactions), 9 (mapping, issuing, snapshots), 10, 11 |
| 3 ARES | 8 (lookup failures), 9 (mapping), 11 |
| 4 PDF | 9 (PDF content), 11 |
| 5 E-mail | 8 (Lettr failures, secrets), 9 (templates, request), 11 |
| 6–7 App | 8 (startup, global handler, UI messages), 10 (layers), 12 |
| 8 Release, README | 13 |
| 9 Final | everything |

---

## 6. Step 1: automated checks

Run from the repository root (bash on Linux). In the commands below, `command grep` makes sure the system GNU grep runs even when `grep` is an alias or a shell function. On macOS, use GNU grep or adapt the patterns.

**Build, format, tests, packages**

```bash
dotnet build Invoicing.sln                         # must end with 0 Warning(s), 0 Error(s)
dotnet format Invoicing.sln --verify-no-changes    # must report nothing
dotnet test Invoicing.sln                          # all pass, none skipped; note the test count
dotnet list Invoicing.sln package --vulnerable     # informational; needs access to NuGet
command grep -hoE 'Include="Avalonia[^"]*" Version="[^"]+"' src/*/*.csproj \
  | sed -E 's/.*Version="([^"]+)"/\1/' | sort -u    # must print exactly one version
command grep -rnE --include='*.csproj' 'Version="[^"]*\*' .   # floating versions: must print nothing
command grep -n 'image:' docker-compose.yml        # exact Flyway tag, never "latest"
```

Also compare `Directory.Build.props` and `.editorconfig` with `AGENTS.md` §8.3–8.4.

**Secrets and git hygiene** (these commands never print a secret value)

```bash
git check-ignore -q .env && echo ".env is ignored"            # must print the message
git ls-files | command grep -E '(^|/)\.env$|\.db$|/(bin|obj)/|^\.idea/'   # must print nothing
git log --all --format=%h -- .env                              # must print nothing (never committed)
git grep -lE 'lttr_[A-Za-z0-9_]{10,}' $(git rev-list --all)    # must print nothing (prints commit:file only)
```

**Release** (from phase 8)

```bash
unzip -p artifacts/release/Invoicing-win-x64.zip '*/.env' | command grep -cE '^LETTR_API_KEY=.+'   # must print 0
unzip -l artifacts/release/Invoicing-win-x64.zip | command grep -E 'invoicing\.db|Invoicing\.App\.exe'  # both listed
```

**Code checks.** Save Appendix A as `/tmp/review_checks.sh` and Appendix B as `/tmp/review_metrics.py` (outside the repository; never commit them), then:

```bash
bash /tmp/review_checks.sh                 # grep candidates, numbered sections 1–20
python3 /tmp/review_metrics.py src         # methods > 15 lines, > 4 parameters, nesting > 2, empty catch
python3 /tmp/review_metrics.py tests 25    # test methods: only clearly long ones
find src tests -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -print0 \
  | xargs -0 wc -l | sort -n | tail -15    # files over ~200 lines: check their responsibilities
```

**Layers and views**

```bash
command grep -n 'Reference Include' src/*/*.csproj tests/*/*.csproj   # compare with AGENTS.md §6.1
command grep -rnE 'Sqlite|Dapper|HttpClient|QuestPDF|QRCoder|SELECT |INSERT |UPDATE |DELETE ' \
  src/Invoicing.App/ViewModels                                          # must print nothing
command grep -rn 'Avalonia' src/Invoicing.App/ViewModels                # check every hit: no windows or controls
for f in src/Invoicing.App/Views/*.axaml; do command grep -q 'x:DataType' "$f" || echo "missing x:DataType: $f"; done
wc -l src/Invoicing.App/Views/*.axaml.cs                                # code-behind: InitializeComponent only
```

**Required tests and known values** (`AGENTS.md` §15.1)

```bash
for c in InvoiceLineTests VatSummaryTests TaxInvoiceValidationTests NonVatInvoiceValidationTests \
         CreditNoteValidationTests SupplierTests CustomerTests DocumentLifecycleTests DocumentNumberingTests \
         SpaydPaymentTests CzechBankAccountTests IcoValidatorTests EnvFileParserTests AppSettingsTests \
         EmailAvailabilityTests CzechFormatTests CzechNumberParserTests AresResponseMapperTests \
         AresCompanyRegistryTests EmailTemplateRendererTests EmailTemplateStoreTests DocumentEmailComposerTests \
         LettrEmailSenderTests DocumentEmailServiceTests DocumentPdfGeneratorTests; do
  command grep -rqE "class $c\b" tests || echo "missing test class: $c"
done
for v in 89.99 100.01 -25.03 2450.45 514.59 557.79 3368.24 CZ6508000000192000145399 \
         CZ7908000000002000145399 CZ1801000000000123456789 CZ6020100000002400123456 \
         OD-2026-0012 20260001 'FAKTURA C. 2026-0001' 27074358; do
  command grep -rqF -- "$v" tests || echo "known value not found in tests: $v"
done
```

A missing known value means: open the related test and check by hand (it may be written differently, e.g. `100.01m`). Also check that the `Data/` tests exist (`AGENTS.md` §15.1, "Data").

---

## 7. Step 2: course requirements

Fill in a status for every row in the report (section 14).

| # | Requirement (CZ) | What to check | Typical findings |
|---|---|---|---|
| 1 | aplikace má grafické rozhraní | Avalonia app, three tabs, modal detail and send windows | – |
| 2 | konvence pro pojmenovávání v .NET | Clean build with the naming rules; `Async` suffix (check 9), `I` prefix, PascalCase constants, `_camelCase` private fields, `camelCase` locals; test names `Method_Condition_Expected` | `GetAll()` returning `Task`; a public field `Name` |
| 3 | srozumitelné a popisné pojmenování | Names say *what*, in English; only the domain abbreviations from `AGENTS.md` Appendix A (`Ico`, `Dic`); booleans read as questions (`IsEditable`, `CanCancel`) | `Calc`, `tmp`, `data`, `DoWork`, `Helper`, `Manager`, Czech identifiers |
| 4 | kód je přehledně formátován | `dotnet format --verify-no-changes`; file-scoped namespaces; one type per file (check 20); file name equals type name | two records in one file |
| 5 | kód neobsahuje dlouhé metody | `review_metrics.py`; aim ≤ 15 lines, hard limit ~25 (`AGENTS.md` §2) | a 40-line `IssueAsync` in a ViewModel |
| 6 | projekt neobsahuje "mrtvý kód" | Build (IDE0005, IDE0051, IDE0052); checks 15–16; public members nobody calls (Rider's solution-wide analysis or "Find Usages"); public members used only by tests; unused NuGet packages; template leftovers | `Class1.cs`, the greeting property, a `ViewLocator` |
| 7 | každá metoda provádí jen jednu činnost | Method names with "And"; bodies with separate blocks (load / validate / save); `bool` parameters that switch behaviour; methods that both calculate and show UI | `LoadAndValidateAndSave()`; `Save(document, bool issue)` |
| 8 | kód je dobře a smysluplně komentován | `/// <summary>` on public classes and non-obvious public members; "why" comments at legal rules (§ 28, § 29, § 37, § 45), rounding, SPAYD clean-up, IBAN mod 97, every `!` | VAT "zdola" with no comment |
| 9 | kód neobsahuje zastaralé komentáře | Compare every comment with the code beside it | "14 days" above a constant of 15; a parameter that no longer exists |
| 10 | kód neobsahuje zbytečné komentáře | Comments that repeat the code; XML docs that only restate the name; banner or region comments; commented-out code (check 16) | `// save customer` above `SaveCustomer()` |
| 11 | data a operace v jedné třídě | Logic lives with its data: `InvoiceLine.CalculateBase`, `Document.Validate` and the lifecycle methods, `Customer.Validate`, `CzechBankAccount.ToIban` | a ViewModel computing a line base; `DocumentHelper.CalculateTotal(document)` |
| 12 | nekonstantní datové atributy privátní | Check 2; `protected` fields in base classes; public setters that let callers break invariants (`Status`, `Number`, `Supplier` must have private setters, `AGENTS.md` §9.4) | `public DocumentStatus Status { get; set; }` |
| 13 | dědičnost = vztah "je" | Only `Document` → `TaxInvoice` / `NonVatInvoice` / `CreditNote` and `Party` → `Customer` / `Supplier`; framework base classes (`ObservableObject`, `Window`, `Application`, `Exception`) are fine | `CustomerRepository : BaseRepository` for code reuse; a subclass that throws `NotSupportedException` for a base member |
| 14 | kód je smysluplně pokryt jednotkovými testy | Section 11 | one test for all validation rules together |
| 15 | všechny jednotkové testy úspěšně projdou | `dotnet test` green, no skipped tests | – |
| 16 | aplikace je rozdělena do vrstev | Section 10 | SQL in a ViewModel |
| 17 | žádné magické konstanty | Check 17; meaningful named `const` / `static readonly`; date formats, header names, content types, regexes, timeouts, setting keys (`SettingKeys`), Czech texts (`Texts/` classes); a repeated literal is always a finding | `days > 15`; `"yyyy-MM-dd"` in three mappers |
| 18 | žádné božské objekty | Files over ~200 lines; classes with more than ~5 dependencies; `Manager` / `Utils` classes doing unrelated jobs; `MainWindowViewModel` only holds the three tab ViewModels (`AGENTS.md` §14.4); check `DocumentDetailViewModel` especially | a 600-line detail ViewModel |
| 19 | nevyužívá se antivzor "skrývání chyb" | Section 8 | `catch { return null; }` |
| 20 | + industry standards | Sections 8 and 10: nullable on, warnings as errors, async I/O, parameterised SQL, one reused `HttpClient`, disposal, culture-independent formatting, no secrets in git, pinned versions | `$"... WHERE id = {id}"` |

---

## 8. Step 3: error handling

### 8.1 Rules

- **No empty `catch`**, and no `catch` that only contains a comment. No `catch (Exception)` or bare `catch` except in the global UI error handler (`AGENTS.md` §3.5, §14.10).
- **Catch only exceptions you expect at that point**, and turn each into a code (`CompanyLookupError`, `EmailSendError`, `DatabaseStatus`, `ConfigurationProblem`) or a Czech message. Never return `null` or `false` from a `catch` without carrying the reason.
- **Rethrow with `throw;`**, never `throw ex;` (it loses the stack trace; check 19).
- **Expected user mistakes are not exceptions.** Validation returns `ValidationMessage`s; number input uses `CzechNumberParser.TryParseDecimal`; account input uses `CzechBankAccount.TryParse`.
- **Programming-error guards** (editing an issued document, a forbidden status change, a PDF of a draft) throw `InvalidOperationException` or `ArgumentException` with an English message, and the UI makes them unreachable by disabling buttons.
- **Every error code has a Czech text** in an exhaustive `switch` expression whose default arm throws `ArgumentOutOfRangeException` (`AGENTS.md` §19.3).
- **Async:** no `async void` (use commands), no `.Result` or `.Wait()`, no fire-and-forget (`_ = SaveAsync();`) (check 8). Async `[RelayCommand]`s keep the default options: exceptions then reach the global handler, and the command is disabled while it runs (which also stops double-clicks). `FlowExceptionsToTaskScheduler = true` hides exceptions: **Blocker**.
- **Timeout or cancellation:** a `TaskCanceledException` is a timeout only when the caller's token was not cancelled (`AGENTS.md` §11, step 4).
- **Disposal:** `await using` / `using` for connections, transactions, HTTP responses and streams. One `HttpClient` for the whole app run, never created or disposed per request (check 13).
- **Transactions:** commit is the last step; an exception before it rolls back. After a failed database operation the UI reloads the document from the database (`AGENTS.md` §10.4, §14.8).
- **Messages:** Czech for the user (from `Texts/` classes), English for developers (exception messages). User messages show no stack traces or exception type names, except `{exception.Message}` in the global handler.
- **Secrets** never appear in exception messages, `Details`, `ToString()`, console output or UI (check 18).

### 8.2 Inventory of expected failures

Report the status of every row: ✅ handled, ❌ not handled, ⚠️ partly, – not built yet.

| # | Failure | Required handling | User sees | Spec / tests |
|---|---|---|---|---|
| E1 | `.env` has a malformed line | `EnvFileFormatException(lineNumber)` → error window → app ends | `Soubor .env obsahuje neplatný řádek {n}.` | §9.9, §19.4; `EnvFileParserTests` |
| E2 | A required setting is missing or invalid | `SettingsLoadResult` errors → error window listing every key → app ends; no fallback values in code | §19.4 messages | §9.9; `AppSettingsTests` |
| E3 | An e-mail setting is missing or invalid | App runs; e-mail disabled; status line names the key; button tooltip explains | §19.6 | §9.9–9.10; `AppSettingsTests`, `EmailAvailabilityTests` |
| E4 | Database file missing, not migrated, or old schema | `DatabaseVersionChecker` → error window with the `docker compose run --rm flyway` instruction; the connection uses `Mode = ReadWrite`, so a missing file is never created | §19.4 | §10.1, §10.5; `Data/` tests |
| E5 | SQL fails at run time (locked file, constraint) | Transaction rolls back; exception reaches the global handler; the detail window reloads the document | `Nastala neočekávaná chyba: …` | §10.4, §14.8, §14.10 |
| E6 | Issuing a document with validation errors | No exception: messages returned, nothing issued, repository not called; warnings shown after a successful issue | §19.3 | §9.13 |
| E7 | Forbidden status change or edit of an issued document | `InvalidOperationException` guard; the buttons are disabled | – | §9.4.1; `DocumentLifecycleTests` |
| E8 | Invalid number typed in the lines grid | `TryParse` → `Neplatné číslo` at that cell; no exception; totals ignore the bad value | §19.7 | §14.8; `CzechNumberParserTests` |
| E9 | Invalid bank account on the supplier form | IBAN field shows `Neplatné číslo účtu`; validation message; saving still allowed | §19.3, §19.7 | §14.5 |
| E10 | ARES: invalid IČO, 404, other status, timeout, no network, bad JSON | `CompanyLookupResult` failure codes; an invalid IČO sends no request; only `TaskCanceledException`, `HttpRequestException` and `JsonException` are caught | §19.5 | §11; `AresCompanyRegistryTests` |
| E11 | Lettr: 400, 401, 403, 409, 422, 429, 5xx, other status, timeout, no network, 200 without `request_id` | `EmailSendResult` failure; the dialog stays open with its data; a retry reuses the idempotency key; no `document_emails` row | §19.6 | §13.4; `LettrEmailSenderTests`, `DocumentEmailServiceTests` |
| E12 | Recipient empty or invalid | Inline message; `Odeslat` disabled | `Zadejte platnou e-mailovou adresu.` | §14.9 |
| E13 | Double-click on `Odeslat` | Disabled while sending; progress bar shown | – | §14.9 |
| E14 | Template placeholder without a value; a template or font resource missing | `TemplateRenderException` / `InvalidOperationException` (programming errors); tests prove every template renders and the PDFs build | – | §12.1, §13.2; template and PDF tests |
| E15 | PDF requested for a draft | `InvalidOperationException`; `Export PDF` disabled for drafts | – | §12.2 |
| E16 | Total above the QR Platba limit | No exception: the PDF shows the Czech note instead of the QR code | `QR Platbu nelze vytvořit, částka je příliš vysoká.` | §9.8, §12.3 |
| E17 | User cancels the save-file picker | Nothing happens, no message | – | §14.8 |
| E18 | The PDF file cannot be written | At minimum it reaches the user through the global handler. A dedicated Czech message is not in `AGENTS.md`: if you think it is needed, raise a Question, not a finding | `Nastala neočekávaná chyba: …` | §14.10 |
| E19 | New document without a supplier; draft saved without a customer | Buttons disabled (with tooltip for "Nový doklad"); the service or repository throws a guard | `Nejdříve vyplňte údaje dodavatele.` | §9.13, §10.4, §14.7 |
| E20 | Any other unexpected exception | Global handler: message window, full exception to `Console.Error`, `Handled = true`; the only `catch (Exception)` | `Nastala neočekávaná chyba: {message}` | §14.10 |

---

## 9. Step 4: correctness against the specification

These are the points that are most often wrong. A wrong point that changes money, a document number or legal content is a **Blocker**; any other deviation is **Major**. Section numbers refer to `AGENTS.md`.

**Money and VAT (§9.2–9.3)**
- `decimal` for every amount, rate, quantity and percentage (check 6).
- `Math.Round(x, 2, MidpointRounding.AwayFromZero)` everywhere, including negative values (check 7).
- Line base = `round(quantity × unitPrice × (1 − discount/100), 2)`, rounded once at the end.
- VAT per rate is calculated from the **sum** of the line bases for that rate, never per line; rows ordered from the highest rate; total = bases + VAT; no rounding line.
- `NonVatInvoice` total = sum of bases; VAT shown nowhere; rate stored as 0.

**Documents and lifecycle (§9.4–9.4.1)**
- Setters on issued documents throw (`EnsureEditable`); `Status`, `Number` and `Supplier` have private setters.
- `Issue` takes snapshots with `CreateSnapshot()` (new objects, not shared references).
- `CanMarkAsPaid`, `CanCancel` and `IsOverdue` follow the subclass table; a credit note is never overdue and never paid.
- `CreateCreditNote`: new draft, negated quantities, `DocumentReference(Id, Number)`, issue date and DUZP = today, due date = today + days, empty reason.
- New document type from `Supplier.IsVatPayer`; due date = issue date + `DefaultDueDays`; changing the issue date later does not move the due date.

**Validation (§9.5)**
- Every rule produces exactly the listed code, target and (for lines) 1-based position.
- 15-day warning only when the issue date is **more than** 15 days after DUZP (15 → nothing, 16 → warning), only on `TaxInvoice`, severity Warning, and it does not block issuing.
- `NonVatInvoice`: no DUZP, VAT rate not checked, `MustNotBeVatPayer`. `CreditNote`: quantity ≠ 0, reason and original invoice required, no 15-day warning.
- IČO checksum `(11 − sum % 11) % 10`; DIČ `^CZ\d{8,10}$`; e-mail via `MailAddress.TryCreate` and comparison with the trimmed input.

**Numbering (§9.6, §10.4)**
- A number is assigned only in `IssueAsync`, in **one transaction** with the save; the year comes from the **issue date**; both invoice kinds share one series, credit notes use `OD-`; yearly reset; four-digit padding; variable symbol = digits only, at most 10.

**IBAN and QR Platba (§9.7–9.8)**
- Account regex; zero padding (prefix 6, number 10); mod 97 with `BigInteger` or piece by piece.
- SPAYD: exact field order and format; `AM` invariant culture with two decimals; MSG cleaned in the right order (remove diacritics → upper case → drop `*` and non-ASCII → trim → cut to 60); limit violations throw `ArgumentException`; QR code only on invoices.

**Configuration (§9.9)**
- `.env` parser splits at the first `=`, removes one pair of quotes, ignores comments, reports line numbers. A defined environment variable (even an empty one) overrides the file. No fallback values. `EmailSettings` is a class and its `ToString()` hides the key.

**Data (§10)**
- Connection with `Mode = ReadWrite` and `ForeignKeys = true`; one connection per method; only `@Parameters` (check 12).
- Money, dates and enums converted explicitly with invariant culture through row classes and mappers, never implicitly by Dapper.
- Drafts read the current customer; issued documents read the snapshot columns; the supplier snapshot is null for drafts.
- `SaveDraftAsync` replaces lines with positions 1..n inside a transaction; `DeleteDraftAsync` has `AND status = 'Draft'`.
- Version check order: file → `flyway_schema_history` table → version 5 with `success = 1`.

**ARES (§11)**
- Absolute URL built as a string (no `BaseAddress`); `Accept: application/json`; street with the letter suffix (`778/3a`); village fallback (`nazevCastiObce`); the `textovaAdresa` fallback leaves city and postal code empty.

**PDF (§12)**
- Licence and fonts registered once; `ThrowOnMissingTextGlyphs` stays on; correct titles; Dodavatel and Odběratel blocks; DIČ only on tax documents; register entry; `Vystavil`; page numbers; `Nejsem plátce DPH.`; the credit-note section; `STORNOVÁNO`; ASCII file names.

**E-mail (§13)**
- Templates are embedded resources with the exact texts; the renderer throws for a missing value and HTML-encodes values only in the HTML body; credit notes do not use `VariableSymbol`; `Invoicing.Email` does not reference `Invoicing.Pdf`.
- Lettr request: absolute `{base}/emails`, all headers including `Idempotency-Key`, snake_case JSON, base64 without line breaks, the status mapping table.
- One GUID per send dialog, reused for every retry; a successful send stores one `document_emails` row with `sent_at` in ISO 8601 with offset.

**Culture (§3, §9.14, §14.1)**
- Persistence, SPAYD and configuration use invariant culture. UI, PDF and e-mail use `CzechFormat` (its own `NumberFormatInfo`, U+00A0 separators). Startup sets `cs-CZ` for the UI threads. No `ToString()` or `Parse` on numbers or dates without an explicit culture or format provider.

---

## 10. Step 5: architecture and layers

- Project references exactly as in `AGENTS.md` §6.1: Core references nothing and has no packages; Email does not reference Pdf; Tests do not reference App.
- NuGet packages per project as in `AGENTS.md` §6.1; nothing extra (an unused package is dead code).
- ViewModels contain no SQL, HTTP or QuestPDF code and know no Views, windows or controls; dialogs go through `IDialogService`. Calling services and `DocumentPdfGenerator.Generate` from a ViewModel is fine.
- Views are XAML plus `InitializeComponent()`; every view and data template sets `x:DataType`.
- `CompositionRoot` is the only place that creates infrastructure objects (`HttpClient`, repositories, senders), with one small method per area.
- Core defines the interfaces; infrastructure implements them; no infrastructure types appear in Core signatures.
- No static mutable state (check 3).
- Nothing from the out-of-scope list (`AGENTS.md` §20).

---

## 11. Step 6: tests

- Every item of `AGENTS.md` §15.1 has a test; every validation rule has its own case that asserts code, target and (for lines) position.
- Names `Method_Condition_Expected`; Arrange / Act / Assert; one behaviour per test; `[Theory]` with `[InlineData]` for value tables.
- Every test asserts something specific: no tests without an assertion, no `Assert.True(true)`, no `Assert.NotNull` where the value can be compared.
- Expected values are literals (from §15.1), not recomputed with the production formula.
- No network, no real e-mail, no `Thread.Sleep`; no dependence on today's date (fixed `DateOnly` values are passed in) or on the machine's culture; a test that changes the culture restores it in `finally`.
- Fakes are small and hand-written; `FakeHttpMessageHandler` reads the request body inside `SendAsync`.
- Data tests use a temporary SQLite file, run the copied `V*.sql` files in version order and clean up after `SqliteConnection.ClearAllPools()`.
- Tests use the public API only (no reflection into private members).
- No skipped tests; no tests of the App project.

---

## 12. Step 7: UI (Avalonia, MVVM)

- Texts match `AGENTS.md` §19 exactly, including diacritics. Czech literals in C# live only in the `Texts/` classes. XAML labels may be literals or `x:Static` references to `UiTexts` (`AGENTS.md` §19 prefers `Texts/`); accept either if it is consistent.
- Buttons are enabled exactly as in the `AGENTS.md` §14.8 table; `CanExecute` plus `NotifyCanExecuteChanged()` whenever a condition changes.
- The e-mail button is always visible (no `IsVisible` binding), disabled with a tooltip (`ToolTip.ShowOnDisabled="True"`), with the envelope icon drawn as a `Path`.
- Confirmation dialogs for Vystavit, Smazat and Stornovat with the exact texts.
- The document list reloads after a detail window closes; the detail reloads after a database error.
- ViewModels inherit `ObservableObject`; one `[ObservableProperty]` style throughout.
- Manual scenario `AGENTS.md` §14.11: if you cannot run the GUI, list the steps for the user to check and mark them "not verified".

---

## 13. Step 8: deliverables (phase 8 and 9)

- `README.md` in English with all 14 sections of `AGENTS.md` §18; commands work on CachyOS; no secrets.
- `docs/class-diagram.md` and `docs/activity-diagram.md` exist, are valid Mermaid and match the code (class names, inheritance); the activity diagram contains the decision "E-mail nastaven?".
- `scripts/build-windows-release.sh` uses `set -euo pipefail` and fails when the release `.env` has a non-empty `LETTR_API_KEY`; the zip contents pass the release checks in section 6.
- Demo data: `INSERT OR IGNORE`, fixed ids, fictional companies, `@example.com` addresses, IČOs with a valid check digit, one draft, one overdue issued invoice and its `number_series` row (`AGENTS.md` §16).
- Migrations match `AGENTS.md` §8.8 exactly; `docker-compose.yml` matches §8.7 (pinned image, `user:`, `demo` profile).
- `.env.example` is committed with the keys from §8.6 and an empty `LETTR_API_KEY`.

---

## 14. Report format

Return the report in the conversation, in English. Do not commit review reports into the repository unless the user asks.

````markdown
# Code review: <phase / branch / full> (<date>)

## Verdict
Ready to submit / Ready after fixes / Not ready. One or two sentences why.
Findings: <n> Blocker, <n> Major, <n> Minor, <n> Level, <n> Questions.

## Automated checks
| Check | Result | Notes |
|---|---|---|
| dotnet build (0 warnings) | ✅ / ❌ | |
| dotnet format --verify-no-changes | | |
| dotnet test | ✅ 152 passed / ❌ 2 failed | |
| Secrets and git hygiene | | |
| Grep checks (Appendix A) | | 23 candidates, 4 confirmed |
| Method metrics (Appendix B) | | |
| Required tests and known values | | |

## Course requirements
| # | Requirement | Status | Evidence / findings |
|---|---|---|---|
| 1 | aplikace má grafické rozhraní | ✅ | |
(all 20 rows from section 7)

## Error handling
| # | Failure | Status | Findings |
|---|---|---|---|
| E1 | .env malformed line | ✅ | |
(all rows from section 8.2)

## Specification
| Area | Status | Findings |
|---|---|---|
| Money and VAT | ✅ | |
(the areas of section 9 that are in scope)

## Findings

### B1 · Blocker · `src/Invoicing.Core/Documents/VatSummary.cs:31`
**Rule:** AGENTS.md §3.2 (rounding), §9.3
**Problem:** VAT is rounded with `Math.Round(value, 2)`, which uses banker's rounding: a base of 2.50 at 21 % gives 0.52 instead of 0.53.
**Why it matters:** wrong VAT on a tax document; the VAT summary test with the midpoint value fails.
**Fix:**
```csharp
decimal vat = Math.Round(taxBase * rate / 100, 2, MidpointRounding.AwayFromZero);
```

(then M…, m…, in this order)

## Too advanced for the level
L1 · `file:line` · what it is, and the simpler version.

## Questions for the author
Q1 · the problem, 1–3 alternatives with trade-offs, recommendation.

## Not verified
What could not be checked and why; manual steps for the user.

## Done well
Up to five short points, so the author knows what to keep.
````

---

## 15. When asked to fix findings

1. Fix in this order: Blocker → Major → Minor and Level. Questions only after the user has decided.
2. Keep the design. Change only what the finding needs; no refactoring on the side.
3. For a Blocker or Major bug, add or fix a test that would have caught it.
4. One logical fix per commit, imperative English message (e.g. `Fix VAT rounding for midpoint values`), plus any attribution lines the environment requires.
5. After each batch: build (0 warnings), `dotnet format --verify-no-changes`, `dotnet test`; then re-run the checks of the affected sections.
6. Finish with a short table: finding → fixed / not fixed (why) / needs the user.

---

## Appendix A: grep checks (`/tmp/review_checks.sh`)

```bash
cat > /tmp/review_checks.sh <<'EOF'
#!/usr/bin/env bash
# Grep checks for the code review. Run from the repository root. Every hit is a candidate, not a finding.
g() { command grep --exclude-dir=obj --exclude-dir=bin "$@"; }
section() { printf '\n=== %s\n' "$1"; }

section "1 Broad or bare catch (allowed only in the global UI error handler)"
g -rnE --include='*.cs' 'catch\s*(\(\s*(System\.)?Exception\b[^)]*\))?\s*(\{.*)?$' src
section "2 Non-private fields (consts are fine)"
g -rnE --include='*.cs' '^\s*(public|internal|protected)( static)?( readonly)? [A-Za-z0-9_<>?,]+ [A-Za-z_][A-Za-z0-9_]*\s*(=[^>]|;)' src
section "3 Static fields and settable static properties (mutable global state?)"
g -rnE --include='*.cs' '\bstatic ([A-Za-z0-9_<>?,]+ )+[A-Za-z_][A-Za-z0-9_]*\s*(=[^>]|;|\{\s*get;\s*(private\s+)?set;)' src
section "4 Null-forgiving operator ! (needs a comment why)"
g -rnE --include='*.cs' '[A-Za-z0-9_)>]!([.;,)[]|$)' src
section "5 Warning suppression"
g -rnE --include='*.cs' --include='*.csproj' --include='*.props' '#pragma|SuppressMessage|NoWarn' src tests Directory.Build.props
section "6 Money as double/float"
g -rnwE --include='*.cs' 'double|float|Double|Single' src
section "7 Rounding without AwayFromZero"
g -rn --include='*.cs' 'Math\.Round' src | g -v 'AwayFromZero'
section "8 Blocking or fire-and-forget async"
g -rnE --include='*.cs' '\.Result\b|\.Wait\(|GetAwaiter\(\)\.GetResult\(|async void|FlowExceptionsToTaskScheduler' src
g -rnE --include='*.cs' '^\s*_ = [A-Za-z_.]+Async\(' src
section "9 Task-returning methods without the Async suffix"
g -rnE --include='*.cs' '\bTask(<.*>)? [A-Za-z_]+\(' src | g -v 'Async('
section "10 Current time read inside Core (today must be passed in)"
g -rnE --include='*.cs' 'DateTime(Offset)?\.(Now|Today|UtcNow)' src/Invoicing.Core
section "11 Czech text in Core or in comments (Core returns codes; comments are English)"
g -rnE --include='*.cs' '[áčďéěíňóřšťúůýžÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ]' src/Invoicing.Core
g -rnE --include='*.cs' '//.*[áčďéěíňóřšťúůýžÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ]' src tests
section "12 Interpolated strings in Data (SQL must use @Parameters)"
g -rnE --include='*.cs' '\$@?"|@\$"' src/Invoicing.Data
section "13 HttpClient created outside the composition root"
g -rn --include='*.cs' 'new HttpClient' src
section "14 Console output (only Console.Error in the global error handler)"
g -rn --include='*.cs' 'Console\.' src
section "15 Leftovers: TODO, template samples, skipped tests"
g -rnE --include='*.cs' --include='*.axaml' 'TODO|FIXME|HACK|Welcome to Avalonia|Greeting|Class1|UnitTest1|Skip *=' src tests
section "16 Commented-out code"
g -rnE --include='*.cs' '^\s*//[^/].*[;{}]\s*$' src tests
section "17 Numbers that may be magic constants (0 and 1 are not listed; const lines are skipped)"
g -rnE --include='*.cs' '(^|[^A-Za-z_0-9."])([2-9]|[0-9]{2,}|[0-9]\.[0-9]+)[mM]?\b' src | g -vE '\bconst\b|^[^:]+:[0-9]+:\s*//'
section "18 Every place that uses the Lettr API key (it must never reach a log, message, ToString or record)"
g -rn --include='*.cs' 'ApiKey' src
section "19 Rethrow that loses the stack trace (use throw;)"
g -rnE --include='*.cs' 'throw [a-z][A-Za-z0-9_]*;' src
section "20 More than one type per file"
find src tests -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' | while read -r file; do
    count=$(g -cE '^(public |internal )?((abstract|static|sealed|partial) )*(class|record|enum|interface|struct) ' "$file")
    if [ "$count" -gt 1 ]; then echo "$file: $count types"; fi
done
true
EOF
```

Expected hits that are **not** findings: the global error handler in checks 1 and 14; `Kč` in `CzechFormat` in check 11; the `2` of the money rounding and `100` in percentage formulas in check 17; the `!` uses described in section 3.1 (with their comment) in check 4.

## Appendix B: method metrics (`/tmp/review_metrics.py`)

Heuristic. It relies on the brace style enforced by `.editorconfig` (braces on their own lines). Expression-bodied members, interface members and records are skipped.

```bash
cat > /tmp/review_metrics.py <<'EOF'
"""Reviewer helper: long methods, deep nesting, many parameters, empty catch blocks.
Heuristic. It relies on the brace style enforced by .editorconfig (braces on their own lines).
Usage: python3 review_metrics.py <folder> [max_method_lines]"""
import pathlib
import re
import sys

MAX_LINES = int(sys.argv[2]) if len(sys.argv) > 2 else 15
MAX_PARAMETERS = 4
SIGNATURE = re.compile(r"^(\s*)(public|private|protected|internal|static|override|async)\b.*\(")
CONTROL = re.compile(r"^\s*(if|else|for|foreach|while|do|switch|try|catch|using)\b")


def indent(line):
    return len(line) - len(line.lstrip(" "))


def find_block_end(lines, open_index):
    for index in range(open_index + 1, len(lines)):
        if lines[index].rstrip() == " " * indent(lines[open_index]) + "}":
            return index
    return open_index


def check_method(path, lines, start):
    signature_end = start
    while ")" not in lines[signature_end] and signature_end + 1 < len(lines):
        signature_end += 1
    open_index = signature_end + 1
    if open_index < len(lines) and lines[open_index].strip().startswith(":"):
        open_index += 1  # constructor initializer ": base(...)" on its own line
    if open_index >= len(lines) or lines[open_index].strip() != "{":
        return
    close_index = find_block_end(lines, open_index)
    body = lines[open_index + 1:close_index]
    signature = " ".join(line.strip() for line in lines[start:signature_end + 1])
    parameters = signature[signature.find("(") + 1:signature.find(")")]
    while "<" in parameters:
        parameters = re.sub(r"<[^<>]*>", "", parameters)  # Func<int, int> counts as one parameter
    parameter_count = len([p for p in parameters.split(",") if p.strip()])
    code_lines = len([line for line in body if line.strip()])
    deep = [line for line in body if CONTROL.match(line) and indent(line) >= indent(lines[open_index]) + 12]
    name = signature.split("(")[0].split()[-1]
    if code_lines > MAX_LINES:
        print(f"{path}:{start + 1}: {name}: {code_lines} lines")
    if parameter_count > MAX_PARAMETERS:
        print(f"{path}:{start + 1}: {name}: {parameter_count} parameters")
    if deep:
        print(f"{path}:{start + 1}: {name}: nesting deeper than 2 levels")


def check_empty_catches(path, lines):
    for index, line in enumerate(lines[:-1]):
        if not line.strip().startswith("catch") or lines[index + 1].strip() != "{":
            continue
        body = lines[index + 2:find_block_end(lines, index + 1)]
        if all(not part.strip() or part.strip().startswith("//") for part in body):
            print(f"{path}:{index + 1}: empty catch block (only comments or nothing inside)")


for path in sorted(pathlib.Path(sys.argv[1]).rglob("*.cs")):
    if "/obj/" in str(path) or "/bin/" in str(path):
        continue
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    for index, line in enumerate(lines):
        if SIGNATURE.match(line) and "=>" not in line and not line.rstrip().endswith(";"):
            check_method(path, lines, index)
    check_empty_catches(path, lines)
EOF
```
