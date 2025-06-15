using System.Diagnostics;

namespace hastane.Services
{
    public class FlaskApiService : BackgroundService
    {
        private readonly ILogger<FlaskApiService> _logger;
        private Process? _flaskProcess;
        private readonly string _polTahminPath;

        public FlaskApiService(ILogger<FlaskApiService> logger)
        {
            _logger = logger;
            _polTahminPath = Path.Combine(Directory.GetCurrentDirectory(), "PolTahmin");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(2000, stoppingToken); // 2 saniye bekle
                StartFlaskApi();
                
                // API kapanana kadar bekle
                while (!stoppingToken.IsCancellationRequested && _flaskProcess != null && !_flaskProcess.HasExited)
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flask API servisinde hata oluştu");
            }
        }

        private void StartFlaskApi()
        {
            try
            {
                // Python ve gerekli dosyaları kontrol et
                if (!Directory.Exists(_polTahminPath))
                {
                    _logger.LogWarning("PolTahmin klasörü bulunamadı: {Path}", _polTahminPath);
                    return;
                }

                var apiFile = Path.Combine(_polTahminPath, "api.py");
                if (!File.Exists(apiFile))
                {
                    _logger.LogWarning("api.py dosyası bulunamadı: {Path}", apiFile);
                    return;
                }

                // Python process başlat - api.py kullan
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "api.py",
                    WorkingDirectory = _polTahminPath,
                    UseShellExecute = false,
                    CreateNoWindow = false, // Terminal penceresini göster
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                _flaskProcess = new Process { StartInfo = processStartInfo };
                _flaskProcess.Start();

                _logger.LogInformation("Flask API başlatıldı (PID: {ProcessId})", _flaskProcess.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flask API başlatılırken hata oluştu");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Flask API servisi durduruluyor...");
            
            if (_flaskProcess != null && !_flaskProcess.HasExited)
            {
                try
                {
                    _flaskProcess.Kill(true);
                    _flaskProcess.WaitForExit(5000);
                    _logger.LogInformation("Flask API durduruldu");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Flask API durdurulurken hata oluştu");
                }
            }

            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _flaskProcess?.Dispose();
            base.Dispose();
        }
    }
} 