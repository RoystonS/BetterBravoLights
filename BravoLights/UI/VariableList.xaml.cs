using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BravoLights.Ast;
using BravoLights.Connections;
using BravoLights.Installation;
using Microsoft.Xaml.Behaviors;

namespace BravoLights.UI
{
    /// <summary>
    /// Interaction logic for VariableList.xaml
    /// </summary>
    public partial class VariableList : Window
    {
        public VariableListViewModel ViewModel { get; private set; }

        private readonly List<DataDefinition> simVarList;

        public VariableList()
        {
            ViewModel = new VariableListViewModel();

            LVarManager.Connection.OnLVarListChanged += HandleLVarsChanged;
            InitializeComponent();

            var csvFile = new CsvFile();
            csvFile.Load(Path.Join(FlightSimulatorPaths.BetterBravoLightsPath, "KnownSimVars.csv"));
            simVarList = csvFile.Rows.Select(row =>
            {
                var variable = new SimVarExpression(row[1], row[2]);

                return new DataDefinition
                {
                    Group = row[0],
                    VariableName = row[1],
                    Units = row[2],
                    Description = row[3],
                    Variable = variable
                };
            }).ToList();

            HandleLVarsChanged(null, EventArgs.Empty);
        }

        private static IList<DataDefinition> GetLVarDefinitions()
        {
            return LVarManager.Connection.LVarList.Select(lvarName =>
            {
                var variable = new LvarExpression { LVarName = lvarName };

                return new DataDefinition
                {
                    Group = "LVar",
                    VariableName = lvarName,
                    Units = "",
                    Description = "",
                    Variable = variable
                };
            }).ToList();
        }

        private void HandleLVarsChanged(object sender, EventArgs e)
        {
            var list = simVarList.Concat(GetLVarDefinitions()).ToList();
            ViewModel.UpdateDefinitions(list);
        }

        /// <summary>
        /// Called when a row becomes visible on screen.
        /// </summary>
        private void DataGrid_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            var context = (Item)e.Row.DataContext;
            context.Subscribe();
        }

        /// <summary>
        /// Called when a row is no longer visible.
        /// </summary>
        private void DataGrid_UnloadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            var context = (Item)e.Row.DataContext;
            context.Unsubscribe();
        }
    }

    public class HighlightValueChangeBehavior : Behavior<TextBlock>
    {
        /// We don't highlight on _every_ value change, as that would cause us to highlight
        /// rows as you scroll around (because the UI reuses rows that scroll off the top, bringing
        /// them back in at the bottom, plus even a new row being _given_ a value would be a 'new' value).
        private bool animateOnChanges = false;
        private Item lastContext;

        // We animate (fairly slowly, so that one-off changes are very noticeable) from a reddish colour
        // to the natural row colour.
        private static readonly ColorAnimation HighlightAnimation = new ColorAnimation
        {
            AutoReverse = false,
            From = Colors.Salmon,
            FillBehavior = FillBehavior.Stop,
            Duration = new Duration(TimeSpan.FromMilliseconds(5000))
        };

        protected override void OnAttached()
        {
            AssociatedObject.Background = new SolidColorBrush(Colors.White);
            AssociatedObject.DataContextChanged += AssociatedObject_DataContextChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.DataContextChanged -= AssociatedObject_DataContextChanged;
            UnsubscribeFromLastContext();
        }

        private void AssociatedObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UnsubscribeFromLastContext();

            AssociatedObject.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);

            var newDataContext = e.NewValue;
            if (newDataContext is Item)
            {
                var item = (Item)newDataContext;
                lastContext = item;
                animateOnChanges = false;
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        private void UnsubscribeFromLastContext()
        {
            if (lastContext != null)
            {
                lastContext.PropertyChanged -= Item_PropertyChanged;
                lastContext = null;
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Item.Value))
            {
                // Something other than Value has changed.
                return;
            }

            // When we first subscribe to an item we _might_ get a 'No value yet from simulator' error then a proper value,
            // and the first value we get shouldn't cause a highlight animation.
            var newValue = ((Item)sender).Value;
            if (!(newValue is double))
            {
                // We don't have a genuine value.
                return;
            }

            if (!animateOnChanges)
            {
                // Animate on future changes, but not this one.
                animateOnChanges = true;
                return;
            }

            AssociatedObject.Background.BeginAnimation(SolidColorBrush.ColorProperty, HighlightAnimation);            
        }
    }
}