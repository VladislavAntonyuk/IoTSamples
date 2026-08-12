using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeManagement.Components.Dialogs;

public partial class DeviceEditDialog
{
    [Inject] private IDialogService DialogService { get; set; } = default!;

    [CascadingParameter] private IMudDialogInstance DialogReference { get; set; } = default!;
    [Parameter] public DeviceEditModel Model { get; set; } = new();
    private MudForm _form = default!;
    private bool _saving;

    private async Task Save()
    {
        _saving = true;
        await _form.ValidateAsync();
        if (!_form.IsValid)
        {
            _saving = false;
            return;
        }

        DialogReference.Close(DialogResult.Ok(Model));
    }

    private void Cancel() => DialogReference.Close(DialogResult.Cancel());

    private void AddAction()
    {
        Model.Actions.Add(new DeviceActionEditModel());
    }

    private async Task RemoveAction(int index)
    {
        if (index < 0 || index >= Model.Actions.Count)
        {
            return;
        }

        if (!await ConfirmDeleteAsync("action"))
        {
            return;
        }

        Model.Actions.RemoveAt(index);
    }

    private void AddConfiguration()
    {
        Model.Configurations.Add(new DeviceConfigurationEditModel());
    }

    private async Task RemoveConfiguration(int index)
    {
        if (index < 0 || index >= Model.Configurations.Count)
        {
            return;
        }

        if (!await ConfirmDeleteAsync("configuration"))
        {
            return;
        }

        Model.Configurations.RemoveAt(index);
    }

    private async Task<bool> ConfirmDeleteAsync(string itemName)
    {
        var parameters = new DialogParameters
        {
            ["Message"] = $"Are you sure you want to delete this {itemName}?"
        };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirm delete", parameters);
        var result = await dialog.Result;
        return result is not null && !result.Canceled;
    }
}