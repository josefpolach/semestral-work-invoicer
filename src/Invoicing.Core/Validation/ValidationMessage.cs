namespace Invoicing.Core.Validation;

/// <summary>A validation issue with its location and severity.</summary>
public record ValidationMessage(ValidationCode Code, ValidationTarget Target,
    ValidationSeverity Severity = ValidationSeverity.Error, int? LinePosition = null);
