using System.Collections.ObjectModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IpCameraMaui.Pages;

namespace IpCameraMaui.ViewModels;

#if ANDROID
using Android.Views;
using Com.Pedro.Encoder.Input.Video;
using Com.Pedro.Rtplibrary.Rtmp;
using RtpLibrary;
#endif

public partial class RtmpCameraViewModel(SettingsViewModel settingsViewModel) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsStreaming { get; private set; }

    public ObservableCollection<string> Logs { get; set; } = [];

#if ANDROID
    private RtmpCamera1? rtmpCamera;

    public async Task Init(SurfaceView surfaceView)
    {
        var cameraRequest = await Permissions.RequestAsync<Permissions.Camera>();
        var microphoneRequest = await Permissions.RequestAsync<Permissions.Microphone>();
        if (cameraRequest != PermissionStatus.Granted || microphoneRequest != PermissionStatus.Granted)
        {
            await Shell.Current.CurrentPage.DisplayAlert("Permission Denied", "Camera and Microphone permissions are required to use this feature.", "OK");
            return;
        }


        rtmpCamera = new RtmpCamera1(surfaceView, new ConnectChecker(s => Logs.Add($"{DateTime.Now} - {s}")));
        IsPowerSavingModeEnabled = settingsViewModel.IsPowerSavingModeEnabled;
    }
#endif

    public async Task StartRtmpStream()
    {
        Logs.Clear();
#if ANDROID
        if (rtmpCamera is null)
        {
            return;
        }

        var resolution = rtmpCamera.ResolutionsBack.OrderByDescending(x => x.Width).FirstOrDefault();
        if (rtmpCamera.PrepareAudio() && rtmpCamera.PrepareVideo(resolution.Width, resolution.Height, 5000 * 1024))
        {
            DeviceDisplay.KeepScreenOn = true;
            if (string.IsNullOrWhiteSpace(settingsViewModel.RecordingsFolder) || !settingsViewModel.SaveRecordingToFileStorage)
            {
                rtmpCamera.StartStream(settingsViewModel.RtmpAddressText);
            }
            else
            {
                await DeleteOldRecords();
                rtmpCamera.StartStreamAndRecord(settingsViewModel.RtmpAddressText, Path.Combine(settingsViewModel.RecordingsFolder, $"recording-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.mp4"));
            }

            if (rtmpCamera.CameraFacing != CameraHelper.Facing.Back)
            {
                rtmpCamera.SwitchCamera();
            }

            IsStreaming = true;
        }
#endif
    }

#if ANDROID

    [RelayCommand]
    private void StopRtmpStream()
    {
        if (rtmpCamera is null)
        {
            return;
        }

        if (rtmpCamera.IsStreaming)
        {
            rtmpCamera.StopStream();
        }

        if (rtmpCamera.IsRecording)
        {
            rtmpCamera.StopRecord();
        }

        IsStreaming = false;
        DeviceDisplay.KeepScreenOn = false;
    }
#endif



    [RelayCommand(AllowConcurrentExecutions = false)]
    async Task OpenSettings()
    {
        var popup = new SettingsPage(settingsViewModel);
        await Shell.Current.ShowPopupAsync(popup, new PopupOptions() { CanBeDismissedByTappingOutsideOfPopup = true });
    }

    [ObservableProperty]
    public partial bool IsPowerSavingModeEnabled { get; set; }


    [RelayCommand]
    void DisablePowerSavingMode()
    {
        IsPowerSavingModeEnabled = false;
    }

    async Task DeleteOldRecords()
    {
        if (string.IsNullOrWhiteSpace(settingsViewModel.RecordingsFolder) || !settingsViewModel.SaveRecordingToFileStorage)
        {
            return;
        }

        var files = Directory.GetFiles(settingsViewModel.RecordingsFolder, "*.mp4");
        if (files.Length >= settingsViewModel.MaxFiles)
        {
            var filesToDelete = files
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Skip(settingsViewModel.MaxFiles - 1)
                .ToList();
            foreach (var file in filesToDelete)
            {
                File.Delete(file);
            }
        }
    }
}