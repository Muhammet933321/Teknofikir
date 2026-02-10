using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.UI
{
    /// <summary>
    /// Sınıf yönetim ekranı. Sınıf ekleme/silme ve öğrenci ekleme/silme işlemleri.
    /// </summary>
    public class ClassManagementUI : MonoBehaviour
    {
        [Header("═══ Paneller ═══")]
        [SerializeField] private GameObject sinifListesiPanel;
        [SerializeField] private GameObject sinifDetayPanel;
        [SerializeField] private GameObject sinifEklePopup;
        [SerializeField] private GameObject ogrenciEklePopup;

        [Header("═══ Sınıf Listesi ═══")]
        [SerializeField] private Transform sinifListesiContent;
        [SerializeField] private GameObject sinifItemPrefab;
        [SerializeField] private Button sinifEkleButton;
        [SerializeField] private Button geriButton;

        [Header("═══ Sınıf Ekleme Popup ═══")]
        [SerializeField] private TMP_InputField sinifAdiInput;
        [SerializeField] private Button sinifKaydetButton;
        [SerializeField] private Button sinifIptalButton;

        [Header("═══ Sınıf Detay (Öğrenci Listesi) ═══")]
        [SerializeField] private TextMeshProUGUI sinifBaslikText;
        [SerializeField] private Transform ogrenciListesiContent;
        [SerializeField] private GameObject ogrenciItemPrefab;
        [SerializeField] private Button ogrenciEkleButton;
        [SerializeField] private Button sinifDetayGeriButton;

        [Header("═══ Öğrenci Ekleme Popup ═══")]
        [SerializeField] private TMP_InputField ogrenciAdInput;
        [SerializeField] private TMP_InputField ogrenciSoyadInput;
        [SerializeField] private TMP_InputField ogrenciNoInput;
        [SerializeField] private Button ogrenciKaydetButton;
        [SerializeField] private Button ogrenciIptalButton;

        [Header("═══ Öğrenci Detay ═══")]
        [SerializeField] private StudentDetailUI ogrenciDetayUI;

        private ClassData seciliSinif;
        private bool listenersReady;

        private void OnEnable()
        {
            EnsureInit();
            PaneliSifirla();
        }

        /// <summary>Listener'ları sadece bir kez bağla.</summary>
        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            if (sinifEkleButton != null) sinifEkleButton.onClick.AddListener(SinifEklePopupGoster);
            if (sinifKaydetButton != null) sinifKaydetButton.onClick.AddListener(SinifKaydet);
            if (sinifIptalButton != null) sinifIptalButton.onClick.AddListener(SinifEklePopupKapat);

            if (ogrenciEkleButton != null) ogrenciEkleButton.onClick.AddListener(OgrenciEklePopupGoster);
            if (ogrenciKaydetButton != null) ogrenciKaydetButton.onClick.AddListener(OgrenciKaydet);
            if (ogrenciIptalButton != null) ogrenciIptalButton.onClick.AddListener(OgrenciEklePopupKapat);

            if (geriButton != null) geriButton.onClick.AddListener(GeriDon);
            if (sinifDetayGeriButton != null) sinifDetayGeriButton.onClick.AddListener(SinifListesiGoster);
        }

        /// <summary>Panel her açıldığında alt durumları güvenli başlangıca getir.</summary>
        private void PaneliSifirla()
        {
            // Popup'ları kapat
            if (sinifEklePopup != null) sinifEklePopup.SetActive(false);
            if (ogrenciEklePopup != null) ogrenciEklePopup.SetActive(false);

            // Sınıf listesini göster, detayı gizle
            SinifListesiGoster();
        }

        // ═══════════════════════════════════════════════════
        //  SINIF LİSTESİ
        // ═══════════════════════════════════════════════════

        private void SinifListesiGoster()
        {
            if (sinifListesiPanel != null) sinifListesiPanel.SetActive(true);
            if (sinifDetayPanel != null) sinifDetayPanel.SetActive(false);
            SiniflariListele();
        }

        private void SiniflariListele()
        {
            if (sinifListesiContent == null || sinifItemPrefab == null) return;
            if (DataManager.Instance == null) return;

            // Mevcut listeyi temizle
            foreach (Transform child in sinifListesiContent)
            {
                Destroy(child.gameObject);
            }

            // Sınıfları ekle
            foreach (var sinif in DataManager.Instance.okulVeritabani.siniflar)
            {
                GameObject item = Instantiate(sinifItemPrefab, sinifListesiContent);
                item.SetActive(true);
                SetupSinifItem(item, sinif);
            }
        }

        private void SetupSinifItem(GameObject item, ClassData sinif)
        {
            // Sınıf adı text'i
            var textler = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (textler.Length > 0)
            {
                textler[0].text = $"{sinif.sinifAdi} ({sinif.ogrenciler.Count} öğrenci)";
            }

            // Sınıfa tıkla - detay göster
            var buton = item.GetComponentInChildren<Button>();
            if (buton != null)
            {
                buton.onClick.AddListener(() => SinifDetayGoster(sinif));
            }

            // Sil butonu (varsa ikinci buton)
            var butonlar = item.GetComponentsInChildren<Button>();
            if (butonlar.Length > 1)
            {
                butonlar[1].onClick.AddListener(() => SinifSil(sinif.id));
            }
        }

        // ═══════════════════════════════════════════════════
        //  SINIF EKLEME
        // ═══════════════════════════════════════════════════

        private void SinifEklePopupGoster()
        {
            if (sinifEklePopup != null) sinifEklePopup.SetActive(true);
            if (sinifAdiInput != null) sinifAdiInput.text = "";
        }

        private void SinifEklePopupKapat()
        {
            if (sinifEklePopup != null) sinifEklePopup.SetActive(false);
        }

        private void SinifKaydet()
        {
            if (sinifAdiInput == null || string.IsNullOrWhiteSpace(sinifAdiInput.text))
            {
                Debug.LogWarning("Sınıf adı boş olamaz!");
                return;
            }

            DataManager.Instance.SinifEkle(sinifAdiInput.text.Trim());
            SinifEklePopupKapat();
            SiniflariListele();
        }

        private void SinifSil(string sinifId)
        {
            DataManager.Instance.SinifSil(sinifId);
            SiniflariListele();
        }

        // ═══════════════════════════════════════════════════
        //  SINIF DETAY (ÖĞRENCİ LİSTESİ)
        // ═══════════════════════════════════════════════════

        private void SinifDetayGoster(ClassData sinif)
        {
            seciliSinif = sinif;

            if (sinifListesiPanel != null) sinifListesiPanel.SetActive(false);
            if (sinifDetayPanel != null) sinifDetayPanel.SetActive(true);

            if (sinifBaslikText != null) sinifBaslikText.text = $"{sinif.sinifAdi} - Öğrenciler";

            OgrencileriListele();
        }

        private void OgrencileriListele()
        {
            if (ogrenciListesiContent == null || ogrenciItemPrefab == null || seciliSinif == null) return;

            // Mevcut listeyi temizle
            foreach (Transform child in ogrenciListesiContent)
            {
                Destroy(child.gameObject);
            }

            // Öğrencileri ekle
            foreach (var ogrenci in seciliSinif.ogrenciler)
            {
                GameObject item = Instantiate(ogrenciItemPrefab, ogrenciListesiContent);
                item.SetActive(true);
                SetupOgrenciItem(item, ogrenci);
            }
        }

        private void SetupOgrenciItem(GameObject item, StudentData ogrenci)
        {
            var textler = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (textler.Length > 0)
            {
                textler[0].text = $"{ogrenci.ogrenciNo} - {ogrenci.TamAd}";
            }

            var butonlar = item.GetComponentsInChildren<Button>();

            // İlk buton (veya metin alanın kendisi) — öğrenci detayını aç
            if (butonlar.Length > 0)
            {
                butonlar[0].onClick.AddListener(() => OgrenciDetayGoster(ogrenci));
            }

            // Son buton sil butonudur (eğer birden fazla buton varsa)
            if (butonlar.Length > 1)
            {
                butonlar[butonlar.Length - 1].onClick.AddListener(() =>
                {
                    OgrenciSil(ogrenci.id);
                });
            }
        }

        /// <summary>Öğrenci detay panelini açar.</summary>
        private void OgrenciDetayGoster(StudentData ogrenci)
        {
            if (ogrenciDetayUI != null)
            {
                ogrenciDetayUI.OgrenciGoster(ogrenci);
            }
            else
            {
                Debug.LogWarning("[ClassManagement] ogrenciDetayUI atanmamış!");
            }
        }

        // ═══════════════════════════════════════════════════
        //  ÖĞRENCİ EKLEME
        // ═══════════════════════════════════════════════════

        private void OgrenciEklePopupGoster()
        {
            if (ogrenciEklePopup != null) ogrenciEklePopup.SetActive(true);
            if (ogrenciAdInput != null) ogrenciAdInput.text = "";
            if (ogrenciSoyadInput != null) ogrenciSoyadInput.text = "";
            if (ogrenciNoInput != null) ogrenciNoInput.text = "";
        }

        private void OgrenciEklePopupKapat()
        {
            if (ogrenciEklePopup != null) ogrenciEklePopup.SetActive(false);
        }

        private void OgrenciKaydet()
        {
            if (seciliSinif == null)
            {
                Debug.LogWarning("Sınıf seçilmedi!");
                return;
            }

            string ad = ogrenciAdInput != null ? ogrenciAdInput.text.Trim() : "";
            string soyad = ogrenciSoyadInput != null ? ogrenciSoyadInput.text.Trim() : "";
            string no = ogrenciNoInput != null ? ogrenciNoInput.text.Trim() : "";

            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(soyad) || string.IsNullOrWhiteSpace(no))
            {
                Debug.LogWarning("Tüm alanlar doldurulmalıdır!");
                return;
            }

            DataManager.Instance.OgrenciEkle(seciliSinif.id, ad, soyad, no);

            // Güncel sınıf verisini al
            seciliSinif = DataManager.Instance.okulVeritabani.SinifBul(seciliSinif.id);

            OgrenciEklePopupKapat();
            OgrencileriListele();
        }

        private void OgrenciSil(string ogrenciId)
        {
            if (seciliSinif == null) return;
            DataManager.Instance.OgrenciSil(seciliSinif.id, ogrenciId);
            seciliSinif = DataManager.Instance.okulVeritabani.SinifBul(seciliSinif.id);
            OgrencileriListele();
        }

        // ═══════════════════════════════════════════════════
        //  GERİ DÖN
        // ═══════════════════════════════════════════════════

        private void GeriDon()
        {
            // Ana menüye dön
            gameObject.SetActive(false);
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.AnaMenuyuGoster();
        }

        private void OnDestroy()
        {
            if (sinifEkleButton != null) sinifEkleButton.onClick.RemoveAllListeners();
            if (sinifKaydetButton != null) sinifKaydetButton.onClick.RemoveAllListeners();
            if (sinifIptalButton != null) sinifIptalButton.onClick.RemoveAllListeners();
            if (ogrenciEkleButton != null) ogrenciEkleButton.onClick.RemoveAllListeners();
            if (ogrenciKaydetButton != null) ogrenciKaydetButton.onClick.RemoveAllListeners();
            if (ogrenciIptalButton != null) ogrenciIptalButton.onClick.RemoveAllListeners();
            if (geriButton != null) geriButton.onClick.RemoveAllListeners();
            if (sinifDetayGeriButton != null) sinifDetayGeriButton.onClick.RemoveAllListeners();
        }
    }
}
