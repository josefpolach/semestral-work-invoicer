namespace Invoicing.Email;

/// <summary>Reports a placeholder the composer did not supply.</summary>
public class TemplateRenderException(string name) : InvalidOperationException($"Missing template value: {name}.");
