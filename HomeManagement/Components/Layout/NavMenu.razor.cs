using Microsoft.AspNetCore.Components;

namespace HomeManagement.Components.Layout;

public partial class NavMenu(NavigationManager navigationManager) : ComponentBase
{
    private readonly Uri _baseUri = new Uri(navigationManager.BaseUri);
}

