namespace MacroX.Domain.Macros;

/// <summary>
/// Tipo de un parámetro de macro. La interfaz traduce cada valor al control que corresponde.
/// </summary>
public enum MacroParameterKind
{
    Text,
    Boolean,
    Integer,
    ColumnList,
}
