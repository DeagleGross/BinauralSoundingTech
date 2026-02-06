using Microsoft.Extensions.Logging;
using BinSoundTech.Services;
using BinSoundTech.ViewModels;
using BinSoundTech.Pages;

namespace BinSoundTech
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureMauiHandlers(handlers =>
                {
#if WINDOWS
    				Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
    				{
    					handler.PlatformView.SingleSelectionFollowsFocus = false;
    				});
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // Services
            builder.Services.AddSingleton<AudioPlaybackService>();
            builder.Services.AddSingleton<AudioDeviceService>();

            // ViewModels
            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddSingleton<AudioInputsPageViewModel>();

            // Pages
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<AudioInputsPage>();

            return builder.Build();
        }
    }
}
