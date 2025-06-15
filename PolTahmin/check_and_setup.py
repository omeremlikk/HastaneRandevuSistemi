import os
import sys

def check_model_files():
    """Model dosyalarının varlığını kontrol et"""
    required_files = ['model.pkl', 'vectorizer.pkl', 'poliklinikler.pkl']
    missing_files = []
    
    for file in required_files:
        if not os.path.exists(file):
            missing_files.append(file)
    
    return missing_files

def main():
    print("Model dosyaları kontrol ediliyor...")
    
    missing_files = check_model_files()
    
    if missing_files:
        print(f"Eksik dosyalar: {missing_files}")
        print("Model eğitiliyor...")
        
        try:
            # Model eğitim scriptini çalıştır
            exec(open('train_and_save_model.py').read())
            print("Model başarıyla eğitildi!")
        except Exception as e:
            print(f"Model eğitimi sırasında hata: {e}")
            sys.exit(1)
    else:
        print("Tüm model dosyaları mevcut!")
    
    # API'yi başlat
    try:
        exec(open('api.py').read())
    except Exception as e:
        print(f"API başlatılırken hata: {e}")

if __name__ == "__main__":
    main() 