using FileUploadTool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileUploadTool.ViewModels
{
    public class FileUploadViewModel : BaseViewModel
    {
        public ObservableCollection<FileUploadItem> Files { get; } = new();

        private int _currentIndex = 0;
        private CancellationTokenSource _cts;
        private bool _isPaused = false;
        private string _sessionId;
        private HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:44346/") };
        private bool _isCancelled = false;
        public ICommand SelectFilesCommand { get; }
        public ICommand StartUploadCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearCommand { get; }

        public FileUploadViewModel()
        {
            SelectFilesCommand = new RelayCommand(SelectFiles);
            StartUploadCommand = new RelayCommand(async () => await StartUploadAsync());
            PauseCommand = new RelayCommand(PauseUpload);
            ResumeCommand = new RelayCommand(async () => await ResumeUploadAsync());
            CancelCommand = new RelayCommand(CancelUpload);
            ClearCommand = new RelayCommand(ClearFiles);
        }

        public async Task InitUploadSessionAsync()
        {
            var payload = JsonSerializer.Serialize(Files.Count);
            var response = await _httpClient.PostAsync("FileUpload/init",
                new StringContent(payload, System.Text.Encoding.UTF8, "application/json")); response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            _sessionId = result["sessionId"];
        }
        private void SelectFiles()
        {
            var dlg = new OpenFileDialog { Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                Files.Clear();
                foreach (var file in dlg.FileNames)
                    Files.Add(new FileUploadItem { FilePath = file });
                _currentIndex = 0;
            }
        }

        private async Task StartUploadAsync()
        {
            _cts = new CancellationTokenSource();
            _isPaused = false;
            await UploadFilesAsync(_cts.Token);
        }
        private void PauseUpload()
        {
            _isPaused = true;
            _isCancelled = false;
            _cts?.Cancel();
            foreach (var file in Files.Where(f => f.Status == "Uploading" || f.Status == "Pending"))
                file.Status = "Paused";
        }
        private async Task ResumeUploadAsync1()
        {
            // Set all paused files to "Pending" so they are picked up by the upload loop
            foreach (var file in Files.Where(f => f.Status == "Paused" || f.Status == "Resumed"))
                file.Status = "Pending"; 
            if (string.IsNullOrEmpty(_sessionId)) return;
            var response = await _httpClient.GetAsync($"FileUpload/status?sessionId={_sessionId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, int[]>>(json);
            var uploaded = result["uploadedFiles"];

            for (int i = 0; i < Files.Count; i++)
            {
                if (uploaded.Contains(i))
                    Files[i].Status = "Uploaded";
                else if (Files[i].Status != "Cancelled" && Files[i].Status != "Failed")
                    Files[i].Status = "Pending";
            }
            _currentIndex = Files.ToList().FindIndex(f => f.Status == "Pending");
            if (_currentIndex == -1) return; // No files left to upload

            await UploadFilesAsync(_cts.Token);
        }
        private async Task ResumeUploadAsync()
        {
            foreach (var file in Files.Where(f => f.Status == "Paused" || f.Status == "Resumed"))
                file.Status = "Pending";

            if (string.IsNullOrEmpty(_sessionId)) return;

            var response = await _httpClient.GetAsync($"FileUpload/status?sessionId={_sessionId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, int[]>>(json);
            var uploaded = result["uploadedFiles"];

            for (int i = 0; i < Files.Count; i++)
            {
                if (uploaded.Contains(i))
                    Files[i].Status = "Uploaded";
                else if (Files[i].Status != "Cancelled" && Files[i].Status != "Failed")
                    Files[i].Status = "Pending";
            }
            _currentIndex = Files.ToList().FindIndex(f => f.Status == "Pending");
            if (_currentIndex == -1) return; // No files left to upload

            _cts = new CancellationTokenSource(); // <-- Ensure new token source
            await UploadFilesAsync(_cts.Token);
        }

        private async Task UploadFilesAsync(CancellationToken token)
        {
            if (string.IsNullOrEmpty(_sessionId))
                await InitUploadSessionAsync();

            for (; _currentIndex < Files.Count; _currentIndex++)
            {
                var file = Files[_currentIndex];

                // Only process files that are Pending
                if (file.Status != "Pending") continue;

                if (token.IsCancellationRequested)
                {
                    if (_currentIndex >= 0 && _currentIndex < Files.Count)
                    {
                        if (_isCancelled)
                            Files[_currentIndex].Status = "Cancelled";
                        else if (_isPaused)
                            Files[_currentIndex].Status = "Paused";
                    }
                    break;
                }
                
                file.Status = "Uploading";
                using var fileStream = File.OpenRead(file.FilePath);
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(_sessionId), nameof(FileUploadModel.SessionId));
                content.Add(new StringContent(_currentIndex.ToString()), nameof(FileUploadModel.FileIndex));
                content.Add(new StreamContent(fileStream), "File", Path.GetFileName(file.FilePath));
                try
                {
                    var response = await _httpClient.PostAsync("FileUpload/upload", content, token);
                    if (response.IsSuccessStatusCode)
                        file.Status = "Uploaded";
                    else
                        file.Status = "Failed";// Mark as failed
                                               // Optionally, log response.StatusCode and response.Content for diagnostics
                }
                catch (Exception ex)
                {
                    file.Status = "Failed";
                    // Optionally, log ex.Message for diagnostics
                }
            }
        }
        private void CancelUpload()
        {
            _isCancelled = true;
            _cts?.Cancel();
            foreach (var file in Files.Where(f => f.Status == "Uploading" || f.Status == "Pending" || f.Status == "Paused"))
                file.Status = "Cancelled";
        }
        private void ClearFiles()
        {
            Files.Clear();
        }
    }
}
