using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizGame.UI
{
    /// <summary>
    /// Ana menü ekranını yöneten controller.
    /// Canvas üzerinde yaşar (her zaman aktif), alt panelleri açıp kapatır.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // ── Singleton ──
        public static MainMenuController Instance { get; private set; }

        [Header("═══ Ana Menü Paneli ═══")]
        [SerializeField] private GameObject anaMenuPanel;

        [Header("═══ Butonlar ═══")]
        [SerializeField] private Button oynaButton;
        [SerializeField] private Button siniflarButton;
        [SerializeField] private Button sorularButton;
        [SerializeField] private Button ayarlarButton;
        [SerializeField] private Button cikisButton;

        [Header("═══ Alt Paneller ═══")]
        [SerializeField] private GameObject sinifYonetimPanel;
        [SerializeField] private GameObject soruYonetimPanel;
        [SerializeField] private GameObject ayarlarPanel;
        [SerializeField] private GameObject oyuncuSecimPanel;

        [Header("═══ Başlık ═══")]
        [SerializeField] private TextMeshProUGUI baslikText;

        private bool listenersReady;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BindListeners();
            // Başlangıçta sadece ana menüyü göster
            AnaMenuyuGoster();
        }

        private void OnEnable()
        {
            // Start() henüz çalışmamış olabilir, güvenli olsun
            BindListeners();
        }

        private void BindListeners()
        {
            if (listenersReady) return;
            listenersReady = true;

            if (oynaButton != null) oynaButton.onClick.AddListener(OynaButonuTiklandi);
            if (siniflarButton != null) siniflarButton.onClick.AddListener(SiniflarButonuTiklandi);
            if (sorularButton != null) sorularButton.onClick.AddListener(SorularButonuTiklandi);
            if (ayarlarButton != null) ayarlarButton.onClick.AddListener(AyarlarButonuTiklandi);
            if (cikisButton != null) cikisButton.onClick.AddListener(CikisButonuTiklandi);
        }

        // ═══════════════════════════════════════════════════
        //  PANEL GEÇİŞLERİ
        // ═══════════════════════════════════════════════════

        /// <summary>Tüm menü panellerini gizle — her geçişten önce çağrılır.</summary>
        public void TumPanelleriGizle()
        {
            if (anaMenuPanel != null) anaMenuPanel.SetActive(false);
            if (sinifYonetimPanel != null) sinifYonetimPanel.SetActive(false);
            if (soruYonetimPanel != null) soruYonetimPanel.SetActive(false);
            if (ayarlarPanel != null) ayarlarPanel.SetActive(false);
            if (oyuncuSecimPanel != null) oyuncuSecimPanel.SetActive(false);
        }

        /// <summary>Ana menüyü göster, tüm alt panelleri kapat.</summary>
        public void AnaMenuyuGoster()
        {
            TumPanelleriGizle();
            if (anaMenuPanel != null) anaMenuPanel.SetActive(true);
        }

        /// <summary>Belirtilen paneli göster, diğerlerini kapat.</summary>
        public void PanelGoster(GameObject panel)
        {
            TumPanelleriGizle();
            if (panel != null) panel.SetActive(true);
        }

        private void OynaButonuTiklandi()
        {
            Debug.Log("[MainMenu] Oyna butonuna tıklandı");
            PanelGoster(oyuncuSecimPanel);
        }

        private void SiniflarButonuTiklandi()
        {
            Debug.Log("[MainMenu] Sınıflar butonuna tıklandı");
            PanelGoster(sinifYonetimPanel);
        }

        private void SorularButonuTiklandi()
        {
            Debug.Log("[MainMenu] Sorular butonuna tıklandı");
            PanelGoster(soruYonetimPanel);
        }

        private void AyarlarButonuTiklandi()
        {
            Debug.Log("[MainMenu] Ayarlar butonuna tıklandı");
            PanelGoster(ayarlarPanel);
        }

        private void CikisButonuTiklandi()
        {
            Debug.Log("[MainMenu] Çıkış butonuna tıklandı");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (oynaButton != null) oynaButton.onClick.RemoveAllListeners();
            if (siniflarButton != null) siniflarButton.onClick.RemoveAllListeners();
            if (sorularButton != null) sorularButton.onClick.RemoveAllListeners();
            if (ayarlarButton != null) ayarlarButton.onClick.RemoveAllListeners();
            if (cikisButton != null) cikisButton.onClick.RemoveAllListeners();
        }
    }
}
