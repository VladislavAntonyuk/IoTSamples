using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;

namespace RemoteCamera;

public partial class MainPage : ContentPage
{
    private const int port = 5555;
    private PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(300));
    private readonly TcpClient _tcpClient = new();
    private readonly TcpListener _tcpListener = new(IPAddress.Any, port);

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (!MyCameraView.IsAvailable || MyCameraView.IsCameraBusy)
        {
            return;
        }
        
        var cameras = await MyCameraView.GetAvailableCameras(CancellationToken.None);
        MyCameraView.SelectedCamera = cameras.FirstOrDefault(x => x.Position != CameraPosition.Front);
        await MyCameraView.StartCameraPreview(CancellationToken.None);
        MyCameraView.MediaCaptured += CameraView_OnMediaCaptured;
        await MyCameraView.StartCameraPreview(CancellationToken.None);
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _timer.Dispose();
        _tcpClient.Close();
        MyCameraView.MediaCaptured -= CameraView_OnMediaCaptured;
        MyCameraView.StopCameraPreview();
        base.OnNavigatedFrom(args);
    }

    private async void CameraView_OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
    {
        if (!_tcpClient.Connected)
        {
            return;
        }

        try
        {
            using var ms = new MemoryStream();
            await e.Media.CopyToAsync(ms);
            byte[] data = ms.ToArray();

            // Send 4-byte length prefix first
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
            await _tcpClient.GetStream().WriteAsync(lengthPrefix);
            await _tcpClient.GetStream().WriteAsync(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending image: " + ex.Message);
        }
    }

    private async void StartClientClicked(object? sender, EventArgs e)
    {
        _timer = new(TimeSpan.FromMilliseconds(300));
        await _tcpClient.ConnectAsync(IPAddress.Parse(IpAddress.Text), port);
        while (await _timer.WaitForNextTickAsync())
        {
            await MyCameraView.CaptureImage(CancellationToken.None);
        }
    }

    private async void StartServerClicked(object? sender, EventArgs e)
    {
        _tcpListener.Start();
        using var client = await _tcpListener.AcceptTcpClientAsync();
        using var stream = client.GetStream();

        var lengthBuffer = new byte[4];

        while (true)
        {
            // Read 4-byte length prefix
            await ReadExactlyAsync(stream, lengthBuffer, 4);
            int imageSize = BitConverter.ToInt32(lengthBuffer);

            // Read image data
            var imageBuffer = new byte[imageSize];
            await ReadExactlyAsync(stream, imageBuffer, imageSize);

            // Update image on UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Image.Source = ImageSource.FromStream(() => new MemoryStream(imageBuffer));
            });
        }
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int size)
    {
        int offset = 0;
        while (offset < size)
        {
            int read = await stream.ReadAsync(buffer, offset, size - offset);
            if (read == 0)
                throw new IOException("Disconnected");
            offset += read;
        }
    }
}