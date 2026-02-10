#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using QuizGame.UI;

namespace QuizGame.Editor
{
    /// <summary>
    /// Steampunk UI temasina uygun SoruYonetimPanel ve OgrenciDetayPanel olusturur.
    /// Sahneye ekler ve tum referanslari otomatik baglar.
    /// Menu: Tools > Quiz Game > Steampunk Panelleri Olustur
    /// </summary>
    public static class SteampunkPanelBuilder
    {
        // === Sprite Paths ===
        const string ART             = "Assets/Gentleland/SteampunkUI/Art/";
        const string BUTTONS_SHEET   = ART + "Steampunk_UI_Buttons_3.png";
        const string FRAMES_SHEET    = ART + "Steampunk_UI_Frames_2.png";
        const string SLOTS_SHEET     = ART + "Steampunk_UI_Slots_Arrows.png";

        // === Cached Sprites ===
        static Sprite btnN, btnH, btnP;   // normal, highlighted, pressed
        static Sprite frameBg;            // 9-sliced panel frame
        static Sprite slotBg;             // 9-sliced input/slot background

        // === Theme Colors ===
        static readonly Color CREAM       = new Color(0.94f, 0.88f, 0.73f);
        static readonly Color GOLD        = new Color(1f, 0.92f, 0.65f);
        static readonly Color DARK_BG     = new Color(0.06f, 0.05f, 0.04f, 0.97f);
        static readonly Color INPUT_TEXT  = new Color(0.12f, 0.10f, 0.08f);
        static readonly Color PLACEHOLDER = new Color(0.45f, 0.40f, 0.35f);
        static readonly Color SCROLL_BG   = new Color(0.10f, 0.09f, 0.07f, 0.85f);

        // ===================================================================
        //  ENTRY POINT
        // ===================================================================

        [MenuItem("Tools/Quiz Game/Steampunk Panelleri Olustur")]
        public static void Build()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Hata", "Sahnede Canvas bulunamadi!", "Tamam");
                return;
            }

            if (!LoadSprites())
            {
                EditorUtility.DisplayDialog("Hata",
                    "Steampunk sprite'lari yuklenemedi!\n" +
                    "Gentleland/SteampunkUI/Art/ klasorunu kontrol edin.", "Tamam");
                return;
            }

            // Mevcut panelleri kontrol et
            Transform ex1 = FindChild(canvas.transform, "SoruYonetimPanel");
            Transform ex2 = FindChild(canvas.transform, "OgrenciDetayPanel");
            if (ex1 != null || ex2 != null)
            {
                if (!EditorUtility.DisplayDialog("Uyari",
                    "SoruYonetimPanel veya OgrenciDetayPanel zaten mevcut.\nUstune yazilsin mi?",
                    "Evet", "Iptal"))
                    return;
                if (ex1 != null) Undo.DestroyObjectImmediate(ex1.gameObject);
                if (ex2 != null) Undo.DestroyObjectImmediate(ex2.gameObject);
            }

            Undo.SetCurrentGroupName("Steampunk Panelleri Olustur");
            int undoGroup = Undo.GetCurrentGroup();

            // Panelleri olustur
            GameObject soruPanel     = CreateSoruYonetimPanel(canvas.transform);
            GameObject ogrenciPanel  = CreateOgrenciDetayPanel(canvas.transform);

            Undo.RegisterCreatedObjectUndo(soruPanel, "SoruYonetimPanel");
            Undo.RegisterCreatedObjectUndo(ogrenciPanel, "OgrenciDetayPanel");

            // Referanslari bagla
            WireMainMenuController(canvas, soruPanel);
            WireClassManagementUI(canvas, ogrenciPanel);

            // Baslangicta gizli
            soruPanel.SetActive(false);
            ogrenciPanel.SetActive(false);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=#C4A35A>[SteampunkPanelBuilder]</color> " +
                      "SoruYonetimPanel ve OgrenciDetayPanel basariyla olusturuldu!");
            EditorUtility.DisplayDialog("Basarili",
                "SoruYonetimPanel ve OgrenciDetayPanel olusturuldu.\n\n" +
                "Tum SerializeField referanslari otomatik baglandi.\n" +
                "Sahneyi kaydetmeyi unutmayin!", "Tamam");
        }

        // ===================================================================
        //  ASSET LOADING
        // ===================================================================

        static bool LoadSprites()
        {
            btnN    = FindSprite(BUTTONS_SHEET, "Steampunk_UI_Buttons_3_1");
            btnH    = FindSprite(BUTTONS_SHEET, "Steampunk_UI_Buttons_3_0");
            btnP    = FindSprite(BUTTONS_SHEET, "Steampunk_UI_Buttons_3_2");
            frameBg = FindSprite(FRAMES_SHEET,  "Steampunk_UI_Frames_2_1");
            slotBg  = FindSprite(SLOTS_SHEET,   "Steampunk_UI_Slots_Arrows_9");
            return btnN != null && slotBg != null;
        }

        static Sprite FindSprite(string sheetPath, string spriteName)
        {
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            foreach (Object obj in all)
                if (obj is Sprite s && s.name == spriteName) return s;
            Debug.LogWarning($"[SteampunkPanelBuilder] Sprite bulunamadi: {spriteName} ({sheetPath})");
            return null;
        }

        // ===================================================================
        //  SORU YONETIM PANELI
        // ===================================================================

        static GameObject CreateSoruYonetimPanel(Transform parent)
        {
            // --- Root ---
            var root = MakeFullscreen(parent, "SoruYonetimPanel");
            root.AddComponent<Image>().color = DARK_BG;
            var comp = root.AddComponent<RuntimeQuestionManagerUI>();

            // Frame dekorasyon
            if (frameBg != null)
            {
                var frame = MakeUI("FrameDecor", root.transform);
                StretchFull(frame, 20, 20, -20, -20);
                var fi = frame.AddComponent<Image>();
                fi.sprite   = frameBg;
                fi.type     = Image.Type.Sliced;
                fi.color    = new Color(1, 1, 1, 0.12f);
                fi.raycastTarget = false;
            }

            // ============= SORU LISTESI PANEL =============
            var listePanel = MakeFullscreen(root.transform, "SoruListesiPanel");
            var listeC = MakeUI("Container", listePanel.transform);
            StretchFull(listeC, 40, 35, -40, -35);
            AddVLG(listeC, 10, new RectOffset(10, 10, 10, 10));

            // Header satiri
            var hRow = MakeRow(listeC.transform, "Header", 65, 10);
            var geriBtn = MakeButton(hRow.transform, "GeriBtn", "GERI", 130, 55);
            var titleTmp = MakeText(hRow.transform, "Title", "SORU YONETIMI", 30, GOLD, FontStyles.Bold);
            titleTmp.GetComponent<LayoutElement>().flexibleWidth = 1;
            titleTmp.alignment = TextAlignmentOptions.Center;
            var yeniSoruBtn = MakeButton(hRow.transform, "YeniSoruBtn", "+ YENI SORU", 210, 55);

            // Filtre satiri
            var fRow = MakeRow(listeC.transform, "FilterRow", 48, 8);
            var aramaInput  = MakeInputField(fRow.transform, "AramaInput", "Soru ara...", 240, 42);
            var zorlukDD    = MakeDropdown(fRow.transform, "ZorlukFiltre", "Zorluk", 155, 42);
            var dersDD      = MakeDropdown(fRow.transform, "DersFiltre", "Ders", 155, 42);
            var toplamTmp   = MakeText(fRow.transform, "ToplamText", "Toplam: 0", 14, CREAM);
            toplamTmp.GetComponent<LayoutElement>().preferredWidth = 110;

            // Scroll view
            var (listeScrollGO, listeContent) = MakeScrollView(listeC.transform, "SoruListesi");
            listeScrollGO.GetComponent<LayoutElement>().flexibleHeight = 1;

            // ============= SORU DETAY PANEL =============
            var detayPanel = MakeFullscreen(root.transform, "SoruDetayPanel");
            detayPanel.SetActive(false);
            var detayC = MakeUI("Container", detayPanel.transform);
            StretchFull(detayC, 40, 35, -40, -35);
            AddVLG(detayC, 12, new RectOffset(20, 20, 15, 15));

            // Detay header
            var dHRow = MakeRow(detayC.transform, "Header", 55, 10);
            var detayGeriBtn = MakeButton(dHRow.transform, "DetayGeriBtn", "GERI", 120, 48);
            var detayBaslikTmp = MakeText(dHRow.transform, "DetayBaslik", "", 22, GOLD, FontStyles.Bold);
            detayBaslikTmp.GetComponent<LayoutElement>().flexibleWidth = 1;
            var detayDuzenleBtn = MakeButton(dHRow.transform, "DuzenleBtn", "DUZENLE", 150, 48);
            var detaySilBtn = MakeButton(dHRow.transform, "SilBtn", "SIL", 100, 48);

            // Soru metni
            var detaySoruTmp = MakeText(detayC.transform, "SoruText", "", 19, Color.white);
            detaySoruTmp.GetComponent<LayoutElement>().preferredHeight = 75;
            detaySoruTmp.GetComponent<LayoutElement>().flexibleWidth = 1;
            detaySoruTmp.enableWordWrapping = true;

            // 4 sik satiri
            string[] harfler = { "A", "B", "C", "D" };
            var sikTexts  = new TextMeshProUGUI[4];
            var sikImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var sikRow = MakeUI($"Sik{i}", detayC.transform);
                var sikLE  = sikRow.AddComponent<LayoutElement>();
                sikLE.preferredHeight = 42;
                sikLE.flexibleWidth   = 1;
                var sikBg = sikRow.AddComponent<Image>();
                sikBg.color = new Color(0.18f, 0.16f, 0.13f, 0.75f);
                sikImages[i] = sikBg;

                var sikTextObj = MakeUI("Text", sikRow.transform);
                StretchFull(sikTextObj, 12, 4, -12, -4);
                var sikTmp  = sikTextObj.AddComponent<TextMeshProUGUI>();
                sikTmp.text      = "";
                sikTmp.fontSize  = 17;
                sikTmp.color     = CREAM;
                sikTmp.alignment = TextAlignmentOptions.MidlineLeft;
                sikTexts[i] = sikTmp;
            }

            // Aciklama
            var aciklamaTmp = MakeText(detayC.transform, "AciklamaText", "", 15,
                new Color(0.85f, 0.82f, 0.55f));
            aciklamaTmp.GetComponent<LayoutElement>().preferredHeight = 35;

            // ============= SORU DUZENLE PANEL =============
            var duzenlePanel = MakeFullscreen(root.transform, "SoruDuzenlePanel");
            duzenlePanel.SetActive(false);

            // Scroll icinde form
            var (duzenleScrollGO, duzenleContent) =
                MakeScrollView(duzenlePanel.transform, "DuzenleForm");
            StretchFull(duzenleScrollGO, 40, 35, -40, -35);

            // Form baslik
            var formBaslikTmp = MakeText(duzenleContent, "FormBaslik",
                "Yeni Soru Ekle", 26, GOLD, FontStyles.Bold);
            formBaslikTmp.GetComponent<LayoutElement>().preferredHeight = 50;

            // Soru input
            MakeText(duzenleContent, "Label0", "Soru Metni:", 14, CREAM);
            var formSoruInput = MakeInputField(duzenleContent, "FormSoruInput",
                "Soru metnini girin...", 0, 55);
            formSoruInput.lineType = TMP_InputField.LineType.MultiLineNewline;

            MakeSpacer(duzenleContent, 6);

            // 4x sik + toggle
            var formSikInputs    = new TMP_InputField[4];
            var formDogruToggles = new Toggle[4];
            for (int i = 0; i < 4; i++)
            {
                var sikRow = MakeRow(duzenleContent, $"SikRow{i}", 46, 8);
                var sikLabel = MakeText(sikRow.transform, "L", $"{harfler[i]})", 16, CREAM);
                sikLabel.GetComponent<LayoutElement>().preferredWidth = 30;
                formSikInputs[i] = MakeInputField(sikRow.transform, $"SikInput{i}",
                    $"Sik {harfler[i]}...", 0, 42);
                formDogruToggles[i] = MakeToggle(sikRow.transform, $"DogruToggle{i}", "Dogru");
            }

            MakeSpacer(duzenleContent, 6);

            // Zorluk + Ders dropdown
            var ddRow = MakeRow(duzenleContent, "DDRow", 46, 10);
            MakeText(ddRow.transform, "ZL", "Zorluk:", 14, CREAM)
                .GetComponent<LayoutElement>().preferredWidth = 60;
            var formZorlukDD = MakeDropdown(ddRow.transform, "FormZorlukDD", "Kolay", 155, 42);
            MakeText(ddRow.transform, "DL", "Ders:", 14, CREAM)
                .GetComponent<LayoutElement>().preferredWidth = 45;
            var formDersDD = MakeDropdown(ddRow.transform, "FormDersDD", "Matematik", 155, 42);

            MakeSpacer(duzenleContent, 4);

            // Aciklama input
            MakeText(duzenleContent, "Label1", "Aciklama (opsiyonel):", 14, CREAM);
            var formAciklamaInput = MakeInputField(duzenleContent, "FormAciklamaInput",
                "Aciklama...", 0, 45);

            MakeSpacer(duzenleContent, 10);

            // Kaydet / Iptal
            var btnRow = MakeRow(duzenleContent, "FormBtnRow", 58, 15);
            MakeUI("Spacer", btnRow.transform).AddComponent<LayoutElement>().flexibleWidth = 1;
            var formKaydetBtn = MakeButton(btnRow.transform, "KaydetBtn", "KAYDET", 180, 52);
            var formIptalBtn  = MakeButton(btnRow.transform, "IptalBtn", "IPTAL", 150, 52);

            // ============= WIRE COMPONENT =============
            var so = new SerializedObject(comp);
            // Paneller
            SetRef(so, "soruListesiPanel", listePanel);
            SetRef(so, "soruDetayPanel",   detayPanel);
            SetRef(so, "soruDuzenlePanel", duzenlePanel);
            // Liste
            SetRef(so, "soruListesiContent", listeContent);
            SetRef(so, "aramaInput",         aramaInput);
            SetRef(so, "zorlukFiltre",       zorlukDD);
            SetRef(so, "dersFiltre",         dersDD);
            SetRef(so, "toplamText",         toplamTmp);
            SetRef(so, "yeniSoruButton",     yeniSoruBtn);
            SetRef(so, "geriButton",         geriBtn);
            // Detay
            SetRef(so, "detayBaslik",        detayBaslikTmp);
            SetRef(so, "detaySoruText",      detaySoruTmp);
            SetArray(so, "detaySikTexts",    sikTexts);
            SetArray(so, "detaySikBgImages", sikImages);
            SetRef(so, "detayAciklamaText",  aciklamaTmp);
            SetRef(so, "detayDuzenleButton", detayDuzenleBtn);
            SetRef(so, "detaySilButton",     detaySilBtn);
            SetRef(so, "detayGeriButton",    detayGeriBtn);
            // Form
            SetRef(so, "formBaslik",         formBaslikTmp);
            SetRef(so, "formSoruInput",      formSoruInput);
            SetArray(so, "formSikInputs",    formSikInputs);
            SetArray(so, "formDogruToggles", formDogruToggles);
            SetRef(so, "formZorlukDropdown", formZorlukDD);
            SetRef(so, "formDersDropdown",   formDersDD);
            SetRef(so, "formAciklamaInput",  formAciklamaInput);
            SetRef(so, "formKaydetButton",   formKaydetBtn);
            SetRef(so, "formIptalButton",    formIptalBtn);
            so.ApplyModifiedProperties();

            return root;
        }

        // ===================================================================
        //  OGRENCI DETAY PANELI
        // ===================================================================

        static GameObject CreateOgrenciDetayPanel(Transform parent)
        {
            var root = MakeFullscreen(parent, "OgrenciDetayPanel");
            root.AddComponent<Image>().color = DARK_BG;
            var comp = root.AddComponent<StudentDetailUI>();

            // Frame dekorasyon
            if (frameBg != null)
            {
                var frame = MakeUI("FrameDecor", root.transform);
                StretchFull(frame, 20, 20, -20, -20);
                var fi = frame.AddComponent<Image>();
                fi.sprite = frameBg;
                fi.type   = Image.Type.Sliced;
                fi.color  = new Color(1, 1, 1, 0.12f);
                fi.raycastTarget = false;
            }

            // Ana container
            var container = MakeUI("Container", root.transform);
            StretchFull(container, 40, 35, -40, -35);
            AddVLG(container, 8, new RectOffset(10, 10, 10, 10));

            // Header
            var hRow = MakeRow(container.transform, "Header", 60, 10);
            var geriBtn = MakeButton(hRow.transform, "GeriBtn", "GERI", 120, 50);
            var baslikTmp = MakeText(hRow.transform, "BaslikText", "Ogrenci Detay", 24,
                GOLD, FontStyles.Bold);
            baslikTmp.GetComponent<LayoutElement>().flexibleWidth = 1;
            baslikTmp.alignment = TextAlignmentOptions.Center;

            // Tab Bar
            var tabRow = MakeRow(container.transform, "TabBar", 48, 5);
            string[] tabNames      = { "Genel", "Dersler", "Haftalik", "Gunluk", "Trendler" };
            string[] tabFields     = { "tabGenel", "tabDersler", "tabHaftalik",
                                       "tabGunluk", "tabTrendler" };
            string[] panelFields   = { "genelPanel", "derslerPanel", "haftalikPanel",
                                       "gunlukPanel", "trendlerPanel" };
            string[] contentFields = { "genelContent", "derslerContent", "haftalikContent",
                                       "gunlukContent", "trendlerContent" };

            var tabButtons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                tabButtons[i] = MakeButton(tabRow.transform,
                    "Tab" + tabNames[i], tabNames[i], 0, 42);
                tabButtons[i].GetComponent<LayoutElement>().flexibleWidth = 1;
            }

            // Tab panelleri (her biri scroll view)
            var panels   = new GameObject[5];
            var contents = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                var (scrollGO, contentT) =
                    MakeScrollView(container.transform, tabNames[i]);
                scrollGO.GetComponent<LayoutElement>().flexibleHeight = 1;
                panels[i]   = scrollGO;
                contents[i] = contentT;
                if (i > 0) scrollGO.SetActive(false);
            }

            // Wire
            var so = new SerializedObject(comp);
            SetRef(so, "baslikText", baslikTmp);
            SetRef(so, "geriButton", geriBtn);
            for (int i = 0; i < 5; i++)
            {
                SetRef(so, tabFields[i],     tabButtons[i]);
                SetRef(so, panelFields[i],   panels[i]);
                SetRef(so, contentFields[i], contents[i]);
            }
            so.ApplyModifiedProperties();

            return root;
        }

        // ===================================================================
        //  WIRING
        // ===================================================================

        static void WireMainMenuController(Canvas canvas, GameObject soruPanel)
        {
            var mc = canvas.GetComponentInChildren<MainMenuController>(true);
            if (mc == null)
            {
                mc = Resources.FindObjectsOfTypeAll<MainMenuController>()
                    .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            }
            if (mc == null)
            {
                Debug.LogWarning("[SteampunkPanelBuilder] MainMenuController bulunamadi! " +
                                 "soruYonetimPanel ve sorularButton referanslarini elle baglayin.");
                return;
            }

            var so = new SerializedObject(mc);
            SetRef(so, "soruYonetimPanel", soruPanel);

            // SorularBtn'i bul
            Transform sorularBtnT = FindChild(canvas.transform, "SorularBtn");
            if (sorularBtnT == null)
                sorularBtnT = FindChild(canvas.transform, "SorularButton");

            if (sorularBtnT != null)
            {
                var btn = sorularBtnT.GetComponent<Button>();
                if (btn != null)
                    SetRef(so, "sorularButton", btn);
                else
                    Debug.LogWarning("[SteampunkPanelBuilder] SorularBtn uzerinde Button yok!");
            }
            else
            {
                Debug.LogWarning("[SteampunkPanelBuilder] SorularBtn bulunamadi. " +
                                 "sorularButton referansini elle baglayin.");
            }

            so.ApplyModifiedProperties();
            Debug.Log("[SteampunkPanelBuilder] MainMenuController referanslari baglandi.");
        }

        static void WireClassManagementUI(Canvas canvas, GameObject ogrenciPanel)
        {
            var classUI = canvas.GetComponentInChildren<ClassManagementUI>(true);
            if (classUI == null)
            {
                classUI = Resources.FindObjectsOfTypeAll<ClassManagementUI>()
                    .FirstOrDefault(c => !EditorUtility.IsPersistent(c));
            }
            if (classUI == null)
            {
                Debug.LogWarning("[SteampunkPanelBuilder] ClassManagementUI bulunamadi! " +
                                 "ogrenciDetayUI referansini elle baglayin.");
                return;
            }

            var detayComp = ogrenciPanel.GetComponent<StudentDetailUI>();
            if (detayComp == null) return;

            var so = new SerializedObject(classUI);
            SetRef(so, "ogrenciDetayUI", detayComp);
            so.ApplyModifiedProperties();
            Debug.Log("[SteampunkPanelBuilder] ClassManagementUI referanslari baglandi.");
        }

        // ===================================================================
        //  UI ELEMENT FACTORIES
        // ===================================================================

        /// <summary>Steampunk temali buton. SpriteSwap transition ile.</summary>
        static Button MakeButton(Transform parent, string name, string text,
            float w, float h)
        {
            var go = MakeUI(name, parent);
            var le = go.AddComponent<LayoutElement>();
            if (w > 0) le.preferredWidth = w;
            else le.flexibleWidth = 1;
            le.preferredHeight = h;

            var img  = go.AddComponent<Image>();
            img.sprite = btnN;
            img.type   = Image.Type.Sliced;
            img.color  = Color.white;

            var btn = go.AddComponent<Button>();
            btn.transition    = Selectable.Transition.SpriteSwap;
            btn.targetGraphic = img;

            var ss = new SpriteState();
            ss.highlightedSprite = btnH;
            ss.pressedSprite     = btnP;
            ss.selectedSprite    = btnH;
            btn.spriteState = ss;

            // Text child
            var textGo = MakeUI("Text", go.transform);
            StretchFull(textGo, 8, 4, -8, -4);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = 18;
            tmp.color     = CREAM;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        /// <summary>Steampunk temali TMP_InputField.</summary>
        static TMP_InputField MakeInputField(Transform parent, string name,
            string placeholder, float w, float h)
        {
            var go = MakeUI(name, parent);
            var le = go.AddComponent<LayoutElement>();
            if (w > 0) le.preferredWidth = w;
            else le.flexibleWidth = 1;
            le.preferredHeight = h;

            var bgImg  = go.AddComponent<Image>();
            bgImg.sprite = slotBg;
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = new Color(0.90f, 0.85f, 0.75f);

            var inputField = go.AddComponent<TMP_InputField>();

            // Text Area
            var textArea   = MakeUI("Text Area", go.transform);
            StretchFull(textArea, 10, 5, -10, -5);
            textArea.AddComponent<RectMask2D>();
            var textAreaRT = textArea.GetComponent<RectTransform>();

            // Text
            var textGo  = MakeUI("Text", textArea.transform);
            StretchFull(textGo);
            var textTMP = textGo.AddComponent<TextMeshProUGUI>();
            textTMP.fontSize  = 15;
            textTMP.color     = INPUT_TEXT;
            textTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Placeholder
            var phGo  = MakeUI("Placeholder", textArea.transform);
            StretchFull(phGo);
            var phTMP = phGo.AddComponent<TextMeshProUGUI>();
            phTMP.text      = placeholder;
            phTMP.fontSize  = 15;
            phTMP.color     = PLACEHOLDER;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Wire via SerializedObject
            var ifSO = new SerializedObject(inputField);
            ifSO.FindProperty("m_TextViewport").objectReferenceValue  = textAreaRT;
            ifSO.FindProperty("m_TextComponent").objectReferenceValue = textTMP;
            ifSO.FindProperty("m_Placeholder").objectReferenceValue   = phTMP;
            ifSO.ApplyModifiedProperties();

            inputField.targetGraphic  = bgImg;
            inputField.caretColor     = INPUT_TEXT;
            inputField.selectionColor = new Color(0.6f, 0.5f, 0.3f, 0.4f);

            return inputField;
        }

        /// <summary>Steampunk temali TMP_Dropdown.</summary>
        static TMP_Dropdown MakeDropdown(Transform parent, string name,
            string caption, float w, float h)
        {
            var go = MakeUI(name, parent);
            var le = go.AddComponent<LayoutElement>();
            if (w > 0) le.preferredWidth = w;
            else le.flexibleWidth = 1;
            le.preferredHeight = h;

            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = slotBg;
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = new Color(0.90f, 0.85f, 0.75f);

            var dd = go.AddComponent<TMP_Dropdown>();
            dd.targetGraphic = bgImg;

            // --- Caption Label ---
            var labelGO = MakeUI("Label", go.transform);
            StretchFull(labelGO, 10, 4, -28, -4);
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text      = caption;
            labelTMP.fontSize  = 14;
            labelTMP.color     = INPUT_TEXT;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // --- Arrow ---
            var arrowGO = MakeUI("Arrow", go.transform);
            var arrowRT = arrowGO.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1, 0);
            arrowRT.anchorMax = new Vector2(1, 1);
            arrowRT.pivot     = new Vector2(1, 0.5f);
            arrowRT.sizeDelta = new Vector2(22, 0);
            arrowRT.anchoredPosition = new Vector2(-4, 0);
            var arrowTMP = arrowGO.AddComponent<TextMeshProUGUI>();
            arrowTMP.text      = "\u25BC";
            arrowTMP.fontSize  = 12;
            arrowTMP.color     = INPUT_TEXT;
            arrowTMP.alignment = TextAlignmentOptions.Center;

            // --- Template (inactive) ---
            var template   = MakeUI("Template", go.transform);
            var templateRT = template.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot     = new Vector2(0.5f, 1);
            templateRT.sizeDelta = new Vector2(0, 150);
            var templateBg = template.AddComponent<Image>();
            templateBg.sprite = slotBg;
            templateBg.type   = Image.Type.Sliced;
            templateBg.color  = new Color(0.92f, 0.88f, 0.78f);
            var scroll = template.AddComponent<ScrollRect>();

            // Viewport
            var viewport = MakeUI("Viewport", template.transform);
            StretchFull(viewport);
            viewport.AddComponent<RectMask2D>();

            // Content
            var content   = MakeUI("Content", viewport.transform);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot     = new Vector2(0.5f, 1);
            contentRT.sizeDelta = Vector2.zero;

            // Item template
            var item   = MakeUI("Item", content.transform);
            var itemRT = item.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0, 0.5f);
            itemRT.anchorMax = new Vector2(1, 0.5f);
            itemRT.sizeDelta = new Vector2(0, 30);
            var itemToggle = item.AddComponent<Toggle>();

            var itemBgGO   = MakeUI("Item Background", item.transform);
            StretchFull(itemBgGO);
            var itemBgImg  = itemBgGO.AddComponent<Image>();
            itemBgImg.color = new Color(0.88f, 0.82f, 0.70f);

            var itemChkGO  = MakeUI("Item Checkmark", item.transform);
            StretchFull(itemChkGO);
            var itemChkImg = itemChkGO.AddComponent<Image>();
            itemChkImg.color = new Color(0.70f, 0.60f, 0.40f, 0.5f);

            var itemLblGO  = MakeUI("Item Label", item.transform);
            StretchFull(itemLblGO, 10, 2, -10, -2);
            var itemLblTMP = itemLblGO.AddComponent<TextMeshProUGUI>();
            itemLblTMP.fontSize  = 14;
            itemLblTMP.color     = INPUT_TEXT;
            itemLblTMP.alignment = TextAlignmentOptions.MidlineLeft;

            itemToggle.targetGraphic = itemBgImg;
            itemToggle.graphic       = itemChkImg;

            scroll.content      = contentRT;
            scroll.viewport     = viewport.GetComponent<RectTransform>();
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.horizontal   = false;

            // Wire dropdown internals
            var ddSO = new SerializedObject(dd);
            ddSO.FindProperty("m_Template").objectReferenceValue    = templateRT;
            ddSO.FindProperty("m_CaptionText").objectReferenceValue = labelTMP;
            ddSO.FindProperty("m_ItemText").objectReferenceValue    = itemLblTMP;
            ddSO.ApplyModifiedProperties();

            template.SetActive(false);
            return dd;
        }

        /// <summary>Steampunk temali Toggle (checkbox).</summary>
        static Toggle MakeToggle(Transform parent, string name, string label)
        {
            var go = MakeUI(name, parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = 100;
            le.preferredHeight = 40;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            var toggle = go.AddComponent<Toggle>();

            // Background
            var bgGO = MakeUI("Background", go.transform);
            var bgLE = bgGO.AddComponent<LayoutElement>();
            bgLE.preferredWidth  = 26;
            bgLE.preferredHeight = 26;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = slotBg;
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = new Color(0.85f, 0.80f, 0.70f);

            // Checkmark
            var chkGO = MakeUI("Checkmark", bgGO.transform);
            StretchFull(chkGO, 4, 4, -4, -4);
            var chkImg  = chkGO.AddComponent<Image>();
            chkImg.color = new Color(0.45f, 0.70f, 0.30f);

            // Label
            var lblGO = MakeUI("Label", go.transform);
            lblGO.AddComponent<LayoutElement>().flexibleWidth = 1;
            var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
            lblTMP.text      = label;
            lblTMP.fontSize  = 14;
            lblTMP.color     = CREAM;
            lblTMP.alignment = TextAlignmentOptions.MidlineLeft;

            toggle.targetGraphic = bgImg;
            toggle.graphic       = chkImg;
            toggle.isOn = false;

            return toggle;
        }

        /// <summary>ScrollRect + Viewport + VLG Content olusturur.</summary>
        static (GameObject scrollGO, Transform content) MakeScrollView(
            Transform parent, string name)
        {
            var scrollGO = MakeUI(name + "Scroll", parent);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;
            scrollLE.flexibleWidth  = 1;

            var scrollBg  = scrollGO.AddComponent<Image>();
            scrollBg.color = SCROLL_BG;

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal        = false;
            scrollRect.movementType      = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 20f;

            // Viewport
            var viewport = MakeUI("Viewport", scrollGO.transform);
            StretchFull(viewport);
            viewport.AddComponent<RectMask2D>();

            // Content
            var content   = MakeUI(name + "Content", viewport.transform);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot     = new Vector2(0.5f, 1);
            contentRT.sizeDelta = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content  = contentRT;

            return (scrollGO, content.transform);
        }

        // ===================================================================
        //  LOW-LEVEL HELPERS
        // ===================================================================

        static GameObject MakeUI(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static GameObject MakeFullscreen(Transform parent, string name)
        {
            var go = MakeUI(name, parent);
            StretchFull(go);
            return go;
        }

        static void StretchFull(GameObject go,
            float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(right, top);
        }

        static void AddVLG(GameObject go, float spacing, RectOffset padding)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = padding;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
        }

        static GameObject MakeRow(Transform parent, string name, float height, float spacing)
        {
            var go = MakeUI(name, parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth   = 1;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(4, 4, 2, 2);

            return go;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string text,
            float fontSize, Color color, FontStyles style = FontStyles.Normal)
        {
            var go = MakeUI(name, parent);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = fontSize;
            tmp.color         = color;
            tmp.fontStyle     = style;
            tmp.alignment     = TextAlignmentOptions.MidlineLeft;
            tmp.overflowMode  = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;

            return tmp;
        }

        static void MakeSpacer(Transform parent, float height)
        {
            var sp = MakeUI("Spacer", parent);
            sp.AddComponent<LayoutElement>().preferredHeight = height;
        }

        // ===================================================================
        //  SEARCH & WIRE HELPERS
        // ===================================================================

        static Transform FindChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindChild(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        static void SetRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[SteampunkPanelBuilder] Property bulunamadi: {propName}");
        }

        static void SetArray<T>(SerializedObject so, string propName, T[] values)
            where T : Object
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[SteampunkPanelBuilder] Array bulunamadi: {propName}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
