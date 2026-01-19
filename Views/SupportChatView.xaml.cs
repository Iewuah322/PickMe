using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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

using TaxiWPF.Models;
using TaxiWPF.ViewModels;

namespace TaxiWPF.Views
{
    public partial class SupportChatView : Window
    {
        public SupportChatView(User currentUser, SupportTicket ticket)
        {
            InitializeComponent();
            var viewModel = new SupportChatViewModel(currentUser, ticket);
            viewModel.RequestClose += () => this.Close();
            DataContext = viewModel;

            this.Closing += OnWindowClosing;

            if (viewModel.Messages is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += (s, e) =>
                {
                    MessagesScrollViewer.ScrollToBottom();
                };
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (DataContext is SupportChatViewModel vm)
            {
                vm.Cleanup();
            }
        }

        // --- ДОБАВЛЕНЫ НОВЫЕ МЕТОДЫ ---
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove(); // Позволяет перетаскивать окно
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Закрывает окно
        }
    }
}
