using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using BravoLights.Common;

namespace DCSBravoLights
{
    /// <summary>
    /// Interaction logic for DebuggerUI.xaml
    /// </summary>
    public partial class DebuggerUI : Window
    {
        private readonly DebuggerViewModel viewModel;

        public DebuggerUI()
        {
            viewModel = new DebuggerViewModel(VariablesManager.DcsBiosState);

            InitializeComponent();
        }

        private DcsVariablesManager VariablesManager
        {
            get { return DcsConnection.Connection.DcsVariablesManager; }
        }

        public DebuggerViewModel ViewModel
        {
            get { return viewModel; }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            VariablesManager.AircraftNameChanged += AircraftNameChanged;
            VariablesManager.DefinitionsChanged += DefinitionsChanged;
            
            viewModel.AircraftName = VariablesManager.AircraftName;
            DefinitionsChanged(null, EventArgs.Empty);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            viewModel.Dispose();
        }

        private void DefinitionsChanged(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync( () => { 
                viewModel.UpdateDefinitions(VariablesManager.DataDefinitions);
            });
        }

        private void AircraftNameChanged(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                viewModel.AircraftName = VariablesManager.AircraftName;
            });
        }
    }

    public class DebuggerViewModel : ViewModelBase, IDisposable
    {
        private readonly Dictionary<DataDefinition, Item> itemEntries = new();
        private readonly ObservableCollection<Item> items = new();
        
        private readonly DcsBiosState dcsBiosState;
        private readonly ICollectionView filteredView;

        public DebuggerViewModel(DcsBiosState dcsBiosState)
        {
            this.dcsBiosState = dcsBiosState;
            this.filteredView = CollectionViewSource.GetDefaultView(items);

        }

        public ICollectionView FilteredItems { get; private set; }

        public ObservableCollection<Item> Items
        {
            get { return items; }
        }

        public void UpdateDefinitions(IEnumerable<DataDefinition> definitions)
        {
            UnsubscribeAll();

            foreach (var def in definitions)
            {
                var item = new Item { DataDefinition = def, Value = "Not yet received from simulator" };
                itemEntries.Add(def, item);
                Items.Add(item);
                dcsBiosState.AddHandler(def, HandleValueChanged);
            }
        }

        private string aircraftName;
        public string AircraftName
        {
            get { return aircraftName; }
            set { SetProperty(ref aircraftName, value); }
        }

        private string filterText;
        public string FilterText
        {
            get { return filterText; }
            set
            {
                SetProperty(ref filterText, value);
                if (value.Length > 0)
                {
                    filteredView.Filter = (x) =>
                    {
                        var defn = ((Item)x).DataDefinition;
                        var variableName = defn.VariableName;
                        return variableName.DcsIdentifier.Contains(value, StringComparison.CurrentCultureIgnoreCase) ||
                            variableName.DcsCategory.Contains(value, StringComparison.CurrentCultureIgnoreCase) ||
                            defn.Description.Contains(value, StringComparison.CurrentCultureIgnoreCase);
                    };
                }
                else
                {
                    filteredView.Filter = null;
                }
                filteredView.Refresh();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            UnsubscribeAll();
        }

        private void UnsubscribeAll()
        {
            foreach (var item in itemEntries.Keys)
            {
                dcsBiosState.RemoveHandler(item, HandleValueChanged);
            }

            itemEntries.Clear();
            Items.Clear();
        }

        private void HandleValueChanged(object sender, ValueChangedEventArgs e)
        {
            var def = (DataDefinition)sender;

            if (itemEntries.TryGetValue(def, out var item))
            {
                item.Value = e.NewValue;
            }
        }
    }

    public class Item : ViewModelBase
    {
        private DataDefinition key;
        public DataDefinition DataDefinition
        {
            get { return key; }
            set { SetProperty(ref key, value); }
        }

        private object mValue;
        public object Value
        {
            get { return mValue; }
            set
            {
                SetProperty(ref mValue, value);
                RaisePropertyChanged(nameof(ValueText));
                RaisePropertyChanged(nameof(IsError));
            }
        }

        public string ValueText
        {
            get
            {
                if (mValue is Exception ex)
                {
                    return ex.Message;
                }
                return Convert.ToString(mValue);
            }
        }

        public bool IsError
        {
            get { return mValue is Exception; }
        }
    }
}
