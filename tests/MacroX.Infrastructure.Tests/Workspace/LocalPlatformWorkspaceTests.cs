using MacroX.Infrastructure.Workspace;

namespace MacroX.Infrastructure.Tests.Workspace;

public class LocalPlatformWorkspaceTests
{
    [Fact]
    public async Task ImportWorkbookAsync_CopiaElArchivoYNoModificaElOriginal()
    {
        var root = Path.Combine(Path.GetTempPath(), "MacroX", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(root, "descargas");
        Directory.CreateDirectory(sourceDirectory);
        var source = Path.Combine(sourceDirectory, "practica_trim.xlsx");
        await File.WriteAllBytesAsync(source, [1, 2, 3, 4]);
        var original = await File.ReadAllBytesAsync(source);
        var workspace = new LocalPlatformWorkspace(Path.Combine(root, "plataforma"));

        try
        {
            var stored = await workspace.ImportWorkbookAsync(source, CancellationToken.None);

            Assert.NotEqual(Path.GetFullPath(source), Path.GetFullPath(stored));
            Assert.Equal(original, await File.ReadAllBytesAsync(source));
            Assert.Equal(original, await File.ReadAllBytesAsync(stored));
            Assert.Equal(stored, Assert.Single(workspace.ListWorkbooks()));

            var result = workspace.CreateResultPath(stored);
            Assert.EndsWith("_macrox.xlsx", result, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(Path.GetFullPath(stored), Path.GetFullPath(result));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DeleteWorkbook_QuitaLaCopiaYRechazaUnaRutaExterna()
    {
        var root = Path.Combine(Path.GetTempPath(), "MacroX", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(root, "descargas");
        Directory.CreateDirectory(sourceDirectory);
        var source = Path.Combine(sourceDirectory, "clientes.xlsx");
        await File.WriteAllBytesAsync(source, [9, 8, 7]);
        var workspace = new LocalPlatformWorkspace(Path.Combine(root, "plataforma"));

        try
        {
            var stored = await workspace.ImportWorkbookAsync(source, CancellationToken.None);
            workspace.DeleteWorkbook(stored);

            Assert.Empty(workspace.ListWorkbooks());
            Assert.True(File.Exists(source));
            Assert.Throws<InvalidOperationException>(() => workspace.DeleteWorkbook(source));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void HideCatalogMacro_LaRecuerda()
    {
        var root = Path.Combine(Path.GetTempPath(), "MacroX", Guid.NewGuid().ToString("N"));
        var workspace = new LocalPlatformWorkspace(root);

        try
        {
            workspace.HideCatalogMacro("trim");
            workspace.HideCatalogMacro("trim");

            Assert.Equal(["trim"], workspace.ListHiddenCatalogMacros());

            workspace.RestoreCatalogMacro("trim");
            workspace.RestoreCatalogMacro("trim");

            Assert.Empty(workspace.ListHiddenCatalogMacros());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ImportMacroModuleAsync_RechazaUnExcel()
    {
        var root = Path.Combine(Path.GetTempPath(), "MacroX", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "ModTrim.xlsx");
        await File.WriteAllBytesAsync(source, [1]);
        var workspace = new LocalPlatformWorkspace(Path.Combine(root, "plataforma"));

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                workspace.ImportMacroModuleAsync(source, CancellationToken.None));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
