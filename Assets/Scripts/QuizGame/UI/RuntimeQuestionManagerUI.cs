using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.UI
{
    /// <summary>
    /// Runtime soru yönetim paneli. Build sonrası çalışır.
    /// Ana menüden erişim: Sorular butonu.
    /// Kullanıcı soru ekleyip çıkartabilir, düzenleyebilir.
    /// </summary>
    public class RuntimeQuestionManagerUI : MonoBehaviour
    {
        [Header("═══ Paneller ═══")]
        [SerializeField] private GameObject soruListesiPanel;
        [SerializeField] private GameObject soruDetayPanel;
        [SerializeField] private GameObject soruDuzenlePanel;

        [Header("═══ Soru Listesi ═══")]
        [SerializeField] private Transform soruListesiContent;
        [SerializeField] private TMP_InputField aramaInput;
        [SerializeField] private TMP_Dropdown zorlukFiltre;
        [SerializeField] private TMP_Dropdown dersFiltre;
        [SerializeField] private TextMeshProUGUI toplamText;
        [SerializeField] private Button yeniSoruButton;
        [SerializeField] private Button geriButton;

        [Header("═══ Soru Detay ═══")]
        [SerializeField] private TextMeshProUGUI detayBaslik;
        [SerializeField] private TextMeshProUGUI detaySoruText;
        [SerializeField] private TextMeshProUGUI[] detaySikTexts;   // 4 adet
        [SerializeField] private Image[] detaySikBgImages;          // 4 adet (doğru = yeşil)
        [SerializeField] private TextMeshProUGUI detayAciklamaText;
        [SerializeField] private Button detayDuzenleButton;
        [SerializeField] private Button detaySilButton;
        [SerializeField] private Button detayGeriButton;

        [Header("═══ Soru Düzenleme Formu ═══")]
        [SerializeField] private TextMeshProUGUI formBaslik;
        [SerializeField] private TMP_InputField formSoruInput;
        [SerializeField] private TMP_InputField[] formSikInputs;    // 4 adet
        [SerializeField] private Toggle[] formDogruToggles;         // 4 adet
        [SerializeField] private TMP_Dropdown formZorlukDropdown;
        [SerializeField] private TMP_Dropdown formDersDropdown;
        [SerializeField] private TMP_InputField formAciklamaInput;
        [SerializeField] private Button formKaydetButton;
        [SerializeField] private Button formIptalButton;

        // ── Durum ──
        private QuestionData seciliSoru;
        private bool yeniModMu;
        private List<QuestionData> filtreliSorular;
        private bool listenersReady;

        // ── Prefab (dinamik) ──
        private GameObject soruItemPrefab;

        private void OnEnable()
        {
            EnsureInit();
            ListeGoster();
        }

        private void EnsureInit()
        {
            if (listenersReady) return;
            listenersReady = true;

            if (yeniSoruButton != null) yeniSoruButton.onClick.AddListener(YeniSoruFormuAc);
            if (geriButton != null) geriButton.onClick.AddListener(GeriDon);
            if (detayDuzenleButton != null) detayDuzenleButton.onClick.AddListener(DuzenleFormuAc);
            if (detaySilButton != null) detaySilButton.onClick.AddListener(SeciliSoruSil);
            if (detayGeriButton != null) detayGeriButton.onClick.AddListener(ListeGoster);
            if (formKaydetButton != null) formKaydetButton.onClick.AddListener(FormuKaydet);
            if (formIptalButton != null) formIptalButton.onClick.AddListener(ListeGoster);

            if (aramaInput != null) aramaInput.onValueChanged.AddListener((_) => SorulariGuncelle());
            if (zorlukFiltre != null)
            {
                FiltreDropdownDoldur(zorlukFiltre, new string[] { "Tümü", "Kolay", "Orta", "Zor" });
                zorlukFiltre.onValueChanged.AddListener((_) => SorulariGuncelle());
            }
            if (dersFiltre != null)
            {
                FiltreDropdownDoldur(dersFiltre, new string[] { "Tümü", "Matematik", "Türkçe", "Fen", "Sosyal", "İngilizce", "Genel Kültür" });
                dersFiltre.onValueChanged.AddListener((_) => SorulariGuncelle());
            }

            // form zorluk/ders dropdown
            if (formZorlukDropdown != null)
                FiltreDropdownDoldur(formZorlukDropdown, new string[] { "Kolay", "Orta", "Zor" });
            if (formDersDropdown != null)
                FiltreDropdownDoldur(formDersDropdown, new string[] { "Matematik", "Türkçe", "Fen", "Sosyal", "İngilizce", "Genel Kültür" });

            // Toggle group davranışı
            if (formDogruToggles != null)
            {
                for (int i = 0; i < formDogruToggles.Length; i++)
                {
                    int idx = i;
                    if (formDogruToggles[i] != null)
                        formDogruToggles[i].onValueChanged.AddListener((val) =>
                        {
                            if (val) DogruToggleSec(idx);
                        });
                }
            }

            // Prefab oluştur
            if (soruItemPrefab == null) soruItemPrefab = SoruItemPrefabOlustur();
        }

        // ═══════════════════════════════════════════════════
        //  PANEL YÖNETİMİ
        // ═══════════════════════════════════════════════════

        private void ListeGoster()
        {
            PanelGoster(soruListesiPanel);
            SorulariGuncelle();
        }

        private void DetayGoster(QuestionData soru)
        {
            seciliSoru = soru;
            PanelGoster(soruDetayPanel);
            DetayDoldur(soru);
        }

        private void DuzenleFormuAc()
        {
            if (seciliSoru == null) return;
            yeniModMu = false;
            PanelGoster(soruDuzenlePanel);
            FormuDoldur(seciliSoru);
        }

        private void YeniSoruFormuAc()
        {
            yeniModMu = true;
            seciliSoru = null;
            PanelGoster(soruDuzenlePanel);
            FormuTemizle();
        }

        private void PanelGoster(GameObject panel)
        {
            if (soruListesiPanel != null) soruListesiPanel.SetActive(panel == soruListesiPanel);
            if (soruDetayPanel != null) soruDetayPanel.SetActive(panel == soruDetayPanel);
            if (soruDuzenlePanel != null) soruDuzenlePanel.SetActive(panel == soruDuzenlePanel);
        }

        // ═══════════════════════════════════════════════════
        //  LİSTE
        // ═══════════════════════════════════════════════════

        private void SorulariGuncelle()
        {
            if (DataManager.Instance == null) return;
            var db = DataManager.Instance.soruVeritabani;
            if (db == null) return;

            // Filtrele
            filtreliSorular = db.sorular.ToList();

            // Zorluk filtresi
            if (zorlukFiltre != null && zorlukFiltre.value > 0)
                filtreliSorular = filtreliSorular.Where(s => (int)s.zorluk == zorlukFiltre.value - 1).ToList();

            // Ders filtresi
            if (dersFiltre != null && dersFiltre.value > 0)
                filtreliSorular = filtreliSorular.Where(s => (int)s.kategori == dersFiltre.value - 1).ToList();

            // Arama
            if (aramaInput != null && !string.IsNullOrWhiteSpace(aramaInput.text))
            {
                string arama = aramaInput.text.ToLowerInvariant();
                filtreliSorular = filtreliSorular.Where(s =>
                    s.soruMetni.ToLowerInvariant().Contains(arama) ||
                    s.siklar.Any(sik => sik.ToLowerInvariant().Contains(arama))).ToList();
            }

            // Toplam text
            if (toplamText != null)
                toplamText.text = $"Gösterilen: {filtreliSorular.Count} / {db.sorular.Count}";

            // Listeyi güncelle
            ListeyiDoldur();
        }

        private void ListeyiDoldur()
        {
            if (soruListesiContent == null) return;

            // Temizle
            foreach (Transform child in soruListesiContent)
                Destroy(child.gameObject);

            if (filtreliSorular == null) return;

            foreach (var soru in filtreliSorular)
            {
                GameObject item = Instantiate(soruItemPrefab, soruListesiContent);
                item.SetActive(true);
                SoruItemKur(item, soru);
            }
        }

        private void SoruItemKur(GameObject item, QuestionData soru)
        {
            var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                // Zorluk etiketi
                texts[0].text = soru.zorluk == ZorlukSeviyesi.Kolay ? "●K" :
                                soru.zorluk == ZorlukSeviyesi.Orta ? "●O" : "●Z";
                texts[0].color = soru.zorluk == ZorlukSeviyesi.Kolay ? new Color(0.4f, 0.9f, 0.4f) :
                                 soru.zorluk == ZorlukSeviyesi.Orta ? new Color(1f, 0.8f, 0.2f) :
                                 new Color(1f, 0.3f, 0.3f);

                // Ders adı
                texts[1].text = RuntimeGraphRenderer.DersAdi(soru.kategori);
                int dI = (int)soru.kategori;
                texts[1].color = RuntimeGraphRenderer.DersRenkleri[dI % RuntimeGraphRenderer.DersRenkleri.Length];

                // Soru kısa metin
                texts[2].text = soru.soruMetni.Length > 55 ? soru.soruMetni.Substring(0, 55) + "..." : soru.soruMetni;
            }

            // Tıklama - detay aç
            var butonlar = item.GetComponentsInChildren<Button>();
            if (butonlar.Length > 0)
            {
                butonlar[0].onClick.AddListener(() => DetayGoster(soru));
            }
            // Sil butonu
            if (butonlar.Length > 1)
            {
                butonlar[1].onClick.AddListener(() =>
                {
                    DataManager.Instance.soruVeritabani.sorular.Remove(soru);
                    DataManager.Instance.SoruVerisiniKaydet();
                    SorulariGuncelle();
                });
            }
        }

        // ═══════════════════════════════════════════════════
        //  DETAY
        // ═══════════════════════════════════════════════════

        private void DetayDoldur(QuestionData soru)
        {
            if (detayBaslik != null)
            {
                string zorlukStr = soru.zorluk == ZorlukSeviyesi.Kolay ? "Kolay" :
                                   soru.zorluk == ZorlukSeviyesi.Orta ? "Orta" : "Zor";
                detayBaslik.text = $"{RuntimeGraphRenderer.DersAdi(soru.kategori)} — {zorlukStr}";
            }

            if (detaySoruText != null) detaySoruText.text = soru.soruMetni;

            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                if (detaySikTexts != null && i < detaySikTexts.Length && detaySikTexts[i] != null)
                {
                    string prefix = (i == soru.dogruSikIndex) ? "✓ " : "";
                    detaySikTexts[i].text = $"{prefix}{harfler[i]}) {soru.siklar[i]}";
                    detaySikTexts[i].color = (i == soru.dogruSikIndex) ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
                    detaySikTexts[i].fontStyle = (i == soru.dogruSikIndex) ? FontStyles.Bold : FontStyles.Normal;
                }

                if (detaySikBgImages != null && i < detaySikBgImages.Length && detaySikBgImages[i] != null)
                {
                    detaySikBgImages[i].color = (i == soru.dogruSikIndex)
                        ? new Color(0.15f, 0.35f, 0.15f, 0.6f)
                        : new Color(0.2f, 0.2f, 0.25f, 0.4f);
                }
            }

            if (detayAciklamaText != null)
            {
                if (soru.AciklamaVar)
                {
                    detayAciklamaText.gameObject.SetActive(true);
                    detayAciklamaText.text = $"💡 {soru.aciklama}";
                }
                else
                {
                    detayAciklamaText.gameObject.SetActive(false);
                }
            }
        }

        private void SeciliSoruSil()
        {
            if (seciliSoru == null || DataManager.Instance == null) return;
            DataManager.Instance.soruVeritabani.sorular.Remove(seciliSoru);
            DataManager.Instance.SoruVerisiniKaydet();
            seciliSoru = null;
            ListeGoster();
        }

        // ═══════════════════════════════════════════════════
        //  DÜZENLEME FORMU
        // ═══════════════════════════════════════════════════

        private void FormuDoldur(QuestionData soru)
        {
            if (formBaslik != null) formBaslik.text = "Soru Düzenle";
            if (formSoruInput != null) formSoruInput.text = soru.soruMetni;

            for (int i = 0; i < 4; i++)
            {
                if (formSikInputs != null && i < formSikInputs.Length && formSikInputs[i] != null)
                    formSikInputs[i].text = soru.siklar[i];
                if (formDogruToggles != null && i < formDogruToggles.Length && formDogruToggles[i] != null)
                    formDogruToggles[i].isOn = (i == soru.dogruSikIndex);
            }

            if (formZorlukDropdown != null) formZorlukDropdown.value = (int)soru.zorluk;
            if (formDersDropdown != null) formDersDropdown.value = (int)soru.kategori;
            if (formAciklamaInput != null) formAciklamaInput.text = soru.aciklama ?? "";
        }

        private void FormuTemizle()
        {
            if (formBaslik != null) formBaslik.text = "Yeni Soru Ekle";
            if (formSoruInput != null) formSoruInput.text = "";
            for (int i = 0; i < 4; i++)
            {
                if (formSikInputs != null && i < formSikInputs.Length && formSikInputs[i] != null)
                    formSikInputs[i].text = "";
                if (formDogruToggles != null && i < formDogruToggles.Length && formDogruToggles[i] != null)
                    formDogruToggles[i].isOn = (i == 0);
            }
            if (formZorlukDropdown != null) formZorlukDropdown.value = 0;
            if (formDersDropdown != null) formDersDropdown.value = 0;
            if (formAciklamaInput != null) formAciklamaInput.text = "";
        }

        private void DogruToggleSec(int idx)
        {
            if (formDogruToggles == null) return;
            for (int i = 0; i < formDogruToggles.Length; i++)
            {
                if (i != idx && formDogruToggles[i] != null)
                    formDogruToggles[i].SetIsOnWithoutNotify(false);
            }
        }

        private void FormuKaydet()
        {
            if (DataManager.Instance == null) return;

            string soruMetni = formSoruInput != null ? formSoruInput.text.Trim() : "";
            if (string.IsNullOrWhiteSpace(soruMetni))
            {
                Debug.LogWarning("Soru metni boş olamaz!");
                return;
            }

            string[] siklar = new string[4];
            for (int i = 0; i < 4; i++)
            {
                siklar[i] = (formSikInputs != null && i < formSikInputs.Length && formSikInputs[i] != null)
                    ? formSikInputs[i].text.Trim() : "";
                if (string.IsNullOrWhiteSpace(siklar[i]))
                {
                    Debug.LogWarning($"Şık {i + 1} boş olamaz!");
                    return;
                }
            }

            int dogruSik = 0;
            if (formDogruToggles != null)
                for (int i = 0; i < formDogruToggles.Length; i++)
                    if (formDogruToggles[i] != null && formDogruToggles[i].isOn) { dogruSik = i; break; }

            ZorlukSeviyesi zorluk = formZorlukDropdown != null ? (ZorlukSeviyesi)formZorlukDropdown.value : ZorlukSeviyesi.Kolay;
            DersKategorisi ders = formDersDropdown != null ? (DersKategorisi)formDersDropdown.value : DersKategorisi.Matematik;
            string aciklama = formAciklamaInput != null ? formAciklamaInput.text.Trim() : "";

            if (yeniModMu)
            {
                var yeni = new QuestionData(soruMetni, siklar, dogruSik, zorluk, ders, aciklama);
                DataManager.Instance.soruVeritabani.sorular.Add(yeni);
            }
            else if (seciliSoru != null)
            {
                seciliSoru.soruMetni = soruMetni;
                seciliSoru.siklar = siklar;
                seciliSoru.dogruSikIndex = dogruSik;
                seciliSoru.zorluk = zorluk;
                seciliSoru.kategori = ders;
                seciliSoru.aciklama = aciklama;
            }

            DataManager.Instance.SoruVerisiniKaydet();
            ListeGoster();
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCILAR
        // ═══════════════════════════════════════════════════

        private void FiltreDropdownDoldur(TMP_Dropdown dropdown, string[] secenekler)
        {
            dropdown.ClearOptions();
            var opts = new List<TMP_Dropdown.OptionData>();
            foreach (var s in secenekler) opts.Add(new TMP_Dropdown.OptionData(s));
            dropdown.AddOptions(opts);
        }

        private void GeriDon()
        {
            gameObject.SetActive(false);
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.AnaMenuyuGoster();
        }

        private void OnDestroy()
        {
            if (yeniSoruButton != null) yeniSoruButton.onClick.RemoveAllListeners();
            if (geriButton != null) geriButton.onClick.RemoveAllListeners();
            if (detayDuzenleButton != null) detayDuzenleButton.onClick.RemoveAllListeners();
            if (detaySilButton != null) detaySilButton.onClick.RemoveAllListeners();
            if (detayGeriButton != null) detayGeriButton.onClick.RemoveAllListeners();
            if (formKaydetButton != null) formKaydetButton.onClick.RemoveAllListeners();
            if (formIptalButton != null) formIptalButton.onClick.RemoveAllListeners();
        }

        // ═══════════════════════════════════════════════════
        //  DİNAMİK PREFAB
        // ═══════════════════════════════════════════════════

        private GameObject SoruItemPrefabOlustur()
        {
            // Layout: [ZorlukTag] [DersTag] [Soru Metni Buton] [Sil]
            GameObject item = new GameObject("SoruItem_Prefab");
            item.SetActive(false);

            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 50);

            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(8, 8, 4, 4);

            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.18f, 0.25f, 0.85f);

            // Zorluk tag
            GameObject ztObj = new GameObject("ZorlukTag");
            ztObj.transform.SetParent(item.transform, false);
            ztObj.AddComponent<RectTransform>();
            LayoutElement ztLE = ztObj.AddComponent<LayoutElement>();
            ztLE.preferredWidth = 30;
            var ztTmp = ztObj.AddComponent<TextMeshProUGUI>();
            ztTmp.fontSize = 14;
            ztTmp.alignment = TextAlignmentOptions.Center;

            // Ders tag
            GameObject dtObj = new GameObject("DersTag");
            dtObj.transform.SetParent(item.transform, false);
            dtObj.AddComponent<RectTransform>();
            LayoutElement dtLE = dtObj.AddComponent<LayoutElement>();
            dtLE.preferredWidth = 85;
            var dtTmp = dtObj.AddComponent<TextMeshProUGUI>();
            dtTmp.fontSize = 13;
            dtTmp.alignment = TextAlignmentOptions.Left;

            // Soru butonu
            GameObject soruBtn = new GameObject("SoruButton");
            soruBtn.transform.SetParent(item.transform, false);
            soruBtn.AddComponent<RectTransform>();
            LayoutElement soruLE = soruBtn.AddComponent<LayoutElement>();
            soruLE.flexibleWidth = 1;
            soruLE.preferredHeight = 42;
            Image soruBg = soruBtn.AddComponent<Image>();
            soruBg.color = new Color(0.2f, 0.22f, 0.3f, 0.5f);
            soruBtn.AddComponent<Button>();

            GameObject soruText = new GameObject("Text");
            soruText.transform.SetParent(soruBtn.transform, false);
            RectTransform stRect = soruText.AddComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = new Vector2(8, 0);
            stRect.offsetMax = new Vector2(-8, 0);
            var stTmp = soruText.AddComponent<TextMeshProUGUI>();
            stTmp.fontSize = 14;
            stTmp.color = Color.white;
            stTmp.alignment = TextAlignmentOptions.Left;

            // Sil butonu
            GameObject silBtn = new GameObject("SilButton");
            silBtn.transform.SetParent(item.transform, false);
            silBtn.AddComponent<RectTransform>();
            LayoutElement silLE = silBtn.AddComponent<LayoutElement>();
            silLE.preferredWidth = 55;
            silLE.preferredHeight = 42;
            Image silBg = silBtn.AddComponent<Image>();
            silBg.color = new Color(0.65f, 0.2f, 0.2f);
            silBtn.AddComponent<Button>();

            GameObject silText = new GameObject("Text");
            silText.transform.SetParent(silBtn.transform, false);
            RectTransform slRect = silText.AddComponent<RectTransform>();
            slRect.anchorMin = Vector2.zero;
            slRect.anchorMax = Vector2.one;
            slRect.offsetMin = Vector2.zero;
            slRect.offsetMax = Vector2.zero;
            var slTmp = silText.AddComponent<TextMeshProUGUI>();
            slTmp.text = "SİL";
            slTmp.fontSize = 14;
            slTmp.color = Color.white;
            slTmp.alignment = TextAlignmentOptions.Center;
            slTmp.fontStyle = FontStyles.Bold;

            item.transform.SetParent(transform, false);
            return item;
        }
    }
}
