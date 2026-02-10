using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace QuizGame.Editor
{
    /// <summary>
    /// Unity Editor içinde grafik çizme yardımcı sınıfı.
    /// Çubuk grafik, çizgi grafik, pasta grafik ve ilerleme çubukları çizer.
    /// ProgressDashboard ve QuestionEditorPro tarafından kullanılır.
    /// </summary>
    public static class GraphRenderer
    {
        // ═══════════════════════════════════════════════════
        //  RENK PALETİ
        // ═══════════════════════════════════════════════════

        public static readonly Color[] DersRenkleri = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f, 1f),   // Matematik - Mavi
            new Color(0.9f, 0.4f, 0.3f, 1f),   // Türkçe - Kırmızı
            new Color(0.3f, 0.8f, 0.4f, 1f),   // Fen - Yeşil
            new Color(0.9f, 0.7f, 0.2f, 1f),   // Sosyal - Sarı
            new Color(0.7f, 0.4f, 0.9f, 1f),   // İngilizce - Mor
            new Color(0.5f, 0.8f, 0.9f, 1f),   // Genel Kültür - Açık mavi
        };

        public static readonly Color ZorlukKolay = new Color(0.4f, 0.9f, 0.4f, 1f);
        public static readonly Color ZorlukOrta = new Color(1.0f, 0.8f, 0.2f, 1f);
        public static readonly Color ZorlukZor = new Color(1.0f, 0.3f, 0.3f, 1f);

        public static readonly Color ArkaplanKoyu = new Color(0.18f, 0.18f, 0.18f, 1f);
        public static readonly Color ArkaplanAcik = new Color(0.22f, 0.22f, 0.22f, 1f);
        public static readonly Color IzgaraRengi = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public static readonly Color MetinRengi = new Color(0.7f, 0.7f, 0.7f, 1f);

        // ═══════════════════════════════════════════════════
        //  ÇUBUK GRAFİK (BAR CHART)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Yatay çubuk grafik çizer.
        /// </summary>
        public static void CubukGrafik(Rect alan, string[] etiketler, float[] degerler,
            Color[] renkler, string baslik = "", float maxDeger = -1, bool yuzdeGoster = false)
        {
            if (etiketler.Length == 0) return;

            // Arkaplan
            EditorGUI.DrawRect(alan, ArkaplanKoyu);

            float padding = 10f;
            float etiketGenislik = 100f;
            float ustBosluk = string.IsNullOrEmpty(baslik) ? padding : 30f;

            // Başlık
            if (!string.IsNullOrEmpty(baslik))
            {
                var baslikRect = new Rect(alan.x + padding, alan.y + 5, alan.width - padding * 2, 20);
                var baslikStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUI.Label(baslikRect, baslik, baslikStyle);
            }

            float grafikX = alan.x + padding + etiketGenislik;
            float grafikGenislik = alan.width - padding * 2 - etiketGenislik - 50f;
            float grafikY = alan.y + ustBosluk;
            float kullanilabilirYukseklik = alan.height - ustBosluk - padding;

            if (maxDeger <= 0)
                maxDeger = degerler.Max() > 0 ? degerler.Max() * 1.1f : 100f;

            float cubukYukseklik = Mathf.Min(30f, (kullanilabilirYukseklik - 5f * etiketler.Length) / etiketler.Length);
            float cubukAralik = cubukYukseklik + 5f;

            var etiketStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = MetinRengi },
                fontSize = 11
            };

            var degerStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };

            for (int i = 0; i < etiketler.Length; i++)
            {
                float y = grafikY + i * cubukAralik;

                // Etiket
                var etiketRect = new Rect(alan.x + padding, y, etiketGenislik - 5, cubukYukseklik);
                GUI.Label(etiketRect, etiketler[i], etiketStyle);

                // Çubuk arkaplan
                var cubukBg = new Rect(grafikX, y, grafikGenislik, cubukYukseklik);
                EditorGUI.DrawRect(cubukBg, ArkaplanAcik);

                // Çubuk
                float oran = maxDeger > 0 ? degerler[i] / maxDeger : 0;
                float cubukGen = grafikGenislik * Mathf.Clamp01(oran);
                var cubukRect = new Rect(grafikX, y, cubukGen, cubukYukseklik);
                Color renk = (renkler != null && i < renkler.Length) ? renkler[i] : DersRenkleri[i % DersRenkleri.Length];
                EditorGUI.DrawRect(cubukRect, renk);

                // Değer
                string degerStr = yuzdeGoster ? $"%{degerler[i]:F0}" : $"{degerler[i]:F0}";
                var degerRect = new Rect(grafikX + cubukGen + 4, y, 50, cubukYukseklik);
                GUI.Label(degerRect, degerStr, degerStyle);
            }
        }

        // ═══════════════════════════════════════════════════
        //  DİKEY ÇUBUK GRAFİK (VERTICAL BAR)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Dikey çubuk grafik çizer. Haftalık/günlük karşılaştırmalar için idealdir.
        /// </summary>
        public static void DikeyCubukGrafik(Rect alan, string[] etiketler, float[][] veriSerileri,
            string[] seriAdlari, Color[] seriRenkleri, string baslik = "", float maxDeger = -1)
        {
            if (etiketler.Length == 0 || veriSerileri.Length == 0) return;

            EditorGUI.DrawRect(alan, ArkaplanKoyu);

            float padding = 12f;
            float ustBosluk = string.IsNullOrEmpty(baslik) ? padding : 32f;
            float altBosluk = 40f; // Etiketler için
            float legendYukseklik = 20f;
            float solBosluk = 45f;

            // Başlık
            if (!string.IsNullOrEmpty(baslik))
            {
                var baslikRect = new Rect(alan.x + padding, alan.y + 6, alan.width - padding * 2, 22);
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    fontSize = 12
                };
                GUI.Label(baslikRect, baslik, style);
            }

            // Grafik alanı
            float gX = alan.x + solBosluk;
            float gY = alan.y + ustBosluk;
            float gW = alan.width - solBosluk - padding;
            float gH = alan.height - ustBosluk - altBosluk - legendYukseklik;

            if (maxDeger <= 0)
            {
                maxDeger = 100f;
                foreach (var seri in veriSerileri)
                    if (seri.Length > 0) maxDeger = Mathf.Max(maxDeger, seri.Max() * 1.1f);
            }

            // Izgara çizgileri
            int izgaraSayisi = 5;
            var izgaraStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = MetinRengi },
                fontSize = 9
            };

            for (int i = 0; i <= izgaraSayisi; i++)
            {
                float yOran = (float)i / izgaraSayisi;
                float yPos = gY + gH - gH * yOran;
                float deger = maxDeger * yOran;

                // Çizgi
                EditorGUI.DrawRect(new Rect(gX, yPos, gW, 1), IzgaraRengi);

                // Değer etiketi
                GUI.Label(new Rect(alan.x + 2, yPos - 8, solBosluk - 5, 16), $"{deger:F0}", izgaraStyle);
            }

            // Çubuklar
            int seriSayisi = veriSerileri.Length;
            float grupGenislik = gW / etiketler.Length;
            float cubukGenislik = Mathf.Min(25f, (grupGenislik - 8f) / seriSayisi);

            for (int e = 0; e < etiketler.Length; e++)
            {
                float grupX = gX + e * grupGenislik;

                for (int s = 0; s < seriSayisi; s++)
                {
                    if (e >= veriSerileri[s].Length) continue;

                    float deger = veriSerileri[s][e];
                    float oran = maxDeger > 0 ? deger / maxDeger : 0;
                    float cubukH = gH * Mathf.Clamp01(oran);

                    float cubukX = grupX + (grupGenislik - cubukGenislik * seriSayisi) / 2f + s * cubukGenislik;
                    float cubukY = gY + gH - cubukH;

                    Color renk = (seriRenkleri != null && s < seriRenkleri.Length)
                        ? seriRenkleri[s] : DersRenkleri[s % DersRenkleri.Length];

                    EditorGUI.DrawRect(new Rect(cubukX, cubukY, cubukGenislik - 1, cubukH), renk);
                }

                // Alt etiket
                var etiketRect = new Rect(grupX, gY + gH + 4, grupGenislik, 20);
                var eStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = MetinRengi },
                    fontSize = 9
                };
                GUI.Label(etiketRect, etiketler[e], eStyle);
            }

            // Legend
            float legendY = gY + gH + altBosluk - 8;
            float legendX = gX;
            var legendStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 10
            };

            for (int s = 0; s < seriSayisi && s < seriAdlari.Length; s++)
            {
                Color renk = (seriRenkleri != null && s < seriRenkleri.Length)
                    ? seriRenkleri[s] : DersRenkleri[s % DersRenkleri.Length];

                EditorGUI.DrawRect(new Rect(legendX, legendY + 3, 12, 12), renk);
                GUI.Label(new Rect(legendX + 16, legendY, 100, 18), seriAdlari[s], legendStyle);
                legendX += 120f;
            }
        }

        // ═══════════════════════════════════════════════════
        //  ÇİZGİ GRAFİK (LINE CHART)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Çizgi grafik çizer. Gelişim trendi gösterimi için.
        /// </summary>
        public static void CizgiGrafik(Rect alan, string[] xEtiketler, float[][] veriSerileri,
            string[] seriAdlari, Color[] seriRenkleri, string baslik = "",
            float maxY = -1, float minY = 0, bool noktaGoster = true)
        {
            if (xEtiketler.Length == 0) return;

            EditorGUI.DrawRect(alan, ArkaplanKoyu);

            float padding = 12f;
            float ustBosluk = string.IsNullOrEmpty(baslik) ? padding : 32f;
            float altBosluk = 40f;
            float legendYukseklik = 22f;
            float solBosluk = 45f;

            // Başlık
            if (!string.IsNullOrEmpty(baslik))
            {
                var baslikRect = new Rect(alan.x + padding, alan.y + 6, alan.width - padding * 2, 22);
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    fontSize = 12
                };
                GUI.Label(baslikRect, baslik, style);
            }

            float gX = alan.x + solBosluk;
            float gY = alan.y + ustBosluk;
            float gW = alan.width - solBosluk - padding;
            float gH = alan.height - ustBosluk - altBosluk - legendYukseklik;

            if (maxY <= 0)
            {
                maxY = 100f;
                foreach (var seri in veriSerileri)
                    if (seri.Length > 0) maxY = Mathf.Max(maxY, seri.Max() * 1.1f);
            }

            // Izgara
            int izgaraSayisi = 5;
            var izgaraStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = MetinRengi },
                fontSize = 9
            };

            for (int i = 0; i <= izgaraSayisi; i++)
            {
                float yOran = (float)i / izgaraSayisi;
                float yPos = gY + gH - gH * yOran;
                float deger = minY + (maxY - minY) * yOran;

                EditorGUI.DrawRect(new Rect(gX, yPos, gW, 1), IzgaraRengi);
                GUI.Label(new Rect(alan.x + 2, yPos - 8, solBosluk - 5, 16), $"%{deger:F0}", izgaraStyle);
            }

            // X Etiketleri
            float xAralik = xEtiketler.Length > 1 ? gW / (xEtiketler.Length - 1) : gW;
            var eStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = MetinRengi },
                fontSize = 9
            };

            for (int i = 0; i < xEtiketler.Length; i++)
            {
                float xPos = gX + i * xAralik;
                // Dikey ince çizgi
                EditorGUI.DrawRect(new Rect(xPos, gY, 1, gH), new Color(0.3f, 0.3f, 0.3f, 0.3f));
                GUI.Label(new Rect(xPos - 25, gY + gH + 4, 50, 20), xEtiketler[i], eStyle);
            }

            // Çizgiler
            for (int s = 0; s < veriSerileri.Length; s++)
            {
                var seri = veriSerileri[s];
                if (seri.Length < 2) continue;

                Color renk = (seriRenkleri != null && s < seriRenkleri.Length)
                    ? seriRenkleri[s] : DersRenkleri[s % DersRenkleri.Length];

                for (int i = 0; i < seri.Length - 1; i++)
                {
                    float x1 = gX + i * xAralik;
                    float y1 = gY + gH - gH * Mathf.Clamp01((seri[i] - minY) / (maxY - minY));
                    float x2 = gX + (i + 1) * xAralik;
                    float y2 = gY + gH - gH * Mathf.Clamp01((seri[i + 1] - minY) / (maxY - minY));

                    // Kalın çizgi simülasyonu (rect ile)
                    CizgiCiz(new Vector2(x1, y1), new Vector2(x2, y2), renk, 2f);
                }

                // Noktalar
                if (noktaGoster)
                {
                    for (int i = 0; i < seri.Length; i++)
                    {
                        float x = gX + i * xAralik;
                        float y = gY + gH - gH * Mathf.Clamp01((seri[i] - minY) / (maxY - minY));
                        EditorGUI.DrawRect(new Rect(x - 3, y - 3, 7, 7), renk);
                        EditorGUI.DrawRect(new Rect(x - 2, y - 2, 5, 5), Color.white);
                        EditorGUI.DrawRect(new Rect(x - 1, y - 1, 3, 3), renk);
                    }
                }
            }

            // Legend
            float legendY = gY + gH + altBosluk;
            float legendX = gX;
            var legendStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                fontSize = 10
            };

            for (int s = 0; s < veriSerileri.Length && s < seriAdlari.Length; s++)
            {
                Color renk = (seriRenkleri != null && s < seriRenkleri.Length)
                    ? seriRenkleri[s] : DersRenkleri[s % DersRenkleri.Length];

                EditorGUI.DrawRect(new Rect(legendX, legendY + 3, 12, 12), renk);
                GUI.Label(new Rect(legendX + 16, legendY, 100, 18), seriAdlari[s], legendStyle);
                legendX += 120f;
            }
        }

        // ═══════════════════════════════════════════════════
        //  İLERLEME ÇUBUĞU (PROGRESS BAR)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Renkli ilerleme çubuğu çizer. Başarı yüzdesi gösterimi için.
        /// </summary>
        public static void IlerlemeÇubugu(Rect alan, float yuzde, string etiket = "",
            Color? renk = null, bool degerGoster = true)
        {
            Color cubukRenk = renk ?? BasariRengi(yuzde);
            Color arkaPlan = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Arkaplan
            EditorGUI.DrawRect(alan, arkaPlan);

            // Dolgu
            float oran = Mathf.Clamp01(yuzde / 100f);
            var dolguRect = new Rect(alan.x, alan.y, alan.width * oran, alan.height);
            EditorGUI.DrawRect(dolguRect, cubukRenk);

            // Kenarlık
            var kenStyle = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            EditorGUI.DrawRect(new Rect(alan.x, alan.y, alan.width, 1), kenStyle);
            EditorGUI.DrawRect(new Rect(alan.x, alan.yMax - 1, alan.width, 1), kenStyle);
            EditorGUI.DrawRect(new Rect(alan.x, alan.y, 1, alan.height), kenStyle);
            EditorGUI.DrawRect(new Rect(alan.xMax - 1, alan.y, 1, alan.height), kenStyle);

            // Metin
            string metin = degerGoster ? $"{etiket}  %{yuzde:F0}" : etiket;
            var metinStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold,
                fontSize = 10
            };
            GUI.Label(alan, metin, metinStyle);
        }

        // ═══════════════════════════════════════════════════
        //  PASTA GRAFİK (PIE CHART) - Basitleştirilmiş
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Mini pasta grafik simülasyonu (yatay segmentli çubuk).
        /// </summary>
        public static void SegmentliCubuk(Rect alan, float[] degerler, Color[] renkler,
            string[] etiketler = null)
        {
            float toplam = degerler.Sum();
            if (toplam <= 0) return;

            float x = alan.x;
            for (int i = 0; i < degerler.Length; i++)
            {
                float oran = degerler[i] / toplam;
                float genislik = alan.width * oran;
                Color renk = (renkler != null && i < renkler.Length) ? renkler[i] : DersRenkleri[i % DersRenkleri.Length];

                EditorGUI.DrawRect(new Rect(x, alan.y, genislik, alan.height), renk);
                x += genislik;
            }

            // Legend altında
            if (etiketler != null)
            {
                float legendX = alan.x;
                float legendY = alan.yMax + 4;
                var legendStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = MetinRengi },
                    fontSize = 9
                };

                for (int i = 0; i < etiketler.Length && i < degerler.Length; i++)
                {
                    Color renk = (renkler != null && i < renkler.Length) ? renkler[i] : DersRenkleri[i % DersRenkleri.Length];
                    EditorGUI.DrawRect(new Rect(legendX, legendY + 2, 8, 8), renk);
                    float oran = toplam > 0 ? degerler[i] / toplam * 100f : 0;
                    string txt = $"{etiketler[i]} (%{oran:F0})";
                    GUI.Label(new Rect(legendX + 12, legendY, 100, 14), txt, legendStyle);
                    legendX += 110f;
                }
            }
        }

        // ═══════════════════════════════════════════════════
        //  ÖZET KART (STAT CARD)
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// İstatistik kartı çizer (büyük sayı + etiket).
        /// </summary>
        public static void OzetKart(Rect alan, string deger, string etiket, Color renk)
        {
            // Arkaplan
            EditorGUI.DrawRect(alan, ArkaplanKoyu);

            // Sol renkli şerit
            EditorGUI.DrawRect(new Rect(alan.x, alan.y, 4, alan.height), renk);

            // Büyük değer
            var degerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = renk }
            };
            GUI.Label(new Rect(alan.x + 8, alan.y + 4, alan.width - 16, alan.height * 0.55f), deger, degerStyle);

            // Etiket
            var etiketStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = MetinRengi },
                fontSize = 10
            };
            GUI.Label(new Rect(alan.x + 8, alan.y + alan.height * 0.5f, alan.width - 16, alan.height * 0.4f), etiket, etiketStyle);
        }

        // ═══════════════════════════════════════════════════
        //  YARDIMCILAR
        // ═══════════════════════════════════════════════════

        /// <summary>
        /// Başarı yüzdesine göre renk döndürür (kırmızı → sarı → yeşil).
        /// </summary>
        public static Color BasariRengi(float yuzde)
        {
            if (yuzde >= 80) return new Color(0.3f, 0.85f, 0.4f, 1f);    // Yeşil
            if (yuzde >= 60) return new Color(0.5f, 0.8f, 0.3f, 1f);     // Açık yeşil
            if (yuzde >= 40) return new Color(1.0f, 0.8f, 0.2f, 1f);     // Sarı
            if (yuzde >= 20) return new Color(1.0f, 0.5f, 0.2f, 1f);     // Turuncu
            return new Color(1.0f, 0.3f, 0.3f, 1f);                       // Kırmızı
        }

        /// <summary>
        /// Trend okunu döndürür.
        /// </summary>
        public static string TrendOku(float onceki, float simdiki)
        {
            float fark = simdiki - onceki;
            if (fark > 10) return "▲▲";
            if (fark > 3) return "▲";
            if (fark > -3) return "▶";
            if (fark > -10) return "▼";
            return "▼▼";
        }

        public static Color TrendRengi(float onceki, float simdiki)
        {
            float fark = simdiki - onceki;
            if (fark > 5) return new Color(0.3f, 0.9f, 0.4f, 1f);
            if (fark > -5) return new Color(0.8f, 0.8f, 0.3f, 1f);
            return new Color(1.0f, 0.3f, 0.3f, 1f);
        }

        /// <summary>
        /// İki nokta arasına kalın çizgi çizer (rect tabanlı).
        /// </summary>
        private static void CizgiCiz(Vector2 baslangic, Vector2 bitis, Color renk, float kalinlik)
        {
            Vector2 fark = bitis - baslangic;
            float uzunluk = fark.magnitude;
            if (uzunluk < 0.1f) return;

            float aci = Mathf.Atan2(fark.y, fark.x) * Mathf.Rad2Deg;

            var matris = GUI.matrix;
            GUIUtility.RotateAroundPivot(aci, baslangic);
            EditorGUI.DrawRect(new Rect(baslangic.x, baslangic.y - kalinlik / 2f, uzunluk, kalinlik), renk);
            GUI.matrix = matris;
        }
    }
}
