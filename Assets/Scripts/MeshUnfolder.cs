using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Herhangi bir 3D mesh'i alıp yüzeylerini otomatik tespit edip
/// düzleme açan (unfold / net) script.
///
/// Küp, piramit, silindir, prizma, ikosahedron vb. HER şekilde çalışır.
/// Mesh'i siz verirsiniz, kod yüzeyleri otomatik bulur ve açar.
///
/// Algoritma:
///   1. Mesh'teki üçgenleri ayrıştır
///   2. Aynı düzlemdeki komşu üçgenleri tek yüzey olarak birleştir
///   3. Yüzeyler arası komşuluk grafiğini çıkar
///   4. BFS ile açılım ağacı (spanning tree) kur
///   5. Her yüzey için menteşe noktası (pivot) ve dihedral açı hesapla
///   6. Animasyonlu açılım/katlama yap
///
/// Kullanım:
///   1. Sahneye bir 3D obje (Cube, Cylinder vs.) ekleyin
///   2. Bu scripti objeye ekleyin
///   3. Inspector'dan "Kaynak Mesh" alanına mesh atayın (boş bırakırsanız
///      objenin kendi MeshFilter'ındaki mesh kullanılır)
///   4. Play modunda Space tuşuyla aç/kapat
/// </summary>
public class MeshUnfolder : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  INSPECTOR AYARLARI
    // ═══════════════════════════════════════════════════════

    [Header("═══ Kaynak ═══")]
    [Tooltip("Açılımı yapılacak mesh. Boş bırakılırsa bu objenin MeshFilter'ı kullanılır.")]
    public Mesh kaynakMesh;

    [Header("═══ Yüzey Birleştirme ═══")]
    [Tooltip("Bu açıdan küçük farka sahip komşu üçgenler tek yüzey olarak birleştirilir.\n" +
             "Küp/Prizma için 1-5°, Silindir için 10-30° önerilir.")]
    [Range(0.1f, 45f)]
    public float birlestirmeAcisi = 5f;

    [Header("═══ Kök (Sabit) Yüzey ═══")]
    [Tooltip("Sabit kalacak kök yüzeyin indeksi.\n-1 = en alttaki yüzey otomatik seçilir.")]
    public int kokYuzeyIndeksi = -1;

    [Header("═══ Animasyon ═══")]
    [Tooltip("Açılma animasyonunun süresi (saniye)")]
    [Range(0.2f, 5f)]
    public float animasyonSuresi = 1.2f;

    [Tooltip("Katman grupları arası bekleme süresi")]
    [Range(0f, 2f)]
    public float katmanArasiBekleme = 0.25f;

    [Tooltip("Animasyon eğrisi")]
    public AnimationCurve animasyonEgrisi = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("═══ Görünüm ═══")]
    [Tooltip("Yüzey kenar çizgilerini göster")]
    public bool kenarCizgileri = true;

    [Tooltip("Kenar çizgisi kalınlığı")]
    [Range(0.002f, 0.04f)]
    public float kenarKalinligi = 0.012f;

    [Tooltip("Kenar çizgisi rengi")]
    public Color kenarRengi = new Color(0.12f, 0.12f, 0.12f);

    [Tooltip("Her yüzeye rastgele renk ata")]
    public bool rastgeleRenkler = true;

    [Tooltip("Rastgele renkler kapalıysa bu renk kullanılır")]
    public Color varsayilanRenk = new Color(0.82f, 0.82f, 0.87f);

    [Tooltip("Rastgele renk tohumu (aynı değer = aynı renkler)")]
    public int renkTohumu = 42;

    // ═══════════════════════════════════════════════════════
    //  DAHİLİ VERİ YAPILARI
    // ═══════════════════════════════════════════════════════

    private const float MERGE_EPSILON = 0.0001f;

    // ── Birleştirilmiş köşe sistemi ──
    private List<Vector3> mergedVerts;       // Birleştirilmiş köşe pozisyonları
    private int[] origToMerged;              // Orijinal köşe indeksi → birleştirilmiş id

    // ── Yüzey verileri ──
    private List<Face> faces;

    // ── Açılım ağacı ──
    private UnfoldNode rootNode;
    private List<List<UnfoldNode>> depthLayers; // Derinliğe göre gruplu düğümler

    // ── Çalışma zamanı ──
    private GameObject unfoldRoot;
    private bool isUnfolded = false;
    private bool isAnimating = false;

    // ── Edge anahtarı (v0 < v1 garantili) ──
    private struct EdgeKey : System.IEquatable<EdgeKey>
    {
        public readonly int v0, v1;
        public EdgeKey(int a, int b)
        {
            v0 = a < b ? a : b;
            v1 = a < b ? b : a;
        }
        public override int GetHashCode() => v0 * 100003 + v1;
        public bool Equals(EdgeKey other) => v0 == other.v0 && v1 == other.v1;
        public override bool Equals(object obj) => obj is EdgeKey e && Equals(e);
    }

    // ── Yüzey bilgisi ──
    private class Face
    {
        public int id;
        public List<int> triangles;          // Her 3'lü = 1 üçgen (birleştirilmiş köşe id'leri)
        public Vector3 normal;
        public Vector3 centroid;
        public HashSet<int> vertexIds;       // Bu yüzeydeki tüm birleştirilmiş köşe id'leri
    }

    // ── Açılım ağacı düğümü ──
    private class UnfoldNode
    {
        public Face face;
        public UnfoldNode parent;
        public int sharedV0, sharedV1;       // Ebeveynle paylaşılan kenar
        public List<UnfoldNode> children = new List<UnfoldNode>();
        public int depth;

        // Çalışma zamanı
        public Transform pivot;
        public Vector3 pivotObjPos;          // Pivot'un obje uzayındaki konumu
        public float unfoldAngle;            // Açılma açısı (derece)
        public Vector3 unfoldAxis;           // Dönme ekseni (pivot lokal uzayında)
    }

    // ═══════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════

    void Start()
    {
        Olustur();
    }

    void Update()
    {
        if (BosTusunaBasildiMi() && !isAnimating)
            AcKapat();
    }

    private bool BosTusunaBasildiMi()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space)) return true;
#endif
        return false;
    }

    // ═══════════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════════

    /// <summary>Açılım durumunu değiştirir (aç ↔ kapat).</summary>
    public void AcKapat()
    {
        if (isAnimating) return;
        StartCoroutine(isUnfolded ? KatlamaAnimasyonu() : AcilimAnimasyonu());
    }

    /// <summary>Anında aç (animasyonsuz).</summary>
    public void HemenAc()
    {
        if (isAnimating || depthLayers == null) return;
        for (int d = 1; d < depthLayers.Count; d++)
            foreach (var node in depthLayers[d])
                node.pivot.localRotation = Quaternion.AngleAxis(node.unfoldAngle, node.unfoldAxis);
        isUnfolded = true;
    }

    /// <summary>Anında kapat (animasyonsuz).</summary>
    public void HemenKapat()
    {
        if (isAnimating || depthLayers == null) return;
        for (int d = 1; d < depthLayers.Count; d++)
            foreach (var node in depthLayers[d])
                node.pivot.localRotation = Quaternion.identity;
        isUnfolded = false;
    }

    /// <summary>Açılımı sıfırdan yeniden oluşturur.</summary>
    [ContextMenu("Yeniden Oluştur")]
    public void Olustur()
    {
        Temizle();

        Mesh mesh = KaynakMeshiAl();
        if (mesh == null)
        {
            Debug.LogError("MeshUnfolder: Mesh bulunamadı! " +
                "Inspector'dan bir mesh atayın veya MeshFilter olan bir objeye ekleyin.");
            return;
        }

        // Orijinal renderer'ı gizle
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Adım adım çalıştır
        KoseleriSikilastir(mesh);
        UcgenleriCikar(mesh);
        YuzeyleriSikilastir();
        AcilimAgaciKur();
        HiyerarsiOlustur();

        Debug.Log($"MeshUnfolder: {faces.Count} yüzey tespit edildi, " +
            $"{(depthLayers != null ? depthLayers.Count : 0)} katman oluşturuldu.");
    }

    // ═══════════════════════════════════════════════════════
    //  ADIM 1: KÖŞE SIKILAŞTIRMA (Vertex Welding)
    // ═══════════════════════════════════════════════════════
    //
    //  Unity mesh'lerinde aynı pozisyondaki köşeler farklı
    //  normal/UV değerleri için çoğaltılır. Biz pozisyona
    //  göre birleştiriyoruz ki kenar paylaşımını doğru bulalım.

    private void KoseleriSikilastir(Mesh mesh)
    {
        Vector3[] verts = mesh.vertices;
        mergedVerts = new List<Vector3>();
        origToMerged = new int[verts.Length];

        Dictionary<long, int> posMap = new Dictionary<long, int>();

        for (int i = 0; i < verts.Length; i++)
        {
            long key = PozisyonuKuantize(verts[i]);
            if (posMap.TryGetValue(key, out int mevcutId))
            {
                origToMerged[i] = mevcutId;
            }
            else
            {
                int yeniId = mergedVerts.Count;
                mergedVerts.Add(verts[i]);
                posMap[key] = yeniId;
                origToMerged[i] = yeniId;
            }
        }
    }

    private long PozisyonuKuantize(Vector3 v)
    {
        // Pozisyonu grid hücrelerine yuvarla
        long x = Mathf.RoundToInt(v.x / MERGE_EPSILON) + 500000L;
        long y = Mathf.RoundToInt(v.y / MERGE_EPSILON) + 500000L;
        long z = Mathf.RoundToInt(v.z / MERGE_EPSILON) + 500000L;
        return (x * 1000000L + y) * 1000000L + z;
    }

    // ═══════════════════════════════════════════════════════
    //  ADIM 2: ÜÇGENLERİ ÇIKAR
    // ═══════════════════════════════════════════════════════
    //
    //  Mesh'teki her üçgeni ayrı bir "yüzey" olarak kaydeder.
    //  Sonraki adımda aynı düzlemdekiler birleştirilir.

    private void UcgenleriCikar(Mesh mesh)
    {
        faces = new List<Face>();
        int[] tris = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            int mv0 = origToMerged[tris[i]];
            int mv1 = origToMerged[tris[i + 1]];
            int mv2 = origToMerged[tris[i + 2]];

            // Dejenere üçgenleri atla
            if (mv0 == mv1 || mv1 == mv2 || mv0 == mv2) continue;

            Vector3 p0 = mergedVerts[mv0];
            Vector3 p1 = mergedVerts[mv1];
            Vector3 p2 = mergedVerts[mv2];

            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            if (normal.sqrMagnitude < 0.0001f) continue;

            faces.Add(new Face
            {
                id = faces.Count,
                triangles = new List<int> { mv0, mv1, mv2 },
                normal = normal,
                centroid = (p0 + p1 + p2) / 3f,
                vertexIds = new HashSet<int> { mv0, mv1, mv2 }
            });
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ADIM 3: AYNI DÜZLEMDEKİ YÜZEYLERİ BİRLEŞTİR
    // ═══════════════════════════════════════════════════════
    //
    //  Ortak kenarı olan ve normalleri neredeyse aynı yöne
    //  bakan (birlestirmeAcisi içinde) üçgenleri Union-Find
    //  ile tek bir mantıksal yüzey olarak birleştirir.
    //
    //  Örnek: Küpün bir yüzü = 2 üçgen → 1 kare yüzey
    //         Silindirin üst kapağı = N üçgen → 1 daire yüzey

    private void YuzeyleriSikilastir()
    {
        if (faces.Count == 0) return;

        float cosEsik = Mathf.Cos(birlestirmeAcisi * Mathf.Deg2Rad);

        // Kenar → yüzey eşleştirmesi
        Dictionary<EdgeKey, List<int>> kenarYuzeyler = new Dictionary<EdgeKey, List<int>>();

        for (int fi = 0; fi < faces.Count; fi++)
        {
            var tris = faces[fi].triangles;
            for (int t = 0; t < tris.Count; t += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    EdgeKey ek = new EdgeKey(tris[t + e], tris[t + (e + 1) % 3]);
                    if (!kenarYuzeyler.ContainsKey(ek))
                        kenarYuzeyler[ek] = new List<int>();
                    if (!kenarYuzeyler[ek].Contains(fi))
                        kenarYuzeyler[ek].Add(fi);
                }
            }
        }

        // Union-Find
        int[] parent = new int[faces.Count];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;

        int Bul(int x)
        {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }
        void Birlestir(int a, int b) { parent[Bul(a)] = Bul(b); }

        // Aynı düzlemdeki komşu üçgenleri birleştir
        foreach (var kvp in kenarYuzeyler)
        {
            var liste = kvp.Value;
            for (int i = 0; i < liste.Count; i++)
            {
                for (int j = i + 1; j < liste.Count; j++)
                {
                    int fi = liste[i], fj = liste[j];
                    if (Bul(fi) == Bul(fj)) continue;

                    // Normal açılarını karşılaştır
                    float dot = Vector3.Dot(faces[fi].normal, faces[fj].normal);
                    if (dot >= cosEsik)
                    {
                        // Aynı düzlemde mi? (Bir yüzeyin noktası diğerinin düzleminde mi)
                        float mesafe = Mathf.Abs(Vector3.Dot(
                            faces[fi].normal,
                            faces[fj].centroid - faces[fi].centroid));
                        if (mesafe < MERGE_EPSILON * 100f)
                        {
                            Birlestir(fi, fj);
                        }
                    }
                }
            }
        }

        // Birleştirilmiş yüzeyleri yeniden oluştur
        Dictionary<int, Face> birlesmisYuzeyler = new Dictionary<int, Face>();

        for (int i = 0; i < faces.Count; i++)
        {
            int kok = Bul(i);
            if (!birlesmisYuzeyler.ContainsKey(kok))
            {
                birlesmisYuzeyler[kok] = new Face
                {
                    id = -1,
                    triangles = new List<int>(),
                    normal = faces[kok].normal,
                    centroid = Vector3.zero,
                    vertexIds = new HashSet<int>()
                };
            }

            var birlesik = birlesmisYuzeyler[kok];
            birlesik.triangles.AddRange(faces[i].triangles);
            foreach (var vid in faces[i].vertexIds)
                birlesik.vertexIds.Add(vid);
        }

        // Listeyi güncelle
        faces = new List<Face>();
        foreach (var kvp in birlesmisYuzeyler)
        {
            var face = kvp.Value;
            face.id = faces.Count;

            // Centroid'i yeniden hesapla
            Vector3 toplam = Vector3.zero;
            foreach (int vid in face.vertexIds)
                toplam += mergedVerts[vid];
            face.centroid = toplam / face.vertexIds.Count;

            // Normal'i yeniden hesapla (ilk üçgenden)
            if (face.triangles.Count >= 3)
            {
                Vector3 p0 = mergedVerts[face.triangles[0]];
                Vector3 p1 = mergedVerts[face.triangles[1]];
                Vector3 p2 = mergedVerts[face.triangles[2]];
                Vector3 n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
                if (n.sqrMagnitude > 0.001f)
                    face.normal = n;
            }

            faces.Add(face);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ADIM 4: AÇILIM AĞACI KUR (BFS Spanning Tree)
    // ═══════════════════════════════════════════════════════
    //
    //  Yüzeyler arası komşuluk grafiğinden bir BFS ağacı kurar.
    //  Kök yüzey sabit kalır, diğer yüzeyler kökten uzaklaşarak
    //  katman katman açılır.
    //
    //  Hiyerarşi örneği (küp):
    //    Alt yüz (kök, depth=0)
    //    ├─ Ön yüz (depth=1)
    //    │  └─ Üst yüz (depth=2)
    //    ├─ Arka yüz (depth=1)
    //    ├─ Sol yüz (depth=1)
    //    └─ Sağ yüz (depth=1)

    private void AcilimAgaciKur()
    {
        if (faces.Count == 0) return;

        // ── Kenar-yüzey eşleştirmesi (birleştirilmiş yüzeyler için) ──
        Dictionary<EdgeKey, List<int>> kenarYuzeyler = new Dictionary<EdgeKey, List<int>>();

        for (int fi = 0; fi < faces.Count; fi++)
        {
            var tris = faces[fi].triangles;
            for (int t = 0; t < tris.Count; t += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    EdgeKey ek = new EdgeKey(tris[t + e], tris[t + (e + 1) % 3]);
                    if (!kenarYuzeyler.ContainsKey(ek))
                        kenarYuzeyler[ek] = new List<int>();
                    if (!kenarYuzeyler[ek].Contains(fi))
                        kenarYuzeyler[ek].Add(fi);
                }
            }
        }

        // ── Yüzey komşuluk grafiği ──
        // Anahtar: yüzey id → (komşu id, paylaşılan kenar) listesi
        Dictionary<int, List<(int komsu, EdgeKey kenar)>> komsuluk =
            new Dictionary<int, List<(int, EdgeKey)>>();

        for (int i = 0; i < faces.Count; i++)
            komsuluk[i] = new List<(int, EdgeKey)>();

        foreach (var kvp in kenarYuzeyler)
        {
            var liste = kvp.Value;
            // Bu kenar tam olarak 2 farklı yüzeye aitse → komşuluk
            if (liste.Count == 2 && liste[0] != liste[1])
            {
                int a = liste[0], b = liste[1];

                // Aynı komşuluk zaten var mı kontrol et
                // (birden fazla kenar paylaşılabilir, sadece en uzununu tut)
                bool mevcutMu = false;
                for (int k = 0; k < komsuluk[a].Count; k++)
                {
                    if (komsuluk[a][k].komsu == b)
                    {
                        mevcutMu = true;
                        // Daha uzun kenarı tercih et
                        var mevcutKenar = komsuluk[a][k].kenar;
                        float mevcutUzunluk = (mergedVerts[mevcutKenar.v1] - mergedVerts[mevcutKenar.v0]).sqrMagnitude;
                        float yeniUzunluk = (mergedVerts[kvp.Key.v1] - mergedVerts[kvp.Key.v0]).sqrMagnitude;
                        if (yeniUzunluk > mevcutUzunluk)
                        {
                            komsuluk[a][k] = (b, kvp.Key);
                            // b tarafını da güncelle
                            for (int m = 0; m < komsuluk[b].Count; m++)
                                if (komsuluk[b][m].komsu == a)
                                { komsuluk[b][m] = (a, kvp.Key); break; }
                        }
                        break;
                    }
                }

                if (!mevcutMu)
                {
                    komsuluk[a].Add((b, kvp.Key));
                    komsuluk[b].Add((a, kvp.Key));
                }
            }
        }

        // ── Kök yüzeyi seç ──
        int kokId;
        if (kokYuzeyIndeksi >= 0 && kokYuzeyIndeksi < faces.Count)
        {
            kokId = kokYuzeyIndeksi;
        }
        else
        {
            // En alttaki yüzeyi bul (en düşük centroid Y)
            kokId = 0;
            float minY = faces[0].centroid.y;
            for (int i = 1; i < faces.Count; i++)
            {
                if (faces[i].centroid.y < minY)
                {
                    minY = faces[i].centroid.y;
                    kokId = i;
                }
            }
        }

        // ── BFS ile ağaç kur ──
        UnfoldNode[] dugumler = new UnfoldNode[faces.Count];
        for (int i = 0; i < faces.Count; i++)
            dugumler[i] = new UnfoldNode { face = faces[i], depth = -1 };

        rootNode = dugumler[kokId];
        rootNode.depth = 0;

        Queue<int> kuyruk = new Queue<int>();
        kuyruk.Enqueue(kokId);
        HashSet<int> ziyaretEdilen = new HashSet<int> { kokId };

        depthLayers = new List<List<UnfoldNode>>();
        depthLayers.Add(new List<UnfoldNode> { rootNode });

        while (kuyruk.Count > 0)
        {
            int mevcutId = kuyruk.Dequeue();

            foreach (var (komsuId, kenar) in komsuluk[mevcutId])
            {
                if (ziyaretEdilen.Contains(komsuId)) continue;
                ziyaretEdilen.Add(komsuId);

                var cocukDugum = dugumler[komsuId];
                cocukDugum.parent = dugumler[mevcutId];
                cocukDugum.sharedV0 = kenar.v0;
                cocukDugum.sharedV1 = kenar.v1;
                cocukDugum.depth = dugumler[mevcutId].depth + 1;
                dugumler[mevcutId].children.Add(cocukDugum);

                while (depthLayers.Count <= cocukDugum.depth)
                    depthLayers.Add(new List<UnfoldNode>());
                depthLayers[cocukDugum.depth].Add(cocukDugum);

                kuyruk.Enqueue(komsuId);
            }
        }

        // Ulaşılamayan yüzeyler varsa uyar
        if (ziyaretEdilen.Count < faces.Count)
        {
            Debug.LogWarning($"MeshUnfolder: {faces.Count - ziyaretEdilen.Count} yüzeye " +
                "ulaşılamadı (bağlantısız mesh parçaları olabilir).");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  ADIM 5: GAMEOBJECT HİYERARŞİSİ OLUŞTUR
    // ═══════════════════════════════════════════════════════
    //
    //  Her yüzey için:
    //  - Pivot obje: Paylaşılan kenarda, dönme menteşesi
    //  - Yüzey mesh: Pivot'un çocuğu, çift taraflı
    //
    //  Pivot'lar ebeveyn-çocuk zinciri şeklinde bağlanır
    //  böylece ebeveyn açılınca çocukları da taşır.

    private void HiyerarsiOlustur()
    {
        if (rootNode == null) return;

        unfoldRoot = new GameObject("MeshAcilim");
        unfoldRoot.transform.SetParent(transform, false);

        System.Random rng = new System.Random(renkTohumu);

        // Kök yüzey (sabit, pivot yok)
        YuzeyMeshOlustur(rootNode.face, unfoldRoot.transform,
            Vector3.zero, YuzeyRengiAl(rootNode.face.id, rng));

        // Alt düğümleri özyinelemeli oluştur
        CocukDugumleriOlustur(rootNode, unfoldRoot.transform, Vector3.zero, rng);
    }

    private void CocukDugumleriOlustur(UnfoldNode ebeveyn, Transform ebeveynTransform,
        Vector3 ebeveynPivotObjPos, System.Random rng)
    {
        foreach (var cocuk in ebeveyn.children)
        {
            // Paylaşılan kenar bilgileri
            Vector3 e0 = mergedVerts[cocuk.sharedV0];
            Vector3 e1 = mergedVerts[cocuk.sharedV1];
            Vector3 kenarOrta = (e0 + e1) * 0.5f;

            // ── Pivot oluştur ──
            // Pivot konumu paylaşılan kenarın başlangıç noktası (e0)
            cocuk.pivotObjPos = e0;

            GameObject pivotGo = new GameObject($"Pivot_Yuzey{cocuk.face.id}");
            pivotGo.transform.SetParent(ebeveynTransform, false);
            pivotGo.transform.localPosition = e0 - ebeveynPivotObjPos;
            cocuk.pivot = pivotGo.transform;

            // ── Dönme ekseni ve açısı hesapla ──
            Vector3 kenarYonu = (e1 - e0).normalized;
            cocuk.unfoldAxis = kenarYonu;

            // Ebeveyn ve çocuk ağırlık merkezlerinin kenara dik projeksiyonları
            Vector3 ebeveynYonu = ebeveyn.face.centroid - kenarOrta;
            ebeveynYonu -= Vector3.Dot(ebeveynYonu, kenarYonu) * kenarYonu;

            Vector3 cocukYonu = cocuk.face.centroid - kenarOrta;
            cocukYonu -= Vector3.Dot(cocukYonu, kenarYonu) * kenarYonu;

            // Dejenere durumları kontrol et
            if (ebeveynYonu.sqrMagnitude < 0.00001f || cocukYonu.sqrMagnitude < 0.00001f)
            {
                cocuk.unfoldAngle = 0f;
            }
            else
            {
                ebeveynYonu.Normalize();
                cocukYonu.Normalize();

                // Kenar etrafındaki işaretli açı
                float mevcutAci = Vector3.SignedAngle(ebeveynYonu, cocukYonu, kenarYonu);

                // Açılım açısı: 180° - |mevcut açı| yönünde döndür
                // Hedef: Çocuk yüzey, ebeveynin tam karşı tarafına gelsin (180°)
                cocuk.unfoldAngle = (180f - Mathf.Abs(mevcutAci)) * Mathf.Sign(mevcutAci);
            }

            // ── Yüzey mesh oluştur ──
            Color renk = YuzeyRengiAl(cocuk.face.id, rng);
            YuzeyMeshOlustur(cocuk.face, pivotGo.transform, cocuk.pivotObjPos, renk);

            // ── Alt çocukları oluştur ──
            CocukDugumleriOlustur(cocuk, pivotGo.transform, cocuk.pivotObjPos, rng);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  MESH OLUŞTURMA YARDIMCILARI
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Bir yüzey için çift taraflı mesh oluşturur.
    /// Köşeler pivot'un lokal uzayına taşınır.
    /// </summary>
    private void YuzeyMeshOlustur(Face face, Transform parent,
        Vector3 pivotObjPos, Color renk)
    {
        var go = new GameObject($"Yuzey_{face.id}");
        go.transform.SetParent(parent, false);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        // Birleştirilmiş köşe id → lokal mesh köşe id eşleştirmesi
        Dictionary<int, int> mergedToLokal = new Dictionary<int, int>();
        List<Vector3> lokalKoseler = new List<Vector3>();
        List<int> lokalUcgenler = new List<int>();

        for (int t = 0; t < face.triangles.Count; t += 3)
        {
            for (int i = 0; i < 3; i++)
            {
                int mv = face.triangles[t + i];
                if (!mergedToLokal.ContainsKey(mv))
                {
                    mergedToLokal[mv] = lokalKoseler.Count;
                    lokalKoseler.Add(mergedVerts[mv] - pivotObjPos);
                }
            }

            // Ön yüz üçgeni
            lokalUcgenler.Add(mergedToLokal[face.triangles[t]]);
            lokalUcgenler.Add(mergedToLokal[face.triangles[t + 1]]);
            lokalUcgenler.Add(mergedToLokal[face.triangles[t + 2]]);
        }

        // Arka yüz (ters sarımlı çiftler)
        int koseSayisi = lokalKoseler.Count;
        for (int i = 0; i < koseSayisi; i++)
            lokalKoseler.Add(lokalKoseler[i]); // Köşeleri çoğalt

        for (int t = 0; t < face.triangles.Count; t += 3)
        {
            // Ters sarım (arka yüz)
            lokalUcgenler.Add(mergedToLokal[face.triangles[t + 2]] + koseSayisi);
            lokalUcgenler.Add(mergedToLokal[face.triangles[t + 1]] + koseSayisi);
            lokalUcgenler.Add(mergedToLokal[face.triangles[t]] + koseSayisi);
        }

        // Mesh oluştur
        Mesh mesh = new Mesh();
        mesh.name = $"Yuzey_{face.id}_Mesh";
        mesh.SetVertices(lokalKoseler);
        mesh.SetTriangles(lokalUcgenler, 0);

        // Normal vektörleri
        Vector3[] normaller = new Vector3[lokalKoseler.Count];
        for (int i = 0; i < koseSayisi; i++) normaller[i] = face.normal;
        for (int i = koseSayisi; i < normaller.Length; i++) normaller[i] = -face.normal;
        mesh.normals = normaller;

        mesh.RecalculateBounds();
        mf.mesh = mesh;

        // Malzeme
        mr.material = MalzemeOlustur(renk);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        // Kenar çizgileri
        if (kenarCizgileri)
            KenarCizgisiEkle(go.transform, face, pivotObjPos);
    }

    /// <summary>URP veya Standard shader ile malzeme oluşturur.</summary>
    private Material MalzemeOlustur(Color renk)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material mat = new Material(shader);
        mat.color = renk;
        return mat;
    }

    /// <summary>
    /// Yüzeyin sınır kenarlarına (boundary edges) çizgi ekler.
    /// İç kenarlar (aynı yüzeydeki üçgenler arası) çizilmez.
    /// </summary>
    private void KenarCizgisiEkle(Transform parent, Face face, Vector3 pivotObjPos)
    {
        // Her kenarın kaç üçgende göründüğünü say
        Dictionary<EdgeKey, int> kenarSayisi = new Dictionary<EdgeKey, int>();

        for (int t = 0; t < face.triangles.Count; t += 3)
        {
            for (int e = 0; e < 3; e++)
            {
                EdgeKey ek = new EdgeKey(face.triangles[t + e], face.triangles[t + (e + 1) % 3]);
                if (!kenarSayisi.ContainsKey(ek)) kenarSayisi[ek] = 0;
                kenarSayisi[ek]++;
            }
        }

        // Sınır kenarları: sadece 1 üçgende görünen kenarlar
        // Bunları birleşik bir LineRenderer ile çiz
        List<Vector3> sinirNoktalar = new List<Vector3>();
        List<(int v0, int v1)> sinirKenarlar = new List<(int, int)>();

        foreach (var kvp in kenarSayisi)
        {
            if (kvp.Value == 1) // Sınır kenarı
            {
                sinirKenarlar.Add((kvp.Key.v0, kvp.Key.v1));
            }
        }

        if (sinirKenarlar.Count == 0) return;

        // Sınır kenarlarını sıralı bir döngüye diz
        List<int> siraliKoseler = SinirKenarlariSirala(sinirKenarlar);

        if (siraliKoseler.Count > 0)
        {
            // Tek bir LineRenderer ile çiz
            var lineGo = new GameObject("Kenar");
            lineGo.transform.SetParent(parent, false);

            var lr = lineGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = siraliKoseler.Count;
            lr.numCornerVertices = 3;

            Vector3 offset = face.normal * 0.001f;
            for (int i = 0; i < siraliKoseler.Count; i++)
            {
                lr.SetPosition(i, mergedVerts[siraliKoseler[i]] - pivotObjPos + offset);
            }

            lr.startWidth = kenarKalinligi;
            lr.endWidth = kenarKalinligi;

            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("Unlit/Color");

            Material lineMat = new Material(lineShader);
            lineMat.color = kenarRengi;
            lr.material = lineMat;
            lr.startColor = kenarRengi;
            lr.endColor = kenarRengi;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }
        else
        {
            // Sıralama başarısız → her kenarı ayrı çiz
            foreach (var (v0, v1) in sinirKenarlar)
            {
                var lineGo = new GameObject("Kenar");
                lineGo.transform.SetParent(parent, false);

                var lr = lineGo.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.positionCount = 2;

                Vector3 offset = face.normal * 0.001f;
                lr.SetPosition(0, mergedVerts[v0] - pivotObjPos + offset);
                lr.SetPosition(1, mergedVerts[v1] - pivotObjPos + offset);

                lr.startWidth = kenarKalinligi;
                lr.endWidth = kenarKalinligi;

                Shader lineShader = Shader.Find("Sprites/Default");
                if (lineShader == null) lineShader = Shader.Find("Unlit/Color");

                Material lineMat = new Material(lineShader);
                lineMat.color = kenarRengi;
                lr.material = lineMat;
                lr.startColor = kenarRengi;
                lr.endColor = kenarRengi;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
            }
        }
    }

    /// <summary>
    /// Sınır kenarlarını sıralı bir köşe döngüsüne dizer.
    /// Basit çokgenler için çalışır (delikli yüzeyler hariç).
    /// </summary>
    private List<int> SinirKenarlariSirala(List<(int v0, int v1)> kenarlar)
    {
        if (kenarlar.Count == 0) return new List<int>();

        // Köşe → komşu köşeler eşleştirmesi
        Dictionary<int, List<int>> komsuluk = new Dictionary<int, List<int>>();
        foreach (var (v0, v1) in kenarlar)
        {
            if (!komsuluk.ContainsKey(v0)) komsuluk[v0] = new List<int>();
            if (!komsuluk.ContainsKey(v1)) komsuluk[v1] = new List<int>();
            komsuluk[v0].Add(v1);
            komsuluk[v1].Add(v0);
        }

        // İlk köşeden başla ve döngüyü takip et
        List<int> sonuc = new List<int>();
        HashSet<int> ziyaretEdilen = new HashSet<int>();

        int mevcut = kenarlar[0].v0;
        sonuc.Add(mevcut);
        ziyaretEdilen.Add(mevcut);

        for (int i = 0; i < kenarlar.Count; i++) // en fazla kenar sayısı kadar döngü
        {
            bool bulundu = false;
            if (komsuluk.ContainsKey(mevcut))
            {
                foreach (int komsu in komsuluk[mevcut])
                {
                    if (!ziyaretEdilen.Contains(komsu))
                    {
                        sonuc.Add(komsu);
                        ziyaretEdilen.Add(komsu);
                        mevcut = komsu;
                        bulundu = true;
                        break;
                    }
                }
            }
            if (!bulundu) break;
        }

        return sonuc;
    }

    /// <summary>Yüzey rengi döndürür.</summary>
    private Color YuzeyRengiAl(int faceId, System.Random rng)
    {
        if (rastgeleRenkler)
        {
            float h = (float)rng.NextDouble();
            float s = 0.25f + (float)rng.NextDouble() * 0.45f;
            float v = 0.70f + (float)rng.NextDouble() * 0.28f;
            return Color.HSVToRGB(h, s, v);
        }
        return varsayilanRenk;
    }

    // ═══════════════════════════════════════════════════════
    //  ANİMASYON
    // ═══════════════════════════════════════════════════════

    /// <summary>Katman katman açılım animasyonu.</summary>
    private IEnumerator AcilimAnimasyonu()
    {
        isAnimating = true;

        for (int d = 1; d < depthLayers.Count; d++)
        {
            var katman = depthLayers[d];
            float gecen = 0f;

            while (gecen < animasyonSuresi)
            {
                gecen += Time.deltaTime;
                float t = animasyonEgrisi.Evaluate(Mathf.Clamp01(gecen / animasyonSuresi));

                foreach (var dugum in katman)
                {
                    dugum.pivot.localRotation = Quaternion.AngleAxis(
                        dugum.unfoldAngle * t, dugum.unfoldAxis);
                }
                yield return null;
            }

            // Kesin değere sabitle
            foreach (var dugum in katman)
                dugum.pivot.localRotation = Quaternion.AngleAxis(
                    dugum.unfoldAngle, dugum.unfoldAxis);

            if (d < depthLayers.Count - 1 && katmanArasiBekleme > 0)
                yield return new WaitForSeconds(katmanArasiBekleme);
        }

        isUnfolded = true;
        isAnimating = false;
    }

    /// <summary>Ters sırada katlama animasyonu.</summary>
    private IEnumerator KatlamaAnimasyonu()
    {
        isAnimating = true;

        for (int d = depthLayers.Count - 1; d >= 1; d--)
        {
            var katman = depthLayers[d];
            float gecen = 0f;

            while (gecen < animasyonSuresi)
            {
                gecen += Time.deltaTime;
                float t = animasyonEgrisi.Evaluate(Mathf.Clamp01(gecen / animasyonSuresi));

                foreach (var dugum in katman)
                {
                    dugum.pivot.localRotation = Quaternion.AngleAxis(
                        dugum.unfoldAngle * (1f - t), dugum.unfoldAxis);
                }
                yield return null;
            }

            foreach (var dugum in katman)
                dugum.pivot.localRotation = Quaternion.identity;

            if (d > 1 && katmanArasiBekleme > 0)
                yield return new WaitForSeconds(katmanArasiBekleme);
        }

        isUnfolded = false;
        isAnimating = false;
    }

    // ═══════════════════════════════════════════════════════
    //  YARDIMCI
    // ═══════════════════════════════════════════════════════

    private Mesh KaynakMeshiAl()
    {
        if (kaynakMesh != null) return kaynakMesh;
        var mf = GetComponent<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
    }

    private void Temizle()
    {
        if (unfoldRoot != null)
        {
            if (Application.isPlaying) Destroy(unfoldRoot);
            else DestroyImmediate(unfoldRoot);
        }
        mergedVerts = null;
        origToMerged = null;
        faces = null;
        rootNode = null;
        depthLayers = null;
        isUnfolded = false;
        isAnimating = false;
    }

    // ═══════════════════════════════════════════════════════
    //  EDITOR GIZMOS
    // ═══════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Mesh mesh = KaynakMeshiAl();
            if (mesh != null)
            {
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(mesh.bounds.center, mesh.bounds.size);
            }
        }
    }
}
