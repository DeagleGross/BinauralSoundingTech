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

    private void OnFrontClicked(object? sender, EventArgs e)
    {
        _viewModel.Azimuth = 0;
        _viewModel.Elevation = 0;
    }

    private void OnLeftClicked(object? sender, EventArgs e)
    {
        _viewModel.Azimuth = -80;
        _viewModel.Elevation = 0;
    }

    private void OnRightClicked(object? sender, EventArgs e)
    {
        _viewModel.Azimuth = 80;
        _viewModel.Elevation = 0;
    }

    private void OnAboveClicked(object? sender, EventArgs e)
    {
        _viewModel.Azimuth = 0;
        _viewModel.Elevation = 90;
    }

    private void OnBehindClicked(object? sender, EventArgs e)
    {
        _viewModel.Azimuth = 0;
        _viewModel.Elevation = 180;
    }

    private async void OnSelectAudioFileClicked(object? sender, EventArgs e)
    {
        await _viewModel.SelectAudioFileAsync();
    }

    private async void OnPlayAudioClicked(object? sender, EventArgs e)
    {
        await _viewModel.PlayAudioAsync();
    }

    private void OnPauseAudioClicked(object? sender, EventArgs e)
    {
        _viewModel.PauseAudio();
    }
}
