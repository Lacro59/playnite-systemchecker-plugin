using CommonPluginsShared;
using Playnite.SDK;
using System.Windows;
using SystemChecker.ViewModels;
using SystemChecker.Views;

namespace SystemChecker.Services
{
	/// <summary>
	/// Shows a modal dialog to pick a PassMark benchmark entry for manual CPU or GPU configuration.
	/// </summary>
	public static class BenchmarkPickerDialog
	{
		/// <summary>
		/// Opens the benchmark picker and returns the selected PassMark component name.
		/// </summary>
		/// <param name="isGpu"><c>true</c> for GPU catalog; <c>false</c> for CPU catalog.</param>
		/// <param name="detectedRawName">WMI-detected hardware name used for fuzzy suggestion.</param>
		/// <param name="currentManualValue">Current manual override, if any.</param>
		/// <returns>The selected benchmark name, or <c>null</c> when the dialog is cancelled.</returns>
		public static string Pick(bool isGpu, string detectedRawName, string currentManualValue)
		{
			string suggestFrom = !string.IsNullOrWhiteSpace(currentManualValue)
				? currentManualValue
				: detectedRawName;

			BenchmarkPickerViewModel viewModel = new BenchmarkPickerViewModel(isGpu, suggestFrom);
			BenchmarkPickerView view = new BenchmarkPickerView(viewModel);

			string titleKey = isGpu ? "LOCSystemCheckerPickGpu" : "LOCSystemCheckerPickCpu";
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = true,
				ShowCloseButton = true,
				CanBeResizable = true,
				Width = 720,
				Height = 600,
				MinWidth = 500,
				MinHeight = 400
			};

			Window window = PlayniteUiHelper.CreateExtensionWindow(
				ResourceProvider.GetString(titleKey),
				view,
				windowOptions);

			viewModel.AttachWindow(window);

			bool? dialogResult = window.ShowDialog();
			return dialogResult == true ? viewModel.SelectedBenchmarkName : null;
		}
	}
}
