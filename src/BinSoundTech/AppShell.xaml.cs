namespace BinSoundTech
{
    public partial class AppShell : Shell
    {
        public AppShell(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            
            // Resolve MainPage from DI container
            MainShellContent.Content = serviceProvider.GetRequiredService<MainPage>();
        }
    }
}
