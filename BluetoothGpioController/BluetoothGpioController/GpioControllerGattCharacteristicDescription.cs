using System.Device.Gpio;
using System.Text;
using DotnetBleServer.Gatt.Description;
using Microsoft.Extensions.Options;

namespace BluetoothGpioController;

internal class GpioControllerGattCharacteristicDescription(ILogger logger, IOptions<AppSettings> options) : GattCharacteristicDescription
{
    public override Task WriteValueAsync(byte[] value)
    {
        var command = Encoding.UTF8.GetString(value);
        logger.LogInformation("Received value: {Value}", command);

        var gpioCommand = command.Split(';');
        if (gpioCommand.Length != 4)
        {
            logger.LogError("Invalid command format. Expected: PIN;MODE;VALUE;PASSWORD");
            return base.WriteValueAsync(value);
        }
        
        if (gpioCommand[3] != options.Value.Password)
        {
            logger.LogError("Invalid password");
            return base.WriteValueAsync(value);
        }
        
        var pinNumber = Convert.ToInt32(gpioCommand[0]);
        using var controller = new GpioController();
        var pin = controller.OpenPin(pinNumber, Enum.Parse<PinMode>(gpioCommand[1], true));
        pin.Write(Convert.ToInt32(gpioCommand[2]));

        return base.WriteValueAsync(value);
    }

    public override Task<byte[]> ReadValueAsync()
    {
        using var controller = new GpioController();
        var pinResults = new StringBuilder();
        for (var pinNumber = 1; pinNumber < controller.PinCount; pinNumber++)
        {
            var pin = controller.OpenPin(pinNumber);
            var mode = pin.GetPinMode();
            var pinValue = pin.Read();
            logger.LogInformation("Reading value PIN: {Pin}, MODE: {Mode}, VALUE: {Value}", pinNumber, mode, pinValue);
            pinResults.AppendLine($"{pinNumber};{mode};{pinValue}");
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(pinResults.ToString()));
    }
}