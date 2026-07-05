using System.Net.NetworkInformation;
using HomeManagement.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NetworkManager = HomeManagement.Application.DeviceManagement.NetworkManager;

namespace HomeManagement.Components.Dialogs;

public partial class AddressDialog
{
    [CascadingParameter] private IMudDialogInstance DialogReference { get; set; } = default!;
    private string Address { get; set; } = string.Empty;
    private string? _error;
    private bool _isBusy;

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            return;
        }

        _isBusy = true;
        var address = Address.Trim();
        Device? device;
        if (PhysicalAddress.TryParse(address, out _))
        {
            device = new BluetoothDevice()
            {
                Name = "AP300",
                Address = address,
                Actions = [
                    new DeviceAction("GET", CommandType.Get, "GET")
                ]
            };
        }
        else
        {
            device = await NetworkManager.GetDeviceInfoAsync(address, CancellationToken.None);
            _isBusy = false;
            if (device is null)
            {
                _error = "Could not reach device at this address.";
                return;
            }
        }

        DialogReference.Close(DialogResult.Ok(device));
    }

    private void Cancel() => DialogReference.Close(DialogResult.Cancel());
}