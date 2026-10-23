using AsistenteIA.Views;

namespace AsistenteIA;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("detail", typeof(DetailPage));
	}
}
