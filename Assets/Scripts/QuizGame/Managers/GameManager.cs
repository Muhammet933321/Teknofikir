using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.UI;
using QuizGame.Gameplay;

namespace QuizGame.Managers
{
    /// <summary>
    /// Oyunun tüm akışını yöneten ana manager.
    /// Singleton pattern kullanır, sahneler arası korunur.
    /// 
    /// Oyun Akışı:
    /// 1. Ana Menü → Oyna / Sınıflar / Ayarlar / Çıkış
    /// 2. Oyuncu Seçimi → Her oyuncu sınıf ve öğrenci seçer
    /// 3. Zorluk Seçimi → Ok döndürme mekaniği
    /// 4. Soru-Cevap Turu → Doğru yapan vuruş yapar, yanlış yapan ceza alır
    /// 5. Oyun Sonu → İstatistikler ve analiz
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ═══════════════════════════════════════════════════
        //  OYUN DURUMLARI
        // ═══════════════════════════════════════════════════

        public enum OyunDurumu
        {
            AnaMenu,
            OyuncuSecimi,
            ZorlukSecimi,
            SoruGosterim,
            VurusAnimasyonu,
            OyunSonu
        }

        [Header("═══ Durum ═══")]
        [SerializeField] private OyunDurumu mevcutDurum = OyunDurumu.AnaMenu;

        [Header("═══ UI Referansları ═══")]
        [SerializeField] private QuizUI quizUI;
        [SerializeField] private DifficultySpinnerUI spinnerUI;
        [SerializeField] private GameHUD gameHUD;
        [SerializeField] private GameOverUI gameOverUI;

        [Header("═══ Karakter Referansları ═══")]
        [SerializeField] private PlayerCharacter oyuncu1Karakter;
        [SerializeField] private PlayerCharacter oyuncu2Karakter;

        [Header("═══ Oyun Alanı ═══")]
        [SerializeField] private GameObject oyunAlaniPanel; // Karakterlerin olduğu alan

        // Oyuncu verileri
        private StudentData oyuncu1Data;
        private StudentData oyuncu2Data;
        private ClassData sinif1Data;
        private ClassData sinif2Data;

        // Oyun durumu
        private List<QuestionData> mevcutSorular;
        private int soruIndex = 0;
        private ZorlukSeviyesi mevcutZorluk;
        private DersKategorisi mevcutDers;
        private string mevcutMacId;

        // Sonuç verileri
        private PlayerGameResult oyuncu1Sonuc;
        private PlayerGameResult oyuncu2Sonuc;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            DurumDegistir(OyunDurumu.AnaMenu);
        }

        // ═══════════════════════════════════════════════════
        //  DURUM YÖNETİMİ
        // ═══════════════════════════════════════════════════

        public void DurumDegistir(OyunDurumu yeniDurum)
        {
            mevcutDurum = yeniDurum;
            Debug.Log($"Oyun durumu: {yeniDurum}");

            // ── Önce tüm panelleri gizle ──
            // Menü panelleri
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.TumPanelleriGizle();

            // Oyun panelleri (VurusAnimasyonu sırasında quiz paneli kendi animasyonuyla kapanır)
            if (yeniDurum != OyunDurumu.VurusAnimasyonu)
            {
                if (quizUI != null) quizUI.gameObject.SetActive(false);
            }
            if (spinnerUI != null) spinnerUI.Gizle();
            if (gameOverUI != null) gameOverUI.Gizle();

            // HUD: oyun içi durumlarda göster, menüde gizle
            bool oyunIcinde = yeniDurum == OyunDurumu.ZorlukSecimi ||
                              yeniDurum == OyunDurumu.SoruGosterim ||
                              yeniDurum == OyunDurumu.VurusAnimasyonu ||
                              yeniDurum == OyunDurumu.OyunSonu;
            if (gameHUD != null)
            {
                if (oyunIcinde) gameHUD.Goster();
                else gameHUD.Gizle();
            }

            // ── Sonra ilgili durumu başlat ──
            switch (yeniDurum)
            {
                case OyunDurumu.AnaMenu:
                    AnaMenuDurumuBaslat();
                    break;
                case OyunDurumu.OyuncuSecimi:
                    // PlayerSelectionUI kendisi yönetiyor
                    break;
                case OyunDurumu.ZorlukSecimi:
                    ZorlukSecimiBaslat();
                    break;
                case OyunDurumu.SoruGosterim:
                    SiradakiSoruyuGoster();
                    break;
                case OyunDurumu.VurusAnimasyonu:
                    // Coroutine ile yönetiliyor
                    break;
                case OyunDurumu.OyunSonu:
                    OyunSonuGoster();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════
        //  ANA MENÜ
        // ═══════════════════════════════════════════════════

        private void AnaMenuDurumuBaslat()
        {
            // DurumDegistir zaten tüm panelleri gizledi, sadece ana menüyü göster
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.AnaMenuyuGoster();

            if (oyunAlaniPanel != null) oyunAlaniPanel.SetActive(false);
        }

        // ═══════════════════════════════════════════════════
        //  OYUNU BAŞLAT (PlayerSelectionUI'dan çağrılır)
        // ═══════════════════════════════════════════════════

        public void OyunuBaslat()
        {
            // Seçili oyuncu verilerini al
            oyuncu1Data = PlayerSelectionUI.SecilenOyuncu1;
            oyuncu2Data = PlayerSelectionUI.SecilenOyuncu2;
            sinif1Data = PlayerSelectionUI.SecilenSinif1;
            sinif2Data = PlayerSelectionUI.SecilenSinif2;
            mevcutDers = PlayerSelectionUI.SecilenDers;
            mevcutMacId = System.Guid.NewGuid().ToString();

            if (oyuncu1Data == null || oyuncu2Data == null)
            {
                Debug.LogError("Oyuncular seçilmemiş!");
                return;
            }

            // Sonuç verilerini başlat
            oyuncu1Sonuc = new PlayerGameResult
            {
                ogrenciId = oyuncu1Data.id,
                ogrenciAd = oyuncu1Data.TamAd,
                sinifAdi = sinif1Data.sinifAdi
            };

            oyuncu2Sonuc = new PlayerGameResult
            {
                ogrenciId = oyuncu2Data.id,
                ogrenciAd = oyuncu2Data.TamAd,
                sinifAdi = sinif2Data.sinifAdi
            };

            // Karakterleri başlat
            if (oyuncu1Karakter != null) oyuncu1Karakter.Baslat(0, oyuncu1Data.TamAd);
            if (oyuncu2Karakter != null) oyuncu2Karakter.Baslat(1, oyuncu2Data.TamAd);

            // HUD'ı ayarla
            if (gameHUD != null)
            {
                gameHUD.Goster();
                gameHUD.OyuncuBilgileriniAyarla(
                    oyuncu1Data.TamAd, sinif1Data.sinifAdi,
                    oyuncu2Data.TamAd, sinif2Data.sinifAdi
                );
            }

            // Quiz UI event'lerini bağla
            if (quizUI != null)
            {
                quizUI.OnDogruCevap = DogruCevapVerildi;
                quizUI.OnYanlisCevap = YanlisCevapVerildi;
            }

            // Oyun alanını göster
            if (oyunAlaniPanel != null) oyunAlaniPanel.SetActive(true);

            // Zorluk seçimine geç
            DurumDegistir(OyunDurumu.ZorlukSecimi);
        }

        // ═══════════════════════════════════════════════════
        //  ZORLUK SEÇİMİ
        // ═══════════════════════════════════════════════════

        private void ZorlukSecimiBaslat()
        {
            if (spinnerUI != null)
            {
                spinnerUI.OnZorlukSecildi = ZorlukSecildi;
                spinnerUI.Goster(oyuncu1Data.TamAd, oyuncu2Data.TamAd);
            }
            else
            {
                // Spinner yoksa varsayılan zorluk kullan
                ZorlukSecildi(ZorlukSeviyesi.Kolay);
            }
        }

        private void ZorlukSecildi(ZorlukSeviyesi zorluk)
        {
            mevcutZorluk = zorluk;
            Debug.Log($"Zorluk seçildi: {zorluk}");

            // Soruları hazırla
            SorulariHazirla(zorluk);

            // İlk soruyu göster
            soruIndex = 0;
            DurumDegistir(OyunDurumu.SoruGosterim);
        }

        private void SorulariHazirla(ZorlukSeviyesi zorluk)
        {
            if (DataManager.Instance == null)
            {
                Debug.LogError("DataManager bulunamadı!");
                mevcutSorular = new List<QuestionData>();
                return;
            }

            // Seçilen ders + zorluktaki soruları al
            mevcutSorular = DataManager.Instance.soruVeritabani.SorulariGetir(zorluk, mevcutDers);

            // Yeteri kadar soru yoksa aynı dersteki diğer zorluk seviyelerinden ekle
            if (mevcutSorular.Count < 5)
            {
                var ekSorular = DataManager.Instance.soruVeritabani.SorulariGetir(mevcutDers)
                    .Where(s => s.zorluk != zorluk)
                    .ToList();
                mevcutSorular.AddRange(ekSorular);
            }

            // Hâlâ yeteri kadar yoksa tüm derslerden ekle
            if (mevcutSorular.Count < 5)
            {
                var ekSorular = DataManager.Instance.soruVeritabani.sorular
                    .Where(s => !mevcutSorular.Contains(s))
                    .ToList();
                mevcutSorular.AddRange(ekSorular);
                if (mevcutSorular.Count > 0)
                    Debug.LogWarning($"{mevcutDers} dersinde yeterli soru bulunamadı, başka derslerden eklendi.");
            }

            // Soruları karıştır
            mevcutSorular = mevcutSorular.OrderBy(x => Random.value).ToList();

            Debug.Log($"Toplam {mevcutSorular.Count} soru hazırlandı. (Ders: {mevcutDers}, Zorluk: {zorluk})");
        }

        // ═══════════════════════════════════════════════════
        //  SORU GÖSTERİMİ
        // ═══════════════════════════════════════════════════

        private void SiradakiSoruyuGoster()
        {
            // Oyun bitti mi kontrol et
            if (oyuncu1Karakter != null && !oyuncu1Karakter.HayattaMi)
            {
                OyunBitti(1); // Oyuncu 2 kazandı
                return;
            }
            if (oyuncu2Karakter != null && !oyuncu2Karakter.HayattaMi)
            {
                OyunBitti(0); // Oyuncu 1 kazandı
                return;
            }

            // Soru bitti mi?
            if (soruIndex >= mevcutSorular.Count)
            {
                // Sorular bittiyse, canı fazla olan kazanır
                int kazanan = KazananiHesapla();
                OyunBitti(kazanan);
                return;
            }

            // Quiz panelini aktif et ve soruyu göster
            var soru = mevcutSorular[soruIndex];
            if (quizUI != null)
            {
                quizUI.gameObject.SetActive(true);
                quizUI.CezalariSifirla();
                quizUI.SoruGoster(soru, soruIndex + 1, oyuncu1Data.TamAd, oyuncu2Data.TamAd);
            }
            else
            {
                Debug.LogError("QuizUI referansı atanmamış! Panel Builder → Wire Manager Refs çalıştırın.");
            }

            // HUD güncelle
            if (gameHUD != null)
            {
                gameHUD.SoruSayaciniGuncelle(soruIndex + 1, mevcutSorular.Count);
                gameHUD.TurBilgisiniGuncelle($"{mevcutDers} | Zorluk: {mevcutZorluk}");
            }
        }

        // ═══════════════════════════════════════════════════
        //  CEVAP İŞLEME
        // ═══════════════════════════════════════════════════

        private void DogruCevapVerildi(int oyuncuIndex, QuestionData soru, float cevapSuresi)
        {
            string tarihStr = System.DateTime.Now.ToString("o");

            // Sonucu kaydet
            var dogruSonuc = new RoundResult
            {
                soruId = soru.id,
                soruMetni = soru.soruMetni,
                kategori = soru.kategori,
                zorluk = soru.zorluk,
                dogruMu = true,
                cevapSuresi = cevapSuresi,
                secilenSikIndex = soru.dogruSikIndex,
                dogruSikIndex = soru.dogruSikIndex
            };

            // Diğer oyuncu yanlış yaptı sayılır (cevap veremedi)
            var yanlisSonuc = new RoundResult
            {
                soruId = soru.id,
                soruMetni = soru.soruMetni,
                kategori = soru.kategori,
                zorluk = soru.zorluk,
                dogruMu = false,
                cevapSuresi = -1f, // Cevap veremedi
                secilenSikIndex = -1,
                dogruSikIndex = soru.dogruSikIndex
            };

            if (oyuncuIndex == 0)
            {
                oyuncu1Sonuc.SonucEkle(dogruSonuc);
                oyuncu2Sonuc.SonucEkle(yanlisSonuc);
            }
            else
            {
                oyuncu2Sonuc.SonucEkle(dogruSonuc);
                oyuncu1Sonuc.SonucEkle(yanlisSonuc);
            }

            // ── Performans veritabanına kalıcı kaydet ──
            if (DataManager.Instance != null)
            {
                // Doğru cevap veren
                var dogruKayit = new AnswerRecord
                {
                    soruId = soru.id, soruMetni = soru.soruMetni,
                    ders = soru.kategori, zorluk = soru.zorluk,
                    dogruMu = true, secilenSikIndex = soru.dogruSikIndex,
                    dogruSikIndex = soru.dogruSikIndex,
                    cevapSuresi = cevapSuresi, tarih = tarihStr, macId = mevcutMacId
                };

                // Cevap veremeyen
                var yanlisKayit = new AnswerRecord
                {
                    soruId = soru.id, soruMetni = soru.soruMetni,
                    ders = soru.kategori, zorluk = soru.zorluk,
                    dogruMu = false, secilenSikIndex = -1,
                    dogruSikIndex = soru.dogruSikIndex,
                    cevapSuresi = -1f, tarih = tarihStr, macId = mevcutMacId
                };

                if (oyuncuIndex == 0)
                {
                    DataManager.Instance.CevapPerformansKaydet(oyuncu1Data.id, oyuncu1Data.TamAd, dogruKayit);
                    DataManager.Instance.CevapPerformansKaydet(oyuncu2Data.id, oyuncu2Data.TamAd, yanlisKayit);
                }
                else
                {
                    DataManager.Instance.CevapPerformansKaydet(oyuncu2Data.id, oyuncu2Data.TamAd, dogruKayit);
                    DataManager.Instance.CevapPerformansKaydet(oyuncu1Data.id, oyuncu1Data.TamAd, yanlisKayit);
                }
            }

            // HUD güncelle
            SkorlariGuncelle();

            // Vuruş animasyonuna geç
            DurumDegistir(OyunDurumu.VurusAnimasyonu);
            StartCoroutine(VurusVeSoruGecisi(oyuncuIndex));
        }

        private void YanlisCevapVerildi(int oyuncuIndex, QuestionData soru, float cevapSuresi, int secilenSik)
        {
            Debug.Log($"Oyuncu {oyuncuIndex + 1} yanlış cevap verdi. Sıra diğerinde.");

            // Yanlış cevabı da performans veritabanına kaydet
            if (DataManager.Instance != null)
            {
                var kayit = new AnswerRecord
                {
                    soruId = soru.id, soruMetni = soru.soruMetni,
                    ders = soru.kategori, zorluk = soru.zorluk,
                    dogruMu = false, secilenSikIndex = secilenSik,
                    dogruSikIndex = soru.dogruSikIndex,
                    cevapSuresi = cevapSuresi,
                    tarih = System.DateTime.Now.ToString("o"),
                    macId = mevcutMacId
                };

                string id = oyuncuIndex == 0 ? oyuncu1Data.id : oyuncu2Data.id;
                string ad = oyuncuIndex == 0 ? oyuncu1Data.TamAd : oyuncu2Data.TamAd;
                DataManager.Instance.CevapPerformansKaydet(id, ad, kayit);
            }
        }

        private IEnumerator VurusVeSoruGecisi(int dogruOyuncuIndex)
        {
            // Soru kaybolma animasyonu bitmesini bekle
            yield return new WaitForSeconds(2.0f);

            // Doğru yapan, yanlış yapana vuruş yapar
            PlayerCharacter vuran = dogruOyuncuIndex == 0 ? oyuncu1Karakter : oyuncu2Karakter;
            PlayerCharacter vurulan = dogruOyuncuIndex == 0 ? oyuncu2Karakter : oyuncu1Karakter;

            if (vuran != null && vurulan != null)
            {
                vuran.VurusYap(vurulan);
                yield return new WaitForSeconds(0.5f);
                vurulan.HasarAl(1);
            }

            // Can güncelle
            if (gameHUD != null)
            {
                if (oyuncu1Karakter != null) gameHUD.CanlariGuncelle(0, oyuncu1Karakter.MevcutCan);
                if (oyuncu2Karakter != null) gameHUD.CanlariGuncelle(1, oyuncu2Karakter.MevcutCan);
            }

            yield return new WaitForSeconds(1.0f);

            // Sonraki soruya geç
            soruIndex++;

            // Yeni zorluk seçimi (her soru için) veya direkt sonraki soru
            // Her soru için yeni zorluk seçmek isteniyorsa:
            // DurumDegistir(OyunDurumu.ZorlukSecimi);
            // Aynı zorlukla devam:
            DurumDegistir(OyunDurumu.SoruGosterim);
        }

        // ═══════════════════════════════════════════════════
        //  SKOR VE KAZANAN
        // ═══════════════════════════════════════════════════

        private void SkorlariGuncelle()
        {
            if (gameHUD != null)
            {
                gameHUD.SkoruGuncelle(0, oyuncu1Sonuc.toplamDogru, oyuncu1Sonuc.toplamYanlis);
                gameHUD.SkoruGuncelle(1, oyuncu2Sonuc.toplamDogru, oyuncu2Sonuc.toplamYanlis);
            }
        }

        private int KazananiHesapla()
        {
            int can1 = oyuncu1Karakter != null ? oyuncu1Karakter.MevcutCan : 0;
            int can2 = oyuncu2Karakter != null ? oyuncu2Karakter.MevcutCan : 0;

            if (can1 > can2) return 0;
            if (can2 > can1) return 1;

            // Canlar eşitse doğru sayısına bak
            if (oyuncu1Sonuc.toplamDogru > oyuncu2Sonuc.toplamDogru) return 0;
            if (oyuncu2Sonuc.toplamDogru > oyuncu1Sonuc.toplamDogru) return 1;

            return 0; // Tam berabere, Oyuncu 1 kazanır
        }

        // ═══════════════════════════════════════════════════
        //  OYUN SONU
        // ═══════════════════════════════════════════════════

        private void OyunBitti(int kazananIndex)
        {
            DurumDegistir(OyunDurumu.OyunSonu);
        }

        private void OyunSonuGoster()
        {
            int kazananIndex = KazananiHesapla();

            // Sonuçları kaydet
            oyuncu1Sonuc.kalanCan = oyuncu1Karakter != null ? oyuncu1Karakter.MevcutCan : 0;
            oyuncu2Sonuc.kalanCan = oyuncu2Karakter != null ? oyuncu2Karakter.MevcutCan : 0;
            oyuncu1Sonuc.kazandiMi = (kazananIndex == 0);
            oyuncu2Sonuc.kazandiMi = (kazananIndex == 1);

            // Maç sonucunu veritabanına kaydet
            var macSonucu = new MatchResult();
            macSonucu.oyuncu1Sonuc = oyuncu1Sonuc;
            macSonucu.oyuncu2Sonuc = oyuncu2Sonuc;
            macSonucu.kazananOgrenciId = kazananIndex == 0 ? oyuncu1Data.id : oyuncu2Data.id;

            if (DataManager.Instance != null)
            {
                DataManager.Instance.MacSonucuKaydet(macSonucu);
            }

            // Zayıf dersleri hesapla
            string o1ZayifDersler = ZayifDersleriGetir(oyuncu1Sonuc);
            string o2ZayifDersler = ZayifDersleriGetir(oyuncu2Sonuc);

            // Game Over ekranını göster
            if (gameOverUI != null)
            {
                string kazananAd = kazananIndex == 0 ? oyuncu1Data.TamAd : oyuncu2Data.TamAd;
                string kaybedenAd = kazananIndex == 0 ? oyuncu2Data.TamAd : oyuncu1Data.TamAd;

                gameOverUI.OnTekrarOyna = TekrarOyna;
                gameOverUI.OnAnaMenu = AnaMenuyeDon;

                gameOverUI.Goster(
                    kazananAd, kaybedenAd,
                    oyuncu1Sonuc.toplamDogru, oyuncu1Sonuc.toplamYanlis, oyuncu1Sonuc.kalanCan, o1ZayifDersler,
                    oyuncu2Sonuc.toplamDogru, oyuncu2Sonuc.toplamYanlis, oyuncu2Sonuc.kalanCan, o2ZayifDersler,
                    oyuncu1Data.TamAd, oyuncu2Data.TamAd
                );
            }

            // Quiz UI ve HUD'ı gizle
            if (quizUI != null) quizUI.Gizle();
        }

        private string ZayifDersleriGetir(PlayerGameResult sonuc)
        {
            var zayiflar = new List<string>();
            foreach (DersKategorisi ders in System.Enum.GetValues(typeof(DersKategorisi)))
            {
                float yuzde = sonuc.DersBasariYuzdesi(ders);
                if (yuzde >= 0 && yuzde < 50f)
                {
                    zayiflar.Add(ders.ToString());
                }
            }
            return zayiflar.Count > 0 ? string.Join(", ", zayiflar) : "Yok";
        }

        // ═══════════════════════════════════════════════════
        //  TEKRAR OYNA & ANA MENÜ
        // ═══════════════════════════════════════════════════

        private void TekrarOyna()
        {
            if (gameOverUI != null) gameOverUI.Gizle();

            // Karakterleri sıfırla
            if (oyuncu1Karakter != null) oyuncu1Karakter.CanYenile();
            if (oyuncu2Karakter != null) oyuncu2Karakter.CanYenile();

            // Sonuçları sıfırla
            oyuncu1Sonuc = new PlayerGameResult
            {
                ogrenciId = oyuncu1Data.id,
                ogrenciAd = oyuncu1Data.TamAd,
                sinifAdi = sinif1Data.sinifAdi
            };
            oyuncu2Sonuc = new PlayerGameResult
            {
                ogrenciId = oyuncu2Data.id,
                ogrenciAd = oyuncu2Data.TamAd,
                sinifAdi = sinif2Data.sinifAdi
            };

            // Zorluk seçiminden tekrar başla
            DurumDegistir(OyunDurumu.ZorlukSecimi);
        }

        private void AnaMenuyeDon()
        {
            if (gameOverUI != null) gameOverUI.Gizle();
            if (gameHUD != null) gameHUD.Gizle();
            if (oyunAlaniPanel != null) oyunAlaniPanel.SetActive(false);

            // Ana menü sahnesine geç
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}
