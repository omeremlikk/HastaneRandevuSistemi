from flask import Flask, request, jsonify
from flask_cors import CORS
import json
import pickle
import os

app = Flask(__name__)
CORS(app)  # Web projesinden AJAX istekleri için CORS desteği

# Global değişkenler
model = None
vectorizer = None
poliklinikler = None

# Ağır belirtiler (öncelikli belirti tanımlamaları)
AGIR_BELIRTILER = {
    "Kalp Çarpıntısı": {
        "poliklinik": "Kardiyoloji", 
        "mesaj": "Kalp çarpıntısından ötürü diğer belirtilere bakmaksızın acilen kardiyolojiye başvurmanız gerekmektedir.",
        "acil": True
    },
    "Solunum Güçlüğü": {
        "poliklinik": "Göğüs Hastalıkları", 
        "mesaj": "Solunum güçlüğünden ötürü diğer belirtilere bakmaksızın acilen göğüs hastalıkları polikliniğine başvurmanız gerekmektedir.",
        "acil": True
    },
    "Şiddetli Baş Ağrısı": {
        "poliklinik": "Nöroloji", 
        "mesaj": "Şiddetli baş ağrısından ötürü diğer belirtilere bakmaksızın acilen nörolojiye başvurmanız gerekmektedir.",
        "acil": True
    },
    "Ağır Karın Ağrısı": {
        "poliklinik": "Gastroenteroloji", 
        "mesaj": "Ağır karın ağrısından ötürü diğer belirtilere bakmaksızın acilen gastroenteroloji polikliniğine başvurmanız gerekmektedir.",
        "acil": True
    },
    "Felç": {
        "poliklinik": "Nöroloji", 
        "mesaj": "Felç belirtileri tespit edilmiştir, acilen nöroloji polikliniğine başvurmanız gerekmektedir.",
        "acil": True
    }
}

def load_model():
    """Eğitilmiş modeli yükle"""
    global model, vectorizer, poliklinikler
    
    try:
        # Model dosyalarını yükle
        with open('model.pkl', 'rb') as f:
            model = pickle.load(f)
        
        with open('vectorizer.pkl', 'rb') as f:
            vectorizer = pickle.load(f)
        
        with open('poliklinikler.pkl', 'rb') as f:
            poliklinikler = pickle.load(f)
        
        print("Model başarıyla yüklendi!")
        return True
    except FileNotFoundError as e:
        print(f"Model dosyası bulunamadı: {e}")
        print("Lütfen önce train_and_save_model.py dosyasını çalıştırın!")
        return False
    except Exception as e:
        print(f"Model yüklenirken hata: {e}")
        return False

def predict_poliklinik(belirti1, belirti2="", belirti3=""):
    """Belirtilere göre poliklinik tahmini yap"""
    global model, vectorizer
    
    if model is None or vectorizer is None:
        return None, "Model yüklenmemiş!"
    
    # Belirtileri temizle ve birleştir
    belirtiler = [str(b).strip() for b in [belirti1, belirti2, belirti3] if str(b).strip()]
    
    if not belirtiler:
        return None, "En az bir belirti girmelisiniz!"
    
    # Ağır belirtiler kontrolü
    for belirti in belirtiler:
        if belirti in AGIR_BELIRTILER:
            agir_belirti = AGIR_BELIRTILER[belirti]
            return {
                "poliklinik": agir_belirti["poliklinik"],
                "mesaj": agir_belirti["mesaj"],
                "acil": agir_belirti["acil"],
                "belirtiler": belirtiler,
                "yontem": "Ağır belirti tespiti"
            }, None
    
    # Model ile tahmin yap
    try:
        belirtiler_str = " ".join(belirtiler)
        belirtiler_vec = vectorizer.transform([belirtiler_str])
        tahmin = model.predict(belirtiler_vec)[0]
        
        # Tahmin olasılıklarını al
        probabilities = model.predict_proba(belirtiler_vec)[0]
        max_prob = max(probabilities)
        
        return {
            "poliklinik": tahmin,
            "mesaj": f"Belirtilerinize göre {tahmin} polikliniğine başvurmanız önerilir.",
            "acil": False,
            "belirtiler": belirtiler,
            "yontem": "Makine öğrenmesi tahmini",
            "guven": round(max_prob * 100, 2)
        }, None
    
    except Exception as e:
        return None, f"Tahmin yapılırken hata: {str(e)}"

@app.route('/api/belirtiler', methods=['GET'])
def get_belirtiler():
    """Tüm belirtileri listele"""
    if poliklinikler is None:
        return jsonify({"error": "Model yüklenmemiş!"}), 500
    
    # Tüm belirtileri topla
    tum_belirtiler = []
    for poliklinik, belirtiler_list in poliklinikler.items():
        for belirti in belirtiler_list:
            tum_belirtiler.append({
                "belirti": belirti,
                "poliklinik": poliklinik
            })
    
    # Benzersiz belirtileri al
    benzersiz_belirtiler = list(set([item["belirti"] for item in tum_belirtiler]))
    benzersiz_belirtiler.sort()
    
    return jsonify({
        "belirtiler": benzersiz_belirtiler,
        "poliklinikler": list(poliklinikler.keys())
    })

@app.route('/api/tahmin', methods=['POST'])
def tahmin_yap():
    """Belirti tahmini endpoint'i"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({"error": "JSON veri gönderilmedi!"}), 400
        
        belirti1 = data.get('belirti1', '').strip()
        belirti2 = data.get('belirti2', '').strip()
        belirti3 = data.get('belirti3', '').strip()
        
        if not belirti1:
            return jsonify({"error": "En az birinci belirti girmelisiniz!"}), 400
        
        sonuc, hata = predict_poliklinik(belirti1, belirti2, belirti3)
        
        if hata:
            return jsonify({"error": hata}), 500
        
        return jsonify({
            "success": True,
            "sonuc": sonuc
        })
    
    except Exception as e:
        return jsonify({"error": f"İstek işlenirken hata: {str(e)}"}), 500

@app.route('/api/health', methods=['GET'])
def health_check():
    """API sağlık kontrolü"""
    return jsonify({
        "status": "OK",
        "model_loaded": model is not None,
        "vectorizer_loaded": vectorizer is not None,
        "poliklinikler_loaded": poliklinikler is not None
    })

@app.route('/', methods=['GET'])
def index():
    """Ana sayfa - API durumu"""
    return jsonify({
        "message": "Poliklinik Tahmin API'si",
        "version": "1.0",
        "endpoints": {
            "/api/health": "API sağlık kontrolü",
            "/api/belirtiler": "Belirtileri listele",
            "/api/tahmin": "Poliklinik tahmini yap (POST)"
        }
    })

if __name__ == '__main__':
    print("Model yükleniyor...")
    if load_model():
        print("Flask API başlatılıyor...")
        app.run(debug=True, host='0.0.0.0', port=5001)
    else:
        print("Model yüklenemedi! API başlatılamıyor.")
        print("Önce 'python train_and_save_model.py' komutunu çalıştırın.") 