using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizGame.UI
{
    /// <summary>
    /// Liste elemanı (sınıf veya öğrenci satırı) için dinamik prefab oluşturucu.
    /// Runtime'da sınıf/öğrenci listeleri için kullanılır.
    /// 
    /// Bu script, ClassManagementUI tarafından kullanılan prefab'ları
    /// runtime'da oluşturabilmek için yardımcı metotlar sağlar.
    /// </summary>
    public class ListItemFactory : MonoBehaviour
    {
        public static ListItemFactory Instance { get; private set; }

        [Header("═══ Prefab Referansları ═══")]
        [Tooltip("Sınıf listesi için kullanılacak prefab. Boş bırakılırsa runtime'da oluşturulur.")]
        [SerializeField] private GameObject sinifItemPrefab;

        [Tooltip("Öğrenci listesi için kullanılacak prefab. Boş bırakılırsa runtime'da oluşturulur.")]
        [SerializeField] private GameObject ogrenciItemPrefab;

        private void Awake()
        {
            Instance = this;

            // Prefab'lar atanmamışsa oluştur
            if (sinifItemPrefab == null) sinifItemPrefab = SinifItemOlustur();
            if (ogrenciItemPrefab == null) ogrenciItemPrefab = OgrenciItemOlustur();
        }

        public GameObject SinifPrefabGetir() => sinifItemPrefab;
        public GameObject OgrenciPrefabGetir() => ogrenciItemPrefab;

        /// <summary>
        /// Sınıf listesi elemanı oluşturur.
        /// Layout: [Sınıf Adı (Tıklanabilir)] [Sil Butonu]
        /// </summary>
        private GameObject SinifItemOlustur()
        {
            GameObject item = new GameObject("SinifItem_Prefab");
            item.SetActive(false); // Prefab olarak kullanılacak

            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 60);

            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(10, 10, 5, 5);

            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.35f, 0.8f);

            // Sınıf ad butonu (tıklanınca detay açılır)
            GameObject adBtn = new GameObject("AdButton");
            adBtn.transform.SetParent(item.transform, false);
            RectTransform adRect = adBtn.AddComponent<RectTransform>();
            adRect.sizeDelta = new Vector2(600, 50);
            LayoutElement adLE = adBtn.AddComponent<LayoutElement>();
            adLE.flexibleWidth = 1;
            adLE.preferredHeight = 50;

            Image adBg = adBtn.AddComponent<Image>();
            adBg.color = new Color(0.25f, 0.35f, 0.5f);
            Button adButton = adBtn.AddComponent<Button>();

            GameObject adText = new GameObject("Text");
            adText.transform.SetParent(adBtn.transform, false);
            RectTransform adTextRect = adText.AddComponent<RectTransform>();
            adTextRect.anchorMin = Vector2.zero;
            adTextRect.anchorMax = Vector2.one;
            adTextRect.offsetMin = new Vector2(15, 0);
            adTextRect.offsetMax = new Vector2(-15, 0);
            TextMeshProUGUI adTmp = adText.AddComponent<TextMeshProUGUI>();
            adTmp.text = "Sınıf Adı";
            adTmp.fontSize = 20;
            adTmp.color = Color.white;
            adTmp.alignment = TextAlignmentOptions.Left;

            // Sil butonu
            GameObject silBtn = new GameObject("SilButton");
            silBtn.transform.SetParent(item.transform, false);
            RectTransform silRect = silBtn.AddComponent<RectTransform>();
            silRect.sizeDelta = new Vector2(80, 50);
            LayoutElement silLE = silBtn.AddComponent<LayoutElement>();
            silLE.preferredWidth = 80;
            silLE.preferredHeight = 50;

            Image silBg = silBtn.AddComponent<Image>();
            silBg.color = new Color(0.7f, 0.2f, 0.2f);
            silBtn.AddComponent<Button>();

            GameObject silText = new GameObject("Text");
            silText.transform.SetParent(silBtn.transform, false);
            RectTransform silTextRect = silText.AddComponent<RectTransform>();
            silTextRect.anchorMin = Vector2.zero;
            silTextRect.anchorMax = Vector2.one;
            silTextRect.offsetMin = Vector2.zero;
            silTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI silTmp = silText.AddComponent<TextMeshProUGUI>();
            silTmp.text = "SİL";
            silTmp.fontSize = 18;
            silTmp.color = Color.white;
            silTmp.alignment = TextAlignmentOptions.Center;
            silTmp.fontStyle = FontStyles.Bold;

            item.transform.SetParent(transform, false);
            return item;
        }

        /// <summary>
        /// Öğrenci listesi elemanı oluşturur.
        /// Layout: [Numara - Ad Soyad] [Sil Butonu]
        /// </summary>
        private GameObject OgrenciItemOlustur()
        {
            GameObject item = new GameObject("OgrenciItem_Prefab");
            item.SetActive(false);

            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800, 50);

            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(10, 10, 5, 5);

            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.22f, 0.3f, 0.8f);

            // Öğrenci bilgi buton (tıklanabilir alan — detay görüntüleme için)
            GameObject bilgiBtn = new GameObject("BilgiButton");
            bilgiBtn.transform.SetParent(item.transform, false);
            RectTransform bilgiBtnRect = bilgiBtn.AddComponent<RectTransform>();
            bilgiBtnRect.sizeDelta = new Vector2(600, 40);
            LayoutElement bilgiBtnLE = bilgiBtn.AddComponent<LayoutElement>();
            bilgiBtnLE.flexibleWidth = 1;
            bilgiBtnLE.preferredHeight = 40;
            Image bilgiBtnBg = bilgiBtn.AddComponent<Image>();
            bilgiBtnBg.color = new Color(0.2f, 0.25f, 0.35f, 0.5f);
            bilgiBtn.AddComponent<Button>();

            // Öğrenci bilgi text'i (bilgiBtn'nin çocuğu)
            GameObject bilgiObj = new GameObject("BilgiText");
            bilgiObj.transform.SetParent(bilgiBtn.transform, false);
            RectTransform bilgiRect = bilgiObj.AddComponent<RectTransform>();
            bilgiRect.anchorMin = Vector2.zero;
            bilgiRect.anchorMax = Vector2.one;
            bilgiRect.offsetMin = new Vector2(8, 0);
            bilgiRect.offsetMax = new Vector2(-8, 0);

            TextMeshProUGUI bilgiTmp = bilgiObj.AddComponent<TextMeshProUGUI>();
            bilgiTmp.text = "1234 - Ad Soyad";
            bilgiTmp.fontSize = 18;
            bilgiTmp.color = Color.white;
            bilgiTmp.alignment = TextAlignmentOptions.Left;

            // Sil butonu
            GameObject silBtn = new GameObject("SilButton");
            silBtn.transform.SetParent(item.transform, false);
            RectTransform silRect = silBtn.AddComponent<RectTransform>();
            silRect.sizeDelta = new Vector2(70, 40);
            LayoutElement silLE = silBtn.AddComponent<LayoutElement>();
            silLE.preferredWidth = 70;
            silLE.preferredHeight = 40;

            Image silBg = silBtn.AddComponent<Image>();
            silBg.color = new Color(0.7f, 0.2f, 0.2f);
            silBtn.AddComponent<Button>();

            GameObject silText = new GameObject("Text");
            silText.transform.SetParent(silBtn.transform, false);
            RectTransform silTextRect = silText.AddComponent<RectTransform>();
            silTextRect.anchorMin = Vector2.zero;
            silTextRect.anchorMax = Vector2.one;
            silTextRect.offsetMin = Vector2.zero;
            silTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI silTmp = silText.AddComponent<TextMeshProUGUI>();
            silTmp.text = "SİL";
            silTmp.fontSize = 16;
            silTmp.color = Color.white;
            silTmp.alignment = TextAlignmentOptions.Center;

            item.transform.SetParent(transform, false);
            return item;
        }
    }
}
