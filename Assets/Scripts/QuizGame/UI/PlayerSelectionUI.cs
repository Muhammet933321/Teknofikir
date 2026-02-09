using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.UI
{
    /// <summary>
    /// Oyun başlamadan önce iki oyuncunun sınıf ve öğrenci seçim ekranı.
    /// Her oyuncu kendi sınıfını ve ardından kendisini seçer.
    /// </summary>
    public class PlayerSelectionUI : MonoBehaviour
    {
        [Header("═══ Ana Panel ═══")]
        [SerializeField] private GameObject oyuncuSecimPanel;

        [Header("═══ Oyuncu 1 Seçimi ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu1BaslikText;
        [SerializeField] private TMP_Dropdown oyuncu1SinifDropdown;
        [SerializeField] private TMP_Dropdown oyuncu1OgrenciDropdown;

        [Header("═══ Oyuncu 2 Seçimi ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu2BaslikText;
        [SerializeField] private TMP_Dropdown oyuncu2SinifDropdown;
        [SerializeField] private TMP_Dropdown oyuncu2OgrenciDropdown;

        [Header("═══ Butonlar ═══")]
        [SerializeField] private Button baslaButton;
        [SerializeField] private Button geriButton;

        [Header("═══ Ders Seçimi ═══")]
        [SerializeField] private TMP_Dropdown dersDropdown;

        [Header("═══ Uyarı ═══")]
        [SerializeField] private TextMeshProUGUI uyariText;

        // Seçili veriler (diğer scriptler tarafından erişilir)
        public static StudentData SecilenOyuncu1 { get; private set; }
        public static StudentData SecilenOyuncu2 { get; private set; }
        public static ClassData SecilenSinif1 { get; private set; }
        public static ClassData SecilenSinif2 { get; private set; }
        public static DersKategorisi SecilenDers { get; private set; }

        private List<ClassData> siniflar;
        private bool listenersReady;

        private void OnEnable()
        {
            EnsureInit();
            SiniflariYukle();
            DersDropdownDoldur();
            if (uyariText != null) uyariText.text = "";
        }

        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            if (oyuncu1SinifDropdown != null)
                oyuncu1SinifDropdown.onValueChanged.AddListener((_) => Oyuncu1SinifSecildi());
            if (oyuncu2SinifDropdown != null)
                oyuncu2SinifDropdown.onValueChanged.AddListener((_) => Oyuncu2SinifSecildi());

            if (baslaButton != null) baslaButton.onClick.AddListener(OyunaBasla);
            if (geriButton != null) geriButton.onClick.AddListener(GeriDon);
        }

        private void DersDropdownDoldur()
        {
            if (dersDropdown == null) return;
            dersDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (DersKategorisi ders in System.Enum.GetValues(typeof(DersKategorisi)))
            {
                options.Add(new TMP_Dropdown.OptionData(DersAdi(ders)));
            }
            dersDropdown.AddOptions(options);
        }

        private string DersAdi(DersKategorisi ders)
        {
            switch (ders)
            {
                case DersKategorisi.Matematik: return "Matematik";
                case DersKategorisi.Turkce: return "Türkçe";
                case DersKategorisi.Fen: return "Fen Bilimleri";
                case DersKategorisi.Sosyal: return "Sosyal Bilgiler";
                case DersKategorisi.Ingilizce: return "İngilizce";
                case DersKategorisi.GenelKultur: return "Genel Kültür";
                default: return ders.ToString();
            }
        }

        private void SiniflariYukle()
        {
            if (DataManager.Instance == null) return;
            siniflar = DataManager.Instance.okulVeritabani.siniflar;

            // Dropdown'ları doldur
            SinifDropdownDoldur(oyuncu1SinifDropdown);
            SinifDropdownDoldur(oyuncu2SinifDropdown);

            // Varsayılan seçimleri temizle
            OgrenciDropdownTemizle(oyuncu1OgrenciDropdown);
            OgrenciDropdownTemizle(oyuncu2OgrenciDropdown);

            // İlk sınıfı otomatik seç
            if (siniflar.Count > 0)
            {
                Oyuncu1SinifSecildi();
                Oyuncu2SinifSecildi();
            }
        }

        private void SinifDropdownDoldur(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;
            dropdown.ClearOptions();

            var options = new List<TMP_Dropdown.OptionData>();
            foreach (var sinif in siniflar)
            {
                options.Add(new TMP_Dropdown.OptionData(sinif.sinifAdi));
            }

            if (options.Count == 0)
            {
                options.Add(new TMP_Dropdown.OptionData("-- Sınıf Yok --"));
            }

            dropdown.AddOptions(options);
        }

        private void OgrenciDropdownDoldur(TMP_Dropdown dropdown, ClassData sinif)
        {
            if (dropdown == null) return;
            dropdown.ClearOptions();

            var options = new List<TMP_Dropdown.OptionData>();
            if (sinif != null)
            {
                foreach (var ogrenci in sinif.ogrenciler)
                {
                    options.Add(new TMP_Dropdown.OptionData($"{ogrenci.ogrenciNo} - {ogrenci.TamAd}"));
                }
            }

            if (options.Count == 0)
            {
                options.Add(new TMP_Dropdown.OptionData("-- Öğrenci Yok --"));
            }

            dropdown.AddOptions(options);
        }

        private void OgrenciDropdownTemizle(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("-- Önce sınıf seçin --")
            });
        }

        private void Oyuncu1SinifSecildi()
        {
            if (siniflar == null || siniflar.Count == 0) return;
            int index = oyuncu1SinifDropdown != null ? oyuncu1SinifDropdown.value : 0;
            if (index >= 0 && index < siniflar.Count)
            {
                OgrenciDropdownDoldur(oyuncu1OgrenciDropdown, siniflar[index]);
            }
        }

        private void Oyuncu2SinifSecildi()
        {
            if (siniflar == null || siniflar.Count == 0) return;
            int index = oyuncu2SinifDropdown != null ? oyuncu2SinifDropdown.value : 0;
            if (index >= 0 && index < siniflar.Count)
            {
                OgrenciDropdownDoldur(oyuncu2OgrenciDropdown, siniflar[index]);
            }
        }

        private void OyunaBasla()
        {
            if (siniflar == null || siniflar.Count == 0)
            {
                GosterUyari("Henüz sınıf eklenmemiş! Önce sınıf eklemeniz gerekiyor.");
                return;
            }

            // Oyuncu 1 seçimini al
            int sinif1Index = oyuncu1SinifDropdown != null ? oyuncu1SinifDropdown.value : -1;
            int ogrenci1Index = oyuncu1OgrenciDropdown != null ? oyuncu1OgrenciDropdown.value : -1;

            // Oyuncu 2 seçimini al
            int sinif2Index = oyuncu2SinifDropdown != null ? oyuncu2SinifDropdown.value : -1;
            int ogrenci2Index = oyuncu2OgrenciDropdown != null ? oyuncu2OgrenciDropdown.value : -1;

            // Validasyon
            if (sinif1Index < 0 || sinif1Index >= siniflar.Count ||
                sinif2Index < 0 || sinif2Index >= siniflar.Count)
            {
                GosterUyari("Lütfen her iki oyuncu için de sınıf seçin!");
                return;
            }

            SecilenSinif1 = siniflar[sinif1Index];
            SecilenSinif2 = siniflar[sinif2Index];

            if (ogrenci1Index < 0 || ogrenci1Index >= SecilenSinif1.ogrenciler.Count)
            {
                GosterUyari("Oyuncu 1 için öğrenci seçilmedi veya sınıfta öğrenci yok!");
                return;
            }

            if (ogrenci2Index < 0 || ogrenci2Index >= SecilenSinif2.ogrenciler.Count)
            {
                GosterUyari("Oyuncu 2 için öğrenci seçilmedi veya sınıfta öğrenci yok!");
                return;
            }

            SecilenOyuncu1 = SecilenSinif1.ogrenciler[ogrenci1Index];
            SecilenOyuncu2 = SecilenSinif2.ogrenciler[ogrenci2Index];

            // Aynı öğrenci seçilemez
            if (SecilenOyuncu1.id == SecilenOyuncu2.id)
            {
                GosterUyari("Aynı öğrenci iki kez seçilemez!");
                return;
            }

            Debug.Log($"Oyun başlıyor: {SecilenOyuncu1.TamAd} vs {SecilenOyuncu2.TamAd}");

            // Seçilen dersi kaydet
            int dersIndex = dersDropdown != null ? dersDropdown.value : 0;
            SecilenDers = (DersKategorisi)dersIndex;
            Debug.Log($"Seçilen ders: {SecilenDers}");

            // Oyun sahnesine geç
            GameManager.Instance?.OyunuBaslat();
        }

        private void GosterUyari(string mesaj)
        {
            if (uyariText != null) uyariText.text = mesaj;
            Debug.LogWarning(mesaj);
        }

        private void GeriDon()
        {
            gameObject.SetActive(false);
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.AnaMenuyuGoster();
        }

        private void OnDestroy()
        {
            if (oyuncu1SinifDropdown != null) oyuncu1SinifDropdown.onValueChanged.RemoveAllListeners();
            if (oyuncu2SinifDropdown != null) oyuncu2SinifDropdown.onValueChanged.RemoveAllListeners();
            if (baslaButton != null) baslaButton.onClick.RemoveAllListeners();
            if (geriButton != null) geriButton.onClick.RemoveAllListeners();
        }
    }
}
