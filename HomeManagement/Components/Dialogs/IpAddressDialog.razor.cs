using HomeManagement.Application.DeviceManagement;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeManagement.Components.Dialogs;

public partial class IpAddressDialog
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
        var networkDevice = await NetworkManager.GetDeviceInfoAsync(address, CancellationToken.None);
        _isBusy = false;
        if (networkDevice is null)
        {
            _error = "Could not reach device at this address.";
            return;
        }

        DialogReference.Close(DialogResult.Ok(networkDevice));
    }

    private void Cancel() => DialogReference.Close(DialogResult.Cancel());
}