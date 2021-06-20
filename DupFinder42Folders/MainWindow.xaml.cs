using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DupFinder42Folders
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    partial class MainWindow : Window
    {
        public enum EnumInteractionStep { SourceFolders, SearchOptions, SearchResults, SearchErrors, Actions };

        #region private fields
        private readonly MainViewModel vm;
        private bool allowTabChange = false;
        private object currentTab = null;
        #endregion

        #region constructor
        public MainWindow()
        {
            InitializeComponent();

            this.vm = this.DataContext as MainViewModel;

            // ensure first tab is selected
            TabControlMain.Items.CurrentChanged += TabItems_CurrentChanged;
            TabControlMain.Items.MoveCurrentToFirst();
            currentTab = TabControlMain.SelectedItem;
        }
        #endregion

        #region event handlers
        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            if (TabControlMain.SelectedIndex > 0)
            {
                allowTabChange = true; // make sure the tab change is allowed
                TabControlMain.SelectedIndex--; // move to previous tab
            }
        }
        private async void ButtonNext_Click(object sender, RoutedEventArgs e)
        {
            if (TabControlMain.SelectedIndex < TabControlMain.Items.Count - 1)
            {
                // First try to process operations for going to next step, return directly if failed
                if (await TryGoNext() == false) return;

                allowTabChange = true; // make sure the tab change is allowed
                TabControlMain.SelectedIndex++; // move to next tab
            }
        }
        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // enable/disable buttons based on the current selected tab
            if (ButtonBack != null) ButtonBack.IsEnabled = TabControlMain.SelectedIndex > 0;
            if (ButtonNext != null) ButtonNext.IsEnabled = TabControlMain.SelectedIndex < TabControlMain.Items.Count - 1;
        }
        private void ButtonSelectFolder1_Click(object sender, RoutedEventArgs e)
        {
            this.TextBoxSourceFolder1.Text = SelectFolder();
            BindingOperations.GetBindingExpression(TextBoxSourceFolder1, TextBox.TextProperty).UpdateSource();
        }
        private void ButtonSelectFolder2_Click(object sender, RoutedEventArgs e)
        {
            this.TextBoxSourceFolder2.Text = SelectFolder();
            BindingOperations.GetBindingExpression(TextBoxSourceFolder2, TextBox.TextProperty).UpdateSource();
        }
        private void TabItems_CurrentChanged(object sender, EventArgs e)
        {
            // only allow selecting new tab in code behide, not but user interaction
            if (allowTabChange)
            {
                allowTabChange = false;
                currentTab = TabControlMain.SelectedItem; // store new current tab
            }
            else
            {
                // reset to original tab if tab change is not allowed (i.e. caused by user interaction)
                Dispatcher.BeginInvoke(new Action(() => TabControlMain.Items.MoveCurrentTo(currentTab)));
            }
        }
        private void OnTreeViewItemMouseDoubleClick(object sender, MouseButtonEventArgs args)
        {
            if (!(sender is TreeViewItem tv) || tv.IsSelected == false) return;
            if (!(tv.DataContext is PathTreeViewItem item)) return;

            System.Diagnostics.Process.Start(item.Path);
        }
        #endregion

        #region private methods
        private string SelectFolder()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
                return dialog.FileName;

            return "";
        }
        private async Task<bool> TryGoNext()
        {
            // get the tab item and corresponding interaction step
            TabItem item = TabControlMain.SelectedItem as TabItem;
            if (item.Tag is EnumInteractionStep == false) return true;
            EnumInteractionStep step = (EnumInteractionStep)item.Tag;

            switch (step)
            {
                case EnumInteractionStep.SourceFolders:
                    if (vm.CheckSourceFolders() == false)
                    {
                        // figure out target control
                        UIElement targetControl = null;
                        if (vm.LastErrorPropertyName == nameof(vm.SourceFolder1)) targetControl = TextBoxSourceFolder1;
                        if (vm.LastErrorPropertyName == nameof(vm.SourceFolder2)) targetControl = TextBoxSourceFolder2;

                        // show popup error message
                        ShowPopupErrorMessage(targetControl, vm.LastErrorMessage);
                        return false;
                    }
                    break;

                case EnumInteractionStep.SearchOptions:
                    {
                        Binding progressBinding = new Binding
                        {
                            Source = vm,
                            Path = new PropertyPath("ProgressMessage"),
                            Mode = BindingMode.OneWay,
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                        };

                        ProgressDialog d = new ProgressDialog(
                            "Scanning files...", progressBinding, vm.CancelScanningFolders);
                        d.Owner = this;
                        var dialogTask = d.ShowDialogAsync();

                        await vm.ScanFolders();

                        d.FinishAndClose();
                        await dialogTask;
                    }
                    break;
            }

            return true;
        }
        void ShowPopupErrorMessage(UIElement target, string message)
        {
            TextBlockErrorMessage.Text = message;
            PopupErrorMessage.PlacementTarget = target;
            PopupErrorMessage.IsOpen = true;
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSourceFolder1.Text = "D:\\goldfish\\Downloads\\CrystalDiskInfo8_12_0";
            TextBoxSourceFolder2.Text = "D:\\goldfish\\Downloads\\OpenHardwareMonitor";
            BindingOperations.GetBindingExpression(TextBoxSourceFolder1, TextBox.TextProperty).UpdateSource();
            BindingOperations.GetBindingExpression(TextBoxSourceFolder2, TextBox.TextProperty).UpdateSource();
        }
    }
}
