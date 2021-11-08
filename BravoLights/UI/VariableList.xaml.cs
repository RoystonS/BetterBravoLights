using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BravoLights.Ast;
using BravoLights.Connections;
using BravoLights.Installation;

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
}
