using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ByteStore.Models;
using Microsoft.Win32;

namespace ByteStore
{
    public partial class ProductFormWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private int? _productId;
        private string _selectedPhotoPath;

        public ProductFormWindow(string token, int? productId)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            InitializeComponent();

            _token = token;
            _productId = productId;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            if (_productId.HasValue)
            {
                txtTitle.Text = "Редактировать товар";
            }
            else
            {
                txtTitle.Text = "Добавить товар";
            }

            Loaded += async (s, e) =>
            {
                await LoadDictionariesAsync();

                if (_productId.HasValue)
                {
                    await LoadProductAsync();
                }
            };
        }

        private async Task LoadDictionariesAsync()
        {
            var catResp = await _http.GetAsync("api/categories");
            if (catResp.IsSuccessStatusCode)
            {
                List<Category> cats = await catResp.Content.ReadFromJsonAsync<List<Category>>();
                cmbCategory.ItemsSource = cats;
                cmbCategory.DisplayMemberPath = "Name";
                cmbCategory.SelectedValuePath = "Id";
            }

            var manResp = await _http.GetAsync("api/manufacturers");
            if (manResp.IsSuccessStatusCode)
            {
                List<Manufacturer> mans = await manResp.Content.ReadFromJsonAsync<List<Manufacturer>>();
                cmbManufacturer.ItemsSource = mans;
                cmbManufacturer.DisplayMemberPath = "CompanyName";
                cmbManufacturer.SelectedValuePath = "Id";
            }

            var supResp = await _http.GetAsync("api/suppliers");
            if (supResp.IsSuccessStatusCode)
            {
                List<Supplier> sups = await supResp.Content.ReadFromJsonAsync<List<Supplier>>();
                cmbSupplier.ItemsSource = sups;
                cmbSupplier.DisplayMemberPath = "CompanyName";
                cmbSupplier.SelectedValuePath = "Id";
            }
        }

        private async Task LoadProductAsync()
        {
            var response = await _http.GetAsync($"api/products/{_productId}");

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Не удалось загрузить товар.");
                return;
            }

            JsonElement p = await response.Content.ReadFromJsonAsync<JsonElement>();

            txtArticle.Text = p.GetProperty("article").GetString();
            txtName.Text = p.GetProperty("name").GetString();
            txtPurchasePrice.Text = p.GetProperty("purchasePrice").GetDecimal().ToString();
            txtSalePrice.Text = p.GetProperty("salePrice").GetDecimal().ToString();
            txtQuantity.Text = p.GetProperty("quantity").GetInt32().ToString();

            cmbCategory.SelectedValue = p.GetProperty("categoryId").GetInt32();

            if (p.GetProperty("manufacturerId").ValueKind != JsonValueKind.Null)
            {
                cmbManufacturer.SelectedValue = p.GetProperty("manufacturerId").GetInt32();
            }

            if (p.GetProperty("supplierId").ValueKind != JsonValueKind.Null)
            {
                cmbSupplier.SelectedValue = p.GetProperty("supplierId").GetInt32();
            }

            if (p.GetProperty("color").ValueKind != JsonValueKind.Null)
            {
                txtColor.Text = p.GetProperty("color").GetString();
            }

            if (p.GetProperty("warrantyMonths").ValueKind != JsonValueKind.Null)
            {
                txtWarranty.Text = p.GetProperty("warrantyMonths").GetInt32().ToString();
            }

            if (p.GetProperty("description").ValueKind != JsonValueKind.Null)
            {
                txtDescription.Text = p.GetProperty("description").GetString();
            }

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
                catch { }
            }
        }

        private void btnChoosePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png";
            dialog.Title = "Выберите фото";

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            _selectedPhotoPath = dialog.FileName;

            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(_selectedPhotoPath);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();

                imgPhoto.Source = bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtArticle.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                cmbCategory.SelectedValue == null)
            {
                MessageBox.Show("Заполните артикул, название и категорию.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPurchasePrice.Text, out decimal purchasePrice) ||
                !decimal.TryParse(txtSalePrice.Text, out decimal salePrice) ||
                !int.TryParse(txtQuantity.Text, out int quantity))
            {
                MessageBox.Show("Проверьте цены и количество.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? warranty = null;
            if (!string.IsNullOrWhiteSpace(txtWarranty.Text))
            {
                if (int.TryParse(txtWarranty.Text, out int w))
                {
                    warranty = w;
                }
            }

            var data = new
            {
                article = txtArticle.Text.Trim(),
                name = txtName.Text.Trim(),
                categoryId = (int)cmbCategory.SelectedValue,
                manufacturerId = cmbManufacturer.SelectedValue,
                supplierId = cmbSupplier.SelectedValue,
                purchasePrice = purchasePrice,
                salePrice = salePrice,
                quantity = quantity,
                color = string.IsNullOrWhiteSpace(txtColor.Text) ? null : txtColor.Text.Trim(),
                warrantyMonths = warranty,
                description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim()
            };

            try
            {
                HttpResponseMessage response;

                if (_productId.HasValue)
                {
                    response = await _http.PutAsJsonAsync($"api/products/{_productId}", data);
                }
                else
                {
                    response = await _http.PostAsJsonAsync("api/products", data);
                }

                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Ошибка {response.StatusCode}: {err}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int savedId = _productId ?? 0;

                if (!_productId.HasValue)
                {
                    JsonElement created = await response.Content.ReadFromJsonAsync<JsonElement>();
                    savedId = created.GetProperty("id").GetInt32();
                }

                if (!string.IsNullOrEmpty(_selectedPhotoPath))
                {
                    await UploadPhotoAsync(savedId, _selectedPhotoPath);
                }

                MessageBox.Show("Товар сохранён.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UploadPhotoAsync(int productId, string filePath)
        {
            using (var form = new MultipartFormDataContent())
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                form.Add(fileContent, "photo", Path.GetFileName(filePath));

                await _http.PostAsync($"api/products/{productId}/photo", form);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}