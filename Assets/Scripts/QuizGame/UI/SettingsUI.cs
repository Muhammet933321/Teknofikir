using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizGame.UI
{
    /// <summary>
    /// Ayarlar paneli. Ses, müzik ve diğer oyun ayarlarını kontrol eder.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("═══ Panel ═══")]
        [SerializeField] private GameObject ayarlarPanel;

        [Header("═══ Ses Ayarları ═══")]
        [SerializeField] private Slider sesSlider;
        [SerializeField] private TextMeshProUGUI sesYuzdeText;
        [SerializeField] private Slider muzikSlider;
        [SerializeField] private TextMeshProUGUI muzikYuzdeText;

        [Header("═══ Oyun Ayarları ═══")]
        [SerializeField] private TMP_Dropdown cezaSuresiDropdown;
        [SerializeField] private TMP_Dropdown canSayisiDropdown;

        [Header("═══ Butonlar ═══")]
        [SerializeField] private Button kaydetButton;
        [SerializeField] private Button geriButton;

        // Varsayılan ayarlar
        public static float SesHacmi { get; private set; } = 1f;
        public static float MuzikHacmi { get; private set; } = 0.7f;
        public static float CezaSuresi { get; private set; } = 10f;
        public static int CanSayisi { get; private set; } = 3;

        private bool listenersReady;

        private void OnEnable()
        {
            EnsureInit();
            // Her açıldığında güncel ayarları göster
            AyarlariYukle();
            AyarlariGorselleGuncelle();
        }

        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            // Slider dinleyicileri
            if (sesSlider != null) sesSlider.onValueChanged.AddListener(SesAyarla);
            if (muzikSlider != null) muzikSlider.onValueChanged.AddListener(MuzikAyarla);

            // Butonlar
            if (kaydetButton != null) kaydetButton.onClick.AddListener(Kaydet);
            if (geriButton != null) geriButton.onClick.AddListener(GeriDon);
        }

        private void AyarlariGorselleGuncelle()
        {
            if (sesSlider != null) sesSlider.value = SesHacmi;
            if (muzikSlider != null) muzikSlider.value = MuzikHacmi;
            CezaSuresiDropdownDoldur();
            CanSayisiDropdownDoldur();
        }

        private void SesAyarla(float deger)
        {
            SesHacmi = deger;
            if (sesYuzdeText != null) sesYuzdeText.text = $"{(int)(deger * 100)}%";
            AudioListener.volume = deger;
        }

        private void MuzikAyarla(float deger)
        {
            MuzikHacmi = deger;
            if (muzikYuzdeText != null) muzikYuzdeText.text = $"{(int)(deger * 100)}%";
        }

        private void CezaSuresiDropdownDoldur()
        {
            if (cezaSuresiDropdown == null) return;
            cezaSuresiDropdown.ClearOptions();
            cezaSuresiDropdown.AddOptions(new System.Collections.Generic.List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("5 Saniye"),
                new TMP_Dropdown.OptionData("10 Saniye"),
                new TMP_Dropdown.OptionData("15 Saniye"),
                new TMP_Dropdown.OptionData("20 Saniye")
            });

            // Mevcut değere göre seç
            switch ((int)CezaSuresi)
            {
                case 5: cezaSuresiDropdown.value = 0; break;
                case 10: cezaSuresiDropdown.value = 1; break;
                case 15: cezaSuresiDropdown.value = 2; break;
                case 20: cezaSuresiDropdown.value = 3; break;
                default: cezaSuresiDropdown.value = 1; break;
            }
        }

        private void CanSayisiDropdownDoldur()
        {
            if (canSayisiDropdown == null) return;
            canSayisiDropdown.ClearOptions();
            canSayisiDropdown.AddOptions(new System.Collections.Generic.List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("1 Can"),
                new TMP_Dropdown.OptionData("2 Can"),
                new TMP_Dropdown.OptionData("3 Can"),
                new TMP_Dropdown.OptionData("5 Can")
            });

            // Mevcut değere göre seç
            switch (CanSayisi)
            {
                case 1: canSayisiDropdown.value = 0; break;
                case 2: canSayisiDropdown.value = 1; break;
                case 3: canSayisiDropdown.value = 2; break;
                case 5: canSayisiDropdown.value = 3; break;
                default: canSayisiDropdown.value = 2; break;
            }
        }

        private void Kaydet()
        {
            // Ceza süresi
            if (cezaSuresiDropdown != null)
            {
                int[] sureler = { 5, 10, 15, 20 };
                CezaSuresi = sureler[cezaSuresiDropdown.value];
            }

            // Can sayısı
            if (canSayisiDropdown != null)
            {
                int[] canlar = { 1, 2, 3, 5 };
                CanSayisi = canlar[canSayisiDropdown.value];
            }

            // PlayerPrefs'e kaydet
            PlayerPrefs.SetFloat("SesHacmi", SesHacmi);
            PlayerPrefs.SetFloat("MuzikHacmi", MuzikHacmi);
            PlayerPrefs.SetFloat("CezaSuresi", CezaSuresi);
            PlayerPrefs.SetInt("CanSayisi", CanSayisi);
            PlayerPrefs.Save();

            Debug.Log($"Ayarlar kaydedildi: Ses={SesHacmi}, Müzik={MuzikHacmi}, Ceza={CezaSuresi}s, Can={CanSayisi}");
        }

        private void AyarlariYukle()
        {
            SesHacmi = PlayerPrefs.GetFloat("SesHacmi", 1f);
            MuzikHacmi = PlayerPrefs.GetFloat("MuzikHacmi", 0.7f);
            CezaSuresi = PlayerPrefs.GetFloat("CezaSuresi", 10f);
            CanSayisi = PlayerPrefs.GetInt("CanSayisi", 3);

            AudioListener.volume = SesHacmi;
        }

        private void GeriDon()
        {
            Kaydet();
            gameObject.SetActive(false);
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.AnaMenuyuGoster();
        }

        private void OnDestroy()
        {
            if (sesSlider != null) sesSlider.onValueChanged.RemoveAllListeners();
            if (muzikSlider != null) muzikSlider.onValueChanged.RemoveAllListeners();
            if (kaydetButton != null) kaydetButton.onClick.RemoveAllListeners();
            if (geriButton != null) geriButton.onClick.RemoveAllListeners();
        }
    }
}
