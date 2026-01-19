using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaxiWPF.Views;


namespace TaxiWPF.ViewModels
{
    public class RoleSelectionViewModel
    {
        public ICommand OpenClientViewCommand { get; }
        public ICommand OpenDriverViewCommand { get; }

        public RoleSelectionViewModel()
        {
            OpenClientViewCommand = new RelayCommand(OpenClientView);
            OpenDriverViewCommand = new RelayCommand(OpenDriverView);
        }

        private void OpenClientView()
        {
            ClientView clientView = new ClientView();
            clientView.Show();
            // Тут можно добавить логику закрытия текущего окна, если нужно
        }

        private void OpenDriverView()
        {
            DriverView driverView = new DriverView();
            driverView.Show();
            // Тут можно добавить логику закрытия текущего окна
        }
    }
}
