using IpCameraMaui.ViewModels;
using Microsoft.Maui.Platform;

namespace IpCameraMaui.Pages;

public partial class MainPage
{
    private readonly RtmpCameraViewModel _viewModel;

    public MainPage(RtmpCameraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this._viewModel = viewModel;
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
#if ANDROID
        await _viewModel.Init(CameraView.ToPlatform(Handler.MauiContext) as Android.Views.SurfaceView);
#endif
        Loaded -= OnPageLoaded;
    }

    private async void StartRtmpStreamClicked(object? sender, EventArgs e)
    {
#if ANDROID
        await _viewModel.StartRtmpStream();
#endif
    }
}