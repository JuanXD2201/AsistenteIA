using AsistenteIA.ViewModels;

namespace AsistenteIA.Views;

public partial class HistoryPage : ContentPage
{
	private readonly HistoryViewModel _viewModel;

	public HistoryPage(HistoryViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		List.SelectedItem = null;
		_viewModel.LoadCommand.Execute(null);
	}
}
