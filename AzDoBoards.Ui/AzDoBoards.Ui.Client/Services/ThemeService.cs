using MudBlazor;

namespace AzDoBoards.Ui.Client.Services
{
    public class ThemeService
    {
        public MudTheme DefaultTheme { get; } = new()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = Colors.Blue.Default,
                Secondary = Colors.Green.Accent4,
                AppbarBackground = Colors.Blue.Default,
                Background = Colors.Gray.Lighten5,
                DrawerBackground = "#FFF",
                DrawerText = "rgba(0,0,0, 0.7)",
                Success = Colors.Green.Accent3
            },
            PaletteDark = new PaletteDark()
            {
                Primary = Colors.Blue.Lighten1,
                Secondary = Colors.Green.Accent4,
                AppbarBackground = Colors.BlueGray.Darken4,
                Background = Colors.Gray.Darken4,
                DrawerBackground = Colors.BlueGray.Darken4,
                DrawerText = "rgba(255,255,255, 0.7)",
                Success = Colors.Green.Accent3
            },
            LayoutProperties = new LayoutProperties()
            {
                DrawerWidthLeft = "260px",
                DrawerWidthRight = "300px"
            }
        };
    }
}