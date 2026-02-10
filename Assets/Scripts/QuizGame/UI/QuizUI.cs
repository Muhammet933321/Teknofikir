using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.UI
{
    /// <summary>
    /// Soru ekranını yöneten UI controller.
    /// - Üstte soru metni
    /// - Sol tarafta Oyuncu 1'in 4 şıkkı
    /// - Sağ tarafta Oyuncu 2'nin 4 şıkkı
    /// - Yanlış cevapta 10 saniye ceza
    /// - Doğru cevapta soru aşağı iner ve kaybolur
    /// </summary>
    public class QuizUI : MonoBehaviour
    {
        [Header("═══ Soru Paneli ═══")]
        [SerializeField] private RectTransform soruPanel;
        [SerializeField] private TextMeshProUGUI soruText;
        [SerializeField] private TextMeshProUGUI soruNumarasiText;
        [SerializeField] private TextMeshProUGUI zorlukText;
        [SerializeField] private TextMeshProUGUI kategoriText;

        [Header("═══ Oyuncu 1 Şıkları ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu1AdText;
        [SerializeField] private Button[] oyuncu1Butonlar = new Button[4];
        [SerializeField] private TextMeshProUGUI[] oyuncu1SikTextleri = new TextMeshProUGUI[4];

        [Header("═══ Oyuncu 2 Şıkları ═══")]
        [SerializeField] private TextMeshProUGUI oyuncu2AdText;
        [SerializeField] private Button[] oyuncu2Butonlar = new Button[4];
        [SerializeField] private TextMeshProUGUI[] oyuncu2SikTextleri = new TextMeshProUGUI[4];

        [Header("═══ Ceza Göstergesi ═══")]
        [SerializeField] private GameObject oyuncu1CezaPanel;
        [SerializeField] private TextMeshProUGUI oyuncu1CezaSureText;
        [SerializeField] private GameObject oyuncu2CezaPanel;
        [SerializeField] private TextMeshProUGUI oyuncu2CezaSureText;

        [Header("═══ Bilgi Paneli ═══")]
        [SerializeField] private TextMeshProUGUI bilgiText; // "Doğru!" veya "Yanlış!" gösterir

        [Header("═══ Açıklama Paneli ═══")]
        [Tooltip("DIKKAT: Bu panel soruPanel'in DISINDA, ayri bir panel olmali!\nQuiz panelinin cocugu olursa quiz kapaninca bu da kapanir.")]
        [SerializeField] private GameObject aciklamaPaneli;
        [SerializeField] private TextMeshProUGUI aciklamaText;
        [SerializeField] private Button aciklamaDevamButton;

        [Header("═══ Animasyon Ayarları ═══")]
        [Tooltip("Quiz panelinin transparan olma süresi (fade-out)")]
        [SerializeField] private float kaybolusSuresi = 1.0f;
        [Tooltip("Yeni soru gösterilirken fade-in süresi")]
        [SerializeField] private float fadeInSuresi = 0.5f;

        // Oyun durumu
        private QuestionData mevcutSoru;
        private bool oyuncu1Cezali = false;
        private bool oyuncu2Cezali = false;
        private float oyuncu1CezaZamani;
        private float oyuncu2CezaZamani;
        private float cezaSuresi = 10f;
        private bool soruAktif = false;
        private float soruBaslangicZamani;
        private bool yanlisYapildi = false; // Bu soruda herhangi biri yanlış yaptı mı?
        private bool aciklamaBekliyor = false; // Açıklama paneli açık mı?
        private CanvasGroup quizCanvasGroup; // Root CanvasGroup (fade animasyonu için)

        // Oyuncu bilgileri
        private int oyuncu1OyuncuIndex = 0;
        private int oyuncu2OyuncuIndex = 1;

        // Event: Doğru cevap verildiğinde tetiklenir
        public System.Action<int, QuestionData, float> OnDogruCevap; // oyuncuIndex, soru, cevapSuresi
        public System.Action<int, QuestionData, float, int> OnYanlisCevap; // oyuncuIndex, soru, cevapSuresi, secilenSik
        /// <summary>Soru paneli tamamen kaybolunca tetiklenir. GameManager bunu bekleyerek karakter animasyonunu başlatır.</summary>
        public System.Action OnSoruPaneliKapandi;
        /// <summary>Savaş animasyonu bittikten sonra açıklama gösterilmesi gerekip gerekmediğini belirler.</summary>
        public bool AciklamaGosterilecekMi => yanlisYapildi && mevcutSoru != null && mevcutSoru.AciklamaVar;

        private bool listenersReady;

        private void OnEnable()
        {
            EnsureInit();
            // Her açıldığında ceza durumlarını sıfırla
            CezalariSifirla();
            if (bilgiText != null) bilgiText.text = "";
            if (aciklamaPaneli != null) aciklamaPaneli.SetActive(false);
            // CanvasGroup'u görünür yap
            var cg = QuizCanvasGroupGetir();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            // Butonları bağla
            for (int i = 0; i < 4; i++)
            {
                int sikIndex = i;
                if (oyuncu1Butonlar[i] != null)
                    oyuncu1Butonlar[i].onClick.AddListener(() => Oyuncu1CevapVerdi(sikIndex));
                if (oyuncu2Butonlar[i] != null)
                    oyuncu2Butonlar[i].onClick.AddListener(() => Oyuncu2CevapVerdi(sikIndex));
            }

            // Açıklama devam butonu
            if (aciklamaDevamButton != null)
                aciklamaDevamButton.onClick.AddListener(AciklamaPaneliKapat);
        }

        private void Update()
        {
            // Ceza süresi kontrolü
            if (oyuncu1Cezali)
            {
                float kalan = cezaSuresi - (Time.time - oyuncu1CezaZamani);
                if (kalan <= 0)
                {
                    oyuncu1Cezali = false;
                    Oyuncu1ButonlariAktifEt(true);
                    if (oyuncu1CezaPanel != null) oyuncu1CezaPanel.SetActive(false);
                }
                else if (oyuncu1CezaSureText != null)
                {
                    oyuncu1CezaSureText.text = $"Ceza: {kalan:F1}s";
                }
            }

            if (oyuncu2Cezali)
            {
                float kalan = cezaSuresi - (Time.time - oyuncu2CezaZamani);
                if (kalan <= 0)
                {
                    oyuncu2Cezali = false;
                    Oyuncu2ButonlariAktifEt(true);
                    if (oyuncu2CezaPanel != null) oyuncu2CezaPanel.SetActive(false);
                }
                else if (oyuncu2CezaSureText != null)
                {
                    oyuncu2CezaSureText.text = $"Ceza: {kalan:F1}s";
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  SORU GÖSTER
        // ═══════════════════════════════════════════════════

        public void SoruGoster(QuestionData soru, int soruNumarasi, string oyuncu1Ad, string oyuncu2Ad)
        {
            mevcutSoru = soru;
            soruAktif = true;
            soruBaslangicZamani = Time.time;

            // Panel'i görünür yap ve konumunu sıfırla
            if (soruPanel != null)
            {
                soruPanel.gameObject.SetActive(true);
                soruPanel.anchoredPosition = Vector2.zero;
                var canvasGroup = soruPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null) canvasGroup.alpha = 1f;
            }

            // Soru metnini ayarla
            if (soruText != null) soruText.text = soru.soruMetni;
            if (soruNumarasiText != null) soruNumarasiText.text = $"Soru {soruNumarasi}";
            if (zorlukText != null) zorlukText.text = $"Zorluk: {soru.zorluk}";
            if (kategoriText != null) kategoriText.text = $"Ders: {soru.kategori}";

            // Oyuncu adlarını göster
            if (oyuncu1AdText != null) oyuncu1AdText.text = oyuncu1Ad;
            if (oyuncu2AdText != null) oyuncu2AdText.text = oyuncu2Ad;

            // Şıkları doldur
            string[] sikHarfleri = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                string sikMetni = $"{sikHarfleri[i]}) {soru.siklar[i]}";

                if (oyuncu1SikTextleri[i] != null) oyuncu1SikTextleri[i].text = sikMetni;
                if (oyuncu2SikTextleri[i] != null) oyuncu2SikTextleri[i].text = sikMetni;

                // Buton renklerini sıfırla
                ButonRengiSifirla(oyuncu1Butonlar[i]);
                ButonRengiSifirla(oyuncu2Butonlar[i]);
            }

            // Butonları aktif et (cezalı değilse)
            if (!oyuncu1Cezali) Oyuncu1ButonlariAktifEt(true);
            if (!oyuncu2Cezali) Oyuncu2ButonlariAktifEt(true);

            // Durumları sıfırla
            yanlisYapildi = false;
            aciklamaBekliyor = false;
            if (bilgiText != null) bilgiText.text = "";
            if (aciklamaPaneli != null) aciklamaPaneli.SetActive(false);

            // Quiz panelini fade-in ile göster
            StartCoroutine(FadeInAnimasyonu());
        }

        // ═══════════════════════════════════════════════════
        //  CEVAP İŞLEME
        // ═══════════════════════════════════════════════════

        private void Oyuncu1CevapVerdi(int sikIndex)
        {
            if (!soruAktif || oyuncu1Cezali) return;
            float cevapSuresi = Time.time - soruBaslangicZamani;
            CevapIsle(1, sikIndex, cevapSuresi);
        }

        private void Oyuncu2CevapVerdi(int sikIndex)
        {
            if (!soruAktif || oyuncu2Cezali) return;
            float cevapSuresi = Time.time - soruBaslangicZamani;
            CevapIsle(2, sikIndex, cevapSuresi);
        }

        private void CevapIsle(int oyuncuNo, int sikIndex, float cevapSuresi)
        {
            bool dogru = (sikIndex == mevcutSoru.dogruSikIndex);

            if (dogru)
            {
                // Doğru cevap!
                soruAktif = false;

                // Doğru butonun rengini yeşil yap
                Button dogruButon = oyuncuNo == 1 ? oyuncu1Butonlar[sikIndex] : oyuncu2Butonlar[sikIndex];
                ButonRengiDegistir(dogruButon, Color.green);

                if (bilgiText != null)
                    bilgiText.text = $"Oyuncu {oyuncuNo} DOĞRU! ({cevapSuresi:F1}s)";

                Debug.Log($"Oyuncu {oyuncuNo} doğru cevap verdi! Süre: {cevapSuresi:F1}s");

                // Event tetikle
                int oyuncuIndex = oyuncuNo - 1;
                OnDogruCevap?.Invoke(oyuncuIndex, mevcutSoru, cevapSuresi);

                // Tüm butonları devre dışı bırak
                Oyuncu1ButonlariAktifEt(false);
                Oyuncu2ButonlariAktifEt(false);

                // Her durumda önce quiz panelini fade-out yap
                // Savaş animasyonu sonrası eğer açıklama gerekiyorsa GameManager yönetecek
                StartCoroutine(SoruKaybolmaAnimasyonu());
            }
            else
            {
                // Yanlış cevap!
                yanlisYapildi = true;

                Button yanlisButon = oyuncuNo == 1 ? oyuncu1Butonlar[sikIndex] : oyuncu2Butonlar[sikIndex];
                ButonRengiDegistir(yanlisButon, Color.red);

                Debug.Log($"Oyuncu {oyuncuNo} yanlış cevap verdi! {cezaSuresi}s ceza.");

                // Ceza uygula
                if (oyuncuNo == 1)
                {
                    oyuncu1Cezali = true;
                    oyuncu1CezaZamani = Time.time;
                    Oyuncu1ButonlariAktifEt(false);
                    if (oyuncu1CezaPanel != null) oyuncu1CezaPanel.SetActive(true);
                }
                else
                {
                    oyuncu2Cezali = true;
                    oyuncu2CezaZamani = Time.time;
                    Oyuncu2ButonlariAktifEt(false);
                    if (oyuncu2CezaPanel != null) oyuncu2CezaPanel.SetActive(true);
                }

                // Event tetikle
                int oyuncuIndex = oyuncuNo - 1;
                OnYanlisCevap?.Invoke(oyuncuIndex, mevcutSoru, cevapSuresi, sikIndex);
            }
        }

        // ═══════════════════════════════════════════════════
        //  AÇIKLAMA PANELİ
        // ═══════════════════════════════════════════════════

        /// <summary>Soru panelini kaybet, ardından açıklama panelini göster.
        /// GameManager savaş animasyonundan sonra bunu çağırır.</summary>
        public void AciklamaPaneliAc()
        {
            AciklamaPaneliGoster();
        }

        private void AciklamaPaneliGoster()
        {
            if (aciklamaPaneli == null)
            {
                Debug.LogWarning("AciklamaPaneli atanmamis! Inspector'da atayin.\n" +
                    "ONEMLI: Panel, soruPanel'in DISINDA bagimsiz bir panel olmalidir.");
                // Panel yoksa direkt kapandı say
                OnSoruPaneliKapandi?.Invoke();
                return;
            }

            aciklamaBekliyor = true;

            if (aciklamaText != null)
                aciklamaText.text = mevcutSoru.aciklama;

            aciklamaPaneli.SetActive(true);
        }

        private void AciklamaPaneliKapat()
        {
            if (aciklamaPaneli != null)
                aciklamaPaneli.SetActive(false);

            aciklamaBekliyor = false;

            // Açıklama kapatıldı → artık karakter animasyonuna geçilebilir
            OnSoruPaneliKapandi?.Invoke();
        }

        // ═══════════════════════════════════════════════════
        //  ANİMASYON
        // ═══════════════════════════════════════════════════

        /// <summary>Soru kaybolma animasyonu + tamamlandı event'i (açıklama yoksa).</summary>
        private IEnumerator SoruKaybolmaAnimasyonu()
        {
            yield return StartCoroutine(SoruKaybolmaAnimasyonuInternal());

            // Açıklama gösterilmediyse direkt "kapandı" event'i fırlat
            OnSoruPaneliKapandi?.Invoke();
        }

        /// <summary>Tüm Quiz UI panelini yavaşça transparan yaparak kaybet (fade-out).
        /// Arkadaki 3D karakterler transparan olan UI'ın arkasından görünür hale gelir.</summary>
        private IEnumerator SoruKaybolmaAnimasyonuInternal()
        {
            // Kısa bekleme (doğru cevabı görsünler)
            yield return new WaitForSeconds(1.0f);

            // Tüm quiz panelinin CanvasGroup'unu fade-out yap
            var cg = QuizCanvasGroupGetir();

            float gecenSure = 0f;
            while (gecenSure < kaybolusSuresi)
            {
                gecenSure += Time.deltaTime;
                float t = gecenSure / kaybolusSuresi;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                cg.alpha = Mathf.Lerp(1f, 0f, smoothT);
                yield return null;
            }

            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        /// <summary>Root CanvasGroup'u lazily oluşturur/alır (fade animasyonu için).</summary>
        private CanvasGroup QuizCanvasGroupGetir()
        {
            if (quizCanvasGroup == null)
            {
                quizCanvasGroup = GetComponent<CanvasGroup>();
                if (quizCanvasGroup == null)
                    quizCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            return quizCanvasGroup;
        }

        /// <summary>Quiz panelini fade-in ile gösterir (yeni soru geldiğinde).</summary>
        private IEnumerator FadeInAnimasyonu()
        {
            var cg = QuizCanvasGroupGetir();
            cg.alpha = 0f;
            cg.blocksRaycasts = true;
            cg.interactable = true;

            float t = 0f;
            while (t < fadeInSuresi)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, t / fadeInSuresi);
                yield return null;
            }

            cg.alpha = 1f;
        }

        // ═══════════════════════════════════════════════════
        //  BUTON YARDIMCILARI
        // ═══════════════════════════════════════════════════

        private void Oyuncu1ButonlariAktifEt(bool aktif)
        {
            foreach (var btn in oyuncu1Butonlar)
            {
                if (btn != null) btn.interactable = aktif;
            }
        }

        private void Oyuncu2ButonlariAktifEt(bool aktif)
        {
            foreach (var btn in oyuncu2Butonlar)
            {
                if (btn != null) btn.interactable = aktif;
            }
        }

        private void ButonRengiDegistir(Button btn, Color renk)
        {
            if (btn == null) return;
            var colors = btn.colors;
            colors.normalColor = renk;
            colors.selectedColor = renk;
            colors.highlightedColor = renk;
            btn.colors = colors;
        }

        private void ButonRengiSifirla(Button btn)
        {
            if (btn == null) return;
            var colors = ColorBlock.defaultColorBlock;
            btn.colors = colors;
        }

        public void CezalariSifirla()
        {
            oyuncu1Cezali = false;
            oyuncu2Cezali = false;
            if (oyuncu1CezaPanel != null) oyuncu1CezaPanel.SetActive(false);
            if (oyuncu2CezaPanel != null) oyuncu2CezaPanel.SetActive(false);
        }

        public void Gizle()
        {
            StopAllCoroutines();
            if (soruPanel != null) soruPanel.gameObject.SetActive(false);
            // CanvasGroup'u sıfırla (sonraki kullanım için)
            var cg = QuizCanvasGroupGetir();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < 4; i++)
            {
                if (oyuncu1Butonlar[i] != null) oyuncu1Butonlar[i].onClick.RemoveAllListeners();
                if (oyuncu2Butonlar[i] != null) oyuncu2Butonlar[i].onClick.RemoveAllListeners();
            }
        }
    }
}
