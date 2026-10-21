using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ByteStore
{
    public partial class ProductProfileWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private int _productId;

        public ProductProfileWindow(string token, int productId)
        {
            InitializeComponent();

            _token = token;
            _productId = productId;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            Loaded += async (s, e) => await LoadProductAsync();
        }

        private async Task LoadProductAsync()
        {
            try
            {
                var response = await _http.GetAsync($"api/products/{_productId}/full");

                if (!response.IsSuccessStatusCode)
                {
                    txtMessage.Text = $"Ошибка загрузки: {response.StatusCode}";
                    return;
                }

                JsonElement p = await response.Content.ReadFromJsonAsync<JsonElement>();

                string name = p.GetProperty("name").GetString() ?? "";
                txtTitle.Text = name;
                txtName.Text = name;

                txtArticle.Text = "Артикул: " + (p.GetProperty("article").GetString() ?? "");

                txtCategory.Text = "Категория: " + (p.GetProperty("categoryName").GetString() ?? "—");
                txtManufacturer.Text = "Производитель: " + (p.GetProperty("manufacturerName").GetString() ?? "—");
                txtSupplier.Text = "Поставщик: " + (p.GetProperty("supplierName").GetString() ?? "—");

                decimal purchasePrice = p.GetProperty("purchasePrice").GetDecimal();
                decimal salePrice = p.GetProperty("salePrice").GetDecimal();
                txtPurchasePrice.Text = $"{purchasePrice:N2} ₽";
                txtSalePrice.Text = $"{salePrice:N2} ₽";

                int quantity = p.GetProperty("quantity").GetInt32();
                txtQuantity.Text = $"{quantity} шт.";

                txtColor.Text = p.GetProperty("color").ValueKind == JsonValueKind.Null
                    ? "—"
                    : p.GetProperty("color").GetString();

                if (p.GetProperty("warrantyMonths").ValueKind == JsonValueKind.Null)
                {
                    txtWarranty.Text = "—";
                }
                else
                {
                    txtWarranty.Text = p.GetProperty("warrantyMonths").GetInt32() + " мес.";
                }

                if (p.GetProperty("description").ValueKind == JsonValueKind.Null)
                {
                    txtDescription.Text = "Описание отсутствует.";
                }
                else
                {
                    txtDescription.Text = p.GetProperty("description").GetString();
                }

                DateTime createdAt = p.GetProperty("createdAt").GetDateTime();
                txtCreatedAt.Text = createdAt.ToString("dd.MM.yyyy");

                if (p.GetProperty("photoPath").ValueKind != JsonValueKind.Null)
                {
                    string photoPath = p.GetProperty("photoPath").GetString();
                    string fullUrl = "https://localhost:7121" + photoPath;

                    try
                    {
                        BitmapImage bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(fullUrl, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();

                        imgPhoto.Source = bmp;
                    }
                    catch
                    {
                    }
                }

                txtTotalSold.Text = p.GetProperty("totalSold").GetInt32() + " шт.";
                txtTotalRevenue.Text = p.GetProperty("totalRevenue").GetDecimal().ToString("N2") + " ₽";
                txtOrderCount.Text = p.GetProperty("orderCount").GetInt32().ToString();

                JsonElement orders = p.GetProperty("orders");

                if (orders.GetArrayLength() == 0)
                {
                    dgOrders.Visibility = Visibility.Collapsed;
                    txtNoOrders.Visibility = Visibility.Visible;
                }
                else
                {
                    var list = new System.Collections.Generic.List<object>();

                    foreach (JsonElement o in orders.EnumerateArray())
                    {
                        list.Add(new
                        {
                            Id = o.GetProperty("id").GetInt32(),
                            OrderNumber = o.GetProperty("orderNumber").GetString(),
                            OrderDate = o.GetProperty("orderDate").GetDateTime(),
                            Status = o.GetProperty("status").GetString(),
                            TotalAmount = o.GetProperty("totalAmount").GetDecimal()
                        });
                    }

                    dgOrders.ItemsSource = list;
                }

                txtMessage.Text = "Данные загружены.";
                txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnUploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png";
            dialog.Title = "Выберите фото товара";

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                using (var form = new MultipartFormDataContent())
                {
                    byte[] fileBytes = File.ReadAllBytes(dialog.FileName);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                    form.Add(fileContent, "photo", Path.GetFileName(dialog.FileName));

                    var response = await _http.PostAsync(
                        $"api/products/{_productId}/photo", form);

                    if (response.IsSuccessStatusCode)
                    {
                        txtMessage.Text = "Фото загружено.";
                        txtMessage.Foreground = System.Windows.Media.Brushes.LightGreen;

                        await LoadProductAsync();
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show(error, "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            ProductFormWindow form = new ProductFormWindow(_token, _productId);
            form.ShowDialog();

            await LoadProductAsync();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}