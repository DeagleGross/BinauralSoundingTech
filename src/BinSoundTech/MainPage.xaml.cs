using BinSoundTech.ViewModels;

namespace BinSoundTech;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnOpenAudioInputsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//AudioInputs");
    }
}
