using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using BravoLights.Common;

namespace BravoLights.UI
{

    /// <summary>
    /// Interaction logic for the variable list view.
    /// </summary>
    public class VariableListViewModel : ViewModelBase, IDisposable
    {
        private readonly Dictionary<DataDefinition, Item> itemEntries = new();
        private readonly ObservableCollection<Item> items = new();

        private readonly ICollectionView filteredView;

        public VariableListViewModel()
        {
            filteredView = CollectionViewSource.GetDefaultView(items);
        }

        /// <summary>
        /// Gets the items that should be displayed in the variable list.
        /// </summary>
        public ObservableCollection<Item> Items
        {
            get { return items; }
        }

        public void UpdateDefinitions(IEnumerable<DataDefinition> definitions)
        {
            UnsubscribeAll();

            foreach (var def in definitions)
            {
                var item = new Item(def);
                itemEntries.Add(def, item);
                Items.Add(item);
            }
        }

        private void UnsubscribeAll()
        {
            foreach (var item in itemEntries.Values)
            {
                item.Unsubscribe();
            }

            itemEntries.Clear();
            Items.Clear();
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
                        return variableName.Contains(value, StringComparison.CurrentCultureIgnoreCase) ||
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

        private int textSize = 12;
        public int TextSize
        {
            get { return textSize; }
            set
            {
                SetProperty(ref textSize, value);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            UnsubscribeAll();
        }
    }

    /// <summary>
    /// Holds the description of an available variable.
    /// </summary>
    public class DataDefinition
    {
        public string Group { get; set; }
        public string VariableName { get; set; }
        public string Units { get; set; }
        public string Description { get; set; }
        public IVariable Variable { get; set; }
    }

    /// <summary>
    /// Represents a variable and its current value.
    /// </summary>
    public class Item : ViewModelBase
    {
        private static readonly Exception NoValueReceivedYetException = new Exception("No value received yet");

        public Item(DataDefinition definition)
        {
            DataDefinition = definition;
            Value = NoValueReceivedYetException;
        }

        private DataDefinition key;
        public DataDefinition DataDefinition
        {
            get { return key; }
            private set { SetProperty(ref key, value); }
        }

        private object mValue;
        public object Value
        {
            get { return mValue; }
            private set
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


        private bool isSubscribed = false;

        public void Subscribe()
        {
            DataDefinition.Variable.ValueChanged += Variable_ValueChanged;
            isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if (isSubscribed)
            {
                DataDefinition.Variable.ValueChanged -= Variable_ValueChanged;

                // We deliberately don't advertise a property change, but we want to remember
                // that the current value is no longer valid.
                mValue = new Exception("Not subscribed");
                isSubscribed = false;
            }
        }

        private void Variable_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            Value = e.NewValue;
        }
    }

    public class ExpressionEditorViewModel : ViewModelBase
    {
        private string expressionText;
        public string ExpressionText
        {
            get { return expressionText; }
            set
            {
                SetProperty(ref expressionText, value);
            }
        }
    }
}
