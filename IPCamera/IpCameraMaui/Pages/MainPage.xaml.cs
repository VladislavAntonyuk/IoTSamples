using IpCameraMaui.ViewModels;
using Microsoft.Maui.Platform;

namespace IpCameraMaui.Pages;

public partial class MainPage
{
    private readonly RtmpCameraViewModel viewModel;

    public MainPage(RtmpCameraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
#if ANDROID
        await viewModel.Init(CameraView.ToPlatform(Handler.MauiContext) as Android.Views.SurfaceView);
#endif
        Loaded -= OnPageLoaded;
    }

    private async void StartRtmpStreamClicked(object? sender, EventArgs e)
    {
#if ANDROID
        await viewModel.StartRtmpStream();
#endif
    }
}