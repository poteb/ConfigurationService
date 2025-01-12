using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using pote.Config.Shared;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace pote.Config.ByteConverter.Client
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        private string _selectedEnvironment = "Development";
        private string _selectedApplication = "Goofy";
        private string _applicationConfigurationJson = "{\"config\":\"$ref:base-configuration#\"}";
        private string _applicationConfigurationBytes = string.Empty;
        private string _bytesFromApi = string.Empty;
        private string _humanReadableTextFromApi = string.Empty;
        private string _configServiceUrl = "http://swiist04:12121";
        private string _responseFromApi = string.Empty;

        public string SelectedEnvironment { get => _selectedEnvironment; set { _selectedEnvironment = value; OnPropertyChanged(); } }
        public string SelectedApplication { get => _selectedApplication; set { _selectedApplication = value; OnPropertyChanged(); } }
        public string ApplicationConfigurationJson { get => _applicationConfigurationJson; set { _applicationConfigurationJson = value; OnPropertyChanged(); } }
        public string ApplicationConfigurationBytes { get => _applicationConfigurationBytes; set { _applicationConfigurationBytes = value; OnPropertyChanged(); } }
        public string BytesFromApi { get => _bytesFromApi; set { _bytesFromApi = value; OnPropertyChanged(); } }
        public string HumanReadableTextFromApi { get => _humanReadableTextFromApi; set { _humanReadableTextFromApi = value; OnPropertyChanged(); } }
        public string ConfigServiceUrl { get => _configServiceUrl; set { _configServiceUrl = value; OnPropertyChanged(); } }
        public string ResponseFromApi { get => _responseFromApi; set { _responseFromApi = value; OnPropertyChanged(); } }
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void BCopyAppConfigBytes_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BConvertApplicationConfigurationToBytes_Click(object sender, RoutedEventArgs e)
        {
            var foo = new ParseRequest { InputJson = Encoding.ASCII.GetBytes(_applicationConfigurationJson), Application = SelectedApplication, Environment = SelectedEnvironment };
            ApplicationConfigurationBytes = JsonConvert.SerializeObject(foo);
        }

        private void BConvertApiResultToText_Click(object sender, RoutedEventArgs e)
        {
            var response = JsonConvert.DeserializeObject<ParseResponse>(_bytesFromApi);
            if (response?.OutputJson == null)
            {
                HumanReadableTextFromApi = "Not an output configuration";
                return;
            }
            HumanReadableTextFromApi = Encoding.ASCII.GetString(response.OutputJson);
        }

        private async void BTestAgainstConfigService_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync($"{_configServiceUrl}/Configuration", new StringContent(_applicationConfigurationBytes, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                var result = await System.Text.Json.JsonSerializer.DeserializeAsync<ParseResponse>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
                ResponseFromApi = result?.OutputJson == null ? "Not an output configuration" : Encoding.ASCII.GetString(result.OutputJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BCopyByteDataToTestTab_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => tc.SelectedIndex = 2));
        }
    }
}