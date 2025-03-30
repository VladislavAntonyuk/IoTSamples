using System.Device.Gpio;
using System.Text;
using DotnetBleServer.Gatt.Description;

namespace BluetoothGpioController;

internal class GpioControllerGattCharacteristicDescription(ILogger logger) : GattCharacteristicDescription
{
    public override Task WriteValueAsync(byte[] value)
    {
        var command = Encoding.Default.GetString(value);
        logger.LogInformation("Received value: {Value}", command);

        var gpioCommand = command.Split(';');
        var pinNumber = Convert.ToInt32(gpioCommand[0]);
        using var controller = new GpioController();
        var pin = controller.OpenPin(pinNumber, Enum.Parse<PinMode>(gpioCommand[1]));
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

        return Task.FromResult(Encoding.ASCII.GetBytes(pinResults.ToString()));
    }
}