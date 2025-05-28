using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace RemoteCamera;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitCamera();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
