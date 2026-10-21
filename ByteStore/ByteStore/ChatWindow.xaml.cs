using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ByteStore
{
    public partial class ChatWindow : Window
    {
        private readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7121/")
        };

        private string _token;
        private int _myUserId;
        private int _selectedUserId;
        private string _selectedUserName = "";
        private DispatcherTimer _timer;
        private bool _isLoadingDialogs = false;

        public ChatWindow(string token, int myUserId)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) => true;

            InitializeComponent();

            _token = token;
            _myUserId = myUserId;

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);

            Loaded += async (s, e) =>
            {
                await LoadUsersAsync();
                await LoadDialogsAsync();
            };

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += async (s, e) => await ReloadDialogAsync();
            _timer.Start();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/messages/users");

                if (response.IsSuccessStatusCode)
                {
                    JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var list = new List<object>();

                    foreach (JsonElement u in arr.EnumerateArray())
                    {
                        list.Add(new
                        {
                            Id = u.GetProperty("id").GetInt32(),
                            FullName = u.GetProperty("fullName").GetString(),
                            Role = u.GetProperty("role").GetString()
                        });
                    }

                    cmbUsers.ItemsSource = list;
                    cmbUsers.DisplayMemberPath = "FullName";
                    cmbUsers.SelectedValuePath = "Id";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task LoadDialogsAsync()
        {
            _isLoadingDialogs = true;

            try
            {
                var response = await _http.GetAsync("api/messages/dialogs");

                if (response.IsSuccessStatusCode)
                {
                    JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var list = new List<object>();

                    foreach (JsonElement d in arr.EnumerateArray())
                    {
                        int unread = d.GetProperty("unreadCount").GetInt32();

                        list.Add(new
                        {
                            UserId = d.GetProperty("userId").GetInt32(),
                            FullName = d.GetProperty("fullName").GetString(),
                            LastMessage = d.GetProperty("lastMessage").GetString(),
                            LastSentAt = d.GetProperty("lastSentAt").GetDateTime(),
                            UnreadCount = unread,
                            UnreadVisibility = unread > 0 ? Visibility.Visible : Visibility.Collapsed
                        });
                    }

                    lstDialogs.ItemsSource = list;
                }
            }
            catch { }

            _isLoadingDialogs = false;
        }

        private async void cmbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUsers.SelectedValue == null)
            {
                return;
            }

            _selectedUserId = (int)cmbUsers.SelectedValue;
            _selectedUserName = cmbUsers.Text;

            txtChatWith.Text = "Чат с " + _selectedUserName;

            await ReloadDialogAsync();
        }

        private async void lstDialogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingDialogs)
            {
                return;
            }

            dynamic selected = lstDialogs.SelectedItem;

            if (selected == null)
            {
                return;
            }

            _selectedUserId = (int)selected.GetType().GetProperty("UserId").GetValue(selected);
            _selectedUserName = (string)selected.GetType().GetProperty("FullName").GetValue(selected);

            txtChatWith.Text = "Чат с " + _selectedUserName;

            cmbUsers.SelectedValue = null;

            await ReloadDialogAsync();
        }

        private async Task ReloadDialogAsync()
        {
            if (_selectedUserId == 0)
            {
                return;
            }

            try
            {
                var response = await _http.GetAsync($"api/messages/with/{_selectedUserId}");

                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                JsonElement arr = await response.Content.ReadFromJsonAsync<JsonElement>();
                var list = new List<object>();

                foreach (JsonElement m in arr.EnumerateArray())
                {
                    bool isMine = m.GetProperty("isMine").GetBoolean();

                    string senderName = isMine ? "Вы" : _selectedUserName;

                    list.Add(new
                    {
                        Id = m.GetProperty("id").GetInt32(),
                        Text = m.GetProperty("text").GetString(),
                        SentAt = m.GetProperty("sentAt").GetDateTime(),
                        IsMine = isMine,
                        SenderName = senderName,
                        Align = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left
                    });
                }

                int count = list.Count;
                lstMessages.ItemsSource = list;

                if (count > 0)
                {
                    lstMessages.ScrollIntoView(list[count - 1]);
                }

                await LoadDialogsAsync();
            }
            catch { }
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            if (_selectedUserId == 0)
            {
                MessageBox.Show("Выберите получателя.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string text = txtMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                var body = new
                {
                    receiverId = _selectedUserId,
                    text = text
                };

                var response = await _http.PostAsJsonAsync("api/messages", body);

                if (response.IsSuccessStatusCode)
                {
                    txtMessage.Text = "";
                    await ReloadDialogAsync();
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(err, "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            this.Close();
        }
    }
}