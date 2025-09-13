using Android.App;
using Android.Runtime;
using BluetoothController.Client;

namespace BluetoothController.Client;

[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}