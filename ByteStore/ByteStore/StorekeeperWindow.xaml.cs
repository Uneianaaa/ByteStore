using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ByteStore
{
    public partial class StorekeeperWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private string _fullName;

        public StorekeeperWindow(string token, string fullName)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            InitializeComponent();

            _token = token;
            _fullName = fullName;

            txtUserInfo.Text = fullName;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            Loaded += async (s, e) => await LoadStatsAsync();
        }

        private void txtUserInfo_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProfileWindow profile = new ProfileWindow(_token);
            profile.ShowDialog();
        }

        private void btnChat_Click(object sender, RoutedEventArgs e)
        {
            int myId = GetMyUserIdFromToken(_token);
            ChatWindow chat = new ChatWindow(_token, myId);
            chat.ShowDialog();
        }

        private int GetMyUserIdFromToken(string token)
        {
            try
            {
                string[] parts = token.Split('.');
                string payload = parts[1];

                int mod4 = payload.Length % 4;
                if (mod4 > 0)
                {
                    payload += new string('=', 4 - mod4);
                }

                byte[] data = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                string json = System.Text.Encoding.UTF8.GetString(data);

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("nameidentifier", out JsonElement idElement))
                    {
                        return int.Parse(idElement.GetString());
                    }

                    foreach (JsonProperty prop in root.EnumerateObject())
                    {
                        if (prop.Name.EndsWith("nameidentifier"))
                        {
                            return int.Parse(prop.Value.GetString());
                        }
                    }
                }
            }
            catch { }

            return 0;
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                var productsResp = await _http.GetAsync("api/products");

                if (productsResp.IsSuccessStatusCode)
                {
                    JsonElement arr = await productsResp.Content.ReadFromJsonAsync<JsonElement>();

                    int totalProducts = arr.GetArrayLength();
                    int totalUnits = 0;
                    decimal stockValue = 0;

                    foreach (JsonElement p in arr.EnumerateArray())
                    {
                        int quantity = p.GetProperty("quantity").GetInt32();
                        decimal purchasePrice = p.GetProperty("purchasePrice").GetDecimal();

                        totalUnits += quantity;
                        stockValue += quantity * purchasePrice;
                    }

                    txtTotalProducts.Text = totalProducts.ToString();
                    txtTotalUnits.Text = totalUnits.ToString();
                    txtStockValue.Text = $"{stockValue:N2} ₽";
                }

                var lowResp = await _http.GetAsync("api/reports/low-stock");
                if (lowResp.IsSuccessStatusCode)
                {
                    JsonElement arr = await lowResp.Content.ReadFromJsonAsync<JsonElement>();
                    txtLowStock.Text = arr.GetArrayLength().ToString();
                }

                txtMessage.Text = "Статистика загружена.";
                txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                txtMessage.Text = "Не удалось загрузить статистику.";
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnProducts_Click(object sender, RoutedEventArgs e)
        {
            CatalogWindow catalog = new CatalogWindow(_token, "Кладовщик", _fullName);
            catalog.Show();

            catalog.Closed += (s, args) => _ = LoadStatsAsync();
        }

        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            ReportsWindow reports = new ReportsWindow(_token, _fullName);
            reports.ShowDialog();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = new MainWindow();
            mainWin.Show();
            this.Close();
        }
    }
}