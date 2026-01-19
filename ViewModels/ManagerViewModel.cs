using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TaxiWPF.Models;
using TaxiWPF.Repositories;
using System.Windows.Input; // <-- Добавьте это
using TaxiWPF.Views;
 

namespace TaxiWPF.ViewModels
{
    public class ManagerViewModel : INotifyPropertyChanged
    {
        private readonly SupportRepository _supportRepository;
        private SupportTicket _selectedTicket;
        private readonly User _managerUser; 
        public ICommand OpenChatCommand { get; }

        public ObservableCollection<SupportTicket> OpenTickets { get; set; }

        public SupportTicket SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                // Сбрасываем IsSelected у всех
                foreach (var ticket in OpenTickets)
                {
                    ticket.IsSelected = false;
                }
                
                _selectedTicket = value;
                
                // Устанавливаем IsSelected для выбранного
                if (_selectedTicket != null)
                {
                    _selectedTicket.IsSelected = true;
                }
                
                OnPropertyChanged();
                (OpenChatCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand RefreshTicketsCommand { get; }
        public ICommand ToggleProfilePanelCommand { get; }

        // Свойства для панели профиля
        private bool _isProfilePanelVisible = false;
        public bool IsProfilePanelVisible
        {
            get => _isProfilePanelVisible;
            set { _isProfilePanelVisible = value; OnPropertyChanged(); }
        }

        public User CurrentManager => _managerUser;

        public ManagerViewModel(User managerUser)
        {
            _managerUser = managerUser; // <-- Сохраняем пользователя-менеджера
            _supportRepository = new SupportRepository();
            OpenTickets = new ObservableCollection<SupportTicket>();

            OpenChatCommand = new RelayCommand(OpenChat, () => SelectedTicket != null); // <-- Инициализируем команду

            RefreshTicketsCommand = new RelayCommand(LoadOpenTickets);
            ToggleProfilePanelCommand = new RelayCommand(() => IsProfilePanelVisible = !IsProfilePanelVisible);

            LoadOpenTickets();
        }

        private void OpenChat()
        {
            var chatView = new SupportChatView(_managerUser, SelectedTicket);
            // ИЗМЕНЕНО: Открываем как обычное, неблокирующее окно
            chatView.Show();
        }

        private void LoadOpenTickets()
        {
            OpenTickets.Clear();
            var tickets = _supportRepository.GetOpenTickets();
            foreach (var ticket in tickets)
            {
                OpenTickets.Add(ticket);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
