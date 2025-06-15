using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace hastane.Controllers
{
    public class PolTahminController : Controller
    {
        private readonly ILogger<PolTahminController> _logger;
        private static readonly string _polTahminPath = Path.Combine(Directory.GetCurrentDirectory(), "PolTahmin");
        private static readonly string _veriJsonPath = Path.Combine(_polTahminPath, "veri.json");
        private readonly HttpClient _httpClient;
        private const string FLASK_API_BASE_URL = "http://localhost:5001";

        // Ağır belirtiler (Flask API ile senkronize)
        private static readonly Dictionary<string, (string poliklinik, string mesaj)> _agirBelirtiler = new Dictionary<string, (string, string)>
        {
            {"Kalp Çarpıntısı", ("Kardiyoloji", "Kalp çarpıntısından ötürü acilen kardiyolojiye başvurmanız gerekmektedir.")},
            {"Solunum Güçlüğü", ("Göğüs Hastalıkları", "Solunum güçlüğünden ötürü acilen göğüs hastalıkları polikliniğine başvurmanız gerekmektedir.")},
            {"Şiddetli Baş Ağrısı", ("Nöroloji", "Şiddetli baş ağrısından ötürü acilen nörolojiye başvurmanız gerekmektedir.")},
            {"Ağır Karın Ağrısı", ("Gastroenteroloji", "Ağır karın ağrısından ötürü acilen gastroenteroloji polikliniğine başvurmanız gerekmektedir.")},
            {"Felç", ("Nöroloji", "Felç belirtileri tespit edilmiştir, acilen nöroloji polikliniğine başvurmanız gerekmektedir.")}
        };

        public PolTahminController(ILogger<PolTahminController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Flask API'den belirtileri al
                var belirtiler = await GetBelirtilerFromAPI();
                
                if (belirtiler == null)
                {
                    // API'den alınamazsa JSON dosyasından al
                    belirtiler = GetBelirtilerFromJson();
                }

                ViewBag.Belirtiler = belirtiler;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PolTahmin/Index sayfası yüklenirken hata oluştu");
                ViewBag.Belirtiler = new List<string>();
                ViewBag.ErrorMessage = "Belirtiler yüklenirken bir hata oluştu.";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Tahmin(string belirti1, string belirti2, string belirti3)
        {
            try
            {
                // Belirtileri temizle
                belirti1 = belirti1?.Trim() ?? "";
                belirti2 = belirti2?.Trim() ?? "";
                belirti3 = belirti3?.Trim() ?? "";

                if (string.IsNullOrEmpty(belirti1))
                {
                    TempData["ErrorMessage"] = "En az bir belirti seçmelisiniz!";
                    return RedirectToAction("Index");
                }

                // Flask API ile tahmin yap
                var tahminSonucu = await TahminYapAPI(belirti1, belirti2, belirti3);
                
                if (tahminSonucu != null)
                {
                    ViewBag.TahminSonucu = tahminSonucu;
                    ViewBag.ModelKullanildi = true;
                }
                else
                {
                    // API çalışmazsa yerel yöntemle tahmin yap
                    var belirtiler = new List<string> { belirti1, belirti2, belirti3 }
                        .Where(b => !string.IsNullOrEmpty(b))
                        .ToList();
                    
                    var yerelTahmin = TahminYap(belirtiler);
                    ViewBag.TahminSonucu = yerelTahmin;
                    ViewBag.ModelKullanildi = false;
                }

                // Belirtileri tekrar yükle
                var tumBelirtiler = await GetBelirtilerFromAPI() ?? GetBelirtilerFromJson();
                ViewBag.Belirtiler = tumBelirtiler;
                
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tahmin yapılırken hata oluştu");
                TempData["ErrorMessage"] = "Tahmin yapılırken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private async Task<List<string>> GetBelirtilerFromAPI()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{FLASK_API_BASE_URL}/api/belirtiler");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ApiResponse>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return apiResponse?.Belirtiler ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Flask API'den belirtiler alınırken hata oluştu");
            }
            
            return null;
        }

        private async Task<TahminSonucu> TahminYapAPI(string belirti1, string belirti2, string belirti3)
        {
            try
            {
                var requestData = new
                {
                    belirti1 = belirti1,
                    belirti2 = belirti2,
                    belirti3 = belirti3
                };

                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{FLASK_API_BASE_URL}/api/tahmin", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<TahminApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (apiResponse?.Success == true && apiResponse.Sonuc != null)
                    {
                        return new TahminSonucu
                        {
                            Poliklinik = apiResponse.Sonuc.Poliklinik,
                            Mesaj = apiResponse.Sonuc.Mesaj,
                            Acil = apiResponse.Sonuc.Acil
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Flask API ile tahmin yapılırken hata oluştu");
            }
            
            return null;
        }

        private List<string> GetBelirtilerFromJson()
        {
            try
            {
                if (System.IO.File.Exists(_veriJsonPath))
                {
                    var json = System.IO.File.ReadAllText(_veriJsonPath, Encoding.UTF8);
                    var poliklinikler = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                    
                    // Tüm polikliniklerden belirtileri topla ve tekil liste oluştur
                    var belirtiler = poliklinikler
                        .SelectMany(p => p.Value)
                        .Distinct()
                        .OrderBy(b => b)
                        .ToList();
                    
                    return belirtiler;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON dosyasından belirtiler okunurken hata oluştu");
            }
            
            return new List<string>();
        }

        private TahminSonucu TahminYap(List<string> belirtiler)
        {
            // Ağır belirtiler kontrolü
            foreach (var belirti in belirtiler)
            {
                if (_agirBelirtiler.ContainsKey(belirti))
                {
                    var (poliklinik, mesaj) = _agirBelirtiler[belirti];
                    return new TahminSonucu
                    {
                        Poliklinik = poliklinik,
                        Mesaj = mesaj,
                        Acil = true
                    };
                }
            }

            // Basit kural tabanlı tahmin (API çalışmazsa)
            var belirtiStr = string.Join(" ", belirtiler).ToLower();
            
            if (belirtiStr.Contains("göğüs") || belirtiStr.Contains("kalp") || belirtiStr.Contains("çarpıntı"))
                return new TahminSonucu { Poliklinik = "Kardiyoloji", Mesaj = "Kardiyoloji polikliniğine başvurmanız önerilir.", Acil = false };
            
            if (belirtiStr.Contains("baş") || belirtiStr.Contains("denge") || belirtiStr.Contains("unutkan"))
                return new TahminSonucu { Poliklinik = "Nöroloji", Mesaj = "Nöroloji polikliniğine başvurmanız önerilir.", Acil = false };
            
            if (belirtiStr.Contains("cilt") || belirtiStr.Contains("kaşıntı") || belirtiStr.Contains("döküntü"))
                return new TahminSonucu { Poliklinik = "Dermatoloji", Mesaj = "Dermatoloji polikliniğine başvurmanız önerilir.", Acil = false };
            
            if (belirtiStr.Contains("göz") || belirtiStr.Contains("görme"))
                return new TahminSonucu { Poliklinik = "Göz Hastalıkları", Mesaj = "Göz Hastalıkları polikliniğine başvurmanız önerilir.", Acil = false };
            
            // Varsayılan
            return new TahminSonucu 
            { 
                Poliklinik = "Dahiliye", 
                Mesaj = "Dahiliye polikliniğine başvurmanız önerilir.", 
                Acil = false 
            };
        }

        public class TahminSonucu
        {
            public string Poliklinik { get; set; } = string.Empty;
            public string Mesaj { get; set; } = string.Empty;
            public bool Acil { get; set; }
        }

        public class ApiResponse
        {
            public List<string> Belirtiler { get; set; } = new List<string>();
            public List<string> Poliklinikler { get; set; } = new List<string>();
        }

        public class TahminApiResponse
        {
            public bool Success { get; set; }
            public TahminDetay Sonuc { get; set; }
        }

        public class TahminDetay
        {
            public string Poliklinik { get; set; } = string.Empty;
            public string Mesaj { get; set; } = string.Empty;
            public bool Acil { get; set; }
            public List<string> Belirtiler { get; set; } = new List<string>();
            public string Yontem { get; set; } = string.Empty;
            public double Guven { get; set; }
        }
    }
} 