using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IpCameraMaui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly IAutoStartService autoStartService;
	private readonly IPreferences preferences;
	private bool isFirstStart = true;

	public SettingsViewModel(IAutoStartService autoStartService, IPreferences preferences)
	{
		this.autoStartService = autoStartService;
		this.preferences = preferences;

		RtmpAddressText = preferences.Get(nameof(RtmpAddressText), "rtmp://localhost:1935/live/demo");
        MaxFiles = preferences.Get(nameof(MaxFiles), 1);
		IsAutoStartEnabled = preferences.Get(nameof(IsAutoStartEnabled), autoStartService.IsAutoStartEnabledAsync().GetAwaiter().GetResult());
		RecordingsFolder = preferences.Get<string?>(nameof(RecordingsFolder), null);
		if (!string.IsNullOrWhiteSpace(RecordingsFolder))
		{
			SaveRecordingToFileStorage = true;
		}
		else
		{
			isFirstStart = false;
		}

        IsPowerSavingModeEnabled = preferences.Get(nameof(IsPowerSavingModeEnabled), false);
	}


	[ObservableProperty]
	public partial string RtmpAddressText { get; set; }

	[ObservableProperty]
	public partial int MaxFiles { get; set; }

	[ObservableProperty]
	public partial bool IsAutoStartEnabled { get; set; }

	[ObservableProperty]
	public partial bool SaveRecordingToFileStorage { get; set; }

	[ObservableProperty]
	public partial string? RecordingsFolder { get; set; }

	[ObservableProperty]
	public partial bool IsPowerSavingModeEnabled { get; set; }

    partial void OnRtmpAddressTextChanged(string value)
    {
        preferences.Set(nameof(RtmpAddressText), value);
    }

    partial void OnMaxFilesChanged(int value)
    {
        preferences.Set(nameof(MaxFiles), value);
    }

    async partial void OnIsAutoStartEnabledChanged(bool value)
	{
		if (value)
		{
			await autoStartService.EnableAutoStartAsync();
		}
		else
		{
			await autoStartService.DisableAutoStartAsync();
		}

		preferences.Set(nameof(IsAutoStartEnabled), await autoStartService.IsAutoStartEnabledAsync());
	}

	async partial void OnSaveRecordingToFileStorageChanged(bool value)
	{
		if (isFirstStart)
		{
			isFirstStart = false;
			return;
		}

		if (value)
		{
			var pickResult = await FolderPicker.PickAsync(CancellationToken.None);
			if (pickResult.IsSuccessful)
			{
				RecordingsFolder = pickResult.Folder.Path;
			}
		}
		else
		{
			RecordingsFolder = null;
		}

		preferences.Set(nameof(RecordingsFolder), RecordingsFolder);
	}

    partial void OnIsPowerSavingModeEnabledChanged(bool value)
    {
        preferences.Set(nameof(IsPowerSavingModeEnabled), value);
    }
}