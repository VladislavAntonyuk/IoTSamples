using System.Diagnostics;
using System.Text;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Exceptions;

namespace LampController.Client;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnCounterClicked(object sender, EventArgs e)
	{
		var ble = CrossBluetoothLE.Current;
		var adapter = CrossBluetoothLE.Current.Adapter;
		ble.StateChanged += (s, e) =>
		{
			Debug.WriteLine($"The bluetooth state changed to {e.NewState}");
		};
		adapter.DeviceDiscovered += async (s, a) =>
		{
			try
			{
				await adapter.ConnectToDeviceAsync(a.Device);
				var service = await a.Device.GetServiceAsync(Guid.Parse("12345678-1234-5678-1234-56789abcdef0"));
				var characteristic = await service.GetCharacteristicAsync(Guid.Parse("12345678-1234-5678-1234-56789abcdef1"));
				await characteristic.WriteAsync(Encoding.Default.GetBytes("10;Output;1"));
				await characteristic.WriteAsync(Encoding.Default.GetBytes("10;Input;0"));
			}
			catch (Exception e)
			{
				// ... could not connect to device
			}
		};
		await adapter.StartScanningForDevicesAsync();
	}
}