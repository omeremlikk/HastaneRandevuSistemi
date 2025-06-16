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

                // veri.json'da farklı poliklinik kontrolü
                var uyumsuzlukUyarisi = KontrolEtFarkliPolikliniklerVeriJson(secilenBelirtiler);
                if (!string.IsNullOrEmpty(uyumsuzlukUyarisi))
                {
                    ViewBag.UyumsuzlukUyarisi = uyumsuzlukUyarisi;
                }

                // Tahmin yap (MODEL İLE)
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

        private string KontrolEtFarkliPolikliniklerVeriJson(List<string> belirtiler)
        {
            try
            {
                if (System.IO.File.Exists(_veriJsonPath))
                {
                    var json = System.IO.File.ReadAllText(_veriJsonPath, Encoding.UTF8);
                    var poliklinikVerileri = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                    
                    var belirtiPoliklinikMap = new Dictionary<string, string>();
                    var farkliPoliklinikler = new HashSet<string>();
                    
                    // Her belirti için hangi poliklinikte olduğunu bul
                    foreach (var belirti in belirtiler)
                    {
                        foreach (var kvp in poliklinikVerileri)
                        {
                            var poliklinikAdi = kvp.Key;
                            var poliklinikBelirtileri = kvp.Value;
                            
                            if (poliklinikBelirtileri.Contains(belirti))
                            {
                                belirtiPoliklinikMap[belirti] = poliklinikAdi;
                                farkliPoliklinikler.Add(poliklinikAdi);
                                break;
                            }
                        }
                    }
                    
                    // Eğer 2 veya daha fazla belirti var ve bunlar farklı polikliniklere aitse uyarı ver
                    if (belirtiler.Count >= 2 && farkliPoliklinikler.Count > 1)
                    {
                        var uyariMesaji = new StringBuilder();
                        uyariMesaji.AppendLine("<div class='alert alert-warning border-0 shadow-sm mb-4' role='alert' style='background: linear-gradient(135deg, #fff3cd 0%, #ffeeba 100%); border-left: 4px solid #ffc107 !important;'>");
                        uyariMesaji.AppendLine("<div class='d-flex align-items-start'>");
                        uyariMesaji.AppendLine("<div class='flex-shrink-0'>");
                        uyariMesaji.AppendLine("<i class='fas fa-exclamation-triangle fa-2x text-warning me-3'></i>");
                        uyariMesaji.AppendLine("</div>");
                        uyariMesaji.AppendLine("<div class='flex-grow-1'>");
                        uyariMesaji.AppendLine("<h5 class='alert-heading mb-3' style='color: #856404; font-weight: 600;'>");
                        uyariMesaji.AppendLine("<i class='fas fa-info-circle me-2'></i>DİKKAT: Girdiğiniz Belirtiler Farklı Polikliniklere Ait!");
                        uyariMesaji.AppendLine("</h5>");
                        
                        uyariMesaji.AppendLine("<div class='row g-2 mb-3'>");
                        
                        for (int i = 0; i < belirtiler.Count; i++)
                        {
                            var belirti = belirtiler[i];
                            if (belirtiPoliklinikMap.TryGetValue(belirti, out var poliklinik))
                            {
                                string badgeColor = i == 0 ? "danger" : (i == 1 ? "warning" : "info");
                                
                                uyariMesaji.AppendLine("<div class='col-12 col-md-4 mb-2'>");
                                uyariMesaji.AppendLine("<div class='d-flex align-items-center p-2 rounded' style='background: rgba(255,255,255,0.7);'>");
                                uyariMesaji.AppendLine($"<span class='badge bg-{badgeColor} rounded-circle me-2' style='width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; font-size: 11px;'>{i + 1}</span>");
                                uyariMesaji.AppendLine($"<small class='text-dark fw-bold'>{belirti}</small>");
                                uyariMesaji.AppendLine("</div>");
                                uyariMesaji.AppendLine("<div class='text-center mt-1'>");
                                uyariMesaji.AppendLine("<i class='fas fa-arrow-down text-muted' style='font-size: 12px;'></i>");
                                uyariMesaji.AppendLine("</div>");
                                uyariMesaji.AppendLine("<div class='text-center'>");
                                uyariMesaji.AppendLine($"<span class='badge bg-primary rounded-pill px-3 py-2' style='font-size: 11px; font-weight: 500;'>");
                                uyariMesaji.AppendLine($"<i class='fas fa-hospital me-1'></i>{poliklinik}");
                                uyariMesaji.AppendLine("</span>");
                                uyariMesaji.AppendLine("</div>");
                                uyariMesaji.AppendLine("</div>");
                            }
                        }
                        
                        uyariMesaji.AppendLine("</div>");
                        
                        uyariMesaji.AppendLine("<div class='bg-light rounded p-3 mt-3' style='border-left: 3px solid #28a745;'>");
                        uyariMesaji.AppendLine("<div class='d-flex align-items-start'>");
                        uyariMesaji.AppendLine("<i class='fas fa-robot text-success me-2 mt-1'></i>");
                        uyariMesaji.AppendLine("<div>");
                        uyariMesaji.AppendLine("<p class='mb-1' style='color: #155724; font-weight: 500; font-size: 14px;'>");
                        uyariMesaji.AppendLine("<strong>AI Analizi Devam Ediyor:</strong>");
                        uyariMesaji.AppendLine("</p>");
                        uyariMesaji.AppendLine("<p class='mb-0' style='color: #155724; font-size: 13px;'>");
                        uyariMesaji.AppendLine("Yapay zeka modelimiz tüm belirtilerinizi birlikte değerlendirerek en uygun poliklinik önerisini sunacaktır.");
                        uyariMesaji.AppendLine("</p>");
                        uyariMesaji.AppendLine("</div>");
                        uyariMesaji.AppendLine("</div>");
                        uyariMesaji.AppendLine("</div>");
                        
                        uyariMesaji.AppendLine("</div>");
                        uyariMesaji.AppendLine("</div>");
                        uyariMesaji.AppendLine("</div>");
                        
                        return uyariMesaji.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "veri.json'dan farklı poliklinik kontrolü yapılırken hata oluştu");
            }
            
            return null;
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