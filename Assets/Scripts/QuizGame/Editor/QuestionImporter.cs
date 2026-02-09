using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using QuizGame.Data;
using QuizGame.Managers;

namespace QuizGame.Editor
{
    /// <summary>
    /// CSV / Excel dosyasından soru içe aktarma aracı.
    /// 
    /// ═══════════════════════════════════════════════════════════
    /// CSV FORMAT:
    /// soruMetni;sikA;sikB;sikC;sikD;dogruSik;zorluk;kategori;aciklama
    ///
    /// Örnek satır:
    /// 5 + 3 = ?;6;7;8;9;C;Kolay;Matematik;5 ile 3 toplandığında 8 elde edilir.
    ///
    /// dogruSik : A, B, C veya D
    /// zorluk   : Kolay, Orta, Zor
    /// kategori : Matematik, Turkce, Fen, Sosyal, Ingilizce, GenelKultur
    /// aciklama : (opsiyonel) Yanlış cevap sonrası gösterilecek açıklama
    /// ═══════════════════════════════════════════════════════════
    /// </summary>
    public class QuestionImporter : EditorWindow
    {
        private string csvDosyaYolu = "";
        private string sonucMesaji = "";
        private MessageType sonucTip = MessageType.None;
        private Vector2 scrollPos;

        // Ayırıcı karakter
        private char ayirici = ';';
        private int ayiriciIndex = 0;
        private readonly string[] ayiriciSecenekler = { "Noktalı Virgül (;)", "Virgül (,)", "Tab (\\t)", "Pipe (|)" };
        private readonly char[] ayiriciKarakterler = { ';', ',', '\t', '|' };

        // İstatistikler
        private int sonEklenenSayi;
        private int sonHataSayi;
        private List<string> sonHatalar = new List<string>();

        [MenuItem("QuizGame/Soru İçe Aktar (CSV)", false, 20)]
        public static void ShowWindow()
        {
            var w = GetWindow<QuestionImporter>("Soru İçe Aktar");
            w.minSize = new Vector2(520, 400);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("═══ Soru İçe Aktarma (CSV) ═══", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "CSV Formatı:\n" +
                "soruMetni;şıkA;şıkB;şıkC;şıkD;doğruŞık;zorluk;kategori;açıklama\n\n" +
                "doğruŞık: A, B, C veya D\n" +
                "zorluk: Kolay, Orta, Zor\n" +
                "kategori: Matematik, Turkce, Fen, Sosyal, Ingilizce, GenelKultur\n" +
                "açıklama: (opsiyonel)",
                MessageType.Info);

            GUILayout.Space(10);

            // Ayırıcı seçimi
            ayiriciIndex = EditorGUILayout.Popup("Ayırıcı Karakter", ayiriciIndex, ayiriciSecenekler);
            ayirici = ayiriciKarakterler[ayiriciIndex];

            GUILayout.Space(5);

            // Dosya seçimi
            EditorGUILayout.BeginHorizontal();
            csvDosyaYolu = EditorGUILayout.TextField("CSV Dosyası", csvDosyaYolu);
            if (GUILayout.Button("Gözat...", GUILayout.Width(80)))
            {
                string yol = EditorUtility.OpenFilePanel("CSV Dosyası Seç",
                    Application.dataPath, "csv");
                if (!string.IsNullOrEmpty(yol))
                    csvDosyaYolu = yol;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Butonlar
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !string.IsNullOrEmpty(csvDosyaYolu) && File.Exists(csvDosyaYolu);
            if (GUILayout.Button("İçe Aktar (Mevcut Sorulara Ekle)", GUILayout.Height(35)))
            {
                IceAktar(temizle: false);
            }
            if (GUILayout.Button("İçe Aktar (Tümünü Değiştir)", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Uyarı",
                    "Bu işlem mevcut TÜM soruları silecek ve CSV'deki sorularla değiştirecek.\n\nEmin misiniz?",
                    "Evet, Değiştir", "İptal"))
                {
                    IceAktar(temizle: true);
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Boş şablon oluştur
            if (GUILayout.Button("Boş CSV Şablonu Oluştur", GUILayout.Height(30)))
            {
                SablonOlustur();
            }

            // Mevcut soruları dışa aktar
            if (GUILayout.Button("Mevcut Soruları CSV'ye Aktar", GUILayout.Height(30)))
            {
                DisaAktar();
            }

            GUILayout.Space(10);

            // Sonuç mesajı
            if (!string.IsNullOrEmpty(sonucMesaji))
            {
                EditorGUILayout.HelpBox(sonucMesaji, sonucTip);
            }

            // Hata listesi
            if (sonHatalar.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField("Hatalar:", EditorStyles.boldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(200));
                foreach (var hata in sonHatalar)
                {
                    EditorGUILayout.HelpBox(hata, MessageType.Warning);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        // ═══════════════════════════════════════════════════
        //  İÇE AKTAR
        // ═══════════════════════════════════════════════════

        private void IceAktar(bool temizle)
        {
            sonHatalar.Clear();
            sonEklenenSayi = 0;
            sonHataSayi = 0;

            if (!File.Exists(csvDosyaYolu))
            {
                sonucMesaji = "Dosya bulunamadı!";
                sonucTip = MessageType.Error;
                return;
            }

            string[] satirlar = File.ReadAllLines(csvDosyaYolu, Encoding.UTF8);

            if (satirlar.Length == 0)
            {
                sonucMesaji = "Dosya boş!";
                sonucTip = MessageType.Error;
                return;
            }

            // soru_veritabani.json dosyasını yükle veya oluştur
            string soruDosyaYolu = Path.Combine(Application.persistentDataPath, "soru_veritabani.json");
            QuestionDatabase db;

            if (temizle || !File.Exists(soruDosyaYolu))
            {
                db = new QuestionDatabase();
            }
            else
            {
                string json = File.ReadAllText(soruDosyaYolu);
                db = JsonUtility.FromJson<QuestionDatabase>(json);
                if (db == null) db = new QuestionDatabase();
            }

            // İlk satır başlık mı kontrol et
            int baslangic = 0;
            string ilkSatir = satirlar[0].ToLowerInvariant();
            if (ilkSatir.Contains("soru") || ilkSatir.Contains("question") ||
                ilkSatir.Contains("metni") || ilkSatir.StartsWith("#"))
            {
                baslangic = 1; // Başlık satırını atla
            }

            for (int i = baslangic; i < satirlar.Length; i++)
            {
                string satir = satirlar[i].Trim();
                if (string.IsNullOrWhiteSpace(satir) || satir.StartsWith("#"))
                    continue;

                var soru = SatirParsele(satir, i + 1);
                if (soru != null)
                {
                    db.sorular.Add(soru);
                    sonEklenenSayi++;
                }
                else
                {
                    sonHataSayi++;
                }
            }

            // Kaydet
            string yeniJson = JsonUtility.ToJson(db, true);
            File.WriteAllText(soruDosyaYolu, yeniJson);

            // Runtime DataManager varsa güncelle
            if (Application.isPlaying && DataManager.Instance != null)
            {
                DataManager.Instance.soruVeritabani = db;
            }

            sonucMesaji = $"Tamamlandı! {sonEklenenSayi} soru eklendi. " +
                          (sonHataSayi > 0 ? $"{sonHataSayi} satır hatalı." : "Hata yok.") +
                          $"\nToplam: {db.sorular.Count} soru." +
                          $"\nDosya: {soruDosyaYolu}";
            sonucTip = sonHataSayi > 0 ? MessageType.Warning : MessageType.Info;

            Debug.Log($"[Soru İçe Aktar] {sonEklenenSayi} soru eklendi, {sonHataSayi} hata. Toplam: {db.sorular.Count}");
        }

        private QuestionData SatirParsele(string satir, int satirNo)
        {
            // CSV satırını ayır (tırnak içindeki ayırıcıları koru)
            string[] parcalar = CSVAyir(satir, ayirici);

            if (parcalar.Length < 7)
            {
                sonHatalar.Add($"Satır {satirNo}: En az 7 alan gerekli (soru, 4 şık, doğruŞık, zorluk). " +
                               $"Bulunan: {parcalar.Length}");
                return null;
            }

            string soruMetni = parcalar[0].Trim();
            string sikA = parcalar[1].Trim();
            string sikB = parcalar[2].Trim();
            string sikC = parcalar[3].Trim();
            string sikD = parcalar[4].Trim();
            string dogruSikStr = parcalar[5].Trim().ToUpperInvariant();
            string zorlukStr = parcalar[6].Trim();

            // Kategori (opsiyonel, varsayılan: GenelKultur)
            string kategoriStr = parcalar.Length > 7 ? parcalar[7].Trim() : "GenelKultur";

            // Açıklama (opsiyonel)
            string aciklama = parcalar.Length > 8 ? parcalar[8].Trim() : "";

            // Doğru şık indeksini çözümle
            int dogruSikIndex;
            switch (dogruSikStr)
            {
                case "A": case "0": dogruSikIndex = 0; break;
                case "B": case "1": dogruSikIndex = 1; break;
                case "C": case "2": dogruSikIndex = 2; break;
                case "D": case "3": dogruSikIndex = 3; break;
                default:
                    sonHatalar.Add($"Satır {satirNo}: Geçersiz doğru şık '{dogruSikStr}'. A, B, C veya D olmalı.");
                    return null;
            }

            // Zorluk seviyesini çözümle
            ZorlukSeviyesi zorluk;
            switch (zorlukStr.ToLowerInvariant())
            {
                case "kolay": case "easy": case "0": zorluk = ZorlukSeviyesi.Kolay; break;
                case "orta": case "medium": case "1": zorluk = ZorlukSeviyesi.Orta; break;
                case "zor": case "hard": case "2": zorluk = ZorlukSeviyesi.Zor; break;
                default:
                    sonHatalar.Add($"Satır {satirNo}: Geçersiz zorluk '{zorlukStr}'. Kolay, Orta veya Zor olmalı.");
                    return null;
            }

            // Kategoriyi çözümle
            DersKategorisi kategori;
            switch (kategoriStr.ToLowerInvariant().Replace("ı", "i").Replace("ü", "u"))
            {
                case "matematik": case "math": case "0": kategori = DersKategorisi.Matematik; break;
                case "turkce": case "turkish": case "1": kategori = DersKategorisi.Turkce; break;
                case "fen": case "science": case "2": kategori = DersKategorisi.Fen; break;
                case "sosyal": case "social": case "3": kategori = DersKategorisi.Sosyal; break;
                case "ingilizce": case "english": case "4": kategori = DersKategorisi.Ingilizce; break;
                case "genelkultur": case "genel": case "general": case "5": kategori = DersKategorisi.GenelKultur; break;
                default:
                    sonHatalar.Add($"Satır {satirNo}: Geçersiz kategori '{kategoriStr}'. Varsayılan GenelKultur kullanıldı.");
                    kategori = DersKategorisi.GenelKultur;
                    break;
            }

            // Boş alan kontrolü
            if (string.IsNullOrWhiteSpace(soruMetni))
            {
                sonHatalar.Add($"Satır {satirNo}: Soru metni boş!");
                return null;
            }

            return new QuestionData(soruMetni,
                new string[] { sikA, sikB, sikC, sikD },
                dogruSikIndex, zorluk, kategori, aciklama);
        }

        /// <summary>Tırnak içindeki ayırıcıları koruyarak CSV satırını ayırır.</summary>
        private string[] CSVAyir(string satir, char sep)
        {
            var sonuc = new List<string>();
            bool tirnakIci = false;
            var mevcut = new StringBuilder();

            for (int i = 0; i < satir.Length; i++)
            {
                char c = satir[i];

                if (c == '"')
                {
                    tirnakIci = !tirnakIci;
                    continue;
                }

                if (c == sep && !tirnakIci)
                {
                    sonuc.Add(mevcut.ToString());
                    mevcut.Clear();
                    continue;
                }

                mevcut.Append(c);
            }
            sonuc.Add(mevcut.ToString());
            return sonuc.ToArray();
        }

        // ═══════════════════════════════════════════════════
        //  ŞABLON OLUŞTUR
        // ═══════════════════════════════════════════════════

        private void SablonOlustur()
        {
            string yol = EditorUtility.SaveFilePanel("CSV Şablonu Kaydet",
                Application.dataPath, "sorular_sablonu", "csv");

            if (string.IsNullOrEmpty(yol)) return;

            var sb = new StringBuilder();
            sb.AppendLine($"soruMetni{ayirici}sikA{ayirici}sikB{ayirici}sikC{ayirici}sikD{ayirici}dogruSik{ayirici}zorluk{ayirici}kategori{ayirici}aciklama");
            sb.AppendLine($"5 + 3 = ?{ayirici}6{ayirici}7{ayirici}8{ayirici}9{ayirici}C{ayirici}Kolay{ayirici}Matematik{ayirici}5 ile 3 toplandığında 8 elde edilir.");
            sb.AppendLine($"Suyun kimyasal formülü?{ayirici}CO2{ayirici}H2O{ayirici}O2{ayirici}NaCl{ayirici}B{ayirici}Kolay{ayirici}Fen{ayirici}Su iki hidrojen ve bir oksijen atomundan oluşur: H2O.");
            sb.AppendLine($"'Cat' ne demektir?{ayirici}Köpek{ayirici}Kuş{ayirici}Kedi{ayirici}Balık{ayirici}C{ayirici}Kolay{ayirici}Ingilizce{ayirici}Cat İngilizce'de kedi anlamına gelir.");

            File.WriteAllText(yol, sb.ToString(), Encoding.UTF8);
            sonucMesaji = $"Şablon oluşturuldu: {yol}";
            sonucTip = MessageType.Info;
            Debug.Log($"[Soru İçe Aktar] Şablon oluşturuldu: {yol}");
        }

        // ═══════════════════════════════════════════════════
        //  DIŞA AKTAR
        // ═══════════════════════════════════════════════════

        private void DisaAktar()
        {
            string soruDosyaYolu = Path.Combine(Application.persistentDataPath, "soru_veritabani.json");

            if (!File.Exists(soruDosyaYolu))
            {
                sonucMesaji = "Soru veritabanı dosyası bulunamadı!";
                sonucTip = MessageType.Error;
                return;
            }

            string json = File.ReadAllText(soruDosyaYolu);
            var db = JsonUtility.FromJson<QuestionDatabase>(json);

            if (db == null || db.sorular.Count == 0)
            {
                sonucMesaji = "Veritabanında soru yok!";
                sonucTip = MessageType.Warning;
                return;
            }

            string yol = EditorUtility.SaveFilePanel("Soruları Dışa Aktar",
                Application.dataPath, "sorular_export", "csv");

            if (string.IsNullOrEmpty(yol)) return;

            string[] sikHarfleri = { "A", "B", "C", "D" };

            var sb = new StringBuilder();
            sb.AppendLine($"soruMetni{ayirici}sikA{ayirici}sikB{ayirici}sikC{ayirici}sikD{ayirici}dogruSik{ayirici}zorluk{ayirici}kategori{ayirici}aciklama");

            foreach (var soru in db.sorular)
            {
                string dogruHarf = sikHarfleri[Mathf.Clamp(soru.dogruSikIndex, 0, 3)];
                string aciklama = (soru.aciklama ?? "").Replace(ayirici.ToString(), " ");
                sb.AppendLine(
                    $"{Escape(soru.soruMetni)}{ayirici}" +
                    $"{Escape(soru.siklar[0])}{ayirici}" +
                    $"{Escape(soru.siklar[1])}{ayirici}" +
                    $"{Escape(soru.siklar[2])}{ayirici}" +
                    $"{Escape(soru.siklar[3])}{ayirici}" +
                    $"{dogruHarf}{ayirici}" +
                    $"{soru.zorluk}{ayirici}" +
                    $"{soru.kategori}{ayirici}" +
                    $"{Escape(aciklama)}");
            }

            File.WriteAllText(yol, sb.ToString(), Encoding.UTF8);
            sonucMesaji = $"{db.sorular.Count} soru dışa aktarıldı: {yol}";
            sonucTip = MessageType.Info;
            Debug.Log($"[Soru İçe Aktar] {db.sorular.Count} soru dışa aktarıldı: {yol}");
        }

        private string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains(ayirici) || text.Contains('"') || text.Contains('\n'))
                return $"\"{text.Replace("\"", "\"\"")}\"";
            return text;
        }
    }
}
