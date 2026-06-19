using System.Windows.Controls;
using System.Windows.Input;
using SystemChecker.ViewModels;

namespace SystemChecker.Views
{
	/// <summary>
	/// Searchable list dialog for choosing a PassMark benchmark entry.
	/// </summary>
	public partial class BenchmarkPickerView : UserControl
	{
		public BenchmarkPickerView(BenchmarkPickerViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}

		private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (DataContext is BenchmarkPickerViewModel viewModel)
			{
				viewModel.CmdConfirm.Execute(null);
			}
		}
	}
}
