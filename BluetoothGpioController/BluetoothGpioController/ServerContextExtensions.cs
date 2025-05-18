using DotnetBleServer.Advertisements;
using DotnetBleServer.Core;
using DotnetBleServer.Gatt;
using DotnetBleServer.Gatt.BlueZModel;
using DotnetBleServer.Gatt.Description;
using Microsoft.Extensions.Options;

namespace BluetoothGpioController;

public static class ServerContextExtensions
{
	public static async Task RegisterAdvertisement(this ServerContext context, string name)
	{
		var advertisementProperties = new LEAdvertisement1Properties
		{
			Type = "peripheral",
			ServiceUUIDs = ["12345678-1234-5678-1234-56789abcdef0"],
			LocalName = name,
			Appearance = 128, // (ushort)Convert.ToUInt32("0x0080", 16),
			Discoverable = true,
			IncludeTxPower = true
		};

		await new AdvertisingManager(context).CreateAdvertisement(advertisementProperties);
	}

	public static async Task RegisterGattApplication(this ServerContext context, ILogger logger, IOptions<AppSettings> options)
	{
		var gattServiceDescription = new GattServiceDescription
		{
			UUID = "12345678-1234-5678-1234-56789abcdef0",
			Primary = true
		};

		var gpioControllerGattCharacteristicDescription = new GpioControllerGattCharacteristicDescription(logger, options)
		{
			UUID = "12345678-1234-5678-1234-56789abcdef1",
			Flags = CharacteristicFlags.Notify
		};

		var grioControllerGattDescriptorDescription = new GattDescriptorDescription
		{
			Value = [(byte)'t'],
			UUID = "12345678-1234-5678-1234-56789abcdef2",
			Flags = ["read", "write"]
		};

		var gab = new GattApplicationBuilder();
		gab
			.AddService(gattServiceDescription)
			.WithCharacteristic(gpioControllerGattCharacteristicDescription, [grioControllerGattDescriptorDescription]);

		await new GattApplicationManager(context).RegisterGattApplication(gab.BuildServiceDescriptions());
	}
}