import json
import random
import pickle
import pandas as pd
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.naive_bayes import MultinomialNB
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score, classification_report

def load_data():
    """Veri setini JSON dosyasından yükle"""
    with open("veri.json", "r", encoding="utf-8") as f:
        poliklinikler = json.load(f)
    return poliklinikler

def create_dataset(poliklinikler, num_samples=50000):
    """Eğitim için veri seti oluştur"""
    data = []
    
    # 1 ve 2 belirtiler oluşturulacak kısım
    for _ in range(num_samples // 2):
        poliklinik = random.choice(list(poliklinikler.keys()))
        
        # 1 veya 2 belirti seçme
        num_belirti = random.choice([1, 2])
        belirtiler = random.sample(poliklinikler[poliklinik], k=min(num_belirti, len(poliklinikler[poliklinik])))
        
        # Veriyi oluştur
        data.append({
            "Belirti1": belirtiler[0],
            "Belirti2": belirtiler[1] if len(belirtiler) > 1 else "",
            "Belirti3": "",
            "Poliklinik": poliklinik
        })
    
    # 3 belirtiler oluşturulacak kısım
    for _ in range(num_samples // 2):
        poliklinik = random.choice(list(poliklinikler.keys()))
        
        # 3 belirti seçme
        belirtiler = random.sample(poliklinikler[poliklinik], k=min(3, len(poliklinikler[poliklinik])))
        
        # Veriyi oluştur
        data.append({
            "Belirti1": belirtiler[0],
            "Belirti2": belirtiler[1] if len(belirtiler) > 1 else "",
            "Belirti3": belirtiler[2] if len(belirtiler) > 2 else "",
            "Poliklinik": poliklinik
        })
    
    return pd.DataFrame(data)

def train_model():
    """Modeli eğit ve kaydet"""
    print("Veri yükleniyor...")
    poliklinikler = load_data()
    
    print("Veri seti oluşturuluyor...")
    veri_seti = create_dataset(poliklinikler)
    
    # Belirtileri birleştir
    veri_seti["Belirtiler"] = veri_seti[["Belirti1", "Belirti2", "Belirti3"]].apply(
        lambda x: " ".join([str(item) for item in x if str(item).strip()]), axis=1
    )
    
    print("Model eğitiliyor...")
    # Model için veriyi hazırlama
    X = veri_seti["Belirtiler"]
    y = veri_seti["Poliklinik"]
    
    # Veriyi eğitim ve test olarak ayırma
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)
    
    # Metin verisini sayısal verilere dönüştürme
    vectorizer = CountVectorizer()
    X_train_vec = vectorizer.fit_transform(X_train)
    X_test_vec = vectorizer.transform(X_test)
    
    # Naive Bayes modeli oluşturma ve eğitme
    model = MultinomialNB()
    model.fit(X_train_vec, y_train)
    
    # Model performansını test et
    y_pred = model.predict(X_test_vec)
    accuracy = accuracy_score(y_test, y_pred)
    print(f"Model doğruluğu: {accuracy:.4f}")
    
    # Modeli ve vectorizer'ı kaydet
    with open('model.pkl', 'wb') as f:
        pickle.dump(model, f)
    
    with open('vectorizer.pkl', 'wb') as f:
        pickle.dump(vectorizer, f)
    
    # Poliklinik verilerini de kaydet
    with open('poliklinikler.pkl', 'wb') as f:
        pickle.dump(poliklinikler, f)
    
    print("Model başarıyla kaydedildi!")
    print("Dosyalar: model.pkl, vectorizer.pkl, poliklinikler.pkl")
    
    return model, vectorizer, poliklinikler

if __name__ == "__main__":
    train_model() 