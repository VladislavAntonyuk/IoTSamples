using DotnetBleServer.Core;

namespace BluetoothGpioController;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var context = new ServerContext();
		await context.ConnectAndSetDefaultAdapter();

		logger.LogInformation("Using bluetooth adapter: {Adapter}", context.Adapter.ObjectPath);
		await context.Adapter.SetPoweredAsync(true);

		if (!await context.Adapter.GetPoweredAsync())
		{
			logger.LogError("Can't power on adapter {Adapter}", context.Adapter.ObjectPath);
			return;
		}

		await context.RegisterAdvertisement("Bluetooth Gpio Controller");
		await context.RegisterGattApplication(logger);

		while (!stoppingToken.IsCancellationRequested)
		{
			await Task.Delay(1000, stoppingToken);
		}
	}
}