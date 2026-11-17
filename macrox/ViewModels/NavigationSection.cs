namespace macrox.ViewModels
{
    /// <summary>
    /// Sección de navegación del shell. <paramref name="Key"/> identifica la vista que se
    /// resolverá cuando existan las pantallas reales.
    /// </summary>
    public sealed record NavigationSection(
        string Key,
        string Title,
        string Description,
        string Icon);
}
