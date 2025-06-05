from flask import Flask, render_template, request 
import json
import random
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.naive_bayes import MultinomialNB
import pandas as pd
from sklearn.model_selection import train_test_split

# Flask uygulaması başlatma
app = Flask(__name__)

# JSON dosyasını oku
with open("veri.json", "r", encoding="utf-8") as f:
    poliklinikler = json.load(f)

# Ağır belirtiler (öncelikli belirti tanımlamaları)
agir_belirtiler = {
    "Kalp Çarpıntısı": {"poliklinik": "Kardiyoloji", "mesaj": "Kalp çarpıntısından ötürü diğer belirtilere bakmaksızın acilen kardiyolojiye başvurmanız gerekmektedir."},
    "Solunum Güçlüğü": {"poliklinik": "Göğüs Hastalıkları", "mesaj": "Solunum güçlüğünden ötürü diğer belirtilere bakmaksızın acilen göğüs hastalıkları polikliniğine başvurmanız gerekmektedir."},
    "Şiddetli Baş Ağrısı": {"poliklinik": "Nöroloji", "mesaj": "Şiddetli baş ağrısından ötürü diğer belirtilere bakmaksızın acilen nörolojiye başvurmanız gerekmektedir."},
    "Ağır Karın Ağrısı": {"poliklinik": "Gastroenteroloji", "mesaj": "Ağır karın ağrısından ötürü diğer belirtilere bakmaksızın acilen gastroenteroloji polikliniğine başvurmanız gerekmektedir."},
    "Felç": {"poliklinik": "Nöroloji", "mesaj": "Felç belirtileri tespit edilmiştir, acilen nöroloji polikliniğine başvurmanız gerekmektedir."}
}

# Veri seti oluşturma fonksiyonu
def veri_olustur(num_samples=100):
    data = []
    
    # 1 ve 2 belirtiler oluşturulacak kısım
    for _ in range(num_samples // 2):  # İlk yarı 1 veya 2 belirtiden oluşacak
        poliklinik = random.choice(list(poliklinikler.keys()))
        
        # 1 veya 2 belirti seçme
        num_belirti = random.choice([1, 2])  # 1 ya da 2 belirti seç
        belirtiler = random.sample(poliklinikler[poliklinik], k=num_belirti)  # Seçilen sayıda belirti
        
        # Veriyi oluştur
        data.append({
            "Belirti1": belirtiler[0],
            "Belirti2": belirtiler[1] if len(belirtiler) > 1 else "",  # İkili veri varsa ikinci belirtinin boş olmaması sağlanır
            "Belirti3": "",  # Üçüncü belirti boş
            "Poliklinik": poliklinik
        })
    
    # 3 belirtiler oluşturulacak kısım
    for _ in range(num_samples // 2):  # İkinci yarı 3 belirti ile olacak
        poliklinik = random.choice(list(poliklinikler.keys()))
        
        # 3 belirti seçme
        belirtiler = random.sample(poliklinikler[poliklinik], k=3)  # 3 belirti seç
        
        # Veriyi oluştur
        data.append({
            "Belirti1": belirtiler[0],
            "Belirti2": belirtiler[1],
            "Belirti3": belirtiler[2],
            "Poliklinik": poliklinik
        })
    
    return pd.DataFrame(data)

# Veri seti oluştur
veri_seti = veri_olustur(50000)

# Belirtileri birleştir
veri_seti["Belirtiler"] = veri_seti[["Belirti1", "Belirti2", "Belirti3"]].apply(lambda x: " ".join(x), axis=1)

# JSON dosyasına veri setini kaydetme
veri_seti_json = veri_seti.to_dict(orient='records')  # JSON formatına dönüştür
with open("olusturulanveri.json", "w", encoding="utf-8") as json_file:
    json.dump(veri_seti_json, json_file, ensure_ascii=False, indent=4)

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

# Yeni belirtilerle tahmin yapma fonksiyonu
def tahmin_yap(belirtiler):
    belirtiler_vec = vectorizer.transform([belirtiler])
    tahmin = model.predict(belirtiler_vec)
    return tahmin[0]

# Ana sayfa route
@app.route('/', methods=['GET', 'POST'])
def index():
    # Select box için belirtiler
    belirtiler_listesi = list(set([item for sublist in poliklinikler.values() for item in sublist]))
    
    if request.method == 'POST':
        # Formdan belirtiler al
        belirtiler = [
            request.form.get('belirti1'),
            request.form.get('belirti2'),
            request.form.get('belirti3')
        ]
        
        # Ağır belirtiler kontrolü
        for belirti in belirtiler:
            if belirti in agir_belirtiler:
                # Ağır bir belirti var, doğrudan o polikliniğe yönlendir
                mesaj = agir_belirtiler[belirti]["mesaj"]
                poliklinik = agir_belirtiler[belirti]["poliklinik"]
                return render_template('index.html', belirtiler_listesi=belirtiler_listesi, tahmin=poliklinik, mesaj=mesaj, belirtiler=belirtiler)
        
        # Belirtileri birleştir
        belirtiler_str = " ".join(belirtiler)
        # Tahmin yap
        tahmin = tahmin_yap(belirtiler_str)
        return render_template('index.html', belirtiler_listesi=belirtiler_listesi, tahmin=tahmin, belirtiler=belirtiler)
    
    return render_template('index.html', belirtiler_listesi=belirtiler_listesi)

if __name__ == '__main__':
    app.run(debug=True)
