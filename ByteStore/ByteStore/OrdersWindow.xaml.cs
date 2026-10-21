using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ByteStore
{
    public partial class OrdersWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private string _fullName;
        private List<OrderItemViewModel> _allOrders = new List<OrderItemViewModel>();

        public OrdersWindow(string token, string fullName)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            InitializeComponent();

            _token = token;
            _fullName = fullName;

            txtUserInfo.Text = fullName;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            Loaded += async (s, e) => await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/orders");

                if (response.IsSuccessStatusCode)
                {
                    JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();

                    _allOrders = new List<OrderItemViewModel>();

                    foreach (JsonElement o in arr.EnumerateArray())
                    {
                        _allOrders.Add(new OrderItemViewModel
                        {
                            Id = o.GetProperty("id").GetInt32(),
                            OrderNumber = o.GetProperty("orderNumber").GetString(),
                            OrderDate = o.GetProperty("orderDate").GetDateTime(),
                            CustomerName = o.GetProperty("customerName").GetString(),
                            SellerName = o.GetProperty("sellerName").GetString(),
                            TotalAmount = o.GetProperty("totalAmount").GetDecimal(),
                            Status = o.GetProperty("status").GetString()
                        });
                    }

                    ApplyFilters();

                    txtMessage.Text = $"Загружено заказов: {_allOrders.Count}";
                    txtMessage.Foreground = Brushes.LightGreen;
                }
                else
                {
                    txtMessage.Text = $"Ошибка загрузки: {response.StatusCode}";
                    txtMessage.Foreground = Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            dgAll.ItemsSource = _allOrders;
            dgNew.ItemsSource = _allOrders.Where(o => o.Status == "Новый").ToList();
            dgPaid.ItemsSource = _allOrders.Where(o => o.Status == "Оплачен").ToList();
            dgShipped.ItemsSource = _allOrders.Where(o => o.Status == "Отправлен").ToList();
            dgCompleted.ItemsSource = _allOrders.Where(o => o.Status == "Завершён").ToList();
            dgCancelled.ItemsSource = _allOrders.Where(o => o.Status == "Отменён").ToList();
        }

        private DataGrid GetCurrentGrid()
        {
            if (tabControl.SelectedIndex == 0) return dgAll;
            if (tabControl.SelectedIndex == 1) return dgNew;
            if (tabControl.SelectedIndex == 2) return dgPaid;
            if (tabControl.SelectedIndex == 3) return dgShipped;
            if (tabControl.SelectedIndex == 4) return dgCompleted;
            return dgCancelled;
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void dg_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is OrderItemViewModel selected)
            {
                ShowOrderDetails(selected.Id);
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadOrdersAsync();
        }

        private async void btnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            var grid = GetCurrentGrid();
            var selected = grid.SelectedItem as OrderItemViewModel;

            if (selected == null)
            {
                MessageBox.Show("Выберите заказ.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Window dlg = new Window
            {
                Title = "Смена статуса",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = Brushes.DarkSlateGray,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock lbl = new TextBlock
            {
                Text = "Выберите новый статус:",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };

            ComboBox cmb = new ComboBox
            {
                Height = 35,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15)
            };

            cmb.Items.Add("Новый");
            cmb.Items.Add("Оплачен");
            cmb.Items.Add("Отправлен");
            cmb.Items.Add("Завершён");
            cmb.Items.Add("Отменён");
            cmb.SelectedIndex = 0;

            Button btnOk = new Button
            {
                Content = "СОХРАНИТЬ",
                Height = 40,
                Background = Brushes.LightGreen,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            btnOk.Click += async (s, args) =>
            {
                string newStatus = cmb.SelectedItem as string;

                if (string.IsNullOrEmpty(newStatus))
                {
                    return;
                }

                string jsonBody = "\"" + newStatus + "\"";
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var resp = await _http.PutAsync($"api/orders/{selected.Id}/status", content);

                if (resp.IsSuccessStatusCode)
                {
                    MessageBox.Show("Статус изменён.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    dlg.Close();
                    await LoadOrdersAsync();
                }
                else
                {
                    string err = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            panel.Children.Add(lbl);
            panel.Children.Add(cmb);
            panel.Children.Add(btnOk);
            dlg.Content = panel;

            dlg.ShowDialog();
        }

        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            var grid = GetCurrentGrid();
            var selected = grid.SelectedItem as OrderItemViewModel;

            if (selected == null)
            {
                MessageBox.Show("Выберите заказ.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowOrderDetails(selected.Id);
        }

        private async void ShowOrderDetails(int orderId)
        {
            var response = await _http.GetAsync($"api/orders/{orderId}");

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Ошибка загрузки заказа.");
                return;
            }

            JsonElement order = await response.Content.ReadFromJsonAsync<JsonElement>();

            string text = "";
            text += $"Заказ: {order.GetProperty("orderNumber").GetString()}\n";
            text += $"Дата: {order.GetProperty("orderDate").GetDateTime():dd.MM.yyyy HH:mm}\n";
            text += $"Покупатель: {order.GetProperty("customerName").GetString()}\n";
            text += $"Продавец: {order.GetProperty("sellerName").GetString()}\n";
            text += $"Статус: {order.GetProperty("status").GetString()}\n";
            text += $"Адрес: {order.GetProperty("deliveryAddress").GetString()}\n";
            text += $"Сумма: {order.GetProperty("totalAmount").GetDecimal():N2} ₽\n";
            text += "\n━━━ ПОЗИЦИИ ━━━\n\n";

            JsonElement items = order.GetProperty("items");

            foreach (JsonElement it in items.EnumerateArray())
            {
                text += $"• {it.GetProperty("productName").GetString()}\n";
                text += $"  {it.GetProperty("quantity").GetInt32()} шт. × ";
                text += $"{it.GetProperty("price").GetDecimal():N2} ₽ = ";
                text += $"{it.GetProperty("sum").GetDecimal():N2} ₽\n\n";
            }

            MessageBox.Show(text, "Подробности заказа",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var grid = GetCurrentGrid();
            var selected = grid.SelectedItem as OrderItemViewModel;

            if (selected == null)
            {
                MessageBox.Show("Выберите заказ для удаления.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить заказ {selected.OrderNumber}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var resp = await _http.DeleteAsync($"api/orders/{selected.Id}");

            if (resp.IsSuccessStatusCode)
            {
                MessageBox.Show("Заказ удалён.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
            else
            {
                string err = await resp.Content.ReadAsStringAsync();
                MessageBox.Show(err, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class OrderItemViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string SellerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}