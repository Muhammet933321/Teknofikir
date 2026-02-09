using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizGame.Setup
{
    /// <summary>
    /// Unity Editor'da çalıştırılarak tüm UI hiyerarşisini otomatik oluşturur.
    /// Bu scripti boş bir GameObject'e ekleyip Inspector'dan "Setup UI" butonuna basın.
    /// 
    /// DİKKAT: Bu script sadece UI hiyerarşisini "otomatik oluşturmak" içindir.
    /// Manuel olarak da kurulum yapılabilir. 
    /// Aşağıdaki kurulum talimatlarını izleyerek elle de oluşturabilirsiniz.
    /// </summary>
    public class QuizGameUISetup : MonoBehaviour
    {
        [Header("Bu scripti çalıştırmak için Play Mode'da Space tuşuna basın.")]
        [Header("Veya Inspector'dan 'Sahneyi Kur' butonunu kullanın.")]
        [Space(10)]
        [SerializeField] private bool sahneyiKur = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && sahneyiKur)
            {
                SahneyiOlustur();
                sahneyiKur = false;
            }
        }

        [ContextMenu("Sahneyi Oluştur")]
        public void SahneyiOlustur()
        {
            Debug.Log("=== Quiz Game UI Kurulumu Başlıyor ===");

            // Canvas oluştur
            GameObject canvasObj = OlusturCanvas();

            // Ana Menü paneli
            GameObject anaMenu = OlusturAnaMenu(canvasObj.transform);

            // Sınıf Yönetim paneli
            GameObject sinifYonetim = OlusturSinifYonetimPaneli(canvasObj.transform);

            // Ayarlar paneli
            GameObject ayarlarPanel = OlusturAyarlarPaneli(canvasObj.transform);

            // Oyuncu Seçim paneli
            GameObject oyuncuSecim = OlusturOyuncuSecimPaneli(canvasObj.transform);

            // Zorluk Spinner paneli
            GameObject spinnerPanel = OlusturSpinnerPaneli(canvasObj.transform);

            // Oyun HUD
            GameObject hudPanel = OlusturHUD(canvasObj.transform);

            // Quiz (Soru) paneli
            GameObject quizPanel = OlusturQuizPaneli(canvasObj.transform);

            // Oyun Sonu paneli
            GameObject gameOverPanel = OlusturGameOverPaneli(canvasObj.transform);

            // Oyun Alanı (Karakterler)
            GameObject oyunAlani = OlusturOyunAlani(canvasObj.transform);

            // Manager objelerini oluştur
            OlusturManagerlar();

            // Panelleri varsayılan olarak kapat (Sadece ana menü açık)
            sinifYonetim.SetActive(false);
            ayarlarPanel.SetActive(false);
            oyuncuSecim.SetActive(false);
            spinnerPanel.SetActive(false);
            hudPanel.SetActive(false);
            quizPanel.SetActive(false);
            gameOverPanel.SetActive(false);

            Debug.Log("=== Quiz Game UI Kurulumu Tamamlandı! ===");
        }

        // ═══════════════════════════════════════════════════
        //  CANVAS
        // ═══════════════════════════════════════════════════

        private GameObject OlusturCanvas()
        {
            GameObject canvasObj = new GameObject("QuizGameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem yoksa oluştur
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvasObj;
        }

        // ═══════════════════════════════════════════════════
        //  ANA MENÜ
        // ═══════════════════════════════════════════════════

        private GameObject OlusturAnaMenu(Transform parent)
        {
            GameObject panel = OlusturPanel(parent, "AnaMenuPanel", new Color(0.1f, 0.1f, 0.2f, 0.95f));

            // Başlık
            OlusturText(panel.transform, "BaslikText", "QUIZ SAVAŞI", 60,
                       new Vector2(0, 200), new Vector2(800, 100), Color.white);

            // Alt başlık
            OlusturText(panel.transform, "AltBaslikText", "Bilgi ile Güç Kazan!", 24,
                       new Vector2(0, 140), new Vector2(600, 50), new Color(0.8f, 0.8f, 0.8f));

            // Butonlar
            OlusturButon(panel.transform, "OynaButton", "OYNA", new Vector2(0, 40), new Vector2(300, 60),
                        new Color(0.2f, 0.7f, 0.3f));
            OlusturButon(panel.transform, "SiniflarButton", "SINIFLAR", new Vector2(0, -40), new Vector2(300, 60),
                        new Color(0.3f, 0.5f, 0.8f));
            OlusturButon(panel.transform, "AyarlarButton", "AYARLAR", new Vector2(0, -120), new Vector2(300, 60),
                        new Color(0.6f, 0.6f, 0.2f));
            OlusturButon(panel.transform, "CikisButton", "ÇIKIŞ", new Vector2(0, -200), new Vector2(300, 60),
                        new Color(0.7f, 0.2f, 0.2f));

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  SINIF YÖNETİM PANELİ
        // ═══════════════════════════════════════════════════

        private GameObject OlusturSinifYonetimPaneli(Transform parent)
        {
            GameObject panel = OlusturPanel(parent, "SinifYonetimPanel", new Color(0.1f, 0.15f, 0.2f, 0.95f));

            // Başlık
            OlusturText(panel.transform, "SinifBaslik", "SINIF YÖNETİMİ", 42,
                       new Vector2(0, 450), new Vector2(600, 80), Color.white);

            // Sınıf Listesi alanı
            GameObject listPanel = OlusturPanel(panel.transform, "SinifListesiPanel",
                                               new Color(0.15f, 0.15f, 0.25f, 0.8f),
                                               new Vector2(0, 0), new Vector2(900, 700));

            // Scroll view simulasyonu (basit content area)
            GameObject content = new GameObject("SinifListesiContent");
            content.transform.SetParent(listPanel.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(20, 80);
            contentRect.offsetMax = new Vector2(-20, -20);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Sınıf Ekle butonu
            OlusturButon(listPanel.transform, "SinifEkleButton", "+ Sınıf Ekle",
                        new Vector2(0, -320), new Vector2(200, 50), new Color(0.2f, 0.7f, 0.3f));

            // Geri butonu
            OlusturButon(panel.transform, "GeriButton", "← GERİ",
                        new Vector2(-380, 450), new Vector2(150, 50), new Color(0.5f, 0.5f, 0.5f));

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  AYARLAR PANELİ
        // ═══════════════════════════════════════════════════

        private GameObject OlusturAyarlarPaneli(Transform parent)
        {
            GameObject panel = OlusturPanel(parent, "AyarlarPanel", new Color(0.1f, 0.1f, 0.2f, 0.95f));

            OlusturText(panel.transform, "AyarlarBaslik", "AYARLAR", 42,
                       new Vector2(0, 350), new Vector2(400, 80), Color.white);

            // Ses slider
            OlusturText(panel.transform, "SesLabel", "Ses Seviyesi:", 22,
                       new Vector2(-200, 200), new Vector2(200, 40), Color.white);

            // Müzik slider
            OlusturText(panel.transform, "MuzikLabel", "Müzik Seviyesi:", 22,
                       new Vector2(-200, 100), new Vector2(200, 40), Color.white);

            // Butonlar
            OlusturButon(panel.transform, "KaydetButton", "KAYDET",
                        new Vector2(100, -250), new Vector2(200, 50), new Color(0.2f, 0.7f, 0.3f));
            OlusturButon(panel.transform, "AyarlarGeriButton", "← GERİ",
                        new Vector2(-100, -250), new Vector2(200, 50), new Color(0.5f, 0.5f, 0.5f));

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  OYUNCU SEÇİM PANELİ
        // ═══════════════════════════════════════════════════

        private GameObject OlusturOyuncuSecimPaneli(Transform parent)
        {
            GameObject panel = OlusturPanel(parent, "OyuncuSecimPanel", new Color(0.1f, 0.12f, 0.2f, 0.95f));

            OlusturText(panel.transform, "OyuncuSecimBaslik", "OYUNCU SEÇİMİ", 42,
                       new Vector2(0, 400), new Vector2(600, 80), Color.white);

            // Oyuncu 1 bölümü (sol)
            OlusturText(panel.transform, "Oyuncu1Baslik", "OYUNCU 1", 28,
                       new Vector2(-300, 280), new Vector2(400, 50), new Color(0.3f, 0.7f, 1f));

            OlusturText(panel.transform, "O1SinifLabel", "Sınıf:", 20,
                       new Vector2(-300, 200), new Vector2(200, 40), Color.white);

            OlusturText(panel.transform, "O1OgrenciLabel", "Öğrenci:", 20,
                       new Vector2(-300, 100), new Vector2(200, 40), Color.white);

            // Oyuncu 2 bölümü (sağ)
            OlusturText(panel.transform, "Oyuncu2Baslik", "OYUNCU 2", 28,
                       new Vector2(300, 280), new Vector2(400, 50), new Color(1f, 0.3f, 0.3f));

            OlusturText(panel.transform, "O2SinifLabel", "Sınıf:", 20,
                       new Vector2(300, 200), new Vector2(200, 40), Color.white);

            OlusturText(panel.transform, "O2OgrenciLabel", "Öğrenci:", 20,
                       new Vector2(300, 100), new Vector2(200, 40), Color.white);

            // Butonlar
            OlusturButon(panel.transform, "BaslaButton", "BAŞLA!",
                        new Vector2(0, -200), new Vector2(300, 70), new Color(0.2f, 0.8f, 0.3f));
            OlusturButon(panel.transform, "OyuncuSecimGeriButton", "← GERİ",
                        new Vector2(-380, 400), new Vector2(150, 50), new Color(0.5f, 0.5f, 0.5f));

            // Uyarı text
            OlusturText(panel.transform, "UyariText", "", 18,
                       new Vector2(0, -300), new Vector2(600, 40), new Color(1f, 0.5f, 0.5f));

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  ZORLUK SPINNER PANELİ
        // ═══════════════════════════════════════════════════

        private GameObject OlusturSpinnerPaneli(Transform parent)
        {
            GameObject panel = OlusturPanel(parent, "SpinnerPanel", new Color(0.1f, 0.1f, 0.15f, 0.95f));

            OlusturText(panel.transform, "SpinnerBaslik", "ZORLUK SEÇİMİ", 42,
                       new Vector2(0, 400), new Vector2(600, 80), Color.white);

            // Oyuncu 1 zorluk butonları (sol)
            OlusturText(panel.transform, "O1SecimText", "Oyuncu 1: Seçim yapın", 22,
                       new Vector2(-350, 280), new Vector2(400, 40), Color.white);
            OlusturButon(panel.transform, "O1KolayBtn", "Kolay",
                        new Vector2(-450, 200), new Vector2(150, 45), new Color(0.3f, 0.8f, 0.3f));
            OlusturButon(panel.transform, "O1OrtaBtn", "Orta",
                        new Vector2(-300, 200), new Vector2(150, 45), new Color(0.8f, 0.7f, 0.2f));
            OlusturButon(panel.transform, "O1ZorBtn", "Zor",
                        new Vector2(-150, 200), new Vector2(150, 45), new Color(0.8f, 0.2f, 0.2f));

            // Oyuncu 2 zorluk butonları (sağ)
            OlusturText(panel.transform, "O2SecimText", "Oyuncu 2: Seçim yapın", 22,
                       new Vector2(350, 280), new Vector2(400, 40), Color.white);
            OlusturButon(panel.transform, "O2KolayBtn", "Kolay",
                        new Vector2(150, 200), new Vector2(150, 45), new Color(0.3f, 0.8f, 0.3f));
            OlusturButon(panel.transform, "O2OrtaBtn", "Orta",
                        new Vector2(300, 200), new Vector2(150, 45), new Color(0.8f, 0.7f, 0.2f));
            OlusturButon(panel.transform, "O2ZorBtn", "Zor",
                        new Vector2(450, 200), new Vector2(150, 45), new Color(0.8f, 0.2f, 0.2f));

            // Ok görseli (basit bir döndürülebilir pointer)
            GameObject okObj = new GameObject("OkImage");
            okObj.transform.SetParent(panel.transform, false);
            RectTransform okRect = okObj.AddComponent<RectTransform>();
            okRect.anchoredPosition = new Vector2(0, 0);
            okRect.sizeDelta = new Vector2(200, 200);
            Image okImg = okObj.AddComponent<Image>();
            okImg.color = Color.white;
            // Ok simgesi olarak basit bir gösterge
            OlusturText(okObj.transform, "OkYon", "▲", 80,
                       Vector2.zero, new Vector2(100, 100), Color.yellow);

            // Taraf göstergeleri
            OlusturText(panel.transform, "SolTarafText", "Oyuncu 1", 26,
                       new Vector2(-250, 0), new Vector2(200, 50), new Color(0.3f, 0.7f, 1f));
            OlusturText(panel.transform, "SagTarafText", "Oyuncu 2", 26,
                       new Vector2(250, 0), new Vector2(200, 50), new Color(1f, 0.3f, 0.3f));

            // Döndür butonu
            OlusturButon(panel.transform, "DondurButton", "DÖNDÜR!",
                        new Vector2(0, -150), new Vector2(250, 60), new Color(0.8f, 0.5f, 0.1f));

            // Sonuç text
            OlusturText(panel.transform, "SonucText", "", 24,
                       new Vector2(0, -230), new Vector2(600, 50), Color.yellow);

            // Devam butonu
            OlusturButon(panel.transform, "DevamButton", "DEVAM →",
                        new Vector2(0, -310), new Vector2(250, 60), new Color(0.2f, 0.7f, 0.3f));

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  OYUN HUD
        // ═══════════════════════════════════════════════════

        private GameObject OlusturHUD(Transform parent)
        {
            GameObject panel = new GameObject("HUDPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Üst bar
            GameObject ustBar = OlusturPanel(panel.transform, "UstBar",
                new Color(0.1f, 0.1f, 0.15f, 0.8f),
                new Vector2(0, 500), new Vector2(1920, 80));

            // Oyuncu 1 bilgileri (sol üst)
            OlusturText(ustBar.transform, "O1AdText", "Oyuncu 1", 22,
                       new Vector2(-700, 0), new Vector2(300, 40), new Color(0.3f, 0.7f, 1f));
            OlusturText(ustBar.transform, "O1SinifText", "Sınıf", 16,
                       new Vector2(-700, -25), new Vector2(300, 30), Color.gray);
            OlusturText(ustBar.transform, "O1SkorText", "Doğru: 0 | Yanlış: 0", 16,
                       new Vector2(-500, 0), new Vector2(300, 40), Color.white);

            // Soru sayacı (orta üst)
            OlusturText(ustBar.transform, "SoruSayaciText", "Soru: 1/10", 26,
                       new Vector2(0, 10), new Vector2(300, 40), Color.white);
            OlusturText(ustBar.transform, "TurBilgiText", "Zorluk: Kolay", 18,
                       new Vector2(0, -20), new Vector2(300, 30), Color.yellow);

            // Oyuncu 2 bilgileri (sağ üst)
            OlusturText(ustBar.transform, "O2AdText", "Oyuncu 2", 22,
                       new Vector2(700, 0), new Vector2(300, 40), new Color(1f, 0.3f, 0.3f));
            OlusturText(ustBar.transform, "O2SinifText", "Sınıf", 16,
                       new Vector2(700, -25), new Vector2(300, 30), Color.gray);
            OlusturText(ustBar.transform, "O2SkorText", "Doğru: 0 | Yanlış: 0", 16,
                       new Vector2(500, 0), new Vector2(300, 40), Color.white);

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  QUIZ (SORU) PANELİ
        // ═══════════════════════════════════════════════════

        private GameObject OlusturQuizPaneli(Transform parent)
        {
            GameObject panel = new GameObject("QuizPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.AddComponent<CanvasGroup>();

            // Soru alanı (üst)
            GameObject soruAlani = OlusturPanel(panel.transform, "SoruAlani",
                new Color(0.15f, 0.15f, 0.25f, 0.9f),
                new Vector2(0, 250), new Vector2(1400, 200));

            OlusturText(soruAlani.transform, "SoruNumarasiText", "Soru 1", 20,
                       new Vector2(-550, 60), new Vector2(200, 40), Color.yellow);
            OlusturText(soruAlani.transform, "ZorlukText", "Zorluk: Kolay", 16,
                       new Vector2(500, 60), new Vector2(200, 30), Color.gray);
            OlusturText(soruAlani.transform, "KategoriText", "Ders: Matematik", 16,
                       new Vector2(500, 30), new Vector2(200, 30), Color.gray);
            OlusturText(soruAlani.transform, "SoruText", "Soru metni burada görünecek...", 30,
                       new Vector2(0, -20), new Vector2(1200, 120), Color.white);

            // Bilgi text (Doğru!/Yanlış!)
            OlusturText(panel.transform, "BilgiText", "", 36,
                       new Vector2(0, 100), new Vector2(600, 60), Color.green);

            // Oyuncu 1 şıkları (sol alt)
            GameObject o1Siklar = OlusturPanel(panel.transform, "O1SiklarPanel",
                new Color(0.1f, 0.15f, 0.25f, 0.85f),
                new Vector2(-380, -150), new Vector2(600, 350));

            OlusturText(o1Siklar.transform, "O1AdLabel", "Oyuncu 1", 22,
                       new Vector2(0, 140), new Vector2(400, 40), new Color(0.3f, 0.7f, 1f));

            for (int i = 0; i < 4; i++)
            {
                string harf = new[] { "A", "B", "C", "D" }[i];
                OlusturButon(o1Siklar.transform, $"O1Sik{i}Button", $"{harf}) Şık {i + 1}",
                            new Vector2(0, 80 - i * 65), new Vector2(500, 55), new Color(0.2f, 0.3f, 0.5f));
            }

            // Oyuncu 1 ceza paneli
            GameObject o1Ceza = OlusturPanel(o1Siklar.transform, "O1CezaPanel",
                new Color(0.8f, 0.1f, 0.1f, 0.7f),
                Vector2.zero, new Vector2(500, 300));
            OlusturText(o1Ceza.transform, "O1CezaSureText", "Ceza: 10.0s", 28,
                       Vector2.zero, new Vector2(300, 50), Color.white);
            o1Ceza.SetActive(false);

            // Oyuncu 2 şıkları (sağ alt)
            GameObject o2Siklar = OlusturPanel(panel.transform, "O2SiklarPanel",
                new Color(0.25f, 0.1f, 0.15f, 0.85f),
                new Vector2(380, -150), new Vector2(600, 350));

            OlusturText(o2Siklar.transform, "O2AdLabel", "Oyuncu 2", 22,
                       new Vector2(0, 140), new Vector2(400, 40), new Color(1f, 0.3f, 0.3f));

            for (int i = 0; i < 4; i++)
            {
                string harf = new[] { "A", "B", "C", "D" }[i];
                OlusturButon(o2Siklar.transform, $"O2Sik{i}Button", $"{harf}) Şık {i + 1}",
                            new Vector2(0, 80 - i * 65), new Vector2(500, 55), new Color(0.5f, 0.2f, 0.3f));
            }

            // Oyuncu 2 ceza paneli
            GameObject o2Ceza = OlusturPanel(o2Siklar.transform, "O2CezaPanel",
                new Color(0.8f, 0.1f, 0.1f, 0.7f),
                Vector2.zero, new Vector2(500, 300));
            OlusturText(o2Ceza.transform, "O2CezaSureText", "Ceza: 10.0s", 28,
                       Vector2.zero, new Vector2(300, 50), Color.white);
            o2Ceza.SetActive(false);

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  OYUN SONU PANELİ
        // ═══════════════════════════════════════════════════

        private GameObject OlusturGameOverPaneli(Transform parent)
        {
            GameObject panel = OlusturPanel(parent, "GameOverPanel", new Color(0.05f, 0.05f, 0.1f, 0.95f));

            OlusturText(panel.transform, "GameOverBaslik", "OYUN BİTTİ!", 50,
                       new Vector2(0, 400), new Vector2(600, 80), Color.white);

            // Kazanan
            OlusturText(panel.transform, "KazananText", "KAZANAN: ...", 36,
                       new Vector2(0, 300), new Vector2(800, 60), Color.yellow);
            OlusturText(panel.transform, "KaybedenText", "Kaybeden: ...", 22,
                       new Vector2(0, 250), new Vector2(800, 40), Color.gray);

            // Oyuncu 1 istatistikleri (sol)
            GameObject o1Stats = OlusturPanel(panel.transform, "O1StatsPanel",
                new Color(0.1f, 0.15f, 0.25f, 0.8f),
                new Vector2(-300, 50), new Vector2(500, 350));

            OlusturText(o1Stats.transform, "O1StatsAdText", "Oyuncu 1", 24,
                       new Vector2(0, 140), new Vector2(400, 40), new Color(0.3f, 0.7f, 1f));
            OlusturText(o1Stats.transform, "O1DogruText", "Doğru: 0", 20,
                       new Vector2(0, 80), new Vector2(400, 35), Color.green);
            OlusturText(o1Stats.transform, "O1YanlisText", "Yanlış: 0", 20,
                       new Vector2(0, 40), new Vector2(400, 35), Color.red);
            OlusturText(o1Stats.transform, "O1CanText", "Kalan Can: 0", 20,
                       new Vector2(0, 0), new Vector2(400, 35), Color.white);
            OlusturText(o1Stats.transform, "O1ZayifText", "Zayıf Dersler: -", 18,
                       new Vector2(0, -50), new Vector2(400, 60), new Color(1f, 0.6f, 0.3f));

            // Oyuncu 2 istatistikleri (sağ)
            GameObject o2Stats = OlusturPanel(panel.transform, "O2StatsPanel",
                new Color(0.25f, 0.1f, 0.15f, 0.8f),
                new Vector2(300, 50), new Vector2(500, 350));

            OlusturText(o2Stats.transform, "O2StatsAdText", "Oyuncu 2", 24,
                       new Vector2(0, 140), new Vector2(400, 40), new Color(1f, 0.3f, 0.3f));
            OlusturText(o2Stats.transform, "O2DogruText", "Doğru: 0", 20,
                       new Vector2(0, 80), new Vector2(400, 35), Color.green);
            OlusturText(o2Stats.transform, "O2YanlisText", "Yanlış: 0", 20,
                       new Vector2(0, 40), new Vector2(400, 35), Color.red);
            OlusturText(o2Stats.transform, "O2CanText", "Kalan Can: 0", 20,
                       new Vector2(0, 0), new Vector2(400, 35), Color.white);
            OlusturText(o2Stats.transform, "O2ZayifText", "Zayıf Dersler: -", 18,
                       new Vector2(0, -50), new Vector2(400, 60), new Color(1f, 0.6f, 0.3f));

            // Butonlar
            OlusturButon(panel.transform, "TekrarOynaButton", "TEKRAR OYNA",
                        new Vector2(-150, -250), new Vector2(250, 60), new Color(0.2f, 0.7f, 0.3f));
            OlusturButon(panel.transform, "AnaMenuButton", "ANA MENÜ",
                        new Vector2(150, -250), new Vector2(250, 60), new Color(0.5f, 0.5f, 0.5f));

            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  OYUN ALANI (KARAKTERLER)
        // ═══════════════════════════════════════════════════

        private GameObject OlusturOyunAlani(Transform parent)
        {
            GameObject panel = new GameObject("OyunAlaniPanel");
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Oyuncu 1 karakter (sol)
            GameObject o1Karakter = new GameObject("Oyuncu1Karakter");
            o1Karakter.transform.SetParent(panel.transform, false);
            RectTransform o1Rect = o1Karakter.AddComponent<RectTransform>();
            o1Rect.anchoredPosition = new Vector2(-400, -50);
            o1Rect.sizeDelta = new Vector2(200, 300);
            Image o1Img = o1Karakter.AddComponent<Image>();
            o1Img.color = new Color(0.3f, 0.7f, 1f);

            OlusturText(o1Karakter.transform, "O1KarakterLabel", "P1", 40,
                       Vector2.zero, new Vector2(100, 60), Color.white);

            // Can ikonları Oyuncu 1
            for (int i = 0; i < 3; i++)
            {
                GameObject canIkon = new GameObject($"O1Can{i}");
                canIkon.transform.SetParent(o1Karakter.transform, false);
                RectTransform canRect = canIkon.AddComponent<RectTransform>();
                canRect.anchoredPosition = new Vector2(-40 + i * 40, 170);
                canRect.sizeDelta = new Vector2(30, 30);
                Image canImg = canIkon.AddComponent<Image>();
                canImg.color = Color.red;
            }

            // Oyuncu 2 karakter (sağ)
            GameObject o2Karakter = new GameObject("Oyuncu2Karakter");
            o2Karakter.transform.SetParent(panel.transform, false);
            RectTransform o2Rect = o2Karakter.AddComponent<RectTransform>();
            o2Rect.anchoredPosition = new Vector2(400, -50);
            o2Rect.sizeDelta = new Vector2(200, 300);
            Image o2Img = o2Karakter.AddComponent<Image>();
            o2Img.color = new Color(1f, 0.3f, 0.3f);

            OlusturText(o2Karakter.transform, "O2KarakterLabel", "P2", 40,
                       Vector2.zero, new Vector2(100, 60), Color.white);

            // Can ikonları Oyuncu 2
            for (int i = 0; i < 3; i++)
            {
                GameObject canIkon = new GameObject($"O2Can{i}");
                canIkon.transform.SetParent(o2Karakter.transform, false);
                RectTransform canRect = canIkon.AddComponent<RectTransform>();
                canRect.anchoredPosition = new Vector2(-40 + i * 40, 170);
                canRect.sizeDelta = new Vector2(30, 30);
                Image canImg = canIkon.AddComponent<Image>();
                canImg.color = Color.red;
            }

            // VS yazısı (ortada)
            OlusturText(panel.transform, "VSText", "VS", 60,
                       new Vector2(0, -50), new Vector2(150, 80), Color.yellow);

            panel.SetActive(false);
            return panel;
        }

        // ═══════════════════════════════════════════════════
        //  MANAGER OBJELERİ
        // ═══════════════════════════════════════════════════

        private void OlusturManagerlar()
        {
            // DataManager
            if (FindObjectOfType<QuizGame.Managers.DataManager>() == null)
            {
                GameObject dataManagerObj = new GameObject("DataManager");
                dataManagerObj.AddComponent<QuizGame.Managers.DataManager>();
            }

            // GameManager
            if (FindObjectOfType<QuizGame.Managers.GameManager>() == null)
            {
                GameObject gameManagerObj = new GameObject("GameManager");
                gameManagerObj.AddComponent<QuizGame.Managers.GameManager>();
            }
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCI METOTLAR
        // ═══════════════════════════════════════════════════

        private GameObject OlusturPanel(Transform parent, string ad, Color renk,
                                        Vector2? pozisyon = null, Vector2? boyut = null)
        {
            GameObject panel = new GameObject(ad);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            if (boyut.HasValue)
            {
                rect.sizeDelta = boyut.Value;
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            if (pozisyon.HasValue)
            {
                rect.anchoredPosition = pozisyon.Value;
            }

            Image img = panel.AddComponent<Image>();
            img.color = renk;

            return panel;
        }

        private GameObject OlusturText(Transform parent, string ad, string metin, int fontSize,
                                       Vector2 pozisyon, Vector2 boyut, Color renk)
        {
            GameObject textObj = new GameObject(ad);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pozisyon;
            rect.sizeDelta = boyut;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = metin;
            tmp.fontSize = fontSize;
            tmp.color = renk;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return textObj;
        }

        private GameObject OlusturButon(Transform parent, string ad, string metin,
                                        Vector2 pozisyon, Vector2 boyut, Color renk)
        {
            GameObject btnObj = new GameObject(ad);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pozisyon;
            rect.sizeDelta = boyut;

            Image img = btnObj.AddComponent<Image>();
            img.color = renk;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = renk;
            colors.highlightedColor = renk * 1.2f;
            colors.pressedColor = renk * 0.8f;
            btn.colors = colors;

            // Buton text'i
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = metin;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btnObj;
        }
    }
}
