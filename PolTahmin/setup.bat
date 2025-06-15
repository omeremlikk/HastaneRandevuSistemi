@echo off
echo Poliklinik Tahmin AI Moduli kurulumu baslatiliyor...
echo.

echo 1. Python versiyonu kontrol ediliyor...
python --version
IF %ERRORLEVEL% NEQ 0 (
    echo HATA: Python bulunamadi! Lutfen Python 3.8+ yukleyin.
    pause
    exit /B 1
)

echo.
echo 2. Gerekli Python paketleri kuruluyor...
pip install -r requirements.txt
IF %ERRORLEVEL% NEQ 0 (
    echo HATA: Paketler kurulamiyor!
    pause
    exit /B 1
)

echo.
echo 3. AI modeli egitiliyor ve kaydediliyor...
python train_and_save_model.py
IF %ERRORLEVEL% NEQ 0 (
    echo HATA: Model egitilemedi!
    pause
    exit /B 1
)

echo.
echo 4. Flask API test ediliyor...
timeout /t 2 /nobreak >nul
echo Flask API baslatiliyor... (CTRL+C ile durdurun)
echo Baska bir terminal acarak 'python api.py' komutunu calistirin
echo.

echo ========================================
echo KURULUM TAMAMLANDI!
echo ========================================
echo.
echo Kullanim:
echo 1. 'python api.py' komutu ile Flask API'yi baslatın
echo 2. Web uygulamasını calistirin
echo 3. PolTahmin sayfasina giderek AI tahmin sistemini kullanin
echo.
pause 