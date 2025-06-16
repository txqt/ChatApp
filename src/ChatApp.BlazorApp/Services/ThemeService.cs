using Blazored.LocalStorage;
using MudBlazor;

namespace ChatApp.BlazorApp.Services
{
    public class ThemeService
    {
        private const string StorageKey = "darkMode";
        private readonly ILocalStorageService _storage;

        public ThemeService(ILocalStorageService storage)
        {
            _storage = storage;
        }

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

        public async Task InitializeAsync()
        {
            // Đọc giá trị từ localStorage (mặc định là false nếu chưa có)
            IsDarkMode = await _storage.GetItemAsync<bool>(StorageKey);
            OnChange?.Invoke();
        }

        public async Task ToggleDarkModeAsync()
        {
            IsDarkMode = !IsDarkMode;
            await _storage.SetItemAsync(StorageKey, IsDarkMode);
            OnChange?.Invoke();
        }
    }
}
