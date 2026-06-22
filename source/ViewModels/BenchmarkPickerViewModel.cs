using CommonPluginsShared;
using CommonPluginsShared.Commands;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.ViewModels
{
	/// <summary>
	/// View model for selecting a PassMark benchmark entry (CPU or GPU) from the cached catalog.
	/// </summary>
	public class BenchmarkPickerViewModel : ObservableObject
	{
		private const int MaxVisibleItems = 300;

		private readonly bool _isGpu;
		private List<BenchmarkData> _allItems = new List<BenchmarkData>();
		private Window _hostWindow;

		public BenchmarkPickerViewModel(bool isGpu, string suggestFromRawName)
		{
			_isGpu = isGpu;
			FilteredItems = new ObservableCollection<BenchmarkData>();
			IsLoading = true;

			CmdConfirm = new RelayCommand(Confirm);
			CmdCancel = new RelayCommand(Cancel);

			Task.Run(() => LoadData(suggestFromRawName));
		}

		/// <summary>Gets the benchmark name chosen by the user, or <c>null</c> when the dialog was cancelled.</summary>
		public string SelectedBenchmarkName => SelectedItem?.Name;

		public RelayCommand CmdConfirm { get; }

		public RelayCommand CmdCancel { get; }

		private bool _isLoading;

		/// <summary>Gets or sets a value indicating whether the benchmark catalog is still loading.</summary>
		public bool IsLoading
		{
			get => _isLoading;
			private set => SetValue(ref _isLoading, value);
		}

		private string _searchText = string.Empty;

		/// <summary>Gets or sets the search filter applied to the benchmark list.</summary>
		public string SearchText
		{
			get => _searchText;
			set
			{
				if (_searchText != value)
				{
					_searchText = value;
					OnPropertyChanged();
					RefreshFilteredItems();
				}
			}
		}

		private ObservableCollection<BenchmarkData> _filteredItems;

		/// <summary>Gets the filtered benchmark entries displayed in the list.</summary>
		public ObservableCollection<BenchmarkData> FilteredItems
		{
			get => _filteredItems;
			private set => SetValue(ref _filteredItems, value);
		}

		private BenchmarkData _selectedItem;

		/// <summary>Gets or sets the currently selected benchmark entry.</summary>
		public BenchmarkData SelectedItem
		{
			get => _selectedItem;
			set
			{
				SetValue(ref _selectedItem, value);
			}
		}

		/// <summary>Associates the modal host window used to close the picker dialog.</summary>
		/// <param name="hostWindow">The extension window hosting this picker.</param>
		public void AttachWindow(Window hostWindow)
		{
			_hostWindow = hostWindow;
		}

		private void LoadData(string suggestFromRawName)
		{
			try
			{
				Benchmark benchmark = new Benchmark();
				List<BenchmarkData> items = _isGpu
					? benchmark.GetGpuBenchmarkEntries().ToList()
					: benchmark.GetCpuBenchmarkEntries().ToList();

				BenchmarkData suggested = _isGpu
					? benchmark.SuggestGpuMatch(suggestFromRawName)
					: benchmark.SuggestCpuMatch(suggestFromRawName);

				Application.Current.Dispatcher.Invoke(() =>
				{
					_allItems = items ?? new List<BenchmarkData>();

					if (suggested != null)
					{
						SelectedItem = suggested;
						_searchText = suggested.Name;
						OnPropertyChanged(nameof(SearchText));
					}

					RefreshFilteredItems();
					IsLoading = false;
				});
			}
			catch (Exception ex)
			{
				Application.Current.Dispatcher.Invoke(() => IsLoading = false);
				Common.LogError(ex, false, "BenchmarkPickerViewModel.LoadData", false, SystemChecker.PluginName);
			}
		}

		private void RefreshFilteredItems()
		{
			if (_allItems == null || _allItems.Count == 0)
			{
				FilteredItems = new ObservableCollection<BenchmarkData>();
				return;
			}

			IEnumerable<BenchmarkData> query = _allItems;
			string term = (SearchText ?? string.Empty).Trim();

			if (!string.IsNullOrEmpty(term))
			{
				query = _allItems.Where(x =>
					x.Name != null &&
					x.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
			}

			List<BenchmarkData> visible = query.Take(MaxVisibleItems).ToList();
			FilteredItems = new ObservableCollection<BenchmarkData>(visible);

			if (SelectedItem != null && !visible.Contains(SelectedItem))
			{
				BenchmarkData stillSelected = visible.FirstOrDefault(x =>
					string.Equals(x.Name, SelectedItem.Name, StringComparison.OrdinalIgnoreCase));
				if (stillSelected != null)
				{
					SelectedItem = stillSelected;
				}
			}
		}

		private void Confirm()
		{
			if (SelectedItem == null || _hostWindow == null)
			{
				return;
			}

			_hostWindow.DialogResult = true;
			_hostWindow.Close();
		}

		private void Cancel()
		{
			if (_hostWindow == null)
			{
				return;
			}

			_hostWindow.DialogResult = false;
			_hostWindow.Close();
		}
	}
}
