using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeManagement.Components.Dialogs;

public partial class DeviceEditDialog
{
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

    private void RemoveAction(int index)
    {
        if (index >= 0 && index < Model.Actions.Count)
        {
            Model.Actions.RemoveAt(index);
        }
    }
}