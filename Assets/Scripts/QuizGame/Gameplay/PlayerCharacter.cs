using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace QuizGame.Gameplay
{
    /// <summary>
    /// 3D oyuncu karakterini ve can sistemini yöneten script.
    /// Her oyuncunun 3 canı, 3D modeli, Animator'ı ve vuruş animasyonu vardır.
    /// 
    /// Kurulum:
    /// - Bu scripti 3D karakter modelinin ROOT objesine ekleyin.
    /// - Animator'a şu trigger'ları ekleyin: "Vurus", "HasarAl", "Olum", "Idle"
    /// - vurusNoktasi: Rakibin vücut merkezine yakın bir boş Transform
    /// </summary>
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("═══ Oyuncu Bilgileri ═══")]
        [SerializeField] private int oyuncuIndex; // 0 = Oyuncu 1, 1 = Oyuncu 2
        [SerializeField] private string oyuncuAdi;

        [Header("═══ Can Sistemi ═══")]
        [SerializeField] private int maksimumCan = 3;
        [SerializeField] private int mevcutCan;

        [Header("═══ Can UI (Canvas üzerindeki kalp ikonları) ═══")]
        [SerializeField] private Image[] canIkonlari; // 3 adet kalp ikonu (HUD'daki)
        [SerializeField] private TextMeshProUGUI canText;
        [SerializeField] private Color canDoluRenk = Color.red;
        [SerializeField] private Color canBosRenk = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("═══ 3D Karakter ═══")]
        [Tooltip("Karakterin Animator componenti. Trigger'lar: Vurus, HasarAl, Olum")]
        [SerializeField] private Animator karakterAnimator;
        [Tooltip("Hasar flash efekti için karakterin tüm SkinnedMeshRenderer'ları.\nBoş bırakılırsa otomatik bulunur.")]
        [SerializeField] private Renderer[] karakterRendererlar;

        [Header("═══ Vuruş Ayarları ═══")]
        [Tooltip("Vuruş efekti prefabı (particle vb.)")]
        [SerializeField] private GameObject vurusEfektiPrefab;
        [Tooltip("Rakibin vuruş alacağı nokta (boş Transform). Karakter modelinin gövde kısmına yerleştirin.")]
        [SerializeField] private Transform vurusNoktasi;
        [Tooltip("Vuruş sırasında karakterin rakibe doğru kayma mesafesi (world unit)")]
        [SerializeField] private float vurusKaymaMesafesi = 1.0f;
        [SerializeField] private float vurusAnimSuresi = 0.6f;

        [Header("═══ Hasar Efekti ═══")]
        [SerializeField] private float titremeSuresi = 0.3f;
        [SerializeField] private float titremeSiddeti = 0.05f;
        [Tooltip("Hasar anında modelin yanıp söneceği renk")]
        [SerializeField] private Color hasarFlashRengi = Color.red;
        [SerializeField] private int flashSayisi = 3;

        // Event'ler
        public System.Action<int> OnCanDegisti; // Kalan can
        public System.Action<int> OnOldu;       // Oyuncu index

        // Durum
        public int MevcutCan => mevcutCan;
        public bool HayattaMi => mevcutCan > 0;
        public int OyuncuIndex => oyuncuIndex;

        // Orijinal material renkleri (flash için)
        private Dictionary<Renderer, Color[]> orijinalRenkler = new Dictionary<Renderer, Color[]>();

        private void Awake()
        {
            mevcutCan = maksimumCan;

            // Renderer'lar atanmamışsa otomatik bul
            if (karakterRendererlar == null || karakterRendererlar.Length == 0)
            {
                karakterRendererlar = GetComponentsInChildren<Renderer>();
            }

            // Orijinal renkleri kaydet
            foreach (var rend in karakterRendererlar)
            {
                Color[] renkler = new Color[rend.materials.Length];
                for (int i = 0; i < rend.materials.Length; i++)
                {
                    if (rend.materials[i].HasProperty("_Color"))
                        renkler[i] = rend.materials[i].color;
                    else
                        renkler[i] = Color.white;
                }
                orijinalRenkler[rend] = renkler;
            }

            // Animator otomatik bul
            if (karakterAnimator == null)
                karakterAnimator = GetComponentInChildren<Animator>();
        }

        public void Baslat(int index, string ad)
        {
            oyuncuIndex = index;
            oyuncuAdi = ad;
            mevcutCan = maksimumCan;
            CanlariGuncelle();

            // Idle animasyona geç
            if (karakterAnimator != null)
                karakterAnimator.SetTrigger("Idle");
        }

        // ═══════════════════════════════════════════════════
        //  CAN SİSTEMİ
        // ═══════════════════════════════════════════════════

        public void HasarAl(int miktar = 1)
        {
            if (!HayattaMi) return;

            mevcutCan = Mathf.Max(0, mevcutCan - miktar);
            CanlariGuncelle();

            // Animator hasar tetikleme
            if (karakterAnimator != null)
                karakterAnimator.SetTrigger("HasarAl");

            // Hasar flash efekti
            StartCoroutine(HasarFlashEfekti());

            // Titreme efekti
            StartCoroutine(TitremeEfekti());

            OnCanDegisti?.Invoke(mevcutCan);
            Debug.Log($"Oyuncu {oyuncuIndex + 1} ({oyuncuAdi}) hasar aldı! Kalan can: {mevcutCan}");

            if (mevcutCan <= 0)
            {
                // Ölüm animasyonu
                if (karakterAnimator != null)
                    karakterAnimator.SetTrigger("Olum");

                Debug.Log($"Oyuncu {oyuncuIndex + 1} ({oyuncuAdi}) elendi!");
                OnOldu?.Invoke(oyuncuIndex);
            }
        }

        public void CanYenile()
        {
            mevcutCan = maksimumCan;
            CanlariGuncelle();

            if (karakterAnimator != null)
                karakterAnimator.SetTrigger("Idle");
        }

        private void CanlariGuncelle()
        {
            // Kalp ikonlarını güncelle (UI tarafı)
            if (canIkonlari != null)
            {
                for (int i = 0; i < canIkonlari.Length; i++)
                {
                    if (canIkonlari[i] != null)
                        canIkonlari[i].color = (i < mevcutCan) ? canDoluRenk : canBosRenk;
                }
            }

            if (canText != null)
                canText.text = $"Can: {mevcutCan}/{maksimumCan}";
        }

        // ═══════════════════════════════════════════════════
        //  VURUŞ ANİMASYONU (3D)
        // ═══════════════════════════════════════════════════

        public void VurusYap(PlayerCharacter hedef)
        {
            StartCoroutine(VurusAnimasyonu(hedef));
        }

        private IEnumerator VurusAnimasyonu(PlayerCharacter hedef)
        {
            // Vuruş animasyonunu tetikle
            if (karakterAnimator != null)
                karakterAnimator.SetTrigger("Vurus");

            // Rakibe doğru küçük bir kayma hareketi
            Vector3 orijinalPoz = transform.position;
            Vector3 yon = (hedef.transform.position - transform.position).normalized;
            Vector3 kaymaPoz = orijinalPoz + yon * vurusKaymaMesafesi;

            // İleri kay
            float t = 0;
            float ileriSure = vurusAnimSuresi * 0.35f;
            while (t < ileriSure)
            {
                t += Time.deltaTime;
                float lerp = Mathf.SmoothStep(0, 1, t / ileriSure);
                transform.position = Vector3.Lerp(orijinalPoz, kaymaPoz, lerp);
                yield return null;
            }

            // Vuruş efekti oluştur (vuruş anı)
            if (vurusEfektiPrefab != null && hedef.vurusNoktasi != null)
            {
                GameObject efekt = Instantiate(vurusEfektiPrefab, hedef.vurusNoktasi.position, Quaternion.identity);
                Destroy(efekt, 2f);
            }

            // Kısa bekleme (vuruş temas anı)
            yield return new WaitForSeconds(0.1f);

            // Geri dön
            t = 0;
            float geriSure = vurusAnimSuresi * 0.55f;
            while (t < geriSure)
            {
                t += Time.deltaTime;
                float lerp = Mathf.SmoothStep(0, 1, t / geriSure);
                transform.position = Vector3.Lerp(kaymaPoz, orijinalPoz, lerp);
                yield return null;
            }

            transform.position = orijinalPoz;
        }

        // ═══════════════════════════════════════════════════
        //  HASAR EFEKTLERİ (3D)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Modelin materyallerini kırmızıya flash yapar.
        /// </summary>
        private IEnumerator HasarFlashEfekti()
        {
            if (karakterRendererlar == null) yield break;

            for (int f = 0; f < flashSayisi; f++)
            {
                // Kırmızıya çevir
                foreach (var rend in karakterRendererlar)
                {
                    foreach (var mat in rend.materials)
                    {
                        if (mat.HasProperty("_Color"))
                            mat.color = hasarFlashRengi;
                    }
                }

                yield return new WaitForSeconds(0.08f);

                // Orijinale geri dön
                foreach (var rend in karakterRendererlar)
                {
                    if (orijinalRenkler.ContainsKey(rend))
                    {
                        Color[] renkler = orijinalRenkler[rend];
                        for (int i = 0; i < rend.materials.Length && i < renkler.Length; i++)
                        {
                            if (rend.materials[i].HasProperty("_Color"))
                                rend.materials[i].color = renkler[i];
                        }
                    }
                }

                yield return new WaitForSeconds(0.08f);
            }
        }

        /// <summary>
        /// 3D modeli yerinde titretir.
        /// </summary>
        private IEnumerator TitremeEfekti()
        {
            Vector3 orijinalPoz = transform.position;
            float gecenSure = 0f;

            while (gecenSure < titremeSuresi)
            {
                gecenSure += Time.deltaTime;
                float x = Random.Range(-titremeSiddeti, titremeSiddeti);
                float z = Random.Range(-titremeSiddeti, titremeSiddeti);
                transform.position = orijinalPoz + new Vector3(x, 0, z);
                yield return null;
            }

            transform.position = orijinalPoz;
        }
    }
}
