using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace QuizGame.UI
{
    /// <summary>
    /// Oyun sonu sonuç ekranı ve oyuncu istatistiklerini gösterir.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("═══ Panel ═══")]
        [SerializeField] private GameObject gameOverPanel;

        [Header("═══ Sonuç Bilgileri ═══")]
        [SerializeField] private TextMeshProUGUI kazananText;
        [SerializeField] private TextMeshProUGUI kaybedenText;

        [Header("═══ Oyuncu 1 İstatistikleri ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu1AdText;
        [SerializeField] private TextMeshProUGUI oyuncu1DogruText;
        [SerializeField] private TextMeshProUGUI oyuncu1YanlisText;
        [SerializeField] private TextMeshProUGUI oyuncu1CanText;
        [SerializeField] private TextMeshProUGUI oyuncu1ZayifDerslerText;

        [Header("═══ Oyuncu 2 İstatistikleri ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu2AdText;
        [SerializeField] private TextMeshProUGUI oyuncu2DogruText;
        [SerializeField] private TextMeshProUGUI oyuncu2YanlisText;
        [SerializeField] private TextMeshProUGUI oyuncu2CanText;
        [SerializeField] private TextMeshProUGUI oyuncu2ZayifDerslerText;

        [Header("═══ Butonlar ═══")]
        [SerializeField] private Button tekrarOynaButton;
        [SerializeField] private Button anaMenuButton;

        public System.Action OnTekrarOyna;
        public System.Action OnAnaMenu;

        private bool listenersReady;

        private void OnEnable()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            if (tekrarOynaButton != null) tekrarOynaButton.onClick.AddListener(() => OnTekrarOyna?.Invoke());
            if (anaMenuButton != null) anaMenuButton.onClick.AddListener(() => OnAnaMenu?.Invoke());
        }

        public void Goster(string kazananAd, string kaybedenAd,
                           int o1Dogru, int o1Yanlis, int o1Can, string o1ZayifDersler,
                           int o2Dogru, int o2Yanlis, int o2Can, string o2ZayifDersler,
                           string oyuncu1Ad, string oyuncu2Ad)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            if (kazananText != null) kazananText.text = $"KAZANAN: {kazananAd}";
            if (kaybedenText != null) kaybedenText.text = $"Kaybeden: {kaybedenAd}";

            // Oyuncu 1 istatistikleri
            if (oyuncu1AdText != null) oyuncu1AdText.text = oyuncu1Ad;
            if (oyuncu1DogruText != null) oyuncu1DogruText.text = $"Doğru: {o1Dogru}";
            if (oyuncu1YanlisText != null) oyuncu1YanlisText.text = $"Yanlış: {o1Yanlis}";
            if (oyuncu1CanText != null) oyuncu1CanText.text = $"Kalan Can: {o1Can}";
            if (oyuncu1ZayifDerslerText != null) oyuncu1ZayifDerslerText.text = $"Zayıf Dersler: {o1ZayifDersler}";

            // Oyuncu 2 istatistikleri
            if (oyuncu2AdText != null) oyuncu2AdText.text = oyuncu2Ad;
            if (oyuncu2DogruText != null) oyuncu2DogruText.text = $"Doğru: {o2Dogru}";
            if (oyuncu2YanlisText != null) oyuncu2YanlisText.text = $"Yanlış: {o2Yanlis}";
            if (oyuncu2CanText != null) oyuncu2CanText.text = $"Kalan Can: {o2Can}";
            if (oyuncu2ZayifDerslerText != null) oyuncu2ZayifDerslerText.text = $"Zayıf Dersler: {o2ZayifDersler}";
        }

        public void Gizle()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (tekrarOynaButton != null) tekrarOynaButton.onClick.RemoveAllListeners();
            if (anaMenuButton != null) anaMenuButton.onClick.RemoveAllListeners();
        }
    }
}
