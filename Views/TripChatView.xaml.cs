using System.Windows;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.ViewModels;

namespace TaxiWPF.Views
{
    public partial class TripChatView : Window
    {
        public TripChatView(User currentUser, Order order)
        {
            InitializeComponent();
            DataContext = new TripChatViewModel(currentUser, order);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TripChatViewModel vm)
            {
                vm.Cleanup();
            }

            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
