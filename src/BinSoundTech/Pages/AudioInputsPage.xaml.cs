using BinSoundTech.Models;
using BinSoundTech.ViewModels;

namespace BinSoundTech.Pages;

public partial class AudioInputsPage : ContentPage
{
    private readonly AudioInputsPageViewModel _viewModel;

    public AudioInputsPage(AudioInputsPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void OnFileTypeClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            source.InputType = AudioInputType.File;
        }
    }

    private void OnMicTypeClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            source.InputType = AudioInputType.Microphone;
            // Set default device if none selected
            if (source.DeviceIndex < 0 && _viewModel.AvailableDevices.Count > 0)
            {
                var defaultDevice = _viewModel.AvailableDevices[0];
                source.DeviceIndex = defaultDevice.DeviceIndex;
                source.DeviceName = defaultDevice.Name;
            }
        }
    }

    private async void OnBrowseFileClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            await _viewModel.SelectFileForSourceAsync(source);
        }
    }

    private async void OnSelectDeviceClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            var devices = _viewModel.AvailableDevices.ToArray();
            if (devices.Length == 0)
            {
                await DisplayAlert("No Devices", "No audio input devices found.", "OK");
                return;
            }

            var deviceNames = devices.Select(d => d.Name).ToArray();
            var result = await DisplayActionSheet("Select Input Device", "Cancel", null, deviceNames);

            if (!string.IsNullOrEmpty(result) && result != "Cancel")
            {
                var selectedDevice = devices.FirstOrDefault(d => d.Name == result);
                if (selectedDevice != null)
                {
                    _viewModel.SetDeviceForSource(source, selectedDevice);
                }
            }
        }
    }

    private async void OnPlayClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            await _viewModel.ToggleSourceAsync(source);
        }
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            _viewModel.StopSource(source);
        }
    }

    private void OnMuteClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            _viewModel.ToggleMute(source);
        }
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (GetSourceFromSender(sender) is AudioInputSource source)
        {
            _viewModel.RemoveSource(source);
        }
    }

    /// <summary>
    /// Gets the AudioInputSource from the sender's BindingContext.
    /// </summary>
    private static AudioInputSource? GetSourceFromSender(object? sender)
    {
        if (sender is BindableObject bindable)
        {
            return bindable.BindingContext as AudioInputSource;
        }
        return null;
    }
}
