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
                var belirtiler = await GetBelirtilerAsync();
                ViewBag.Belirtiler = belirtiler;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Belirtiler yüklenirken bir hata oluştu: " + ex.Message;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Tahmin(string belirti1, string belirti2 = null, string belirti3 = null)
        {
            try
            {
                // Belirti listesini tekrar yükle
                var belirtiler = await GetBelirtilerAsync();
                ViewBag.Belirtiler = belirtiler;

                // Belirtileri kontrol et
                if (string.IsNullOrEmpty(belirti1))
                {
                    ViewBag.ErrorMessage = "En az bir belirti seçmelisiniz.";
                    return View("Index");
                }

                // Seçilen belirtileri listele
                var secilenBelirtiler = new List<string> { belirti1 };
                if (!string.IsNullOrEmpty(belirti2)) secilenBelirtiler.Add(belirti2);
                if (!string.IsNullOrEmpty(belirti3)) secilenBelirtiler.Add(belirti3);

                // Tahmin yap
                var sonuc = await TahminYap(secilenBelirtiler);
                ViewBag.TahminSonucu = sonuc;

                return View("Index");
            }
            catch (Exception ex)
            {
                var belirtiler = await GetBelirtilerAsync();
                ViewBag.Belirtiler = belirtiler;
                TempData["ErrorMessage"] = "Tahmin yapılırken bir hata oluştu: " + ex.Message;
                return View("Index");
            }
        }

        // Belirti-Poliklinik eşleme bilgisini frontend'e sağlamak için API endpoint
        [HttpGet]
        public JsonResult GetSymptomPolyclinicMapping()
        {
            var mapping = new Dictionary<string, string>
            {
                // Temel belirtiler ve eşleşen poliklinikler
                ["Baş Ağrısı"] = "Nöroloji",
                ["Ateş"] = "Dahiliye",
                ["Öksürük"] = "Göğüs Hastalıkları",
                ["Kalp Çarpıntısı"] = "Kardiyoloji",
                ["Karın Ağrısı"] = "Gastroenteroloji",
                ["Eklem Ağrısı"] = "Ortopedi",
                ["Cilt Döküntüsü"] = "Deri ve Zührevi Hastalıklar",
                ["Göz Ağrısı"] = "Göz Hastalıkları",
                ["Kulak Ağrısı"] = "Kulak Burun Boğaz",
                ["Solunum Güçlüğü"] = "Göğüs Hastalıkları",
                ["Mide Bulantısı"] = "Gastroenteroloji",
                ["Baş Dönmesi"] = "Nöroloji",
                ["Yorgunluk"] = "Dahiliye",
                ["İdrarda Yanma"] = "Üroloji",
                ["Menstruel Bozukluk"] = "Kadın Hastalıkları",
                ["Şiddetli Baş Ağrısı"] = "Nöroloji",
                ["Felç"] = "Nöroloji",
                ["Ağır Karın Ağrısı"] = "Gastroenteroloji",
                ["Göğüs Ağrısı"] = "Kardiyoloji",
                ["Nefes Darlığı"] = "Göğüs Hastalıkları",
                ["Böbrek Ağrısı"] = "Üroloji",
                ["Kas Ağrısı"] = "Ortopedi",
                ["Deri Kızarıklığı"] = "Deri ve Zührevi Hastalıklar",
                ["Görme Bozukluğu"] = "Göz Hastalıkları",
                ["İşitme Kaybı"] = "Kulak Burun Boğaz",
                ["Sindirim Problemi"] = "Gastroenteroloji",
                ["Adet Gecikmesi"] = "Kadın Hastalıkları",
                ["Cinsel İşlev Bozukluğu"] = "Üroloji"
            };

            return Json(mapping);
        }

        private async Task<List<string>> GetBelirtilerAsync()
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
                    
                    return apiResponse?.Belirtiler ?? GetBelirtilerFromJson();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Flask API'den belirtiler alınırken hata oluştu");
            }
            
            // API çalışmazsa JSON dosyasından al
            return GetBelirtilerFromJson();
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

        private async Task<TahminSonucu> TahminYap(List<string> belirtiler)
        {
            // Önce Flask API ile tahmin yapmaya çalış
            var apiTahmini = await TahminYapAPI(string.Join(",", belirtiler), "", "");
            
            if (apiTahmini != null)
            {
                ViewBag.ModelKullanildi = true;
                return apiTahmini;
            }

            // API çalışmazsa kural tabanlı sistemle tahmin yap
            ViewBag.ModelKullanildi = false;

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