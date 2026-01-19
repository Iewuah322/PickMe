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

using TaxiWPF.ViewModels;

namespace TaxiWPF.Views
{
    /// <summary>
    /// Логика взаимодействия для PasswordRecoveryView.xaml
    /// </summary>
    public partial class PasswordRecoveryView : Window
    {
        public PasswordRecoveryView()
        {
            InitializeComponent();
            PasswordBox.PasswordChanged += OnPasswordChanged;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // --- ДОБАВЛЕНО ---
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is PasswordRecoveryViewModel viewModel)
            {
                viewModel.NewPassword = (sender as PasswordBox).Password;
            }
        }
    }
}
