using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using QuizGame.Data;

namespace QuizGame.UI
{
    /// <summary>
    /// Zorluk seviyesi seçimi için döndürülen ok mekaniği.
    /// İki oyuncu birer zorluk seçer, ok döndürülür ve
    /// duracağı taraf hangi zorluğun kullanılacağını belirler.
    /// </summary>
    public class DifficultySpinnerUI : MonoBehaviour
    {
        [Header("═══ Panel ═══")]
        [SerializeField] private GameObject spinnerPanel;

        [Header("═══ Zorluk Seçim Butonları - Oyuncu 1 ═══")]
        [SerializeField] private Button oyuncu1KolayBtn;
        [SerializeField] private Button oyuncu1OrtaBtn;
        [SerializeField] private Button oyuncu1ZorBtn;
        [SerializeField] private TextMeshProUGUI oyuncu1SecimText;

        [Header("═══ Zorluk Seçim Butonları - Oyuncu 2 ═══")]
        [SerializeField] private Button oyuncu2KolayBtn;
        [SerializeField] private Button oyuncu2OrtaBtn;
        [SerializeField] private Button oyuncu2ZorBtn;
        [SerializeField] private TextMeshProUGUI oyuncu2SecimText;

        [Header("═══ Ok (Spinner) ═══")]
        [SerializeField] private RectTransform okImage;         // Döndürülecek ok resmi
        [SerializeField] private Button dondurButton;
        [SerializeField] private TextMeshProUGUI sonucText;

        [Header("═══ Oyuncu Taraf Göstergeleri ═══")]
        [SerializeField] private TextMeshProUGUI solTarafText;   // "Oyuncu 1" (sol = 0-180 derece)
        [SerializeField] private TextMeshProUGUI sagTarafText;   // "Oyuncu 2" (sağ = 180-360 derece)

        [Header("═══ Devam Butonu ═══")]
        [SerializeField] private Button devamButton;

        [Header("═══ Animasyon Ayarları ═══")]
        [SerializeField] private float minDonusSuresi = 2f;
        [SerializeField] private float maxDonusSuresi = 4f;
        [SerializeField] private float maxDonusHizi = 720f;   // Derece/saniye

        // Seçimler
        private ZorlukSeviyesi oyuncu1Secim = ZorlukSeviyesi.Kolay;
        private ZorlukSeviyesi oyuncu2Secim = ZorlukSeviyesi.Kolay;
        private bool oyuncu1Secti = false;
        private bool oyuncu2Secti = false;
        private bool donuyorMu = false;

        // Sonuç
        public ZorlukSeviyesi SecilenZorluk { get; private set; }
        public System.Action<ZorlukSeviyesi> OnZorlukSecildi;

        private bool listenersReady;

        private void OnEnable()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            // Oyuncu 1 butonları
            if (oyuncu1KolayBtn != null)
                oyuncu1KolayBtn.onClick.AddListener(() => Oyuncu1Sec(ZorlukSeviyesi.Kolay));
            if (oyuncu1OrtaBtn != null)
                oyuncu1OrtaBtn.onClick.AddListener(() => Oyuncu1Sec(ZorlukSeviyesi.Orta));
            if (oyuncu1ZorBtn != null)
                oyuncu1ZorBtn.onClick.AddListener(() => Oyuncu1Sec(ZorlukSeviyesi.Zor));

            // Oyuncu 2 butonları
            if (oyuncu2KolayBtn != null)
                oyuncu2KolayBtn.onClick.AddListener(() => Oyuncu2Sec(ZorlukSeviyesi.Kolay));
            if (oyuncu2OrtaBtn != null)
                oyuncu2OrtaBtn.onClick.AddListener(() => Oyuncu2Sec(ZorlukSeviyesi.Orta));
            if (oyuncu2ZorBtn != null)
                oyuncu2ZorBtn.onClick.AddListener(() => Oyuncu2Sec(ZorlukSeviyesi.Zor));

            // Döndür butonu
            if (dondurButton != null)
            {
                dondurButton.onClick.AddListener(OkuDondur);
                dondurButton.interactable = false;
            }

            // Devam butonu
            if (devamButton != null)
            {
                devamButton.onClick.AddListener(Devam);
                devamButton.gameObject.SetActive(false);
            }

            if (sonucText != null) sonucText.text = "";
        }

        public void Goster(string oyuncu1Ad, string oyuncu2Ad)
        {
            if (spinnerPanel != null) spinnerPanel.SetActive(true);

            oyuncu1Secti = false;
            oyuncu2Secti = false;
            donuyorMu = false;

            if (oyuncu1SecimText != null) oyuncu1SecimText.text = $"{oyuncu1Ad}: Seçim yapın";
            if (oyuncu2SecimText != null) oyuncu2SecimText.text = $"{oyuncu2Ad}: Seçim yapın";
            if (solTarafText != null) solTarafText.text = oyuncu1Ad;
            if (sagTarafText != null) sagTarafText.text = oyuncu2Ad;
            if (sonucText != null) sonucText.text = "";
            if (dondurButton != null) dondurButton.interactable = false;
            if (devamButton != null) devamButton.gameObject.SetActive(false);

            // Ok'u sıfırla
            if (okImage != null) okImage.rotation = Quaternion.identity;
        }

        public void Gizle()
        {
            if (spinnerPanel != null) spinnerPanel.SetActive(false);
        }

        // ═══════════════════════════════════════════════════
        //  ZORLUK SEÇİMİ
        // ═══════════════════════════════════════════════════

        private void Oyuncu1Sec(ZorlukSeviyesi zorluk)
        {
            oyuncu1Secim = zorluk;
            oyuncu1Secti = true;
            if (oyuncu1SecimText != null)
                oyuncu1SecimText.text = $"Seçim: {ZorlukAdi(zorluk)}";

            SecimleriKontrolEt();
        }

        private void Oyuncu2Sec(ZorlukSeviyesi zorluk)
        {
            oyuncu2Secim = zorluk;
            oyuncu2Secti = true;
            if (oyuncu2SecimText != null)
                oyuncu2SecimText.text = $"Seçim: {ZorlukAdi(zorluk)}";

            SecimleriKontrolEt();
        }

        private void SecimleriKontrolEt()
        {
            // İki oyuncu da seçtiyse döndür butonu aktif olsun
            if (oyuncu1Secti && oyuncu2Secti)
            {
                // Eğer iki seçim de aynıysa, döndürmeye gerek yok
                if (oyuncu1Secim == oyuncu2Secim)
                {
                    SecilenZorluk = oyuncu1Secim;
                    if (sonucText != null)
                        sonucText.text = $"İki oyuncu da {ZorlukAdi(SecilenZorluk)} seçti!";

                    if (dondurButton != null) dondurButton.interactable = false;
                    if (devamButton != null) devamButton.gameObject.SetActive(true);
                }
                else
                {
                    if (dondurButton != null) dondurButton.interactable = true;
                    if (sonucText != null)
                        sonucText.text = "Farklı zorluklar seçildi! Oku döndürün.";
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  OK DÖNDÜRME
        // ═══════════════════════════════════════════════════

        private void OkuDondur()
        {
            if (donuyorMu) return;
            StartCoroutine(OkDonusAnimasyonu());
        }

        private IEnumerator OkDonusAnimasyonu()
        {
            donuyorMu = true;
            if (dondurButton != null) dondurButton.interactable = false;

            float donusSuresi = Random.Range(minDonusSuresi, maxDonusSuresi);
            float gecenSure = 0f;

            // Rastgele toplam dönüş açısı (en az 2 tam tur + rastgele)
            float toplamDonusAcisi = 720f + Random.Range(0f, 360f);

            while (gecenSure < donusSuresi)
            {
                gecenSure += Time.deltaTime;
                float t = gecenSure / donusSuresi;

                // Yavaşlayan dönüş (ease-out)
                float hizCarpani = 1f - Mathf.Pow(t, 2f);
                float mevcutHiz = maxDonusHizi * hizCarpani;

                if (okImage != null)
                {
                    okImage.Rotate(Vector3.forward, -mevcutHiz * Time.deltaTime);
                }

                yield return null;
            }

            // Final açısını belirle
            float finalAci = 0f;
            if (okImage != null)
            {
                finalAci = okImage.eulerAngles.z;
            }

            // 0-180 arası = Oyuncu 1'in seçimi, 180-360 arası = Oyuncu 2'nin seçimi
            bool oyuncu1Kazandi = (finalAci >= 0 && finalAci < 180);

            SecilenZorluk = oyuncu1Kazandi ? oyuncu1Secim : oyuncu2Secim;

            string kazananOyuncu = oyuncu1Kazandi ? "Oyuncu 1" : "Oyuncu 2";
            if (sonucText != null)
                sonucText.text = $"Ok {kazananOyuncu} tarafında durdu!\nZorluk: {ZorlukAdi(SecilenZorluk)}";

            donuyorMu = false;
            if (devamButton != null) devamButton.gameObject.SetActive(true);
        }

        private void Devam()
        {
            OnZorlukSecildi?.Invoke(SecilenZorluk);
            Gizle();
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCI
        // ═══════════════════════════════════════════════════

        private string ZorlukAdi(ZorlukSeviyesi zorluk)
        {
            switch (zorluk)
            {
                case ZorlukSeviyesi.Kolay: return "Kolay";
                case ZorlukSeviyesi.Orta: return "Orta";
                case ZorlukSeviyesi.Zor: return "Zor";
                default: return "Bilinmiyor";
            }
        }

        private void OnDestroy()
        {
            if (oyuncu1KolayBtn != null) oyuncu1KolayBtn.onClick.RemoveAllListeners();
            if (oyuncu1OrtaBtn != null) oyuncu1OrtaBtn.onClick.RemoveAllListeners();
            if (oyuncu1ZorBtn != null) oyuncu1ZorBtn.onClick.RemoveAllListeners();
            if (oyuncu2KolayBtn != null) oyuncu2KolayBtn.onClick.RemoveAllListeners();
            if (oyuncu2OrtaBtn != null) oyuncu2OrtaBtn.onClick.RemoveAllListeners();
            if (oyuncu2ZorBtn != null) oyuncu2ZorBtn.onClick.RemoveAllListeners();
            if (dondurButton != null) dondurButton.onClick.RemoveAllListeners();
            if (devamButton != null) devamButton.onClick.RemoveAllListeners();
        }
    }
}
