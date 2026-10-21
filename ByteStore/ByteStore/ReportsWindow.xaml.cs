using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ByteStore
{
    public partial class ReportsWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private string _fullName;

        public ReportsWindow(string token, string fullName)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            InitializeComponent();

            _token = token;
            _fullName = fullName;

            txtUserInfo.Text = fullName;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            dpFrom.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpTo.SelectedDate = DateTime.Now;
        }

        private async void btnSales_Click(object sender, RoutedEventArgs e)
        {
            panelPeriod.Visibility = Visibility.Visible;

            await LoadSalesAsync();
        }

        private async void btnApplyPeriod_Click(object sender, RoutedEventArgs e)
        {
            await LoadSalesAsync();
        }

        private async Task LoadSalesAsync()
        {
            if (dpFrom.SelectedDate == null || dpTo.SelectedDate == null)
            {
                MessageBox.Show("Выберите период.");
                return;
            }

            try
            {
                string from = dpFrom.SelectedDate.Value.ToString("yyyy-MM-dd");
                string to = dpTo.SelectedDate.Value.ToString("yyyy-MM-dd");

                var response = await _http.GetAsync(
                    $"api/reports/sales?from={from}&to={to}");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Ошибка: {response.StatusCode}");
                    return;
                }

                JsonElement data = await response.Content.ReadFromJsonAsync<JsonElement>();

                int orderCount = data.GetProperty("orderCount").GetInt32();
                decimal totalAmount = data.GetProperty("totalAmount").GetDecimal();

                var list = new List<object>();
                JsonElement items = data.GetProperty("items");

                foreach (JsonElement it in items.EnumerateArray())
                {
                    list.Add(new
                    {
                        Товар = it.GetProperty("productName").GetString(),
                        Количество = it.GetProperty("totalQuantity").GetInt32(),
                        Выручка = it.GetProperty("totalRevenue").GetDecimal()
                    });
                }

                dgReport.ItemsSource = list;

                txtSummary.Text = $"Заказов за период: {orderCount}   |   " +
                                  $"Общая сумма: {totalAmount:N2} ₽   |   " +
                                  $"Позиций в отчёте: {list.Count}";
                txtSummary.Foreground = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnLowStock_Click(object sender, RoutedEventArgs e)
        {
            panelPeriod.Visibility = Visibility.Collapsed;

            try
            {
                var response = await _http.GetAsync("api/reports/low-stock");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Ошибка: {response.StatusCode}");
                    return;
                }

                JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();

                var list = new List<object>();

                foreach (JsonElement p in arr.EnumerateArray())
                {
                    list.Add(new
                    {
                        ID = p.GetProperty("id").GetInt32(),
                        Артикул = p.GetProperty("article").GetString(),
                        Название = p.GetProperty("name").GetString(),
                        Остаток = p.GetProperty("quantity").GetInt32()
                    });
                }

                dgReport.ItemsSource = list;

                txtSummary.Text = $"Товаров на исходе: {list.Count}";
                txtSummary.Foreground = list.Count > 0 ? Brushes.OrangeRed : Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnTopProducts_Click(object sender, RoutedEventArgs e)
        {
            panelPeriod.Visibility = Visibility.Collapsed;

            try
            {
                var response = await _http.GetAsync("api/reports/top-products");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Ошибка: {response.StatusCode}");
                    return;
                }

                JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();

                var list = new List<object>();
                int rank = 1;

                foreach (JsonElement p in arr.EnumerateArray())
                {
                    list.Add(new
                    {
                        Место = rank++,
                        Товар = p.GetProperty("productName").GetString(),
                        Продано = p.GetProperty("totalSold").GetInt32(),
                        Выручка = p.GetProperty("totalRevenue").GetDecimal()
                    });
                }

                dgReport.ItemsSource = list;

                txtSummary.Text = $"Топ-{list.Count} товаров по продажам";
                txtSummary.Foreground = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}