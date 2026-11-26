namespace MacroX.Domain.Macros;

/// <summary>
/// Descripción inmutable de un parámetro que una macro declara en el catálogo.
/// </summary>
public sealed class MacroParameterDefinition
{
    public MacroParameterDefinition(
        string key,
        string displayName,
        MacroParameterKind kind,
        bool isRequired,
        string description)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("La clave del parámetro es obligatoria.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("El nombre visible del parámetro es obligatorio.", nameof(displayName));
        }

        Key = key.Trim();
        DisplayName = displayName.Trim();
        Kind = kind;
        IsRequired = isRequired;
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    public string Key { get; }

    public string DisplayName { get; }

    public MacroParameterKind Kind { get; }

    public bool IsRequired { get; }

    public string Description { get; }
}
