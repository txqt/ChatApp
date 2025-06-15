using MudBlazor;

namespace ChatApp.BlazorApp.Services
{
    public class ThemeService
    {
        public bool IsDarkMode { get; set; } = false;

        private MudTheme LightTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = Colors.Blue.Default,
                Background = Colors.Gray.Lighten5,
                AppbarBackground = Colors.Blue.Default,
            }
        };

        private MudTheme DarkTheme = new MudTheme()
        {
            PaletteDark = new PaletteDark()
            {
                Primary = Colors.Blue.Lighten1,
                Background = "#121212",
                Surface = "#212121",
                AppbarBackground = Colors.Blue.Darken4,
                TextPrimary = Colors.Shades.White
            }
        };

        public MudTheme CurrentTheme => IsDarkMode ? DarkTheme : LightTheme;

        public event Action OnChange;

        public void ToggleDarkMode()
        {
            IsDarkMode = !IsDarkMode;
            OnChange?.Invoke();
        }
    }
}
