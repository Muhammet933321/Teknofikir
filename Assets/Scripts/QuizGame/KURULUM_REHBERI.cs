/*
╔══════════════════════════════════════════════════════════════╗
║              QUIZ SAVAŞI - KURULUM REHBERİ                  ║
║              (Unity Eğitim Oyunu Sistemi)                    ║
╚══════════════════════════════════════════════════════════════╝

Bu belge, Quiz Savaşı oyun sisteminin Unity'de nasıl kurulacağını
adım adım açıklar.

═══════════════════════════════════════════════════════════════
  DOSYA YAPISI
═══════════════════════════════════════════════════════════════

Scripts/QuizGame/
├── Data/
│   ├── StudentData.cs        → Öğrenci, Sınıf, Okul veri modelleri
│   ├── QuestionData.cs       → Soru, Zorluk, Ders veri modelleri
│   └── AnalyticsData.cs      → Performans takip veri modelleri
│
├── Managers/
│   ├── DataManager.cs        → Veri kaydetme/yükleme (JSON)
│   └── GameManager.cs        → Oyun akışı yönetimi (Singleton)
│
├── UI/
│   ├── MainMenuController.cs  → Ana menü (Oyna/Sınıflar/Ayarlar/Çıkış)
│   ├── ClassManagementUI.cs   → Sınıf/öğrenci ekleme-silme UI
│   ├── PlayerSelectionUI.cs   → 2 oyuncu seçim ekranı
│   ├── DifficultySpinnerUI.cs → Zorluk seçimi + ok döndürme
│   ├── QuizUI.cs             → Soru gösterme + 2 ayrı 4 şık
│   ├── GameHUD.cs            → Oyun içi bilgi (can, skor, soru no)
│   ├── GameOverUI.cs         → Oyun sonu sonuç ekranı
│   ├── SettingsUI.cs         → Ayarlar paneli
│   └── ListItemFactory.cs   → Dinamik liste elemanı oluşturucu
│
├── Gameplay/
│   └── PlayerCharacter.cs    → Karakter can/vuruş/hasar sistemi
│
└── Setup/
    └── QuizGameUISetup.cs    → Otomatik UI hiyerarşisi oluşturucu


═══════════════════════════════════════════════════════════════
  ADIM 1: SAHNE KURULUMU (MainMenuScene)
═══════════════════════════════════════════════════════════════

1) Unity Editor'da "MainMenuScene" sahnesini açın.

2) Hierarchy'de boş bir GameObject oluşturun:
   - Adı: "QuizGameSetup"
   - QuizGameUISetup scriptini ekleyin
   - Inspector'da sağ tıklayıp "Sahneyi Oluştur" çalıştırın
   
   Bu script otomatik olarak:
   - Canvas,
   - Tüm UI panelleri,
   - DataManager,
   - GameManager objelerini oluşturur.

3) ALTERNATİF: Elle kurulum yapmak isterseniz aşağıdaki adımları izleyin.


═══════════════════════════════════════════════════════════════
  ADIM 2: ELLE CANVAS OLUŞTURMA
═══════════════════════════════════════════════════════════════

1) Canvas oluşturun:
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Match Width Or Height: 0.5

2) EventSystem oluşturun (yoksa)


═══════════════════════════════════════════════════════════════
  ADIM 3: MANAGER OBJELERI
═══════════════════════════════════════════════════════════════

1) Boş GameObject oluşturun: "DataManager"
   - DataManager.cs scriptini ekleyin
   
2) Boş GameObject oluşturun: "GameManager"
   - GameManager.cs scriptini ekleyin

3) Boş GameObject oluşturun: "ListItemFactory"
   - ListItemFactory.cs scriptini ekleyin


═══════════════════════════════════════════════════════════════
  ADIM 4: ANA MENÜ PANELİ
═══════════════════════════════════════════════════════════════

Canvas altında "AnaMenuPanel" oluşturun:
- Image component (arka plan rengi)
- Alt objeleri:
  * BaslikText (TextMeshPro): "QUIZ SAVAŞI"
  * OynaButton (Button + TextMeshPro): "OYNA"
  * SiniflarButton (Button + TextMeshPro): "SINIFLAR"
  * AyarlarButton (Button + TextMeshPro): "AYARLAR"
  * CikisButton (Button + TextMeshPro): "ÇIKIŞ"

Ana menü GameObject'e MainMenuController.cs ekleyin.
Inspector'dan butonları ve panelleri sürükleyerek atayın.


═══════════════════════════════════════════════════════════════
  ADIM 5: SINIF YÖNETİM PANELİ
═══════════════════════════════════════════════════════════════

"SinifYonetimPanel" oluşturun:
- ClassManagementUI.cs ekleyin
- Alt paneller:
  * SinifListesiPanel → ScrollView + Content (VerticalLayoutGroup)
  * SinifDetayPanel → Öğrenci listesi
  * SinifEklePopup → InputField + Kaydet/İptal butonları
  * OgrenciEklePopup → 3 InputField (Ad/Soyad/No) + butonlar

Önemli: ListItemFactory'den gelen prefab'ları Inspector'a atayın:
- sinifItemPrefab → ListItemFactory.SinifPrefabGetir()
- ogrenciItemPrefab → ListItemFactory.OgrenciPrefabGetir()


═══════════════════════════════════════════════════════════════
  ADIM 6: OYUNCU SEÇİM PANELİ
═══════════════════════════════════════════════════════════════

"OyuncuSecimPanel" oluşturun:
- PlayerSelectionUI.cs ekleyin
- Oyuncu 1: Sınıf Dropdown + Öğrenci Dropdown
- Oyuncu 2: Sınıf Dropdown + Öğrenci Dropdown
- Başla butonu + Geri butonu + Uyarı text


═══════════════════════════════════════════════════════════════
  ADIM 7: ZORLUK SEÇİM (SPINNER) PANELİ
═══════════════════════════════════════════════════════════════

"SpinnerPanel" oluşturun:
- DifficultySpinnerUI.cs ekleyin
- Oyuncu 1 zorluk butonları: Kolay, Orta, Zor
- Oyuncu 2 zorluk butonları: Kolay, Orta, Zor
- Ok görseli (RectTransform olarak döndürülür)
- Döndür butonu
- Sonuç text
- Devam butonu


═══════════════════════════════════════════════════════════════
  ADIM 8: QUIZ (SORU) PANELİ
═══════════════════════════════════════════════════════════════

"QuizPanel" oluşturun:
- QuizUI.cs ekleyin
- CanvasGroup component ekleyin (animasyon için)
- Üst: Soru metni, numara, zorluk, kategori
- Sol alt: Oyuncu 1'in 4 şık butonu + ceza paneli
- Sağ alt: Oyuncu 2'nin 4 şık butonu + ceza paneli
- Bilgi text (Doğru!/Yanlış!)


═══════════════════════════════════════════════════════════════
  ADIM 9: HUD & OYUN SONU
═══════════════════════════════════════════════════════════════

1) "HUDPanel" oluşturun:
   - GameHUD.cs ekleyin
   - Sol üst: Oyuncu 1 ad, sınıf, skor, can ikonları
   - Orta üst: Soru sayacı
   - Sağ üst: Oyuncu 2 ad, sınıf, skor, can ikonları

2) "GameOverPanel" oluşturun:
   - GameOverUI.cs ekleyin
   - Kazanan/kaybeden text
   - Oyuncu 1 istatistikleri (doğru, yanlış, can, zayıf dersler)
   - Oyuncu 2 istatistikleri
   - Tekrar Oyna + Ana Menü butonları


═══════════════════════════════════════════════════════════════
  ADIM 10: OYUNCU KARAKTERLERİ
═══════════════════════════════════════════════════════════════

"OyunAlaniPanel" içinde 2 karakter oluşturun:

1) "Oyuncu1Karakter" (sol taraf):
   - PlayerCharacter.cs ekleyin
   - Image component (karakter görseli)
   - 3 adet can ikonu (Image, kırmızı kalp)
   
2) "Oyuncu2Karakter" (sağ taraf):
   - PlayerCharacter.cs ekleyin
   - Image component
   - 3 adet can ikonu


═══════════════════════════════════════════════════════════════
  ADIM 11: REFERANSLARI BAĞLAMA
═══════════════════════════════════════════════════════════════

GameManager Inspector'ından şu referansları sürükleyin:
- quizUI → QuizUI component'li obje
- spinnerUI → DifficultySpinnerUI component'li obje
- gameHUD → GameHUD component'li obje
- gameOverUI → GameOverUI component'li obje
- oyuncu1Karakter → PlayerCharacter (sol)
- oyuncu2Karakter → PlayerCharacter (sağ)
- oyunAlaniPanel → Karakterlerin parent objesi

MainMenuController Inspector'ından:
- oynaButton → OYNA butonu
- siniflarButton → SINIFLAR butonu
- ayarlarButton → AYARLAR butonu
- cikisButton → ÇIKIŞ butonu
- sinifYonetimPanel → SinifYonetimPanel
- ayarlarPanel → AyarlarPanel
- oyuncuSecimPanel → OyuncuSecimPanel


═══════════════════════════════════════════════════════════════
  OYUN AKIŞI
═══════════════════════════════════════════════════════════════

1. ANA MENÜ açılır
   ├── OYNA → Oyuncu Seçimi ekranı
   │   ├── Her iki oyuncu sınıf ve öğrenci seçer
   │   └── BAŞLA → Zorluk Seçimi
   │       ├── Her oyuncu bir zorluk seçer
   │       ├── Farklıysa OK DÖNDÜRÜLÜR
   │       └── DEVAM → Sorular başlar
   │           ├── Soru + 2x4 şık gösterilir
   │           ├── Yanlış → 10s ceza
   │           ├── Doğru → Soru kaybolur, vuruş yapılır
   │           ├── -1 can rakipten
   │           └── Can 0 → OYUN BİTTİ
   │               ├── İstatistikler gösterilir
   │               ├── Zayıf dersler hesaplanır
   │               ├── Tekrar Oyna → Zorluk Seçimine dön
   │               └── Ana Menü → Başa dön
   │
   ├── SINIFLAR → Sınıf Yönetim
   │   ├── Sınıf Ekle / Sil
   │   └── Öğrenci Ekle / Sil (Ad, Soyad, No)
   │
   ├── AYARLAR → Ses, Müzik, Ceza süresi, Can sayısı
   └── ÇIKIŞ → Uygulamayı kapat


═══════════════════════════════════════════════════════════════
  VERİ KAYIT SİSTEMİ
═══════════════════════════════════════════════════════════════

Tüm veriler JSON olarak Application.persistentDataPath'a kaydedilir:
- okul_veritabani.json → Sınıflar ve öğrenciler
- soru_veritabani.json → Soru havuzu
- analiz_veritabani.json → Maç sonuçları ve performans

Her maç sonucu şunları içerir:
- Her iki oyuncunun doğru/yanlış sayıları
- Hangi soruyu kaçıncı saniyede cevapladıkları
- Ders bazlı başarı yüzdeleri
- Zayıf dersler (<%50 başarı)


═══════════════════════════════════════════════════════════════
  YENİ SORU EKLEME
═══════════════════════════════════════════════════════════════

DataManager.cs'deki VarsayilanSorulariOlustur() metoduna yeni sorular 
ekleyebilirsiniz:

SoruEkle("Soru metni?", 
         new string[] { "A şıkkı", "B şıkkı", "C şıkkı", "D şıkkı" }, 
         dogruSikIndex,           // 0-3 arası
         ZorlukSeviyesi.Kolay,    // Kolay/Orta/Zor
         DersKategorisi.Matematik  // Matematik/Turkce/Fen/Sosyal/Ingilizce/GenelKultur
);

NOT: İlk çalıştırmada varsayılan sorular oluşturulur ve JSON'a kaydedilir.
Sonraki çalıştırmalarda JSON'dan okunur. Yeni soru eklemek için 
ya JSON dosyasını silin ya da soru_veritabani.json'ı düzenleyin.

*/
