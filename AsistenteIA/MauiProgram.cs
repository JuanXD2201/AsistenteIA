using AsistenteIA.Core.Services;
using AsistenteIA.Services;
using AsistenteIA.ViewModels;
using AsistenteIA.Views;
using Microsoft.Extensions.Logging;

namespace AsistenteIA;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Un único HttpClient reutilizable; el timeout lo controla ChatCompletionsClient.
		builder.Services.AddSingleton(new HttpClient { Timeout = Timeout.InfiniteTimeSpan });
		builder.Services.AddSingleton<IAiClient>(sp => new ChatCompletionsClient(sp.GetRequiredService<HttpClient>()));
		builder.Services.AddSingleton<ISecretStore, MauiSecretStore>();
		builder.Services.AddSingleton<ISettingsStore, MauiSettingsStore>();
		builder.Services.AddSingleton<AiConfigService>();
		builder.Services.AddSingleton<AiService>();
		builder.Services.AddSingleton<IHistoryRepository>(_ =>
			new SqliteHistoryRepository(Path.Combine(FileSystem.AppDataDirectory, "asistenteia.db3")));

		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<QueryViewModel>();
		builder.Services.AddTransient<HistoryViewModel>();
		builder.Services.AddTransient<DetailViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<QueryPage>();
		builder.Services.AddTransient<HistoryPage>();
		builder.Services.AddTransient<DetailPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
