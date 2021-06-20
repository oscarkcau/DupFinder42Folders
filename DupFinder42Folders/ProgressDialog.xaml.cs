using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DupFinder42Folders
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        bool isFinished = false;
        Action cancelAction = null;

        public ProgressDialog(string taskName = null, Binding progressBinding = null, Action cancelAction = null)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(taskName))
                this.TextBlockTask.Text = taskName;

            if (progressBinding != null)
                BindingOperations.SetBinding(TextBlockProgress, TextBlock.TextProperty, progressBinding);

            this.cancelAction = cancelAction;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isFinished) return;

            var result = MessageBox.Show(this, "Really cancel?", "Warning", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
            }
            else
            {
                if (cancelAction != null) cancelAction();
            }
        }

        public async Task<bool?> ShowDialogAsync()
        {
            await Task.Yield();
            return ShowDialog();
        }
        public void FinishAndClose()
        {
            isFinished = true;
            Close();
        }
    }
}
