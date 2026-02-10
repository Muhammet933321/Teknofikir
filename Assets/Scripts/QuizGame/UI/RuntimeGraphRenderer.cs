using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace QuizGame.UI
{
    /// <summary>
    /// Runtime grafik çizim sistemi. Unity UI Image/RectTransform tabanlı.
    /// Build sonrası çalışır. Çubuk grafik, çizgi gostergesi, ilerleme çubukları oluşturur.
    /// </summary>
    public class RuntimeGraphRenderer : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════
        //  RENK PALETİ
        // ═══════════════════════════════════════════════════

        public static readonly Color[] DersRenkleri = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f, 1f),   // Matematik
            new Color(0.9f, 0.4f, 0.3f, 1f),   // Türkçe
            new Color(0.3f, 0.8f, 0.4f, 1f),   // Fen
            new Color(0.9f, 0.7f, 0.2f, 1f),   // Sosyal
            new Color(0.7f, 0.4f, 0.9f, 1f),   // İngilizce
            new Color(0.5f, 0.8f, 0.9f, 1f),   // GenelKultur
        };

        public static Color BasariRengi(float yuzde)
        {
            if (yuzde >= 80) return new Color(0.3f, 0.85f, 0.4f);
            if (yuzde >= 60) return new Color(0.5f, 0.8f, 0.3f);
            if (yuzde >= 40) return new Color(1.0f, 0.8f, 0.2f);
            if (yuzde >= 20) return new Color(1.0f, 0.5f, 0.2f);
            return new Color(1.0f, 0.3f, 0.3f);
        }

        public static string TrendOku(float onceki, float simdiki)
        {
            float f = simdiki - onceki;
            if (f > 10) return "▲▲";
            if (f > 3) return "▲";
            if (f > -3) return "►";
            if (f > -10) return "▼";
            return "▼▼";
        }

        public static Color TrendRengi(float onceki, float simdiki)
        {
            float f = simdiki - onceki;
            if (f > 5) return new Color(0.3f, 0.9f, 0.4f);
            if (f > -5) return new Color(0.8f, 0.8f, 0.3f);
            return new Color(1.0f, 0.3f, 0.3f);
        }

        // ═══════════════════════════════════════════════════
        //  İLERLEME ÇUBUĞU
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Renkli ilerleme çubuğu oluşturur ve parent'a ekler. Dönen objeyi tutabilirsin.
        /// </summary>
        public static GameObject IlerlemeCubuguOlustur(Transform parent, float yuzde, string etiket,
            Color? renk = null, float yukseklik = 28f)
        {
            Color cubukRenk = renk ?? BasariRengi(yuzde);

            // Ana container
            GameObject container = new GameObject("ProgressBar");
            container.transform.SetParent(parent, false);
            RectTransform cRect = container.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(0, yukseklik);
            LayoutElement cLE = container.AddComponent<LayoutElement>();
            cLE.flexibleWidth = 1;
            cLE.preferredHeight = yukseklik;

            // Arkaplan
            Image bgImg = container.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);

            // Dolgu
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(container.transform, false);
            RectTransform fRect = fill.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = new Vector2(Mathf.Clamp01(yuzde / 100f), 1f);
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = cubukRenk;

            // Metin
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(container.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(8, 0);
            tRect.offsetMax = new Vector2(-8, 0);
            var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = string.IsNullOrEmpty(etiket) ? $"%{yuzde:F0}" : $"{etiket}  %{yuzde:F0}";
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;

            return container;
        }

        // ═══════════════════════════════════════════════════
        //  YATAY ÇUBUK GRAFİK
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Yatay çubuk grafik oluşturur. Parent'ın VerticalLayoutGroup'a sahip olması beklenir.
        /// </summary>
        public static GameObject YatayCubukGrafikOlustur(Transform parent,
            string[] etiketler, float[] degerler, Color[] renkler,
            string baslik = "", float maxDeger = -1, bool yuzdeGoster = false)
        {
            if (etiketler.Length == 0) return null;

            if (maxDeger <= 0)
            {
                maxDeger = 100f;
                foreach (float d in degerler)
                    if (d > maxDeger) maxDeger = d * 1.1f;
            }

            // Ana container
            GameObject container = new GameObject("BarChart");
            container.transform.SetParent(parent, false);
            RectTransform cRect = container.AddComponent<RectTransform>();
            LayoutElement cLE = container.AddComponent<LayoutElement>();
            cLE.flexibleWidth = 1;

            VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Image cBg = container.AddComponent<Image>();
            cBg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            // Başlık
            if (!string.IsNullOrEmpty(baslik))
            {
                GameObject titleObj = TMPOlustur(container.transform, baslik, 16, Color.white,
                    TMPro.TextAlignmentOptions.Center, 30);
                titleObj.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;
            }

            // Çubuklar
            for (int i = 0; i < etiketler.Length; i++)
            {
                float yuzde = maxDeger > 0 ? (degerler[i] / maxDeger) * 100f : 0;
                Color renk = (renkler != null && i < renkler.Length) ? renkler[i] : DersRenkleri[i % DersRenkleri.Length];
                string degerStr = yuzdeGoster ? $"%{degerler[i]:F0}" : $"{degerler[i]:F0}";
                string label = $"{etiketler[i]}  —  {degerStr}";
                IlerlemeCubuguOlustur(container.transform, yuzde, label, renk, 26);
            }

            float totalH = 40 + etiketler.Length * 32;
            cLE.preferredHeight = totalH;
            cRect.sizeDelta = new Vector2(0, totalH);

            return container;
        }

        // ═══════════════════════════════════════════════════
        //  ÖZET KART
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// İstatistik kartı oluşturur (büyük değer + alt etiket). Parent'a ekler.
        /// </summary>
        public static GameObject OzetKartOlustur(Transform parent, string deger, string etiket,
            Color renk, float genislik = 120f, float yukseklik = 70f)
        {
            GameObject kart = new GameObject("StatCard");
            kart.transform.SetParent(parent, false);
            RectTransform kRect = kart.AddComponent<RectTransform>();
            kRect.sizeDelta = new Vector2(genislik, yukseklik);
            LayoutElement kLE = kart.AddComponent<LayoutElement>();
            kLE.preferredWidth = genislik;
            kLE.preferredHeight = yukseklik;

            Image kBg = kart.AddComponent<Image>();
            kBg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

            VerticalLayoutGroup vlg = kart.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 6, 4);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Sol şerit (sol kenarda renkli çizgi)
            // Büyük değer
            GameObject valObj = TMPOlustur(kart.transform, deger, 22, renk,
                TMPro.TextAlignmentOptions.Center, yukseklik * 0.5f);
            valObj.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;

            // Etiket
            TMPOlustur(kart.transform, etiket, 11, new Color(0.6f, 0.6f, 0.6f),
                TMPro.TextAlignmentOptions.Center, yukseklik * 0.3f);

            return kart;
        }

        // ═══════════════════════════════════════════════════
        //  HAFTALIK / GÜNLÜK TABLO
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Tablo satırı oluşturur. Kolonlar string olarak gelir.
        /// </summary>
        public static GameObject TabloSatiriOlustur(Transform parent, string[] kolonlar,
            bool baslikMi = false, Color? satirRenk = null)
        {
            GameObject satir = new GameObject(baslikMi ? "HeaderRow" : "DataRow");
            satir.transform.SetParent(parent, false);
            RectTransform sRect = satir.AddComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(0, baslikMi ? 32 : 28);
            LayoutElement sLE = satir.AddComponent<LayoutElement>();
            sLE.flexibleWidth = 1;
            sLE.preferredHeight = baslikMi ? 32 : 28;

            Image sBg = satir.AddComponent<Image>();
            sBg.color = satirRenk ?? (baslikMi
                ? new Color(0.18f, 0.2f, 0.28f, 0.95f)
                : new Color(0.14f, 0.14f, 0.18f, 0.6f));

            HorizontalLayoutGroup hlg = satir.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 8, 2, 2);
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            float fontSize = baslikMi ? 13 : 12;
            Color textColor = baslikMi ? Color.white : new Color(0.85f, 0.85f, 0.85f);

            for (int i = 0; i < kolonlar.Length; i++)
            {
                var tmpObj = TMPOlustur(satir.transform, kolonlar[i], fontSize, textColor,
                    TMPro.TextAlignmentOptions.MidlineLeft, -1);
                if (baslikMi)
                    tmpObj.GetComponent<TMPro.TextMeshProUGUI>().fontStyle = TMPro.FontStyles.Bold;
            }

            return satir;
        }

        // ═══════════════════════════════════════════════════
        //  SEGMENTLİ ÇUBUK (Mini Pasta Grafik)
        // ═══════════════════════════════════════════════════

        public static GameObject SegmentliCubukOlustur(Transform parent,
            float[] degerler, Color[] renkler, string[] etiketler, float yukseklik = 20f)
        {
            float toplam = 0;
            foreach (float d in degerler) toplam += d;
            if (toplam <= 0) return null;

            GameObject container = new GameObject("SegmentBar");
            container.transform.SetParent(parent, false);
            RectTransform cRect = container.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(0, yukseklik);
            LayoutElement cLE = container.AddComponent<LayoutElement>();
            cLE.flexibleWidth = 1;
            cLE.preferredHeight = yukseklik;

            HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 1;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;

            for (int i = 0; i < degerler.Length; i++)
            {
                float oran = degerler[i] / toplam;
                if (oran <= 0) continue;

                GameObject seg = new GameObject($"Seg_{i}");
                seg.transform.SetParent(container.transform, false);
                seg.AddComponent<RectTransform>();
                LayoutElement segLE = seg.AddComponent<LayoutElement>();
                segLE.flexibleWidth = oran;

                Image segImg = seg.AddComponent<Image>();
                segImg.color = (renkler != null && i < renkler.Length)
                    ? renkler[i] : DersRenkleri[i % DersRenkleri.Length];
            }

            return container;
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCILAR
        // ═══════════════════════════════════════════════════

        /// <summary>TextMeshPro text nesnesi oluşturur.</summary>
        public static GameObject TMPOlustur(Transform parent, string metin, float fontSize,
            Color renk, TMPro.TextAlignmentOptions hizalama, float yukseklik = -1,
            float genislik = -1, TMPro.FontStyles fontStyle = TMPro.FontStyles.Normal)
        {
            GameObject obj = new GameObject("TMP");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();

            LayoutElement le = obj.AddComponent<LayoutElement>();
            if (yukseklik > 0) le.preferredHeight = yukseklik;
            if (genislik > 0) le.preferredWidth = genislik;
            else le.flexibleWidth = 1;

            var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = metin;
            tmp.fontSize = fontSize;
            tmp.color = renk;
            tmp.alignment = hizalama;
            tmp.fontStyle = fontStyle;
            tmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;

            return obj;
        }

        /// <summary>Ders adını Türkçe döndürür.</summary>
        public static string DersAdi(Data.DersKategorisi ders)
        {
            switch (ders)
            {
                case Data.DersKategorisi.Matematik: return "Matematik";
                case Data.DersKategorisi.Turkce: return "Türkçe";
                case Data.DersKategorisi.Fen: return "Fen";
                case Data.DersKategorisi.Sosyal: return "Sosyal";
                case Data.DersKategorisi.Ingilizce: return "İngilizce";
                case Data.DersKategorisi.GenelKultur: return "Genel Kültür";
                default: return ders.ToString();
            }
        }

        // ═══════════════════════════════════════════════════
        //  EK OVERLOADLAR  (StudentDetailUI / RuntimeQuestionManagerUI için)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// İlerleme çubuğu — etiket + yuzde + maxDeger destekli.
        /// </summary>
        public static GameObject IlerlemeCubuguOlustur(Transform parent,
            string etiket, float deger, float maxDeger, Color renk, float yukseklik = 28f)
        {
            float yuzde = maxDeger > 0 ? (deger / maxDeger) * 100f : 0f;
            return IlerlemeCubuguOlustur(parent, yuzde, etiket, renk, yukseklik);
        }

        /// <summary>
        /// Yatay çubuk grafik — tuple listesi ile.
        /// </summary>
        public static GameObject YatayCubukGrafikOlustur(Transform parent,
            List<(string etiket, float deger, Color renk)> veriler, float maxDeger = 100f)
        {
            if (veriler == null || veriler.Count == 0) return null;
            string[] etiketler = new string[veriler.Count];
            float[] degerler = new float[veriler.Count];
            Color[] renkler = new Color[veriler.Count];
            for (int i = 0; i < veriler.Count; i++)
            {
                etiketler[i] = veriler[i].etiket;
                degerler[i] = veriler[i].deger;
                renkler[i] = veriler[i].renk;
            }
            return YatayCubukGrafikOlustur(parent, etiketler, degerler, renkler, "", maxDeger, true);
        }

        /// <summary>
        /// Tablo satırı — kolon genişlikleri destekli.
        /// </summary>
        public static GameObject TabloSatiriOlustur(Transform parent, string[] kolonlar,
            float[] genislikler, Color satirRenk, bool baslikMi = false)
        {
            GameObject satir = new GameObject(baslikMi ? "HeaderRow" : "DataRow");
            satir.transform.SetParent(parent, false);
            RectTransform sRect = satir.AddComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(0, baslikMi ? 32 : 28);
            LayoutElement sLE = satir.AddComponent<LayoutElement>();
            sLE.flexibleWidth = 1;
            sLE.preferredHeight = baslikMi ? 32 : 28;

            Image sBg = satir.AddComponent<Image>();
            sBg.color = satirRenk;

            HorizontalLayoutGroup hlg = satir.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 8, 2, 2);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            float fontSize = baslikMi ? 13 : 12;
            Color textColor = baslikMi ? Color.white : new Color(0.85f, 0.85f, 0.85f);
            TMPro.FontStyles fs = baslikMi ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;

            for (int i = 0; i < kolonlar.Length; i++)
            {
                float w = (genislikler != null && i < genislikler.Length) ? genislikler[i] : -1;
                TMPOlustur(satir.transform, kolonlar[i], fontSize, textColor,
                    TMPro.TextAlignmentOptions.MidlineLeft, -1, w, fs);
            }

            return satir;
        }

        /// <summary>
        /// Segmentli çubuk — tuple dizisi destekli.
        /// </summary>
        public static GameObject SegmentliCubukOlustur(Transform parent,
            (float deger, Color renk, string etiket)[] segmentler, float toplam, float yukseklik = 20f)
        {
            if (segmentler == null || segmentler.Length == 0 || toplam <= 0) return null;
            float[] d = new float[segmentler.Length];
            Color[] r = new Color[segmentler.Length];
            string[] e = new string[segmentler.Length];
            for (int i = 0; i < segmentler.Length; i++)
            {
                d[i] = segmentler[i].deger;
                r[i] = segmentler[i].renk;
                e[i] = segmentler[i].etiket;
            }
            return SegmentliCubukOlustur(parent, d, r, e, yukseklik);
        }

        /// <summary>
        /// Özet kart — etiket ilk, deger sonra (label-value sıralı).
        /// Genişlik/yükseklik int olarak alarak ana metottan ayırt edilir.
        /// </summary>
        public static GameObject EtiketliKartOlustur(Transform parent, string etiket, string deger,
            Color renk, float genislik = 120f, float yukseklik = 70f)
        {
            return OzetKartOlustur(parent, deger, etiket, renk, genislik, yukseklik);
        }
    }
}
