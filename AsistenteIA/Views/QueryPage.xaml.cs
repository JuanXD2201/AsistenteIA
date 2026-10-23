using AsistenteIA.ViewModels;

namespace AsistenteIA.Views;

public partial class QueryPage : ContentPage
{
	private readonly QueryViewModel _viewModel;

	public QueryPage(QueryViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.LoadCommand.Execute(null);
	}
}
