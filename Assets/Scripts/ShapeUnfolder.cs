using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 3D şekillerin yüzey açılımını (net/unfold) yapan script.
/// Şekillerin yüzeylerini ayırıp düz bir yüzeye açar.
///
/// Kullanım:
/// 1. Sahneye boş bir GameObject ekleyin (veya var olan bir küpe ekleyin)
/// 2. Bu scripti GameObject'e ekleyin
/// 3. Play modunda Space tuşuna basarak açılımı başlatın
/// 4. Tekrar Space tuşuna basarak katlamayı başlatın
///
/// Inspector'dan renkleri, animasyon hızını ve diğer ayarları değiştirebilirsiniz.
/// </summary>
public class ShapeUnfolder : MonoBehaviour
{
    public enum SekilTipi
    {
        Kup = 0,        // Küp (6 yüzey)
        Piramit = 1,    // Kare tabanlı piramit (5 yüzey)
    }

    // ═══════════════════════════════════════════
    //  INSPECTOR AYARLARI
    // ═══════════════════════════════════════════

    [Header("═══ Şekil Ayarları ═══")]
    [Tooltip("Açılımı yapılacak şekil tipi")]
    public SekilTipi sekilTipi = SekilTipi.Kup;

    [Tooltip("Şeklin boyutu (birim cinsinden)")]
    [Range(0.5f, 5f)]
    public float boyut = 1f;

    [Header("═══ Animasyon Ayarları ═══")]
    [Tooltip("Açılma animasyonunun süresi (saniye)")]
    [Range(0.3f, 5f)]
    public float animasyonSuresi = 1.2f;

    [Tooltip("Yüzey grupları arası bekleme süresi")]
    [Range(0f, 2f)]
    public float grupArasiBekleme = 0.25f;

    [Tooltip("Animasyon eğrisi (yumuşak geçiş)")]
    public AnimationCurve animasyonEgrisi = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("═══ Görünüm Ayarları ═══")]
    [Tooltip("Kenar çizgilerini göster")]
    public bool kenarCizgileriniGoster = true;

    [Tooltip("Kenar çizgisi kalınlığı")]
    [Range(0.005f, 0.05f)]
    public float kenarKalinligi = 0.015f;

    [Tooltip("Kenar çizgisi rengi")]
    public Color kenarRengi = new Color(0.15f, 0.15f, 0.15f);

    [Header("═══ Küp Yüzey Renkleri ═══")]
    public Color altYuzRengi   = new Color(0.93f, 0.93f, 0.95f); // Açık gri
    public Color onYuzRengi    = new Color(0.90f, 0.35f, 0.35f); // Kırmızı
    public Color ustYuzRengi   = new Color(0.30f, 0.60f, 0.90f); // Mavi
    public Color arkaYuzRengi  = new Color(0.35f, 0.80f, 0.45f); // Yeşil
    public Color solYuzRengi   = new Color(0.95f, 0.75f, 0.30f); // Sarı
    public Color sagYuzRengi   = new Color(0.70f, 0.40f, 0.85f); // Mor

    [Header("═══ Piramit Yüzey Renkleri ═══")]
    public Color piramitTabanRengi = new Color(0.93f, 0.93f, 0.95f);
    public Color piramitOnRengi    = new Color(0.90f, 0.35f, 0.35f);
    public Color piramitArkaRengi  = new Color(0.35f, 0.80f, 0.45f);
    public Color piramitSolRengi   = new Color(0.95f, 0.75f, 0.30f);
    public Color piramitSagRengi   = new Color(0.70f, 0.40f, 0.85f);

    // ═══════════════════════════════════════════
    //  PRIVATE DEĞİŞKENLER
    // ═══════════════════════════════════════════

    private struct PivotBilgisi
    {
        public Transform pivot;
        public Vector3 acilimAcisi; // Açılmış durumdaki local Euler açısı
        public int sira;            // Açılma sırası (0 = ilk grup, 1 = ikinci...)
    }

    private List<PivotBilgisi> pivotListesi = new List<PivotBilgisi>();
    private bool acilmisMi = false;
    private bool animasyonOynuyorMu = false;
    private GameObject yuzeylerKoku; // Tüm yüzeylerin bağlı olduğu kök obje

    // ═══════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════

    void Start()
    {
        SekliOlustur();
    }

    void Update()
    {
        if (BosTusunaBasildiMi() && !animasyonOynuyorMu)
        {
            AcilimToggle();
        }
    }

    /// <summary>
    /// Hem eski hem yeni Input System'i destekler.
    /// </summary>
    private bool BosTusunaBasildiMi()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
            return true;
#endif
        return false;
    }

    // ═══════════════════════════════════════════
    //  PUBLIC METODLAR
    // ═══════════════════════════════════════════

    /// <summary>
    /// Açılım durumunu değiştirir (aç ↔ kapat).
    /// UI butonlarından veya kendi input sisteminizden çağırabilirsiniz.
    /// </summary>
    public void AcilimToggle()
    {
        if (animasyonOynuyorMu) return;

        if (acilmisMi)
            StartCoroutine(KatlamaAnimasyonu());
        else
            StartCoroutine(AcilimAnimasyonu());
    }

    /// <summary>
    /// Zorla açılım durumuna getirir (animasyonsuz).
    /// </summary>
    public void HemenAc()
    {
        if (animasyonOynuyorMu) return;
        foreach (var p in pivotListesi)
            p.pivot.localRotation = Quaternion.Euler(p.acilimAcisi);
        acilmisMi = true;
    }

    /// <summary>
    /// Zorla kapalı (küp) durumuna getirir (animasyonsuz).
    /// </summary>
    public void HemenKapat()
    {
        if (animasyonOynuyorMu) return;
        foreach (var p in pivotListesi)
            p.pivot.localRotation = Quaternion.identity;
        acilmisMi = false;
    }

    /// <summary>
    /// Şekli sıfırdan yeniden oluşturur. Boyut veya tip değiştiğinde çağırın.
    /// </summary>
    [ContextMenu("Şekli Yeniden Oluştur")]
    public void SekliOlustur()
    {
        // Mevcut yapıyı temizle
        if (yuzeylerKoku != null)
        {
            if (Application.isPlaying)
                Destroy(yuzeylerKoku);
            else
                DestroyImmediate(yuzeylerKoku);
        }
        pivotListesi.Clear();
        acilmisMi = false;

        // Varsa orijinal rendereri gizle
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Kök obje oluştur
        yuzeylerKoku = new GameObject("AcilimYuzeyleri");
        yuzeylerKoku.transform.SetParent(transform, false);

        switch (sekilTipi)
        {
            case SekilTipi.Kup:
                KupOlustur();
                break;
            case SekilTipi.Piramit:
                PiramitOlustur();
                break;
        }
    }

    // ═══════════════════════════════════════════
    //  KÜP OLUŞTURMA
    // ═══════════════════════════════════════════
    //
    //  Küp açılımı (haç şeklinde net):
    //
    //             [Üst]
    //      [Sol] [Alt/Taban] [Sağ]
    //             [Ön]
    //             [Arka]
    //
    //  Alt yüz sabit kalır (taban).
    //  Diğer yüzeyler ortak kenarları etrafında
    //  menteşe gibi 90° dönerek düzleme açılır.
    //
    //  Hiyerarşi:
    //    AcilimYuzeyleri
    //      ├─ Alt Yüz (sabit taban)
    //      ├─ ÖnPivot → Ön Yüz
    //      │    └─ ÜstPivot → Üst Yüz
    //      ├─ ArkaPivot → Arka Yüz
    //      ├─ SolPivot → Sol Yüz
    //      └─ SağPivot → Sağ Yüz

    private void KupOlustur()
    {
        float h = boyut * 0.5f;

        // ─── ALT YÜZ (Taban - sabit, pivot yok) ────────────
        // Normal: aşağı (-Y)
        YuzeyOlustur("Alt Yüz", yuzeylerKoku.transform,
            new Vector3(-h, -h,  h),
            new Vector3( h, -h,  h),
            new Vector3( h, -h, -h),
            new Vector3(-h, -h, -h),
            Vector3.down, altYuzRengi);

        // ─── ÖN YÜZ ────────────────────────────────────────
        // Ortak kenar: Alt-Ön kenarı → z = -h, y = -h
        // Pivot konumu: (0, -h, -h)
        // Açılım: X ekseni etrafında -90°
        Transform onPivot = PivotOlustur("OnPivot", yuzeylerKoku.transform,
            new Vector3(0, -h, -h));
        YuzeyOlustur("On Yuz", onPivot,
            new Vector3(-h,    0, 0),
            new Vector3( h,    0, 0),
            new Vector3( h, boyut, 0),
            new Vector3(-h, boyut, 0),
            Vector3.back, onYuzRengi);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = onPivot,
            acilimAcisi = new Vector3(-90, 0, 0),
            sira = 0
        });

        // ─── ÜST YÜZ (Ön yüzün çocuğu) ────────────────────
        // Ortak kenar: Ön-Üst kenarı
        // Pivot (Ön pivot'a göre): (0, boyut, 0)
        // Açılım: X ekseni etrafında -90°
        Transform ustPivot = PivotOlustur("UstPivot", onPivot,
            new Vector3(0, boyut, 0));
        YuzeyOlustur("Ust Yuz", ustPivot,
            new Vector3(-h, 0,     0),
            new Vector3( h, 0,     0),
            new Vector3( h, 0, boyut),
            new Vector3(-h, 0, boyut),
            Vector3.up, ustYuzRengi);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = ustPivot,
            acilimAcisi = new Vector3(-90, 0, 0),
            sira = 1 // Ön yüz açıldıktan sonra açılır
        });

        // ─── ARKA YÜZ ──────────────────────────────────────
        // Ortak kenar: Alt-Arka kenarı → z = h, y = -h
        // Pivot konumu: (0, -h, h)
        // Açılım: X ekseni etrafında +90°
        Transform arkaPivot = PivotOlustur("ArkaPivot", yuzeylerKoku.transform,
            new Vector3(0, -h, h));
        YuzeyOlustur("Arka Yuz", arkaPivot,
            new Vector3( h,    0, 0),
            new Vector3(-h,    0, 0),
            new Vector3(-h, boyut, 0),
            new Vector3( h, boyut, 0),
            Vector3.forward, arkaYuzRengi);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = arkaPivot,
            acilimAcisi = new Vector3(90, 0, 0),
            sira = 0
        });

        // ─── SOL YÜZ ───────────────────────────────────────
        // Ortak kenar: Alt-Sol kenarı → x = -h, y = -h
        // Pivot konumu: (-h, -h, 0)
        // Açılım: Z ekseni etrafında +90°
        Transform solPivot = PivotOlustur("SolPivot", yuzeylerKoku.transform,
            new Vector3(-h, -h, 0));
        YuzeyOlustur("Sol Yuz", solPivot,
            new Vector3(0, 0,      h),
            new Vector3(0, 0,     -h),
            new Vector3(0, boyut, -h),
            new Vector3(0, boyut,  h),
            Vector3.left, solYuzRengi);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = solPivot,
            acilimAcisi = new Vector3(0, 0, 90),
            sira = 0
        });

        // ─── SAĞ YÜZ ───────────────────────────────────────
        // Ortak kenar: Alt-Sağ kenarı → x = h, y = -h
        // Pivot konumu: (h, -h, 0)
        // Açılım: Z ekseni etrafında -90°
        Transform sagPivot = PivotOlustur("SagPivot", yuzeylerKoku.transform,
            new Vector3(h, -h, 0));
        YuzeyOlustur("Sag Yuz", sagPivot,
            new Vector3(0, 0,     -h),
            new Vector3(0, 0,      h),
            new Vector3(0, boyut,  h),
            new Vector3(0, boyut, -h),
            Vector3.right, sagYuzRengi);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = sagPivot,
            acilimAcisi = new Vector3(0, 0, -90),
            sira = 0
        });
    }

    // ═══════════════════════════════════════════
    //  PİRAMİT OLUŞTURMA
    // ═══════════════════════════════════════════
    //
    //  Kare tabanlı piramit açılımı:
    //
    //        /\
    //       /  \  (Ön üçgen)
    //      /    \
    //   /\ [Taban] /\
    //  (Sol)      (Sağ)
    //       /\
    //      (Arka)
    //
    //  Taban sabit, 4 üçgen yüzey dışarı açılır.

    private void PiramitOlustur()
    {
        float h = boyut * 0.5f;
        float yukseklik = boyut * 0.85f; // Piramit yüksekliği
        Vector3 tepe = new Vector3(0, yukseklik, 0); // Taban y=0'da

        // Taban köşeleri (y = 0 seviyesinde)
        Vector3 onSol  = new Vector3(-h, 0, -h);
        Vector3 onSag  = new Vector3( h, 0, -h);
        Vector3 arkaSag = new Vector3( h, 0,  h);
        Vector3 arkaSol = new Vector3(-h, 0,  h);

        // ─── TABAN (sabit) ──────────────────────────────────
        YuzeyOlustur("Taban", yuzeylerKoku.transform,
            arkaSol, arkaSag, onSag, onSol,
            Vector3.down, piramitTabanRengi);

        // ─── ÖN ÜÇGEN YÜZ ──────────────────────────────────
        // Ortak kenar: Taban ön kenarı (onSol → onSag), y = 0, z = -h
        Transform onPivot = PivotOlustur("PiramitOnPivot", yuzeylerKoku.transform,
            new Vector3(0, 0, -h));

        // Tepe noktasının pivot'a göre konumu
        Vector3 tepeLokal = tepe - new Vector3(0, 0, -h); // (0, yukseklik, h)
        Vector3 solLokal  = onSol - new Vector3(0, 0, -h); // (-h, 0, 0)
        Vector3 sagLokal  = onSag - new Vector3(0, 0, -h); // (h, 0, 0)

        // Üçgen yüzey normal hesapla
        Vector3 onNormal = Vector3.Cross(sagLokal - solLokal, tepeLokal - solLokal).normalized;

        UcgenYuzeyOlustur("On Ucgen", onPivot,
            solLokal, sagLokal, tepeLokal,
            onNormal, piramitOnRengi);

        // Açılım açısını hesapla: yüzey ile taban arası açı
        float onAcilimAcisi = -AcilimAcisiHesapla(tepeLokal, solLokal, sagLokal);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = onPivot,
            acilimAcisi = new Vector3(onAcilimAcisi, 0, 0),
            sira = 0
        });

        // ─── ARKA ÜÇGEN YÜZ ────────────────────────────────
        Transform arkaPivot = PivotOlustur("PiramitArkaPivot", yuzeylerKoku.transform,
            new Vector3(0, 0, h));

        Vector3 arkaSolL = arkaSol - new Vector3(0, 0, h);  // (-h, 0, 0)
        Vector3 arkaSagL = arkaSag - new Vector3(0, 0, h);  // (h, 0, 0)
        Vector3 tepeArkaL = tepe - new Vector3(0, 0, h);    // (0, yukseklik, -h)

        Vector3 arkaNormal = Vector3.Cross(arkaSolL - arkaSagL, tepeArkaL - arkaSagL).normalized;

        UcgenYuzeyOlustur("Arka Ucgen", arkaPivot,
            arkaSagL, arkaSolL, tepeArkaL,
            arkaNormal, piramitArkaRengi);

        float arkaAcilimAcisi = AcilimAcisiHesapla(tepeArkaL, arkaSagL, arkaSolL);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = arkaPivot,
            acilimAcisi = new Vector3(arkaAcilimAcisi, 0, 0),
            sira = 0
        });

        // ─── SOL ÜÇGEN YÜZ ─────────────────────────────────
        Transform solPivot = PivotOlustur("PiramitSolPivot", yuzeylerKoku.transform,
            new Vector3(-h, 0, 0));

        Vector3 solOnL  = onSol - new Vector3(-h, 0, 0);     // (0, 0, -h)
        Vector3 solArkaL = arkaSol - new Vector3(-h, 0, 0);  // (0, 0, h)
        Vector3 tepeSolL = tepe - new Vector3(-h, 0, 0);     // (h, yukseklik, 0)

        Vector3 solNormal = Vector3.Cross(solArkaL - solOnL, tepeSolL - solOnL).normalized;

        UcgenYuzeyOlustur("Sol Ucgen", solPivot,
            solOnL, solArkaL, tepeSolL,
            solNormal, piramitSolRengi);

        float solAcilimAcisi = AcilimAcisiHesapla(tepeSolL, solOnL, solArkaL);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = solPivot,
            acilimAcisi = new Vector3(0, 0, solAcilimAcisi),
            sira = 0
        });

        // ─── SAĞ ÜÇGEN YÜZ ─────────────────────────────────
        Transform sagPivot = PivotOlustur("PiramitSagPivot", yuzeylerKoku.transform,
            new Vector3(h, 0, 0));

        Vector3 sagOnL  = onSag - new Vector3(h, 0, 0);     // (0, 0, -h)
        Vector3 sagArkaL = arkaSag - new Vector3(h, 0, 0);  // (0, 0, h)
        Vector3 tepeSagL = tepe - new Vector3(h, 0, 0);     // (-h, yukseklik, 0)

        Vector3 sagNormal = Vector3.Cross(sagOnL - sagArkaL, tepeSagL - sagArkaL).normalized;

        UcgenYuzeyOlustur("Sag Ucgen", sagPivot,
            sagArkaL, sagOnL, tepeSagL,
            sagNormal, piramitSagRengi);

        float sagAcilimAcisi = -AcilimAcisiHesapla(tepeSagL, sagArkaL, sagOnL);
        pivotListesi.Add(new PivotBilgisi
        {
            pivot = sagPivot,
            acilimAcisi = new Vector3(0, 0, sagAcilimAcisi),
            sira = 0
        });
    }

    /// <summary>
    /// Üçgen yüzeyin taban ile yaptığı açıyı hesaplar.
    /// Bu açı kadar döndürüldüğünde yüzey düzleme yatar.
    /// </summary>
    private float AcilimAcisiHesapla(Vector3 tepe, Vector3 tabanSol, Vector3 tabanSag)
    {
        // Taban kenarının orta noktası
        Vector3 tabanOrta = (tabanSol + tabanSag) * 0.5f;
        // Tepeden taban ortasına vektör (yüzey üzerinde, tabana dik)
        Vector3 yuzeyYonu = tepe - tabanOrta;
        // Bu vektörün taban düzlemi (y=0) ile yaptığı açı
        float aci = Vector3.Angle(yuzeyYonu, new Vector3(yuzeyYonu.x, 0, yuzeyYonu.z));
        // Toplam açılım: yüzey açısı + 90° (tamamen düzleme yatması için)
        // Aslında yüzey açısını bulmak yeterli: taban düzlemi ile yüzey arası açı
        // Yüzey düzleme yatmak için 90° + taban açısı kadar dönmeli
        float tabanAcisi = Mathf.Atan2(yuzeyYonu.y, new Vector2(yuzeyYonu.x, yuzeyYonu.z).magnitude) * Mathf.Rad2Deg;
        return 90f + tabanAcisi;
    }

    // ═══════════════════════════════════════════
    //  MESH OLUŞTURMA YARDIMCI METODLARI
    // ═══════════════════════════════════════════

    /// <summary>Menteşe (pivot) noktası oluşturur.</summary>
    private Transform PivotOlustur(string isim, Transform parent, Vector3 lokalPozisyon)
    {
        var go = new GameObject(isim);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lokalPozisyon;
        return go.transform;
    }

    /// <summary>
    /// 4 köşeli (quad) çift taraflı yüzey oluşturur.
    /// Köşe sıralaması: v0→v1→v2→v3 saat yönünde (dıştan bakıldığında).
    /// </summary>
    private void YuzeyOlustur(string isim, Transform parent,
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 normal, Color renk)
    {
        var go = new GameObject(isim);
        go.transform.SetParent(parent, false);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = isim + "_Mesh";

        // Çift taraflı mesh (ön ve arka yüz)
        mesh.vertices = new Vector3[]
        {
            v0, v1, v2, v3,     // Ön yüz köşeleri
            v0, v1, v2, v3      // Arka yüz köşeleri
        };

        mesh.normals = new Vector3[]
        {
            normal,  normal,  normal,  normal,   // Ön yüz normali
            -normal, -normal, -normal, -normal   // Arka yüz normali (ters)
        };

        mesh.triangles = new int[]
        {
            0, 1, 2,  0, 2, 3,   // Ön yüz üçgenleri
            4, 6, 5,  4, 7, 6    // Arka yüz üçgenleri (ters sarım)
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(1, 1), new Vector2(0, 1)
        };

        mesh.RecalculateBounds();
        mf.mesh = mesh;

        // Malzeme ayarla
        mr.material = MalzemeOlustur(renk);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        // Kenar çizgileri
        if (kenarCizgileriniGoster)
        {
            KenarCizgisiEkle(go.transform, new Vector3[] { v0, v1, v2, v3 }, normal);
        }
    }

    /// <summary>
    /// 3 köşeli (üçgen) çift taraflı yüzey oluşturur.
    /// </summary>
    private void UcgenYuzeyOlustur(string isim, Transform parent,
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector3 normal, Color renk)
    {
        var go = new GameObject(isim);
        go.transform.SetParent(parent, false);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = isim + "_Mesh";

        mesh.vertices = new Vector3[]
        {
            v0, v1, v2,        // Ön yüz
            v0, v1, v2         // Arka yüz
        };

        mesh.normals = new Vector3[]
        {
            normal,  normal,  normal,
            -normal, -normal, -normal
        };

        mesh.triangles = new int[]
        {
            0, 1, 2,           // Ön yüz
            3, 5, 4            // Arka yüz (ters sarım)
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1)
        };

        mesh.RecalculateBounds();
        mf.mesh = mesh;

        mr.material = MalzemeOlustur(renk);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

        if (kenarCizgileriniGoster)
        {
            KenarCizgisiEkle(go.transform, new Vector3[] { v0, v1, v2 }, normal);
        }
    }

    /// <summary>Yüzey için malzeme oluşturur (URP veya Standard).</summary>
    private Material MalzemeOlustur(Color renk)
    {
        // URP → Standard → Diffuse sırasıyla dene
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material mat = new Material(shader);
        mat.color = renk;
        return mat;
    }

    /// <summary>Yüzey kenarlarına çizgi ekler.</summary>
    private void KenarCizgisiEkle(Transform parent, Vector3[] koseler, Vector3 normal)
    {
        var lineGo = new GameObject("Kenar");
        lineGo.transform.SetParent(parent, false);

        var lr = lineGo.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = koseler.Length;

        // Z-fighting önlemek için çok küçük bir normal yönünde offset
        Vector3 offset = normal * 0.002f;
        Vector3[] pozisyonlar = new Vector3[koseler.Length];
        for (int i = 0; i < koseler.Length; i++)
            pozisyonlar[i] = koseler[i] + offset;

        lr.SetPositions(pozisyonlar);
        lr.startWidth = kenarKalinligi;
        lr.endWidth = kenarKalinligi;
        lr.numCornerVertices = 4;

        // Çizgi malzemesi
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

    // ═══════════════════════════════════════════
    //  ANİMASYON
    // ═══════════════════════════════════════════

    /// <summary>Açılım animasyonunu oynatır.</summary>
    private IEnumerator AcilimAnimasyonu()
    {
        animasyonOynuyorMu = true;

        // Maksimum sıra numarasını bul
        int maxSira = 0;
        foreach (var p in pivotListesi)
            if (p.sira > maxSira) maxSira = p.sira;

        // Her sıra grubunu sırasıyla aç
        for (int sira = 0; sira <= maxSira; sira++)
        {
            // Bu sıradaki pivotları topla
            List<PivotBilgisi> grup = new List<PivotBilgisi>();
            foreach (var p in pivotListesi)
                if (p.sira == sira) grup.Add(p);

            // Grup animasyonu
            float gecenSure = 0f;
            while (gecenSure < animasyonSuresi)
            {
                gecenSure += Time.deltaTime;
                float t = animasyonEgrisi.Evaluate(
                    Mathf.Clamp01(gecenSure / animasyonSuresi));

                foreach (var p in grup)
                {
                    p.pivot.localRotation = Quaternion.Slerp(
                        Quaternion.identity,
                        Quaternion.Euler(p.acilimAcisi),
                        t);
                }
                yield return null;
            }

            // Kesin değere sabitle
            foreach (var p in grup)
                p.pivot.localRotation = Quaternion.Euler(p.acilimAcisi);

            // Sonraki grup için bekle
            if (sira < maxSira && grupArasiBekleme > 0)
                yield return new WaitForSeconds(grupArasiBekleme);
        }

        acilmisMi = true;
        animasyonOynuyorMu = false;
    }

    /// <summary>Katlama animasyonunu oynatır (açılımın tersi).</summary>
    private IEnumerator KatlamaAnimasyonu()
    {
        animasyonOynuyorMu = true;

        int maxSira = 0;
        foreach (var p in pivotListesi)
            if (p.sira > maxSira) maxSira = p.sira;

        // Ters sırayla katla (son açılanlar ilk kapanır)
        for (int sira = maxSira; sira >= 0; sira--)
        {
            List<PivotBilgisi> grup = new List<PivotBilgisi>();
            foreach (var p in pivotListesi)
                if (p.sira == sira) grup.Add(p);

            float gecenSure = 0f;
            while (gecenSure < animasyonSuresi)
            {
                gecenSure += Time.deltaTime;
                float t = animasyonEgrisi.Evaluate(
                    Mathf.Clamp01(gecenSure / animasyonSuresi));

                foreach (var p in grup)
                {
                    p.pivot.localRotation = Quaternion.Slerp(
                        Quaternion.Euler(p.acilimAcisi),
                        Quaternion.identity,
                        t);
                }
                yield return null;
            }

            foreach (var p in grup)
                p.pivot.localRotation = Quaternion.identity;

            if (sira > 0 && grupArasiBekleme > 0)
                yield return new WaitForSeconds(grupArasiBekleme);
        }

        acilmisMi = false;
        animasyonOynuyorMu = false;
    }

    // ═══════════════════════════════════════════
    //  EDITOR GIZMOS
    // ═══════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (sekilTipi == SekilTipi.Kup)
            {
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one * boyut);
            }
            else if (sekilTipi == SekilTipi.Piramit)
            {
                float h = boyut * 0.5f;
                float y = boyut * 0.85f;
                Vector3 tepe = new Vector3(0, y, 0);
                Vector3[] taban = {
                    new Vector3(-h, 0, -h), new Vector3(h, 0, -h),
                    new Vector3(h, 0, h), new Vector3(-h, 0, h)
                };
                for (int i = 0; i < 4; i++)
                {
                    Gizmos.DrawLine(taban[i], taban[(i + 1) % 4]);
                    Gizmos.DrawLine(taban[i], tepe);
                }
            }
        }
    }
}
