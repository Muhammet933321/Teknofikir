using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using QuizGame.UI;
using QuizGame.Managers;

namespace QuizGame.Editor
{
    /// <summary>
    /// SteampunkUI assetini kullanarak tüm Quiz Game panellerini otomatik oluşturur.
    /// Ana menüdeki stil aynen korunur: Arvo SDF Gold 1, Button 8, Frame 6.
    /// Menü: QuizGame > Steampunk Panel Builder
    /// </summary>
    public class SteampunkPanelBuilder : EditorWindow
    {
        #region ══════ Constants ══════

        private const string FRAME_FMT = "Assets/Gentleland/SteampunkUI/Prefabs/Frames/Frame {0}.prefab";
        private const string BTN_FMT = "Assets/Gentleland/SteampunkUI/Prefabs/Buttons/Button {0}.prefab";
        private const string BTN_ALT_FMT = "Assets/Gentleland/SteampunkUI/Prefabs/Buttons/Button {0} Alternative Color.prefab";
        private const string INPUT_PATH = "Assets/Gentleland/SteampunkUI/Prefabs/Input/Input1.prefab";
        private const string BAR_FMT = "Assets/Gentleland/SteampunkUI/Prefabs/Sliders And Bars/Bar {0}.prefab";
        private const string ICON_BTN_FMT = "Assets/Gentleland/SteampunkUI/Prefabs/Icon Buttons/{0}_icon_button.prefab";

        // Ana menüde kullanılan font: Arvo SDF + Gold 1 material
        private const string FONT_PATH = "Assets/Gentleland/SteampunkUI/Fonts/Arvo/Arvo SDF.asset";
        private const string GOLD_MAT_PATH = "Assets/Gentleland/SteampunkUI/Fonts/Arvo/Arvo SDF Gold 1.mat";
        private const string HP_SPRITE_PATH = "Assets/Gentleland/SteampunkUI/Art/Steampunk_UI_HP_Bars.png";
        private const string ARROW_SPRITE_PATH = "Assets/Gentleland/SteampunkUI/Art/Steampunk_UI_Slots_Arrows.png";

        // Steampunk color palette
        private static readonly Color GOLD = new Color(0.90f, 0.78f, 0.45f);
        private static readonly Color CREAM = new Color(0.85f, 0.80f, 0.70f);
        private static readonly Color DARK_BG = new Color(0.10f, 0.08f, 0.06f, 0.95f);
        private static readonly Color PANEL_BG = new Color(0.16f, 0.13f, 0.10f, 0.90f);
        private static readonly Color COPPER = new Color(0.70f, 0.55f, 0.25f);
        private static readonly Color RED = new Color(0.80f, 0.22f, 0.18f);
        private static readonly Color GREEN = new Color(0.30f, 0.72f, 0.30f);
        private static readonly Color OVERLAY_BG = new Color(0, 0, 0, 0.70f);
        private static readonly Color SCROLL_BG = new Color(0.08f, 0.06f, 0.05f, 0.60f);

        // Auto Size defaults (ana menüdeki gibi)
        private const float AUTO_SIZE_MIN = 18f;
        private const float AUTO_SIZE_MAX = 120f;

        // UI Standart boyutlar
        private const float BTN_W = 370f;
        private const float BTN_H = 85f;
        private const float BTN_FONT = 42f;
        private const float UI_FONT = 42f;

        #endregion

        #region ══════ Fields ══════

        // Ana menüdeki default değerler
        private int frameStyle = 6;
        private int btnStyle = 8;     // Ana menü Button 8 kullanıyor
        private int altBtnStyle = 3;  // Alternatif renk
        private int barStyle = 1;
        private Vector2 scrollPos;

        private TMP_FontAsset font;
        private Material goldMat;

        #endregion

        #region ══════ Editor Window ══════

        [MenuItem("QuizGame/Steampunk Panel Builder")]
        public static void ShowWindow()
        {
            var w = GetWindow<SteampunkPanelBuilder>("Panel Builder");
            w.minSize = new Vector2(320, 520);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(5);
            GUILayout.Label("STEAMPUNK QUIZ GAME", EditorStyles.boldLabel);
            GUILayout.Label("Ana menü stili: Arvo SDF Gold 1, Button 8, Frame 6", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            frameStyle = EditorGUILayout.IntSlider("Frame Stili (1-16)", frameStyle, 1, 16);
            btnStyle = EditorGUILayout.IntSlider("Ana Buton Stili (1-12)", btnStyle, 1, 12);
            altBtnStyle = EditorGUILayout.IntSlider("Alt Buton Stili (1-5)", altBtnStyle, 1, 5);
            barStyle = EditorGUILayout.IntSlider("Bar/Slider Stili (1-14)", barStyle, 1, 14);

            EditorGUILayout.Space(10);
            EditorGUI.BeginDisabledGroup(FindCanvas() == null);
            if (GUILayout.Button("TÜM PANELLERİ OLUŞTUR VE BAĞLA", GUILayout.Height(36)))
                BuildAll();
            EditorGUI.EndDisabledGroup();

            if (FindCanvas() == null)
                EditorGUILayout.HelpBox("Sahnede Canvas bulunamadı!", MessageType.Warning);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Tek Tek Oluştur:", EditorStyles.boldLabel);

            if (GUILayout.Button("1 - Sınıf Yönetim Paneli")) BuildSinifYonetim();
            if (GUILayout.Button("2 - Oyuncu Seçim Paneli")) BuildOyuncuSecim();
            if (GUILayout.Button("3 - Ayarlar Paneli")) BuildAyarlar();
            if (GUILayout.Button("4 - Zorluk Spinner Paneli")) BuildSpinner();
            if (GUILayout.Button("5 - Quiz Soru Paneli")) BuildQuiz();
            if (GUILayout.Button("6 - HUD Paneli")) BuildHUD();
            if (GUILayout.Button("7 - Oyun Sonu Paneli")) BuildGameOver();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button("Referansları Bağla (MainMenu + GameManager)"))
                WireManagerRefs();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region ══════ Asset Loading ══════

        private void LoadAssets()
        {
            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (goldMat == null)
                goldMat = AssetDatabase.LoadAssetAtPath<Material>(GOLD_MAT_PATH);
        }

        private Canvas FindCanvas()
        {
            return FindObjectOfType<Canvas>();
        }

        private GameObject LoadPrefab(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        #endregion

        #region ══════ UI Helper Methods ══════

        // ─── Full-screen Panel ───
        private GameObject MakePanel(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Panel Mevcut",
                    $"'{name}' zaten var. Yeniden oluşturulsun mu?", "Evet", "Hayır"))
                    return null;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            return go;
        }

        // ─── Steampunk Frame (Frame 6 yapısı: ForeGround, Decoration, Decoration(1), Decoration(2)) ───
        private GameObject MakeFrame(Transform parent, string name = "Frame")
        {
            var prefab = LoadPrefab(string.Format(FRAME_FMT, frameStyle));
            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                go.GetComponent<Image>().color = PANEL_BG;
            }
            go.name = name;
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        /// <summary>
        /// Frame prefab'ın yapısına uygun Content alanı. Frame 6 içindeki
        /// ForeGround, Decoration ve Decoration(1/2) elementlerinin sonrasına yerleşir.
        /// Anchor ile çerçeve kenarlarından yeterli pay bırakılır.
        /// </summary>
        private GameObject MakeFrameContent(Transform frameParent, string contentName = "Content")
        {
            var go = new GameObject(contentName, typeof(RectTransform));
            go.transform.SetParent(frameParent, false);
            var rt = go.GetComponent<RectTransform>();
            // Frame 6 kenarlarına göre uygun iç boşluk (%7 - %93 civarı)
            rt.anchorMin = new Vector2(0.04f, 0.06f);
            rt.anchorMax = new Vector2(0.96f, 0.94f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        // ─── Steampunk Button (Ana menüdeki gibi: Button 8 + Auto Size + Arvo SDF Gold) ───
        private (GameObject go, Button btn, TextMeshProUGUI label) MakeBtn(
            Transform parent, string text, Vector2 size, bool alternative = false)
        {
            string path = alternative
                ? string.Format(BTN_ALT_FMT, altBtnStyle)
                : string.Format(BTN_FMT, btnStyle);
            var prefab = LoadPrefab(path);
            if (prefab == null && alternative)
                prefab = LoadPrefab(string.Format(BTN_FMT, btnStyle));

            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                go.GetComponent<Image>().color = COPPER;
            }

            go.name = SanitizeName(text) + "Btn";
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null)
            {
                var txtGo = new GameObject("Text (TMP)", typeof(RectTransform));
                txtGo.transform.SetParent(go.transform, false);
                Stretch(txtGo.GetComponent<RectTransform>(), 10, 5);
                tmp = txtGo.AddComponent<TextMeshProUGUI>();
            }

            tmp.text = text;
            tmp.fontSize = BTN_FONT;
            tmp.enableAutoSizing = false;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;  // Ana menüdeki gibi beyaz (material gold yapar)
            if (font != null) tmp.font = font;
            if (goldMat != null) tmp.fontSharedMaterial = goldMat;

            return (go, btn, tmp);
        }

        // ─── Small Delete Button (plain red) ───
        private (GameObject go, Button btn) MakeDeleteBtn(Transform parent, Vector2 size)
        {
            var go = new GameObject("SilBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            go.GetComponent<Image>().color = RED;

            var txtGo = new GameObject("Text (TMP)", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            Stretch(txtGo.GetComponent<RectTransform>(), 4, 2);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "SİL";
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12;
            tmp.fontSizeMax = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            if (font != null) tmp.font = font;

            return (go, go.GetComponent<Button>());
        }

        // ─── TMP Text (Ana menüdeki gibi: Arvo SDF, auto-size destekli) ───
        private (GameObject go, TextMeshProUGUI tmp) MakeTxt(
            Transform parent, string text, float fontSize,
            TextAlignmentOptions align = TextAlignmentOptions.Center,
            bool gold = false, bool autoSize = false)
        {
            string goName = string.IsNullOrEmpty(text) ? "Text" : SanitizeName(text) + "Text";
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = align;

            if (autoSize)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = AUTO_SIZE_MIN;
                tmp.fontSizeMax = fontSize; // Üst sınır olarak fontSize kullanılır
            }
            else
            {
                tmp.fontSize = fontSize;
            }

            tmp.color = gold ? Color.white : CREAM;
            if (font != null) tmp.font = font;
            if (gold && goldMat != null) tmp.fontSharedMaterial = goldMat;
            return (go, tmp);
        }

        // ─── TMP Input Field ───
        private (GameObject go, TMP_InputField input) MakeInput(
            Transform parent, string placeholder, Vector2 size)
        {
            var prefab = LoadPrefab(INPUT_PATH);
            GameObject go;
            TMP_InputField input;

            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                input = go.GetComponent<TMP_InputField>();
                go.name = SanitizeName(placeholder) + "Input";
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, size.y); // Width 0, layout ile doldurulacak

                // Layout içinde yatay olarak genişlesin
                var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.preferredHeight = size.y;

                if (input != null)
                {
                    var ph = input.placeholder as TextMeshProUGUI;
                    if (ph != null)
                    {
                        ph.text = placeholder;
                        ph.fontSize = UI_FONT * 0.75f; // Placeholder biraz küçük
                        if (font != null) ph.font = font;
                    }
                    if (input.textComponent != null)
                    {
                        var tc = input.textComponent as TextMeshProUGUI;
                        if (tc != null)
                        {
                            tc.fontSize = UI_FONT * 0.85f;
                            if (font != null) tc.font = font;
                        }
                    }
                }
                return (go, input);
            }

            // Fallback: manual creation
            go = new GameObject(SanitizeName(placeholder) + "Input",
                typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, size.y);
            go.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f, 0.9f);

            // Layout içinde yatay olarak genişlesin
            var leF = go.AddComponent<LayoutElement>();
            leF.flexibleWidth = 1;
            leF.preferredHeight = size.y;

            var textArea = new GameObject("TextArea", typeof(RectTransform));
            textArea.transform.SetParent(go.transform, false);
            Stretch(textArea.GetComponent<RectTransform>(), 10, 6);

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(textArea.transform, false);
            Stretch(phGo.GetComponent<RectTransform>());
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.text = placeholder;
            phTmp.fontSize = UI_FONT * 0.75f;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.5f, 0.45f, 0.35f, 0.6f);
            if (font != null) phTmp.font = font;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(textArea.transform, false);
            Stretch(txtGo.GetComponent<RectTransform>());
            var txtTmp = txtGo.AddComponent<TextMeshProUGUI>();
            txtTmp.fontSize = UI_FONT * 0.85f;
            txtTmp.color = CREAM;
            if (font != null) txtTmp.font = font;

            input = go.AddComponent<TMP_InputField>();
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = txtTmp;
            input.placeholder = phTmp;
            input.fontAsset = font;

            return (go, input);
        }

        // ─── TMP Dropdown ───
        private (GameObject go, TMP_Dropdown dropdown) MakeDrop(
            Transform parent, string defaultText, Vector2 size)
        {
            var go = new GameObject(SanitizeName(defaultText) + "Dropdown",
                typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.10f, 0.92f);

            var dropdown = go.AddComponent<TMP_Dropdown>();

            // Label
            var (labelGo, labelTmp) = MakeTxt(go.transform, defaultText, 16,
                TextAlignmentOptions.Left);
            Stretch(labelGo.GetComponent<RectTransform>(), 10, 0);
            labelGo.GetComponent<RectTransform>().offsetMax = new Vector2(-30, 0);
            dropdown.captionText = labelTmp;

            // Arrow indicator
            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0);
            arrowRt.anchorMax = Vector2.one;
            arrowRt.sizeDelta = new Vector2(25, 0);
            arrowRt.anchoredPosition = new Vector2(-15, 0);
            arrowGo.GetComponent<Image>().color = COPPER;

            // Template
            var templateGo = new GameObject("Template",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(go.transform, false);
            var tplRt = templateGo.GetComponent<RectTransform>();
            tplRt.anchorMin = new Vector2(0, 0);
            tplRt.anchorMax = new Vector2(1, 0);
            tplRt.pivot = new Vector2(0.5f, 1f);
            tplRt.sizeDelta = new Vector2(0, 150);
            templateGo.GetComponent<Image>().color = DARK_BG;

            // Viewport
            var vpGo = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            vpGo.transform.SetParent(templateGo.transform, false);
            Stretch(vpGo.GetComponent<RectTransform>());
            vpGo.GetComponent<Mask>().showMaskGraphic = false;
            vpGo.GetComponent<Image>().color = Color.white;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpGo.transform, false);
            var cRt = contentGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = Vector2.one;
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 28);

            var scrollRect = templateGo.GetComponent<ScrollRect>();
            scrollRect.viewport = vpGo.GetComponent<RectTransform>();
            scrollRect.content = cRt;

            // Item
            var itemGo = new GameObject("Item",
                typeof(RectTransform), typeof(Toggle));
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRt = itemGo.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 28);

            var itemBg = new GameObject("Item Background",
                typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(itemGo.transform, false);
            Stretch(itemBg.GetComponent<RectTransform>());
            itemBg.GetComponent<Image>().color = PANEL_BG;

            var checkGo = new GameObject("Item Checkmark",
                typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(itemGo.transform, false);
            var ckRt = checkGo.GetComponent<RectTransform>();
            ckRt.anchorMin = new Vector2(0, 0.5f);
            ckRt.anchorMax = new Vector2(0, 0.5f);
            ckRt.sizeDelta = new Vector2(20, 20);
            ckRt.anchoredPosition = new Vector2(12, 0);
            checkGo.GetComponent<Image>().color = COPPER;

            var (itemLabelGo, itemLabelTmp) = MakeTxt(itemGo.transform, "Option", 16,
                TextAlignmentOptions.Left);
            Stretch(itemLabelGo.GetComponent<RectTransform>(), 28, 0);

            var toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg.GetComponent<Image>();
            toggle.graphic = checkGo.GetComponent<Image>();
            toggle.isOn = true;

            dropdown.template = tplRt;
            dropdown.itemText = itemLabelTmp;

            templateGo.SetActive(false);
            return (go, dropdown);
        }

        // ─── Slider (tries Steampunk Bar prefab) ───
        private (GameObject go, Slider slider) MakeSlider(Transform parent, Vector2 size)
        {
            var prefab = LoadPrefab(string.Format(BAR_FMT, barStyle));
            if (prefab != null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.GetComponent<RectTransform>().sizeDelta = size;
                var slider = go.GetComponent<Slider>();
                if (slider != null) return (go, slider);
                // If prefab doesn't have Slider, add one
                slider = go.AddComponent<Slider>();
                return (go, slider);
            }

            // Fallback: manual slider
            var sGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sGo.transform.SetParent(parent, false);
            sGo.GetComponent<RectTransform>().sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sGo.transform, false);
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = DARK_BG;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sGo.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), 5, 5);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = COPPER;

            var sl = sGo.GetComponent<Slider>();
            sl.fillRect = fill.GetComponent<RectTransform>();
            sl.targetGraphic = bg.GetComponent<Image>();

            return (sGo, sl);
        }

        // ─── Scroll View (daha geniş padding ve spacing) ───
        private (GameObject go, Transform content) MakeScroll(Transform parent, Vector2 size)
        {
            var go = new GameObject("ScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = size;
            go.GetComponent<Image>().color = SCROLL_BG;

            var scrollRect = go.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var vp = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            vp.transform.SetParent(go.transform, false);
            Stretch(vp.GetComponent<RectTransform>());
            vp.GetComponent<Mask>().showMaskGraphic = false;
            vp.GetComponent<Image>().color = Color.white;
            scrollRect.viewport = vp.GetComponent<RectTransform>();

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vp.transform, false);
            var cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = Vector2.one;
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 6;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cRt;
            return (go, content.transform);
        }

        // ─── Content Container (eski - uyumluluk için) ───
        private GameObject MakeContent(Transform parent, string name = "Content")
        {
            return MakeFrameContent(parent, name);
        }

        // ─── Dark Overlay Background (for popups) ───
        private GameObject MakeOverlay(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            go.GetComponent<Image>().color = OVERLAY_BG;
            go.SetActive(false);
            return go;
        }

        // ─── Horizontal/Vertical Layout Container ───
        private GameObject MakeHLayout(Transform parent, string name, float spacing = 10,
            RectOffset padding = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            if (padding != null) hlg.padding = padding;
            return go;
        }

        private GameObject MakeVLayout(Transform parent, string name, float spacing = 8,
            RectOffset padding = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            if (padding != null) vlg.padding = padding;
            return go;
        }

        // ─── List Item Prefab (daha büyük, okunaklı) ───
        private GameObject MakeListItem(Transform parent, string name, bool hasMainButton)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
            go.GetComponent<Image>().color = new Color(0.20f, 0.17f, 0.13f, 0.85f);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(10, 10, 6, 6);

            if (hasMainButton)
            {
                var adGo = new GameObject("AdButton",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                adGo.transform.SetParent(go.transform, false);
                adGo.GetComponent<Image>().color = new Color(0.25f, 0.20f, 0.15f, 0.8f);
                var adLE = adGo.AddComponent<LayoutElement>();
                adLE.flexibleWidth = 1;
                adLE.preferredHeight = 52;

                var (txtGo, tmp) = MakeTxt(adGo.transform, name, 22, TextAlignmentOptions.Left, false, true);
                Stretch(txtGo.GetComponent<RectTransform>(), 14, 6);
            }
            else
            {
                var (txtGo, tmp) = MakeTxt(go.transform, name, 20, TextAlignmentOptions.Left, false, true);
                var le = txtGo.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.preferredHeight = 52;
            }

            // Delete button
            var (silGo, silBtn) = MakeDeleteBtn(go.transform, new Vector2(80, 52));
            silGo.AddComponent<LayoutElement>().preferredWidth = 80;

            go.SetActive(false);
            return go;
        }

        // ─── Health Icon ───
        private (GameObject go, Image img) MakeHealthIcon(Transform parent, int index)
        {
            var go = new GameObject($"Can_{index}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 32);
            var img = go.GetComponent<Image>();
            img.color = Color.red;  // TODO: Assign HP Bar sprite from SteampunkUI
            return (go, img);
        }

        #endregion

        #region ══════ Utility ══════

        private void Stretch(RectTransform rt, float hPad = 0, float vPad = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(hPad, vPad);
            rt.offsetMax = new Vector2(-hPad, -vPad);
        }

        private void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private string SanitizeName(string text)
        {
            return text.Replace(" ", "").Replace(":", "").Replace("ı", "i")
                .Replace("ö", "o").Replace("ü", "u").Replace("ş", "s")
                .Replace("ç", "c").Replace("ğ", "g").Replace("İ", "I")
                .Replace("Ö", "O").Replace("Ü", "U").Replace("Ş", "S")
                .Replace("Ç", "C").Replace("Ğ", "G");
        }

        private void SetLE(GameObject go, float prefW = -1, float prefH = -1,
            float flexW = -1, float flexH = -1)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (prefW >= 0) le.preferredWidth = prefW;
            if (prefH >= 0) le.preferredHeight = prefH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
        }

        #endregion

        #region ══════ Build All ══════

        private void BuildAll()
        {
            LoadAssets();

            BuildSinifYonetim();
            BuildOyuncuSecim();
            BuildAyarlar();
            BuildSpinner();
            BuildQuiz();
            BuildHUD();
            BuildGameOver();

            WireManagerRefs();

            Debug.Log("[SteampunkPanelBuilder] Tüm paneller oluşturuldu ve bağlandı!");
        }

        #endregion

        #region ══════ 1 - Sınıf Yönetim Paneli ══════

        private void BuildSinifYonetim()
        {
            LoadAssets();
            var canvas = FindCanvas();
            if (canvas == null) { Debug.LogError("Canvas bulunamadı!"); return; }

            var panel = MakePanel(canvas.transform, "SinifYonetimPanel");
            if (panel == null) return;

            // Frame
            var frame = MakeFrame(panel.transform, "SinifFrame");

            // ══ Content Area (Frame içine düzgün yerleştirilmiş) ══
            var content = MakeFrameContent(frame.transform);
            var contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 12;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;
            contentVLG.padding = new RectOffset(15, 15, 15, 15);

            // ── Sınıf Listesi Sub-Panel ──
            var sinifListesi = new GameObject("SinifListesiPanel", typeof(RectTransform));
            sinifListesi.transform.SetParent(content.transform, false);
            SetLE(sinifListesi, flexW: 1, flexH: 1);
            var slVLG = sinifListesi.AddComponent<VerticalLayoutGroup>();
            slVLG.spacing = 6;
            slVLG.childForceExpandWidth = true;
            slVLG.childForceExpandHeight = false;

            // Header
            var header1 = MakeHLayout(sinifListesi.transform, "Header", 15,
                new RectOffset(5, 5, 5, 5));
            SetLE(header1, prefH: (int)BTN_H);
            var (titleGo, titleTmp) = MakeTxt(header1.transform, "Sınıf Yönetimi", UI_FONT,
                TextAlignmentOptions.Left, true, true);
            SetLE(titleGo, flexW: 1);
            var (sinifEkleGo, sinifEkleBtn, _) = MakeBtn(header1.transform, "Sınıf Ekle",
                new Vector2(BTN_W, BTN_H), true);
            SetLE(sinifEkleGo, prefW: (int)BTN_W);
            var (geriGo, geriBtn, _) = MakeBtn(header1.transform, "Geri",
                new Vector2(BTN_W, BTN_H));
            SetLE(geriGo, prefW: (int)BTN_W);

            // Scroll View
            var (scrollGo, scrollContent) = MakeScroll(sinifListesi.transform,
                new Vector2(0, 400));
            SetLE(scrollGo, flexW: 1, flexH: 1);

            // ── Sınıf Detay Sub-Panel (hidden) ──
            var sinifDetay = new GameObject("SinifDetayPanel", typeof(RectTransform));
            sinifDetay.transform.SetParent(content.transform, false);
            SetLE(sinifDetay, flexW: 1, flexH: 1);
            var sdVLG = sinifDetay.AddComponent<VerticalLayoutGroup>();
            sdVLG.spacing = 6;
            sdVLG.childForceExpandWidth = true;
            sdVLG.childForceExpandHeight = false;
            sinifDetay.SetActive(false);

            var header2 = MakeHLayout(sinifDetay.transform, "DetayHeader", 15,
                new RectOffset(5, 5, 5, 5));
            SetLE(header2, prefH: (int)BTN_H);
            var (baslikGo, sinifBaslikTmp) = MakeTxt(header2.transform, "Sınıf Adı", UI_FONT,
                TextAlignmentOptions.Left, true, true);
            SetLE(baslikGo, flexW: 1);
            var (ogrEkleGo, ogrEkleBtn, _) = MakeBtn(header2.transform, "Öğrenci Ekle",
                new Vector2(BTN_W, BTN_H), true);
            SetLE(ogrEkleGo, prefW: (int)BTN_W);
            var (detayGeriGo, detayGeriBtn, _) = MakeBtn(header2.transform, "Geri",
                new Vector2(BTN_W, BTN_H));
            SetLE(detayGeriGo, prefW: (int)BTN_W);

            var (scrollGo2, scrollContent2) = MakeScroll(sinifDetay.transform,
                new Vector2(0, 400));
            SetLE(scrollGo2, flexW: 1, flexH: 1);

            // ── Sınıf Ekle Popup ──
            var sinifPopup = MakeOverlay(panel.transform, "SinifEklePopup");
            var sinifPopupFrame = MakeFrame(sinifPopup.transform, "PopupFrame");
            var spRt = sinifPopupFrame.GetComponent<RectTransform>();
            spRt.anchorMin = new Vector2(0.25f, 0.30f);
            spRt.anchorMax = new Vector2(0.75f, 0.70f);
            spRt.offsetMin = Vector2.zero;
            spRt.offsetMax = Vector2.zero;

            var popContent1 = MakeContent(sinifPopupFrame.transform);
            var pc1VLG = popContent1.AddComponent<VerticalLayoutGroup>();
            pc1VLG.spacing = 12;
            pc1VLG.childForceExpandWidth = true;
            pc1VLG.childForceExpandHeight = false;
            pc1VLG.childAlignment = TextAnchor.MiddleCenter;
            pc1VLG.padding = new RectOffset(20, 20, 20, 20);

            MakeTxt(popContent1.transform, "Yeni Sınıf Ekle", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            var (sinifAdiGo, sinifAdiInp) = MakeInput(popContent1.transform,
                "Sınıf adı giriniz...", new Vector2(0, 70));
            SetLE(sinifAdiGo, prefH: 70, flexW: 1);

            var sinifBtnRow = MakeHLayout(popContent1.transform, "BtnRow", 20);
            SetLE(sinifBtnRow, prefH: (int)BTN_H);
            var (kaydetGo1, kaydetBtn1, _) = MakeBtn(sinifBtnRow.transform, "Kaydet",
                new Vector2(BTN_W, BTN_H), true);
            SetLE(kaydetGo1, prefW: (int)BTN_W);
            var (iptalGo1, iptalBtn1, _) = MakeBtn(sinifBtnRow.transform, "İptal",
                new Vector2(BTN_W, BTN_H));
            SetLE(iptalGo1, prefW: (int)BTN_W);

            // ── Öğrenci Ekle Popup ──
            var ogrPopup = MakeOverlay(panel.transform, "OgrenciEklePopup");
            var ogrPopupFrame = MakeFrame(ogrPopup.transform, "PopupFrame");
            var opRt = ogrPopupFrame.GetComponent<RectTransform>();
            opRt.anchorMin = new Vector2(0.20f, 0.22f);
            opRt.anchorMax = new Vector2(0.80f, 0.78f);
            opRt.offsetMin = Vector2.zero;
            opRt.offsetMax = Vector2.zero;

            var popContent2 = MakeContent(ogrPopupFrame.transform);
            var pc2VLG = popContent2.AddComponent<VerticalLayoutGroup>();
            pc2VLG.spacing = 10;
            pc2VLG.childForceExpandWidth = true;
            pc2VLG.childForceExpandHeight = false;
            pc2VLG.childAlignment = TextAnchor.MiddleCenter;
            pc2VLG.padding = new RectOffset(20, 20, 15, 15);

            MakeTxt(popContent2.transform, "Yeni Öğrenci Ekle", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            var (ogrAdGo, ogrAdInp) = MakeInput(popContent2.transform,
                "Öğrenci adı...", new Vector2(0, 70));
            SetLE(ogrAdGo, prefH: 70, flexW: 1);
            var (ogrSoyadGo, ogrSoyadInp) = MakeInput(popContent2.transform,
                "Soyadı...", new Vector2(0, 70));
            SetLE(ogrSoyadGo, prefH: 70, flexW: 1);
            var (ogrNoGo, ogrNoInp) = MakeInput(popContent2.transform,
                "Öğrenci No...", new Vector2(0, 70));
            SetLE(ogrNoGo, prefH: 70, flexW: 1);

            var ogrBtnRow = MakeHLayout(popContent2.transform, "BtnRow", 20);
            SetLE(ogrBtnRow, prefH: (int)BTN_H);
            var (kaydetGo2, kaydetBtn2, _) = MakeBtn(ogrBtnRow.transform, "Kaydet",
                new Vector2(BTN_W, BTN_H), true);
            SetLE(kaydetGo2, prefW: (int)BTN_W);
            var (iptalGo2, iptalBtn2, _) = MakeBtn(ogrBtnRow.transform, "İptal",
                new Vector2(BTN_W, BTN_H));
            SetLE(iptalGo2, prefW: (int)BTN_W);

            // ── Item Prefabs ──
            var sinifItemPrefab = MakeListItem(panel.transform, "SinifItemPrefab", true);
            var ogrenciItemPrefab = MakeListItem(panel.transform, "OgrenciItemPrefab", false);

            // ══ Wire Component ══
            var comp = panel.AddComponent<ClassManagementUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("sinifListesiPanel").objectReferenceValue = sinifListesi;
            so.FindProperty("sinifDetayPanel").objectReferenceValue = sinifDetay;
            so.FindProperty("sinifEklePopup").objectReferenceValue = sinifPopup;
            so.FindProperty("ogrenciEklePopup").objectReferenceValue = ogrPopup;
            so.FindProperty("sinifListesiContent").objectReferenceValue = scrollContent;
            so.FindProperty("sinifItemPrefab").objectReferenceValue = sinifItemPrefab;
            so.FindProperty("sinifEkleButton").objectReferenceValue = sinifEkleBtn;
            so.FindProperty("geriButton").objectReferenceValue = geriBtn;
            so.FindProperty("sinifAdiInput").objectReferenceValue = sinifAdiInp;
            so.FindProperty("sinifKaydetButton").objectReferenceValue = kaydetBtn1;
            so.FindProperty("sinifIptalButton").objectReferenceValue = iptalBtn1;
            so.FindProperty("sinifBaslikText").objectReferenceValue = sinifBaslikTmp;
            so.FindProperty("ogrenciListesiContent").objectReferenceValue = scrollContent2;
            so.FindProperty("ogrenciItemPrefab").objectReferenceValue = ogrenciItemPrefab;
            so.FindProperty("ogrenciEkleButton").objectReferenceValue = ogrEkleBtn;
            so.FindProperty("sinifDetayGeriButton").objectReferenceValue = detayGeriBtn;
            so.FindProperty("ogrenciAdInput").objectReferenceValue = ogrAdInp;
            so.FindProperty("ogrenciSoyadInput").objectReferenceValue = ogrSoyadInp;
            so.FindProperty("ogrenciNoInput").objectReferenceValue = ogrNoInp;
            so.FindProperty("ogrenciKaydetButton").objectReferenceValue = kaydetBtn2;
            so.FindProperty("ogrenciIptalButton").objectReferenceValue = iptalBtn2;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create SinifYonetim Panel");
            Debug.Log("[Panel Builder] Sınıf Yönetim Paneli oluşturuldu.");
        }

        #endregion

        #region ══════ 2 - Oyuncu Seçim Paneli ══════

        private void BuildOyuncuSecim()
        {
            LoadAssets();
            var canvas = FindCanvas();
            if (canvas == null) return;

            var panel = MakePanel(canvas.transform, "OyuncuSecimPanel");
            if (panel == null) return;

            var frame = MakeFrame(panel.transform, "SecimFrame");
            var content = MakeFrameContent(frame.transform);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.padding = new RectOffset(15, 15, 15, 15);

            // Title
            var (_, baslikTmp) = MakeTxt(content.transform, "Oyuncu Seçimi", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(baslikTmp.gameObject, prefH: (int)BTN_H);

            // Player selection area (horizontal: P1 | VS | P2)
            var selectRow = MakeHLayout(content.transform, "SelectionRow", 25,
                new RectOffset(10, 10, 10, 10));
            SetLE(selectRow, flexH: 1, prefH: 380);

            // ── Oyuncu 1 ──
            var o1Panel = MakeVLayout(selectRow.transform, "Oyuncu1Panel", 12,
                new RectOffset(10, 10, 10, 10));
            SetLE(o1Panel, flexW: 1);
            var (_, o1BaslikTmp) = MakeTxt(o1Panel.transform, "OYUNCU 1", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(o1BaslikTmp.gameObject, prefH: 55);
            var (lbl1Go, _) = MakeTxt(o1Panel.transform, "Sınıf:", UI_FONT, TextAlignmentOptions.Left);
            SetLE(lbl1Go, prefH: 50);
            var (o1SinifGo, o1SinifDrop) = MakeDrop(o1Panel.transform, "Sınıf Seç",
                new Vector2(0, 60));
            SetLE(o1SinifGo, prefH: 60);
            var (lbl2Go, _) = MakeTxt(o1Panel.transform, "Öğrenci:", UI_FONT, TextAlignmentOptions.Left);
            SetLE(lbl2Go, prefH: 50);
            var (o1OgrGo, o1OgrDrop) = MakeDrop(o1Panel.transform, "Öğrenci Seç",
                new Vector2(0, 60));
            SetLE(o1OgrGo, prefH: 60);

            // VS
            var (vsGo, _) = MakeTxt(selectRow.transform, "VS", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(vsGo, prefW: 120);

            // ── Oyuncu 2 ──
            var o2Panel = MakeVLayout(selectRow.transform, "Oyuncu2Panel", 12,
                new RectOffset(10, 10, 10, 10));
            SetLE(o2Panel, flexW: 1);
            var (_, o2BaslikTmp) = MakeTxt(o2Panel.transform, "OYUNCU 2", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(o2BaslikTmp.gameObject, prefH: 55);
            var (lbl3Go, _) = MakeTxt(o2Panel.transform, "Sınıf:", UI_FONT, TextAlignmentOptions.Left);
            SetLE(lbl3Go, prefH: 50);
            var (o2SinifGo, o2SinifDrop) = MakeDrop(o2Panel.transform, "Sınıf Seç",
                new Vector2(0, 60));
            SetLE(o2SinifGo, prefH: 60);
            var (lbl4Go, _) = MakeTxt(o2Panel.transform, "Öğrenci:", UI_FONT, TextAlignmentOptions.Left);
            SetLE(lbl4Go, prefH: 50);
            var (o2OgrGo, o2OgrDrop) = MakeDrop(o2Panel.transform, "Öğrenci Seç",
                new Vector2(0, 60));
            SetLE(o2OgrGo, prefH: 60);

            // Warning text
            var (uyariGo, uyariTmp) = MakeTxt(content.transform, "", UI_FONT,
                TextAlignmentOptions.Center);
            uyariTmp.color = RED;
            SetLE(uyariGo, prefH: 50);

            // Ders seçimi
            var dersRow = MakeHLayout(content.transform, "DersRow", 15);
            SetLE(dersRow, prefH: 60);
            var (dersLblGo, _) = MakeTxt(dersRow.transform, "Ders:", UI_FONT, TextAlignmentOptions.Left);
            SetLE(dersLblGo, prefW: 100);
            var (dersDropGo, dersDrop) = MakeDrop(dersRow.transform, "Ders Seç",
                new Vector2(0, 60));
            SetLE(dersDropGo, flexW: 1, prefH: 60);

            // Buttons
            var btnRow = MakeHLayout(content.transform, "BtnRow", 25);
            SetLE(btnRow, prefH: (int)BTN_H);
            var (baslaGo, baslaBtn, _) = MakeBtn(btnRow.transform, "BAŞLA",
                new Vector2(BTN_W, BTN_H), true);
            SetLE(baslaGo, prefW: (int)BTN_W);
            var (geri2Go, geri2Btn, _) = MakeBtn(btnRow.transform, "Geri",
                new Vector2(BTN_W, BTN_H));
            SetLE(geri2Go, prefW: (int)BTN_W);

            // Wire
            var comp = panel.AddComponent<PlayerSelectionUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("oyuncuSecimPanel").objectReferenceValue = panel;
            so.FindProperty("oyuncu1BaslikText").objectReferenceValue = o1BaslikTmp;
            so.FindProperty("oyuncu1SinifDropdown").objectReferenceValue = o1SinifDrop;
            so.FindProperty("oyuncu1OgrenciDropdown").objectReferenceValue = o1OgrDrop;
            so.FindProperty("oyuncu2BaslikText").objectReferenceValue = o2BaslikTmp;
            so.FindProperty("oyuncu2SinifDropdown").objectReferenceValue = o2SinifDrop;
            so.FindProperty("oyuncu2OgrenciDropdown").objectReferenceValue = o2OgrDrop;
            so.FindProperty("baslaButton").objectReferenceValue = baslaBtn;
            so.FindProperty("geriButton").objectReferenceValue = geri2Btn;
            so.FindProperty("uyariText").objectReferenceValue = uyariTmp;
            so.FindProperty("dersDropdown").objectReferenceValue = dersDrop;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create OyuncuSecim Panel");
            Debug.Log("[Panel Builder] Oyuncu Seçim Paneli oluşturuldu.");
        }

        #endregion

        #region ══════ 3 - Ayarlar Paneli ══════

        private void BuildAyarlar()
        {
            LoadAssets();
            var canvas = FindCanvas();
            if (canvas == null) return;

            var panel = MakePanel(canvas.transform, "AyarlarPanel");
            if (panel == null) return;

            var frame = MakeFrame(panel.transform, "AyarFrame");
            var content = MakeFrameContent(frame.transform);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.padding = new RectOffset(30, 30, 20, 20);

            // Title
            var (ttlGo, _) = MakeTxt(content.transform, "Ayarlar", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(ttlGo, prefH: (int)BTN_H);

            // Ses row
            var sesRow = MakeHLayout(content.transform, "SesRow", 12);
            SetLE(sesRow, prefH: 70);
            var (sesLbl, _) = MakeTxt(sesRow.transform, "Ses Efektleri:", UI_FONT,
                TextAlignmentOptions.Left);
            SetLE(sesLbl, prefW: 320);
            var (sesSliderGo, sesSlider) = MakeSlider(sesRow.transform, new Vector2(0, 50));
            SetLE(sesSliderGo, flexW: 1);
            var (sesYuzGo, sesYuzTmp) = MakeTxt(sesRow.transform, "100%", UI_FONT,
                TextAlignmentOptions.Center);
            SetLE(sesYuzGo, prefW: 120);

            // Müzik row
            var muzRow = MakeHLayout(content.transform, "MuzikRow", 12);
            SetLE(muzRow, prefH: 70);
            var (muzLbl, _) = MakeTxt(muzRow.transform, "Müzik:", UI_FONT,
                TextAlignmentOptions.Left);
            SetLE(muzLbl, prefW: 320);
            var (muzSliderGo, muzSlider) = MakeSlider(muzRow.transform, new Vector2(0, 50));
            SetLE(muzSliderGo, flexW: 1);
            var (muzYuzGo, muzYuzTmp) = MakeTxt(muzRow.transform, "70%", UI_FONT,
                TextAlignmentOptions.Center);
            SetLE(muzYuzGo, prefW: 120);

            // Ceza süresi row
            var cezaRow = MakeHLayout(content.transform, "CezaRow", 12);
            SetLE(cezaRow, prefH: 70);
            var (cezaLbl, _) = MakeTxt(cezaRow.transform, "Ceza Süresi:", UI_FONT,
                TextAlignmentOptions.Left);
            SetLE(cezaLbl, prefW: 320);
            var (cezaDropGo, cezaDrop) = MakeDrop(cezaRow.transform, "10 Saniye",
                new Vector2(0, 60));
            SetLE(cezaDropGo, flexW: 1);

            // Can sayısı row
            var canRow = MakeHLayout(content.transform, "CanRow", 12);
            SetLE(canRow, prefH: 70);
            var (canLbl, _) = MakeTxt(canRow.transform, "Can Sayısı:", UI_FONT,
                TextAlignmentOptions.Left);
            SetLE(canLbl, prefW: 320);
            var (canDropGo, canDrop) = MakeDrop(canRow.transform, "3 Can",
                new Vector2(0, 60));
            SetLE(canDropGo, flexW: 1);

            // Buttons
            var btnRow = MakeHLayout(content.transform, "BtnRow", 25);
            SetLE(btnRow, prefH: (int)BTN_H);
            var (kayGo, kayBtn, _) = MakeBtn(btnRow.transform, "Kaydet",
                new Vector2(BTN_W, BTN_H), true);
            SetLE(kayGo, prefW: (int)BTN_W);
            var (geriGo, geriBtn, _) = MakeBtn(btnRow.transform, "Geri",
                new Vector2(BTN_W, BTN_H));
            SetLE(geriGo, prefW: (int)BTN_W);

            // Wire
            var comp = panel.AddComponent<SettingsUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("ayarlarPanel").objectReferenceValue = panel;
            so.FindProperty("sesSlider").objectReferenceValue = sesSlider;
            so.FindProperty("sesYuzdeText").objectReferenceValue = sesYuzTmp;
            so.FindProperty("muzikSlider").objectReferenceValue = muzSlider;
            so.FindProperty("muzikYuzdeText").objectReferenceValue = muzYuzTmp;
            so.FindProperty("cezaSuresiDropdown").objectReferenceValue = cezaDrop;
            so.FindProperty("canSayisiDropdown").objectReferenceValue = canDrop;
            so.FindProperty("kaydetButton").objectReferenceValue = kayBtn;
            so.FindProperty("geriButton").objectReferenceValue = geriBtn;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create Ayarlar Panel");
            Debug.Log("[Panel Builder] Ayarlar Paneli oluşturuldu.");
        }

        #endregion

        #region ══════ 4 - Zorluk Spinner Paneli ══════

        private void BuildSpinner()
        {
            LoadAssets();
            var canvas = FindCanvas();
            if (canvas == null) return;

            var panel = MakePanel(canvas.transform, "SpinnerPanel");
            if (panel == null) return;

            var frame = MakeFrame(panel.transform, "SpinnerFrame");
            var content = MakeFrameContent(frame.transform);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.padding = new RectOffset(15, 15, 15, 15);

            // Title
            var (spTtlGo, _) = MakeTxt(content.transform, "Zorluk Seçimi", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(spTtlGo, prefH: (int)BTN_H);

            // Selection area: P1 | Spinner | P2
            var mainRow = MakeHLayout(content.transform, "MainRow", 20);
            SetLE(mainRow, flexH: 1, prefH: 400);

            // ── Oyuncu 1 Section ──
            var o1Sec = MakeVLayout(mainRow.transform, "Oyuncu1Secim", 10,
                new RectOffset(10, 10, 10, 10));
            SetLE(o1Sec, flexW: 1);
            var (_, o1SecTmp) = MakeTxt(o1Sec.transform, "Oyuncu 1: Seçim yapın", UI_FONT,
                TextAlignmentOptions.Center, false, true);
            SetLE(o1SecTmp.gameObject, prefH: 55);
            var (o1KB, o1KBtn, _) = MakeBtn(o1Sec.transform, "Kolay",
                new Vector2(0, BTN_H));
            SetLE(o1KB, prefH: (int)BTN_H);
            var (o1OB, o1OBtn, _) = MakeBtn(o1Sec.transform, "Orta",
                new Vector2(0, BTN_H), true);
            SetLE(o1OB, prefH: (int)BTN_H);
            var (o1ZB, o1ZBtn, _) = MakeBtn(o1Sec.transform, "Zor",
                new Vector2(0, BTN_H));
            SetLE(o1ZB, prefH: (int)BTN_H);
            var (solGo, solTmp) = MakeTxt(o1Sec.transform, "← Oyuncu 1", UI_FONT,
                TextAlignmentOptions.Center);

            // ── Spinner Center ──
            var spinCenter = MakeVLayout(mainRow.transform, "SpinnerCenter", 12,
                new RectOffset(5, 5, 10, 10));
            SetLE(spinCenter, prefW: 280);

            // Ok Image (arrow for spinning)
            var okGo = new GameObject("OkImage", typeof(RectTransform), typeof(Image));
            okGo.transform.SetParent(spinCenter.transform, false);
            okGo.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 160);
            SetLE(okGo, prefW: 160, prefH: 160);
            var okImg = okGo.GetComponent<Image>();
            okImg.color = COPPER;
            // TODO: SteampunkUI arrow sprite'ını buraya atayın

            var (dondurGo, dondurBtn, _) = MakeBtn(spinCenter.transform, "DÖNDÜR",
                new Vector2(0, BTN_H), true);
            SetLE(dondurGo, prefH: (int)BTN_H);
            var (sonucGo, sonucTmp) = MakeTxt(spinCenter.transform, "", UI_FONT,
                TextAlignmentOptions.Center, false, true);
            SetLE(sonucGo, prefH: 55, flexH: 1);

            // ── Oyuncu 2 Section ──
            var o2Sec = MakeVLayout(mainRow.transform, "Oyuncu2Secim", 10,
                new RectOffset(10, 10, 10, 10));
            SetLE(o2Sec, flexW: 1);
            var (_, o2SecTmp) = MakeTxt(o2Sec.transform, "Oyuncu 2: Seçim yapın", UI_FONT,
                TextAlignmentOptions.Center, false, true);
            SetLE(o2SecTmp.gameObject, prefH: 55);
            var (o2KB, o2KBtn, _) = MakeBtn(o2Sec.transform, "Kolay",
                new Vector2(0, BTN_H));
            SetLE(o2KB, prefH: (int)BTN_H);
            var (o2OB, o2OBtn, _) = MakeBtn(o2Sec.transform, "Orta",
                new Vector2(0, BTN_H), true);
            SetLE(o2OB, prefH: (int)BTN_H);
            var (o2ZB, o2ZBtn, _) = MakeBtn(o2Sec.transform, "Zor",
                new Vector2(0, BTN_H));
            SetLE(o2ZB, prefH: (int)BTN_H);
            var (sagGo, sagTmp) = MakeTxt(o2Sec.transform, "Oyuncu 2 →", UI_FONT,
                TextAlignmentOptions.Center);

            // Devam button
            var (devamGo, devamBtn, _) = MakeBtn(content.transform, "DEVAM",
                new Vector2(0, BTN_H), true);
            SetLE(devamGo, prefH: (int)BTN_H);
            devamGo.SetActive(false);

            // Wire
            var comp = panel.AddComponent<DifficultySpinnerUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("spinnerPanel").objectReferenceValue = panel;
            so.FindProperty("oyuncu1KolayBtn").objectReferenceValue = o1KBtn;
            so.FindProperty("oyuncu1OrtaBtn").objectReferenceValue = o1OBtn;
            so.FindProperty("oyuncu1ZorBtn").objectReferenceValue = o1ZBtn;
            so.FindProperty("oyuncu1SecimText").objectReferenceValue = o1SecTmp;
            so.FindProperty("oyuncu2KolayBtn").objectReferenceValue = o2KBtn;
            so.FindProperty("oyuncu2OrtaBtn").objectReferenceValue = o2OBtn;
            so.FindProperty("oyuncu2ZorBtn").objectReferenceValue = o2ZBtn;
            so.FindProperty("oyuncu2SecimText").objectReferenceValue = o2SecTmp;
            so.FindProperty("okImage").objectReferenceValue = okGo.GetComponent<RectTransform>();
            so.FindProperty("dondurButton").objectReferenceValue = dondurBtn;
            so.FindProperty("sonucText").objectReferenceValue = sonucTmp;
            so.FindProperty("solTarafText").objectReferenceValue = solTmp;
            so.FindProperty("sagTarafText").objectReferenceValue = sagTmp;
            so.FindProperty("devamButton").objectReferenceValue = devamBtn;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create Spinner Panel");
            Debug.Log("[Panel Builder] Zorluk Spinner Paneli oluşturuldu.");
        }

        #endregion

        #region ══════ 5 - Quiz Soru Paneli ══════

        private void BuildQuiz()
        {
            LoadAssets();
            var canvas = FindCanvas();
            if (canvas == null) return;

            var panel = MakePanel(canvas.transform, "QuizPanel");
            if (panel == null) return;

            // Bu panel frame kullanmaz, doğrudan overlay
            var bgImg = panel.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.04f, 0.03f, 0.85f);

            var mainVLG = panel.AddComponent<VerticalLayoutGroup>();
            mainVLG.spacing = 10;
            mainVLG.childForceExpandWidth = true;
            mainVLG.childForceExpandHeight = false;
            mainVLG.padding = new RectOffset(20, 20, 15, 15);

            // ── Soru Paneli (top, animatable) ──
            var soruPanel = new GameObject("SoruPanel", typeof(RectTransform), typeof(CanvasGroup));
            soruPanel.transform.SetParent(panel.transform, false);
            SetLE(soruPanel, prefH: 280, flexW: 1);

            var soruFrame = MakeFrame(soruPanel.transform, "SoruFrame");
            var soruContent = MakeFrameContent(soruFrame.transform);
            var soruVLG = soruContent.AddComponent<VerticalLayoutGroup>();
            soruVLG.spacing = 8;
            soruVLG.childForceExpandWidth = true;
            soruVLG.childForceExpandHeight = false;
            soruVLG.padding = new RectOffset(15, 15, 12, 12);

            // Info row
            var infoRow = MakeHLayout(soruContent.transform, "InfoRow", 15);
            SetLE(infoRow, prefH: 50);
            var (numGo, soruNumTmp) = MakeTxt(infoRow.transform, "Soru 1", UI_FONT,
                TextAlignmentOptions.Left, false, true);
            SetLE(numGo, flexW: 1);
            var (zorGo, zorlukTmp) = MakeTxt(infoRow.transform, "Zorluk: Kolay", UI_FONT,
                TextAlignmentOptions.Center, false, true);
            SetLE(zorGo, prefW: 280);
            var (katGo, kategoriTmp) = MakeTxt(infoRow.transform, "Ders: Matematik", UI_FONT,
                TextAlignmentOptions.Right, false, true);
            SetLE(katGo, prefW: 300);

            // Question text
            var (soruTxtGo, soruTmp) = MakeTxt(soruContent.transform,
                "Soru metni burada görünecek...", UI_FONT, TextAlignmentOptions.Center, true, true);
            SetLE(soruTxtGo, flexH: 1, prefH: 120);

            // ── Answer Area (Horizontal: P1 answers | P2 answers) ──
            var answerRow = MakeHLayout(panel.transform, "AnswerRow", 30,
                new RectOffset(10, 10, 5, 5));
            SetLE(answerRow, flexH: 1);

            // Oyuncu 1 answers
            var o1Answers = MakeVLayout(answerRow.transform, "Oyuncu1Cevaplar", 8,
                new RectOffset(5, 5, 5, 5));
            SetLE(o1Answers, flexW: 1);
            var (_, o1AdTmp) = MakeTxt(o1Answers.transform, "Oyuncu 1", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(o1AdTmp.gameObject, prefH: 55);

            Button[] o1Btns = new Button[4];
            TextMeshProUGUI[] o1Txts = new TextMeshProUGUI[4];
            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                var (bGo, btn, lbl) = MakeBtn(o1Answers.transform,
                    $"{harfler[i]}) Şık {i + 1}", new Vector2(0, BTN_H));
                SetLE(bGo, prefH: (int)BTN_H);
                o1Btns[i] = btn;
                o1Txts[i] = lbl;
            }

            // Oyuncu 1 ceza paneli
            var o1Ceza = new GameObject("Oyuncu1CezaPanel",
                typeof(RectTransform), typeof(Image));
            o1Ceza.transform.SetParent(o1Answers.transform, false);
            o1Ceza.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.1f, 0.3f);
            SetLE(o1Ceza, prefH: 50);
            var (_, o1CezaTmp) = MakeTxt(o1Ceza.transform, "Ceza: 10s", UI_FONT,
                TextAlignmentOptions.Center);
            Stretch(o1CezaTmp.gameObject.GetComponent<RectTransform>());
            o1CezaTmp.color = RED;
            o1Ceza.SetActive(false);

            // Oyuncu 2 answers
            var o2Answers = MakeVLayout(answerRow.transform, "Oyuncu2Cevaplar", 8,
                new RectOffset(5, 5, 5, 5));
            SetLE(o2Answers, flexW: 1);
            var (_, o2AdTmp) = MakeTxt(o2Answers.transform, "Oyuncu 2", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(o2AdTmp.gameObject, prefH: 55);

            Button[] o2Btns = new Button[4];
            TextMeshProUGUI[] o2Txts = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var (bGo, btn, lbl) = MakeBtn(o2Answers.transform,
                    $"{harfler[i]}) Şık {i + 1}", new Vector2(0, BTN_H));
                SetLE(bGo, prefH: (int)BTN_H);
                o2Btns[i] = btn;
                o2Txts[i] = lbl;
            }

            // Oyuncu 2 ceza paneli
            var o2Ceza = new GameObject("Oyuncu2CezaPanel",
                typeof(RectTransform), typeof(Image));
            o2Ceza.transform.SetParent(o2Answers.transform, false);
            o2Ceza.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.1f, 0.3f);
            SetLE(o2Ceza, prefH: 50);
            var (_, o2CezaTmp) = MakeTxt(o2Ceza.transform, "Ceza: 10s", UI_FONT,
                TextAlignmentOptions.Center);
            Stretch(o2CezaTmp.gameObject.GetComponent<RectTransform>());
            o2CezaTmp.color = RED;
            o2Ceza.SetActive(false);

            // Bilgi text (bottom)
            var (bilgiGo, bilgiTmp) = MakeTxt(panel.transform, "", UI_FONT,
                TextAlignmentOptions.Center, false, true);
            SetLE(bilgiGo, prefH: 55);

            // ── Açıklama Paneli (Overlay, yanlıştan sonra doğru cevap verilince gösterilir) ──
            var aciklamaPanel = new GameObject("AciklamaPanel", typeof(RectTransform), typeof(Image));
            aciklamaPanel.transform.SetParent(panel.transform, false);
            var aciklamaRT = aciklamaPanel.GetComponent<RectTransform>();
            Stretch(aciklamaRT);
            aciklamaPanel.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.02f, 0.92f);

            var aciklamaFrame = MakeFrame(aciklamaPanel.transform, "AciklamaFrame");
            var aciklamaFrameRT = aciklamaFrame.GetComponent<RectTransform>();
            aciklamaFrameRT.anchorMin = new Vector2(0.15f, 0.15f);
            aciklamaFrameRT.anchorMax = new Vector2(0.85f, 0.85f);
            aciklamaFrameRT.offsetMin = Vector2.zero;
            aciklamaFrameRT.offsetMax = Vector2.zero;

            var aciklamaContent = MakeFrameContent(aciklamaFrame.transform);
            var aciklamaVLG = aciklamaContent.AddComponent<VerticalLayoutGroup>();
            aciklamaVLG.spacing = 12;
            aciklamaVLG.childForceExpandWidth = true;
            aciklamaVLG.childForceExpandHeight = false;
            aciklamaVLG.padding = new RectOffset(20, 20, 20, 20);
            aciklamaVLG.childAlignment = TextAnchor.MiddleCenter;

            // Başlık
            var (_, aciklamaBaslikTmp) = MakeTxt(aciklamaContent.transform,
                "Açıklama", 48, TextAlignmentOptions.Center, true, true);
            SetLE(aciklamaBaslikTmp.gameObject, prefH: 60);
            aciklamaBaslikTmp.color = GOLD;

            // Açıklama metni
            var (aciklamaTxtGo, aciklamaTmp) = MakeTxt(aciklamaContent.transform,
                "Doğru cevabın açıklaması burada görünecek...", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(aciklamaTxtGo, flexH: 1, prefH: 200);

            // Devam butonu
            var (devamGo, devamBtn, devamLbl) = MakeBtn(aciklamaContent.transform,
                "Devam Et", new Vector2(BTN_W, BTN_H));
            SetLE(devamGo, prefH: (int)BTN_H);
            var devamLE = devamGo.GetComponent<LayoutElement>();
            if (devamLE != null) { devamLE.flexibleWidth = 0; devamLE.preferredWidth = BTN_W; }

            aciklamaPanel.SetActive(false);

            // Wire
            var comp = panel.AddComponent<QuizUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("soruPanel").objectReferenceValue = soruPanel.GetComponent<RectTransform>();
            so.FindProperty("soruText").objectReferenceValue = soruTmp;
            so.FindProperty("soruNumarasiText").objectReferenceValue = soruNumTmp;
            so.FindProperty("zorlukText").objectReferenceValue = zorlukTmp;
            so.FindProperty("kategoriText").objectReferenceValue = kategoriTmp;
            so.FindProperty("oyuncu1AdText").objectReferenceValue = o1AdTmp;

            var o1BtnProp = so.FindProperty("oyuncu1Butonlar");
            o1BtnProp.arraySize = 4;
            var o1TxtProp = so.FindProperty("oyuncu1SikTextleri");
            o1TxtProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                o1BtnProp.GetArrayElementAtIndex(i).objectReferenceValue = o1Btns[i];
                o1TxtProp.GetArrayElementAtIndex(i).objectReferenceValue = o1Txts[i];
            }

            so.FindProperty("oyuncu2AdText").objectReferenceValue = o2AdTmp;

            var o2BtnProp = so.FindProperty("oyuncu2Butonlar");
            o2BtnProp.arraySize = 4;
            var o2TxtProp = so.FindProperty("oyuncu2SikTextleri");
            o2TxtProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                o2BtnProp.GetArrayElementAtIndex(i).objectReferenceValue = o2Btns[i];
                o2TxtProp.GetArrayElementAtIndex(i).objectReferenceValue = o2Txts[i];
            }

            so.FindProperty("oyuncu1CezaPanel").objectReferenceValue = o1Ceza;
            so.FindProperty("oyuncu1CezaSureText").objectReferenceValue = o1CezaTmp;
            so.FindProperty("oyuncu2CezaPanel").objectReferenceValue = o2Ceza;
            so.FindProperty("oyuncu2CezaSureText").objectReferenceValue = o2CezaTmp;
            so.FindProperty("bilgiText").objectReferenceValue = bilgiTmp;
            so.FindProperty("aciklamaPaneli").objectReferenceValue = aciklamaPanel;
            so.FindProperty("aciklamaText").objectReferenceValue = aciklamaTmp;
            so.FindProperty("aciklamaDevamButton").objectReferenceValue = devamBtn;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create Quiz Panel");
            Debug.Log("[Panel Builder] Quiz Soru Paneli oluşturuldu.");
        }

        #endregion

        #region ══════ 6 - HUD Paneli ══════

        private void BuildHUD()
        {
            LoadAssets();
            var canvas = FindCanvas();
            if (canvas == null) return;

            var panel = MakePanel(canvas.transform, "HUDPanel");
            if (panel == null) return;

            // HUD top bar only
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.88f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bgImg = panel.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.06f, 0.05f, 0.85f);

            var mainHLG = panel.AddComponent<HorizontalLayoutGroup>();
            mainHLG.spacing = 10;
            mainHLG.childForceExpandWidth = false;
            mainHLG.childForceExpandHeight = true;
            mainHLG.padding = new RectOffset(15, 15, 5, 5);

            // ── Oyuncu 1 Info (Left) ──
            var o1Area = MakeVLayout(panel.transform, "Oyuncu1Info", 2,
                new RectOffset(5, 5, 2, 2));
            SetLE(o1Area, flexW: 1);
            var (_, o1AdTmp) = MakeTxt(o1Area.transform, "Oyuncu 1", 28,
                TextAlignmentOptions.Left, true);
            SetLE(o1AdTmp.gameObject, prefH: 32);
            var (_, o1SinifTmp) = MakeTxt(o1Area.transform, "Sınıf", 20,
                TextAlignmentOptions.Left);
            SetLE(o1SinifTmp.gameObject, prefH: 24);

            var o1CanRow = MakeHLayout(o1Area.transform, "CanRow", 4);
            SetLE(o1CanRow, prefH: 30);
            Image[] o1CanIcons = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var (cGo, cImg) = MakeHealthIcon(o1CanRow.transform, i);
                cImg.rectTransform.sizeDelta = new Vector2(30, 30);
                o1CanIcons[i] = cImg;
            }

            var (_, o1SkorTmp) = MakeTxt(o1Area.transform, "D:0 | Y:0", 22,
                TextAlignmentOptions.Left);
            SetLE(o1SkorTmp.gameObject, prefH: 26);

            // ── Center Info ──
            var centerArea = MakeVLayout(panel.transform, "OrtaBilgi", 2);
            SetLE(centerArea, prefW: 300);
            var (_, soruSayacTmp) = MakeTxt(centerArea.transform, "Soru: 1/10", 30,
                TextAlignmentOptions.Center, true);
            SetLE(soruSayacTmp.gameObject, prefH: 35);
            var (_, turBilgiTmp) = MakeTxt(centerArea.transform, "Zorluk: Kolay", 24,
                TextAlignmentOptions.Center);
            SetLE(turBilgiTmp.gameObject, prefH: 28);

            // ── Oyuncu 2 Info (Right) ──
            var o2Area = MakeVLayout(panel.transform, "Oyuncu2Info", 2,
                new RectOffset(5, 5, 2, 2));
            SetLE(o2Area, flexW: 1);
            var (_, o2AdTmp) = MakeTxt(o2Area.transform, "Oyuncu 2", 28,
                TextAlignmentOptions.Right, true);
            SetLE(o2AdTmp.gameObject, prefH: 32);
            var (_, o2SinifTmp) = MakeTxt(o2Area.transform, "Sınıf", 20,
                TextAlignmentOptions.Right);
            SetLE(o2SinifTmp.gameObject, prefH: 24);

            var o2CanRow = MakeHLayout(o2Area.transform, "CanRow", 4);
            o2CanRow.GetComponent<HorizontalLayoutGroup>().childAlignment =
                TextAnchor.MiddleRight;
            SetLE(o2CanRow, prefH: 30);
            Image[] o2CanIcons = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var (cGo, cImg) = MakeHealthIcon(o2CanRow.transform, i);
                cImg.rectTransform.sizeDelta = new Vector2(30, 30);
                o2CanIcons[i] = cImg;
            }

            var (_, o2SkorTmp) = MakeTxt(o2Area.transform, "D:0 | Y:0", 22,
                TextAlignmentOptions.Right);
            SetLE(o2SkorTmp.gameObject, prefH: 26);

            // Wire
            var comp = panel.AddComponent<GameHUD>();
            var so = new SerializedObject(comp);
            so.FindProperty("hudPanel").objectReferenceValue = panel;
            so.FindProperty("oyuncu1AdText").objectReferenceValue = o1AdTmp;
            so.FindProperty("oyuncu1SinifText").objectReferenceValue = o1SinifTmp;
            so.FindProperty("oyuncu1SkorText").objectReferenceValue = o1SkorTmp;
            so.FindProperty("oyuncu2AdText").objectReferenceValue = o2AdTmp;
            so.FindProperty("oyuncu2SinifText").objectReferenceValue = o2SinifTmp;
            so.FindProperty("oyuncu2SkorText").objectReferenceValue = o2SkorTmp;
            so.FindProperty("soruSayaciText").objectReferenceValue = soruSayacTmp;
            so.FindProperty("turBilgiText").objectReferenceValue = turBilgiTmp;

            var o1CanProp = so.FindProperty("oyuncu1CanIkonlari");
            o1CanProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
                o1CanProp.GetArrayElementAtIndex(i).objectReferenceValue = o1CanIcons[i];

            var o2CanProp = so.FindProperty("oyuncu2CanIkonlari");
            o2CanProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
                o2CanProp.GetArrayElementAtIndex(i).objectReferenceValue = o2CanIcons[i];

            so.ApplyModifiedProperties();

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create HUD Panel");
            Debug.Log("[Panel Builder] HUD Paneli oluşturuldu.");
        }

        #endregion

        #region ══════ 7 - Oyun Sonu Paneli ══════

        private void BuildGameOver()
        {
            LoadAssets();
            var canvas = FindCanvas();
            if (canvas == null) return;

            var panel = MakePanel(canvas.transform, "GameOverPanel");
            if (panel == null) return;

            var frame = MakeFrame(panel.transform, "GameOverFrame");
            var content = MakeFrameContent(frame.transform);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.padding = new RectOffset(20, 20, 15, 15);

            // Kazanan
            var (kazGo, kazTmp) = MakeTxt(content.transform, "KAZANAN: ---", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(kazGo, prefH: (int)BTN_H);
            var (kaybGo, kaybTmp) = MakeTxt(content.transform, "Kaybeden: ---", UI_FONT,
                TextAlignmentOptions.Center);
            SetLE(kaybGo, prefH: 55);

            // Separator
            var sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(content.transform, false);
            sep.GetComponent<Image>().color = COPPER;
            SetLE(sep, prefH: 2);

            // Stats area (horizontal: P1 stats | P2 stats)
            var statsRow = MakeHLayout(content.transform, "StatsRow", 25,
                new RectOffset(5, 5, 5, 5));
            SetLE(statsRow, flexH: 1, prefH: 320);

            // ── Oyuncu 1 Stats ──
            var o1Stats = MakeVLayout(statsRow.transform, "Oyuncu1Stats", 8,
                new RectOffset(12, 12, 12, 12));
            SetLE(o1Stats, flexW: 1);
            o1Stats.AddComponent<Image>().color = new Color(0.14f, 0.12f, 0.10f, 0.5f);
            var (_, o1AdTmp) = MakeTxt(o1Stats.transform, "Oyuncu 1", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(o1AdTmp.gameObject, prefH: 55);
            var (_, o1DogruTmp) = MakeTxt(o1Stats.transform, "Doğru: 0", UI_FONT,
                TextAlignmentOptions.Left);
            o1DogruTmp.color = GREEN;
            SetLE(o1DogruTmp.gameObject, prefH: 50);
            var (_, o1YanlisTmp) = MakeTxt(o1Stats.transform, "Yanlış: 0", UI_FONT,
                TextAlignmentOptions.Left);
            o1YanlisTmp.color = RED;
            SetLE(o1YanlisTmp.gameObject, prefH: 50);
            var (_, o1CanTmp) = MakeTxt(o1Stats.transform, "Kalan Can: 0", UI_FONT,
                TextAlignmentOptions.Left);
            SetLE(o1CanTmp.gameObject, prefH: 50);
            var (_, o1ZayifTmp) = MakeTxt(o1Stats.transform, "Zayıf Dersler: -", 32,
                TextAlignmentOptions.Left);
            o1ZayifTmp.color = new Color(1f, 0.7f, 0.3f);
            SetLE(o1ZayifTmp.gameObject, prefH: 42);

            // ── Oyuncu 2 Stats ──
            var o2Stats = MakeVLayout(statsRow.transform, "Oyuncu2Stats", 8,
                new RectOffset(12, 12, 12, 12));
            SetLE(o2Stats, flexW: 1);
            o2Stats.AddComponent<Image>().color = new Color(0.14f, 0.12f, 0.10f, 0.5f);
            var (_, o2AdTmp) = MakeTxt(o2Stats.transform, "Oyuncu 2", UI_FONT,
                TextAlignmentOptions.Center, true, true);
            SetLE(o2AdTmp.gameObject, prefH: 55);
            var (_, o2DogruTmp) = MakeTxt(o2Stats.transform, "Doğru: 0", UI_FONT,
                TextAlignmentOptions.Left);
            o2DogruTmp.color = GREEN;
            SetLE(o2DogruTmp.gameObject, prefH: 50);
            var (_, o2YanlisTmp) = MakeTxt(o2Stats.transform, "Yanlış: 0", UI_FONT,
                TextAlignmentOptions.Left);
            o2YanlisTmp.color = RED;
            SetLE(o2YanlisTmp.gameObject, prefH: 50);
            var (_, o2CanTmp) = MakeTxt(o2Stats.transform, "Kalan Can: 0", UI_FONT,
                TextAlignmentOptions.Left);
            SetLE(o2CanTmp.gameObject, prefH: 50);
            var (_, o2ZayifTmp) = MakeTxt(o2Stats.transform, "Zayıf Dersler: -", 32,
                TextAlignmentOptions.Left);
            o2ZayifTmp.color = new Color(1f, 0.7f, 0.3f);
            SetLE(o2ZayifTmp.gameObject, prefH: 42);

            // Buttons
            var btnRow = MakeHLayout(content.transform, "BtnRow", 25);
            SetLE(btnRow, prefH: (int)BTN_H);
            var (tekrarGo, tekrarBtn, _) = MakeBtn(btnRow.transform, "Tekrar Oyna",
                new Vector2(BTN_W, BTN_H), true);
            SetLE(tekrarGo, prefW: (int)BTN_W);
            var (menuGo, menuBtn, _) = MakeBtn(btnRow.transform, "Ana Menü",
                new Vector2(BTN_W, BTN_H));
            SetLE(menuGo, prefW: (int)BTN_W);

            // Wire
            var comp = panel.AddComponent<GameOverUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("gameOverPanel").objectReferenceValue = panel;
            so.FindProperty("kazananText").objectReferenceValue = kazTmp;
            so.FindProperty("kaybedenText").objectReferenceValue = kaybTmp;
            so.FindProperty("oyuncu1AdText").objectReferenceValue = o1AdTmp;
            so.FindProperty("oyuncu1DogruText").objectReferenceValue = o1DogruTmp;
            so.FindProperty("oyuncu1YanlisText").objectReferenceValue = o1YanlisTmp;
            so.FindProperty("oyuncu1CanText").objectReferenceValue = o1CanTmp;
            so.FindProperty("oyuncu1ZayifDerslerText").objectReferenceValue = o1ZayifTmp;
            so.FindProperty("oyuncu2AdText").objectReferenceValue = o2AdTmp;
            so.FindProperty("oyuncu2DogruText").objectReferenceValue = o2DogruTmp;
            so.FindProperty("oyuncu2YanlisText").objectReferenceValue = o2YanlisTmp;
            so.FindProperty("oyuncu2CanText").objectReferenceValue = o2CanTmp;
            so.FindProperty("oyuncu2ZayifDerslerText").objectReferenceValue = o2ZayifTmp;
            so.FindProperty("tekrarOynaButton").objectReferenceValue = tekrarBtn;
            so.FindProperty("anaMenuButton").objectReferenceValue = menuBtn;
            so.ApplyModifiedProperties();

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create GameOver Panel");
            Debug.Log("[Panel Builder] Oyun Sonu Paneli oluşturuldu.");
        }

        #endregion

        #region ══════ Wire Manager References ══════

        private void WireManagerRefs()
        {
            var canvas = FindCanvas();
            if (canvas == null) return;

            // ── MainMenuController ──
            // Canvas üzerinde (her zaman aktif) olmalı, böylece panel gizlense bile çalışır
            var mainMenu = canvas.transform.Find("MainMenu");
            if (mainMenu != null)
            {
                // Eski MainMenuController varsa MainMenu child'dan kaldır (artık Canvas'ta)
                var oldMmc = mainMenu.GetComponent<MainMenuController>();
                if (oldMmc != null) DestroyImmediate(oldMmc);

                var mmc = canvas.GetComponent<MainMenuController>();
                if (mmc == null)
                    mmc = canvas.gameObject.AddComponent<MainMenuController>();

                var so = new SerializedObject(mmc);

                // Ana menü paneli refs — MainMenu child panelini referans al
                so.FindProperty("anaMenuPanel").objectReferenceValue = mainMenu.gameObject;

                // Find buttons in hierarchy
                var playBtn = FindDeep(mainMenu, "PlayBtn");
                var classBtn = FindDeep(mainMenu, "ClassesBtn");
                var settBtn = FindDeep(mainMenu, "SettingsBtn");
                var exitBtn = FindDeep(mainMenu, "ExitBtn");

                if (playBtn != null)
                    so.FindProperty("oynaButton").objectReferenceValue =
                        playBtn.GetComponent<Button>();
                if (classBtn != null)
                    so.FindProperty("siniflarButton").objectReferenceValue =
                        classBtn.GetComponent<Button>();
                if (settBtn != null)
                    so.FindProperty("ayarlarButton").objectReferenceValue =
                        settBtn.GetComponent<Button>();
                if (exitBtn != null)
                    so.FindProperty("cikisButton").objectReferenceValue =
                        exitBtn.GetComponent<Button>();

                // Sub panels
                var sinifPanel = canvas.transform.Find("SinifYonetimPanel");
                var ayarPanel = canvas.transform.Find("AyarlarPanel");
                var secimPanel = canvas.transform.Find("OyuncuSecimPanel");

                if (sinifPanel != null)
                    so.FindProperty("sinifYonetimPanel").objectReferenceValue =
                        sinifPanel.gameObject;
                if (ayarPanel != null)
                    so.FindProperty("ayarlarPanel").objectReferenceValue =
                        ayarPanel.gameObject;
                if (secimPanel != null)
                    so.FindProperty("oyuncuSecimPanel").objectReferenceValue =
                        secimPanel.gameObject;

                so.ApplyModifiedProperties();
                Debug.Log("[Panel Builder] MainMenuController referansları bağlandı.");
            }
            else
            {
                Debug.LogWarning("[Panel Builder] 'MainMenu' objesi bulunamadı! " +
                    "Canvas altında 'MainMenu' adında bir obje olmalı.");
            }

            // ── GameManager ──
            var gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);

                var quizPanel = canvas.transform.Find("QuizPanel");
                var spinPanel = canvas.transform.Find("SpinnerPanel");
                var hudPanel = canvas.transform.Find("HUDPanel");
                var goPanel = canvas.transform.Find("GameOverPanel");

                if (quizPanel != null)
                    so.FindProperty("quizUI").objectReferenceValue =
                        quizPanel.GetComponent<QuizUI>();
                if (spinPanel != null)
                    so.FindProperty("spinnerUI").objectReferenceValue =
                        spinPanel.GetComponent<DifficultySpinnerUI>();
                if (hudPanel != null)
                    so.FindProperty("gameHUD").objectReferenceValue =
                        hudPanel.GetComponent<GameHUD>();
                if (goPanel != null)
                    so.FindProperty("gameOverUI").objectReferenceValue =
                        goPanel.GetComponent<GameOverUI>();

                so.ApplyModifiedProperties();
                Debug.Log("[Panel Builder] GameManager referansları bağlandı.");
            }
            else
            {
                Debug.LogWarning("[Panel Builder] GameManager bulunamadı!");
            }
        }

        // Recursive child finder
        private Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var result = FindDeep(child, name);
                if (result != null) return result;
            }
            return null;
        }

        #endregion
    }
}
