using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.Editor
{
    /// <summary>
    /// Unity Editor içinden soru ekleme, düzenleme, silme aracı.
    /// Menu: QuizGame > Soru Yöneticisi
    /// </summary>
    public class QuestionManager : EditorWindow
    {
        // ── Veritabanı ──
        private QuestionDatabase db;
        private string dbYolu;
        private bool dbYuklendi;

        // ── Liste görünümü ──
        private Vector2 listeScroll;
        private Vector2 detayScroll;
        private int seciliIndex = -1;

        // ── Filtre ──
        private string aramaMetni = "";
        private int filtreZorluk = -1;   // -1 = tümü
        private int filtreKategori = -1; // -1 = tümü
        private readonly string[] zorlukSecenekler = { "Tümü", "Kolay", "Orta", "Zor" };
        private readonly string[] kategoriSecenekler = { "Tümü", "Matematik", "Türkçe", "Fen", "Sosyal", "İngilizce", "Genel Kültür" };

        // ── Düzenleme ──
        private QuestionData duzenlenecek;
        private bool yeniSoruModu;

        // ── İstatistikler ──
        private int toplamSoru, kolayCount, ortaCount, zorCount;

        [MenuItem("QuizGame/Soru Yöneticisi", false, 10)]
        public static void ShowWindow()
        {
            var w = GetWindow<QuestionManager>("Soru Yöneticisi");
            w.minSize = new Vector2(800, 500);
        }

        private void OnEnable()
        {
            VeritabaniYukle();
        }

        private void OnFocus()
        {
            VeritabaniYukle();
        }

        private void VeritabaniYukle()
        {
            dbYolu = Path.Combine(Application.persistentDataPath, "soru_veritabani.json");

            if (File.Exists(dbYolu))
            {
                string json = File.ReadAllText(dbYolu);
                db = JsonUtility.FromJson<QuestionDatabase>(json);
            }

            if (db == null) db = new QuestionDatabase();
            dbYuklendi = true;
            IstatistikleriGuncelle();
        }

        private void VeritabaniKaydet()
        {
            string json = JsonUtility.ToJson(db, true);
            File.WriteAllText(dbYolu, json);

            // Runtime'da da güncelle
            if (Application.isPlaying && DataManager.Instance != null)
                DataManager.Instance.soruVeritabani = db;

            IstatistikleriGuncelle();
        }

        private void IstatistikleriGuncelle()
        {
            if (db == null) return;
            toplamSoru = db.sorular.Count;
            kolayCount = db.sorular.Count(s => s.zorluk == ZorlukSeviyesi.Kolay);
            ortaCount = db.sorular.Count(s => s.zorluk == ZorlukSeviyesi.Orta);
            zorCount = db.sorular.Count(s => s.zorluk == ZorlukSeviyesi.Zor);
        }

        // ═══════════════════════════════════════════════════
        //  GUI
        // ═══════════════════════════════════════════════════

        private void OnGUI()
        {
            if (!dbYuklendi) VeritabaniYukle();

            EditorGUILayout.BeginHorizontal();

            // Sol panel: Soru listesi
            EditorGUILayout.BeginVertical(GUILayout.Width(360));
            SolPanelCiz();
            EditorGUILayout.EndVertical();

            // Dikey ayırıcı
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

            // Sağ panel: Soru detayı / düzenleme
            EditorGUILayout.BeginVertical();
            SagPanelCiz();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  SOL PANEL (Liste)
        // ═══════════════════════════════════════════════════

        private void SolPanelCiz()
        {
            // İstatistikler
            EditorGUILayout.LabelField($"Toplam: {toplamSoru} | Kolay: {kolayCount} | Orta: {ortaCount} | Zor: {zorCount}",
                EditorStyles.miniLabel);

            GUILayout.Space(4);

            // Filtreler
            EditorGUILayout.BeginHorizontal();
            aramaMetni = EditorGUILayout.TextField("", aramaMetni, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("✕", GUILayout.Width(22))) aramaMetni = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            filtreZorluk = EditorGUILayout.Popup(filtreZorluk + 1, zorlukSecenekler, GUILayout.Width(80)) - 1;
            filtreKategori = EditorGUILayout.Popup(filtreKategori + 1, kategoriSecenekler, GUILayout.Width(110)) - 1;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ Yeni Soru", GUILayout.Width(90)))
            {
                YeniSoruBaslat();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Soru listesi
            var filtrelenmis = FiltreleSorular();

            listeScroll = EditorGUILayout.BeginScrollView(listeScroll);
            for (int i = 0; i < filtrelenmis.Count; i++)
            {
                var soru = filtrelenmis[i];
                int gercekIndex = db.sorular.IndexOf(soru);

                bool secili = (gercekIndex == seciliIndex);
                string zorlukTag = soru.zorluk == ZorlukSeviyesi.Kolay ? "[K]" :
                                   soru.zorluk == ZorlukSeviyesi.Orta ? "[O]" : "[Z]";
                string onIzleme = soru.soruMetni.Length > 40
                    ? soru.soruMetni.Substring(0, 40) + "..."
                    : soru.soruMetni;

                var style = secili ? EditorStyles.selectionRect : EditorStyles.helpBox;
                EditorGUILayout.BeginHorizontal(style);

                EditorGUILayout.LabelField($"{zorlukTag} {soru.kategori}", GUILayout.Width(100));
                if (GUILayout.Button(onIzleme, EditorStyles.linkLabel))
                {
                    seciliIndex = gercekIndex;
                    duzenlenecek = null;
                    yeniSoruModu = false;
                }

                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    if (EditorUtility.DisplayDialog("Soru Sil",
                        $"Bu soruyu silmek istediğinize emin misiniz?\n\n{soru.soruMetni}",
                        "Sil", "İptal"))
                    {
                        db.sorular.RemoveAt(gercekIndex);
                        VeritabaniKaydet();
                        if (seciliIndex == gercekIndex) seciliIndex = -1;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(4);
            EditorGUILayout.LabelField($"Gösterilen: {filtrelenmis.Count} / {toplamSoru}", EditorStyles.centeredGreyMiniLabel);
        }

        // ═══════════════════════════════════════════════════
        //  SAĞ PANEL (Detay / Düzenleme)
        // ═══════════════════════════════════════════════════

        private void SagPanelCiz()
        {
            detayScroll = EditorGUILayout.BeginScrollView(detayScroll);

            if (yeniSoruModu || duzenlenecek != null)
            {
                DuzenlemePaneliCiz();
            }
            else if (seciliIndex >= 0 && seciliIndex < db.sorular.Count)
            {
                DetayPaneliCiz(db.sorular[seciliIndex]);
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Bir soru seçin veya yeni soru ekleyin.",
                    EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DetayPaneliCiz(QuestionData soru)
        {
            EditorGUILayout.LabelField("═══ Soru Detayı ═══", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Zorluk", soru.zorluk.ToString());
            EditorGUILayout.LabelField("Kategori", soru.kategori.ToString());
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Soru:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(soru.soruMetni, EditorStyles.wordWrappedLabel);
            GUILayout.Space(5);

            string[] harfler = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                string prefix = (i == soru.dogruSikIndex) ? "✓ " : "  ";
                var style = (i == soru.dogruSikIndex) ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUILayout.LabelField($"{prefix}{harfler[i]}) {soru.siklar[i]}", style);
            }

            if (soru.AciklamaVar)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Açıklama:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(soru.aciklama, EditorStyles.wordWrappedLabel);
            }

            if (!string.IsNullOrEmpty(soru.animasyonAdi))
                EditorGUILayout.LabelField("Animasyon", soru.animasyonAdi);
            if (!string.IsNullOrEmpty(soru.videoYolu))
                EditorGUILayout.LabelField("Video", soru.videoYolu);

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Düzenle", GUILayout.Height(30)))
            {
                duzenlenecek = soru;
                yeniSoruModu = false;
            }
            if (GUILayout.Button("Sil", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Soru Sil", "Bu soruyu silmek istediğinize emin misiniz?", "Sil", "İptal"))
                {
                    db.sorular.Remove(soru);
                    VeritabaniKaydet();
                    seciliIndex = -1;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════
        //  DÜZENLEME FORMU
        // ═══════════════════════════════════════════════════

        // Form alanları (geçici)
        private string formSoru = "";
        private string[] formSiklar = new string[4] { "", "", "", "" };
        private int formDogruSik = 0;
        private ZorlukSeviyesi formZorluk = ZorlukSeviyesi.Kolay;
        private DersKategorisi formKategori = DersKategorisi.Matematik;
        private string formAciklama = "";
        private string formAnimasyon = "";
        private string formVideo = "";

        private void YeniSoruBaslat()
        {
            yeniSoruModu = true;
            duzenlenecek = null;
            seciliIndex = -1;
            formSoru = "";
            formSiklar = new string[4] { "", "", "", "" };
            formDogruSik = 0;
            formZorluk = ZorlukSeviyesi.Kolay;
            formKategori = DersKategorisi.Matematik;
            formAciklama = "";
            formAnimasyon = "";
            formVideo = "";
        }

        private void DuzenlemePaneliCiz()
        {
            // Eğer düzenleme moduna yeni girdiyse, alanları doldur
            if (duzenlenecek != null && formSoru == "" && !yeniSoruModu)
            {
                formSoru = duzenlenecek.soruMetni;
                formSiklar = (string[])duzenlenecek.siklar.Clone();
                formDogruSik = duzenlenecek.dogruSikIndex;
                formZorluk = duzenlenecek.zorluk;
                formKategori = duzenlenecek.kategori;
                formAciklama = duzenlenecek.aciklama ?? "";
                formAnimasyon = duzenlenecek.animasyonAdi ?? "";
                formVideo = duzenlenecek.videoYolu ?? "";
            }

            string baslik = yeniSoruModu ? "═══ Yeni Soru Ekle ═══" : "═══ Soru Düzenle ═══";
            EditorGUILayout.LabelField(baslik, EditorStyles.boldLabel);
            GUILayout.Space(5);

            // Zorluk ve Kategori
            EditorGUILayout.BeginHorizontal();
            formZorluk = (ZorlukSeviyesi)EditorGUILayout.EnumPopup("Zorluk", formZorluk);
            formKategori = (DersKategorisi)EditorGUILayout.EnumPopup("Kategori", formKategori);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Soru metni
            EditorGUILayout.LabelField("Soru Metni:", EditorStyles.boldLabel);
            formSoru = EditorGUILayout.TextArea(formSoru, GUILayout.MinHeight(50));

            GUILayout.Space(5);

            // Şıklar
            string[] harfler = { "A", "B", "C", "D" };
            EditorGUILayout.LabelField("Şıklar:", EditorStyles.boldLabel);
            for (int i = 0; i < 4; i++)
            {
                EditorGUILayout.BeginHorizontal();
                bool dogruMu = (formDogruSik == i);
                bool yeniDogru = EditorGUILayout.Toggle(dogruMu, GUILayout.Width(20));
                if (yeniDogru && !dogruMu) formDogruSik = i;
                formSiklar[i] = EditorGUILayout.TextField($"{harfler[i]})", formSiklar[i]);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            // Açıklama
            EditorGUILayout.LabelField("Açıklama (Yanlış cevap sonrası gösterilir):", EditorStyles.boldLabel);
            formAciklama = EditorGUILayout.TextArea(formAciklama, GUILayout.MinHeight(40));

            GUILayout.Space(3);

            // Opsiyonel: Animasyon ve Video
            formAnimasyon = EditorGUILayout.TextField("Animasyon Adı (opsiyonel)", formAnimasyon);
            formVideo = EditorGUILayout.TextField("Video Yolu (opsiyonel)", formVideo);

            GUILayout.Space(10);

            // Kaydet / İptal
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !string.IsNullOrWhiteSpace(formSoru) &&
                          formSiklar.All(s => !string.IsNullOrWhiteSpace(s));
            if (GUILayout.Button("Kaydet", GUILayout.Height(30)))
            {
                FormuKaydet();
            }
            GUI.enabled = true;

            if (GUILayout.Button("İptal", GUILayout.Height(30)))
            {
                duzenlenecek = null;
                yeniSoruModu = false;
                formSoru = "";
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrWhiteSpace(formSoru) || formSiklar.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                EditorGUILayout.HelpBox("Soru metni ve tüm şıklar doldurulmalıdır.", MessageType.Warning);
            }
        }

        private void FormuKaydet()
        {
            if (yeniSoruModu)
            {
                var yeniSoru = new QuestionData(formSoru, (string[])formSiklar.Clone(),
                    formDogruSik, formZorluk, formKategori, formAciklama);
                yeniSoru.animasyonAdi = formAnimasyon;
                yeniSoru.videoYolu = formVideo;
                db.sorular.Add(yeniSoru);
                seciliIndex = db.sorular.Count - 1;
            }
            else if (duzenlenecek != null)
            {
                duzenlenecek.soruMetni = formSoru;
                duzenlenecek.siklar = (string[])formSiklar.Clone();
                duzenlenecek.dogruSikIndex = formDogruSik;
                duzenlenecek.zorluk = formZorluk;
                duzenlenecek.kategori = formKategori;
                duzenlenecek.aciklama = formAciklama;
                duzenlenecek.animasyonAdi = formAnimasyon;
                duzenlenecek.videoYolu = formVideo;
            }

            VeritabaniKaydet();
            duzenlenecek = null;
            yeniSoruModu = false;
            formSoru = "";
        }

        // ═══════════════════════════════════════════════════
        //  FİLTRE
        // ═══════════════════════════════════════════════════

        private List<QuestionData> FiltreleSorular()
        {
            var sonuc = db.sorular.AsEnumerable();

            if (filtreZorluk >= 0)
                sonuc = sonuc.Where(s => (int)s.zorluk == filtreZorluk);

            if (filtreKategori >= 0)
                sonuc = sonuc.Where(s => (int)s.kategori == filtreKategori);

            if (!string.IsNullOrWhiteSpace(aramaMetni))
            {
                string kucukArama = aramaMetni.ToLowerInvariant();
                sonuc = sonuc.Where(s =>
                    s.soruMetni.ToLowerInvariant().Contains(kucukArama) ||
                    s.siklar.Any(sik => sik.ToLowerInvariant().Contains(kucukArama)));
            }

            return sonuc.ToList();
        }
    }
}
