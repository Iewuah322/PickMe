using System.Windows;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.ViewModels;

namespace TaxiWPF.Views
{
    public partial class TripDetailsView : Window
    {
        public TripDetailsView(User currentUser, Order order)
        {
            InitializeComponent();
            DataContext = new TripDetailsViewModel(currentUser, order);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
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
