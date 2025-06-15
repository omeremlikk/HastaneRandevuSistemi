# 🧠 AI Destekli Poliklinik Tahmin Sistemi

Bu modül, hastanede belirtilere dayalı poliklinik tahmini yapan yapay zeka sistemidir.

## 🎯 Özellikler

- **Makine Öğrenmesi:** Naive Bayes algoritması ile %95+ doğruluk
- **11 Poliklinik Desteği:** Dahiliye'den Endokrinoloji'ye kadar
- **Acil Durum Tespiti:** Kritik belirtiler için öncelikli yönlendirme
- **REST API:** Flask tabanlı JSON API
- **Modern UI:** Responsive web arayüzü

## 📋 Gereksinimler

- Python 3.8+
- Flask
- scikit-learn
- pandas
- numpy

## 🚀 Kurulum

### 1. Otomatik Kurulum (Windows)
```bash
cd PolTahmin
setup.bat
```

### 2. Manuel Kurulum
```bash
cd PolTahmin

# Bağımlılıkları kur
pip install -r requirements.txt

# Modeli eğit ve kaydet
python train_and_save_model.py

# API'yi başlat
python api.py
```

## 📊 Veri Seti

`veri.json` dosyasında 11 poliklinik için belirtiler:
- **Dahiliye:** Halsizlik, Ateş, Mide Bulantısı...
- **KBB:** Burun Tıkanıklığı, Boğaz Ağrısı...
- **Kardiyoloji:** Göğüs Ağrısı, Çarpıntı...
- **Dermatoloji:** Kaşıntı, Döküntü...
- Ve daha fazlası...

## 🔌 API Endpoints

### GET `/api/belirtiler`
Tüm belirtileri listeler
```json
{
  "belirtiler": ["Ateş", "Baş Ağrısı", "Öksürük", ...],
  "poliklinikler": ["Dahiliye", "KBB", "Kardiyoloji", ...]
}
```

### POST `/api/tahmin`
Belirti tahmini yapar
```json
// İstek
{
  "belirti1": "Göğüs Ağrısı",
  "belirti2": "Çarpıntı",
  "belirti3": ""
}

// Yanıt
{
  "success": true,
  "sonuc": {
    "poliklinik": "Kardiyoloji",
    "mesaj": "Kardiyoloji polikliniğine başvurmanız önerilir.",
    "acil": false,
    "guven": 87.5
  }
}
```

### GET `/api/health`
API durumunu kontrol eder

## ⚡ Acil Durum Belirtileri

Sistem bu belirtileri tespit ederse direk yönlendirme yapar:
- **Kalp Çarpıntısı** → Kardiyoloji (ACİL)
- **Solunum Güçlüğü** → Göğüs Hastalıkları (ACİL)
- **Şiddetli Baş Ağrısı** → Nöroloji (ACİL)
- **Ağır Karın Ağrısı** → Gastroenteroloji (ACİL)
- **Felç** → Nöroloji (ACİL)

## 🎨 Web Entegrasyonu

C# web uygulamasında `/PolTahmin` sayfasından erişilebilir:
- Modern Bootstrap 5 arayüzü
- 3 belirti seçimi
- Anlık tahmin sonuçları
- Randevu alma entegrasyonu

## 📈 Model Performansı

- **Algoritma:** Multinomial Naive Bayes
- **Veri Seti:** 50,000 sentetik kayıt
- **Doğruluk:** ~%95
- **Eğitim Süresi:** <30 saniye

## 🔧 Dosya Yapısı

```
PolTahmin/
├── api.py                    # Flask REST API
├── train_and_save_model.py   # Model eğitimi
├── veri.json                 # Belirti veri seti
├── requirements.txt          # Python bağımlılıkları
├── setup.bat                 # Windows kurulum
├── model.pkl                 # Eğitilmiş model
├── vectorizer.pkl           # Metin vektörleştirici
├── poliklinikler.pkl        # Poliklinik verileri
└── README.md                # Dokümantasyon
```

## 🚨 Önemli Notlar

- Bu sistem tıbbi tavsiye yerine **geçmez**
- Sadece **yönlendirme** amaçlıdır
- Kesin teşhis için **doktor muayenesi** şarttır
- Acil durumlar için **112** arayın

## 📞 Destek

Sorunlar için GitHub issue oluşturun veya dokümantasyonu kontrol edin. 