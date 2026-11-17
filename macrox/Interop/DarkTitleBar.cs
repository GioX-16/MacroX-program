using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace macrox.Interop
{
    /// <summary>
    /// Aplica el modo oscuro y el color del shell a la barra de título nativa de Windows.
    /// En versiones que no soportan personalización de color se conserva el modo oscuro estándar.
    /// </summary>
    internal static class DarkTitleBar
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeLegacy = 19;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwaBorderColor = 34;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaTextColor = 36;

        private const int DwmwcpRound = 2;

        public static void Apply(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);

            var handle = new WindowInteropHelper(window).EnsureHandle();
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var enabled = 1;
            if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, ref enabled, sizeof(int));
            }

            ApplyColor(handle, DwmwaCaptionColor, 0x0B0614);
            ApplyColor(handle, DwmwaTextColor, 0xF5F3FF);
            ApplyColor(handle, DwmwaBorderColor, 0x241436);

            var rounded = DwmwcpRound;
            DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref rounded, sizeof(int));
        }

        private static void ApplyColor(IntPtr handle, int attribute, int rgb)
        {
            var colorRef = ToColorRef(rgb);
            DwmSetWindowAttribute(handle, attribute, ref colorRef, sizeof(int));
        }

        /// <summary>Convierte 0xRRGGBB al COLORREF de Win32 (0x00BBGGRR).</summary>
        private static int ToColorRef(int rgb) =>
            ((rgb >> 16) & 0xFF) | (rgb & 0xFF00) | ((rgb & 0xFF) << 16);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int attributeValue,
            int attributeSize);
    }
}
