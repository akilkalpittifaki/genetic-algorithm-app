
using genetik_sev;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace genetik_sev
{
    // TEMEL SINIFLAR VE ARAYÜZLER

    // KROMOZOM SINIFI
    // Genetik algoritmanın temeli, biyolojideki kromozomlara benzer şekilde çalışan veri yapılarıdır.
    // Bu sınıf, Problem 7'nin çözümü için bir aday temsil eder. Her kromozom, iki gerçek sayıdan (x ve y koordinatları) oluşan
    // bir diziyi içerir. Gerçek hayattaki genetik algoritmalarda, kromozomlar genellikle binary (0-1) dizileri olarak temsil edilir,
    // ancak bizim problemimiz sürekli değişkenler içerdiği için doğrudan reel sayıları kullanmak daha uygundur.
    // UygunlukDegeri özelliği, bu çözüm adayının ne kadar iyi olduğunu gösterir - daha düşük değerler daha iyi çözümleri temsil eder
    // (minimizasyon problemi).
    public class Kromozom
    {
        public double[] Genler { get; set; }  // Parametrelerin reel değerlerini tutan dizi
        public double UygunlukDegeri { get; set; }  // Uygunluk değeri (düşük değer daha iyi)

        // Rastgele başlatma metodu, algoritmanın başlangıcında çeşitli çözüm adayları oluşturmak için kritik öneme sahiptir.
        // Evrimsel süreci başlatmak için yeterince çeşitlilik sağlar.
        // Burada kromozomun her genine, problemin tanım aralığı içinde rastgele bir değer atanıyor.
        // Bu rastgelelik, algoritmanın çözüm uzayını geniş bir şekilde araştırmasını sağlar ve
        // global optimuma ulaşma şansını artırır. global optimum, problem uzayındaki tüm olası çözümler arasında en iyi sonucu veren (en düşük ya da en yüksek 

        public Kromozom(int boyut, double[] altSinirlar, double[] ustSinirlar, Random random)
        {
            Genler = new double[boyut];

            for (int i = 0; i < boyut; i++)
            {
                Genler[i] = altSinirlar[i] + (ustSinirlar[i] - altSinirlar[i]) * random.NextDouble();
            }

            UygunlukDegeri = double.MaxValue; // Minimizasyon problemi için başlangıç değeri
        }

        // Bu kopya konstruktörü, özellikle seçkinlik (elitism) mekanizması ve yeni nesillere çözüm aktarma sürecinde önemlidir.
        // Derin kopya oluşturarak, orijinal kromozomların değişmeden korunmasını sağlar.
        // Bu, özellikle en iyi çözümleri korumak ve algoritmanın bir önceki jenerasyondaki kazanımları kaybetmemesini sağlamak için kritiktir.
        public Kromozom(Kromozom diger)
        {
            Genler = new double[diger.Genler.Length];
            Array.Copy(diger.Genler, Genler, diger.Genler.Length);
            UygunlukDegeri = diger.UygunlukDegeri;
        }
    }

    /// Test problemleri için arayüz
    public interface ITestProblemi
    {
        int Boyut { get; }  // Problem boyutu (parametre/değişken sayısı)
        double[] AltSinirlar { get; }  // Her parametrenin alt sınırı
        double[] UstSinirlar { get; }  // Her parametrenin üst sınırı
        double Degerlendir(double[] cozum);  // Çözümü değerlendirip uygunluk değerini döndürür
        string GetProblemAdi();  // Problemin adını veya formülünü döndürür
    }

    /// Populasyon sınıfı - kromozomların bir koleksiyonunu yönetir


    // POPULASYON SINIFI
    // Popülasyon, genetik algoritmanın bir nesilindeki tüm çözüm adaylarını (kromozomları) bir arada tutar.
    // Doğadaki bir türün popülasyonuna benzer şekilde, çeşitli bireyleri ve onların evrimsel süreçlerini yönetir.
    // Bu sınıf, kromozomların kolektif davranışını ve nesiller arası geçişleri düzenler.
    // EnIyiKromozom özelliği, o ana kadar bulunan en iyi çözümü saklar - bu, algoritmanın "hafızası" olarak düşünülebilir.
    public class Populasyon
    {
        public List<Kromozom> Kromozomlar { get; private set; }
        public Kromozom EnIyiKromozom { get; private set; }

        // Popülasyon oluşturma, genetik algoritmanın ilk aşamasıdır.
        // Bu metot, belirtilen sayıda rastgele kromozom yaratarak çözüm uzayında geniş bir başlangıç dağılımı sağlar.
        // Her kromozomun rastgele başlatılması, algoritmanın çeşitli başlangıç noktalarından çözüm aramasına olanak verir -
        // bu yaklaşım, yerel minimumlardan kaçmak için önemlidir. en iyi çözüm zannetmesi.
        public Populasyon(int boyut, int kromozomBoyutu, double[] altSinirlar, double[] ustSinirlar, Random random)
        {
            Kromozomlar = new List<Kromozom>();

            for (int i = 0; i < boyut; i++)
            {
                Kromozomlar.Add(new Kromozom(kromozomBoyutu, altSinirlar, ustSinirlar, random));
            }
        }

        // Uygunluk değerlerinin hesaplanması, genetik algoritmanın "doğal seçilim" sürecini modellemek için kritik öneme sahiptir.
        // Bu metot, her kromozomun çözüm kalitesini test problemi üzerinden değerlendirir. Ayrıca, global en iyi çözümü de takip eder -
        // bu, algoritmanın nesiller boyunca en iyi çözümü "hatırlamasını" sağlar. Minimizasyon problemlerinde,
        // en düşük uygunluk değerine sahip kromozom en iyi çözümdür.
        public void UygunlukDegerleriniHesapla(ITestProblemi problem)
        {
            foreach (var kromozom in Kromozomlar)
            {
                kromozom.UygunlukDegeri = problem.Degerlendir(kromozom.Genler);
            }

            // En iyi kromozomu bulma ve global en iyi değeri güncelleme
            var enIyi = Kromozomlar.OrderBy(k => k.UygunlukDegeri).First();

            if (EnIyiKromozom == null || enIyi.UygunlukDegeri < EnIyiKromozom.UygunlukDegeri)
            {
                EnIyiKromozom = new Kromozom(enIyi);
            }
        }
    }


    // GENETIK ALGORITMA SINIFI
    // Bu sınıf, genetik algoritmanın tüm mekanizmalarını bir araya getirir ve çalıştırır.
    // Evrimsel süreci modelleyerek, jenerasyonlar boyunca popülasyonu geliştirir.
    // Algoritma, doğadaki evrimsel süreçleri taklit eder: seçilim (daha uygun bireylerin üreme şansı daha yüksektir),
    // çaprazlama (ebeveynlerden genler alınarak yeni bireyler oluşturulur) ve mutasyon (rastgele genetik değişimler) mekanizmalarını kullanır.
    public class GenetikAlgoritma
    {
        private ITestProblemi _problem;  // Çözülecek test problemi
        private Populasyon _populasyon;  // Mevcut popülasyon
        private Random _random;          // Rastgele sayı üreteci

        // Algoritma parametreleri
        public int PopulasyonBoyutu { get; set; }
        public double CaprazlamaOrani { get; set; }
        public double MutasyonOrani { get; set; }
        public int SeckinlikSayisi { get; set; }

        // Sonuç takibi için
        public List<double> EnIyiUygunlukGecmisi { get; private set; }

        // GenetikAlgoritma sınıfını başlatır ve temel parametreleri ayarlar. Bu parametreler algoritmanın performansını önemli ölçüde etkiler.
        // Popülasyon boyutu, çeşitlilik ile hesaplama maliyeti arasındaki dengeyi belirler.
        // Çaprazlama oranı, yeni çözümlerin ne sıklıkta oluşturulacağını kontrol eder.
        // Mutasyon oranı, çeşitliliği korur ve yerel minimumlardan kaçınmaya yardımcı olur.
        // Seçkinlik sayısı, en iyi çözümlerin korunmasını sağlar.
        public GenetikAlgoritma(ITestProblemi problem, int populasyonBoyutu, double caprazlamaOrani,
                                double mutasyonOrani, int seckinlikSayisi)
        {
            _problem = problem;
            PopulasyonBoyutu = populasyonBoyutu;
            CaprazlamaOrani = caprazlamaOrani;
            MutasyonOrani = mutasyonOrani;
            SeckinlikSayisi = seckinlikSayisi;

            _random = new Random();
            EnIyiUygunlukGecmisi = new List<double>();

            // Başlangıç popülasyonunu oluştur
            _populasyon = new Populasyon(populasyonBoyutu, problem.Boyut,
                                         problem.AltSinirlar, problem.UstSinirlar, _random);
        }

        // Bu metot, genetik algoritmanın ana döngüsünü içerir ve evrimsel süreci maksimum jenerasyon sayısı kadar çalıştırır.
        // Her jenerasyonda, yeni bir popülasyon oluşturulur, seçkinler korunur, yeni bireyler seçilim, çaprazlama ve mutasyon işlemleriyle üretilir.
        // Bu süreç, optimum çözüme doğru kademeli yakınsamayı sağlar. Algoritma, her adımda en iyi çözümü takip eder ve sonunda global en iyi çözümü döndürür.
        public Kromozom Calistir(int maksJenerasyonSayisi, Action<int> ilerlemeBildirimi = null)
        {
            // İlk popülasyonu değerlendir
            _populasyon.UygunlukDegerleriniHesapla(_problem);

            // En iyi uygunluk değerini kaydet
            EnIyiUygunlukGecmisi.Add(_populasyon.EnIyiKromozom.UygunlukDegeri);

            // Ana GA döngüsü - her jenerasyon için evrimsel süreci tekrarlar
            for (int jenerasyon = 0; jenerasyon < maksJenerasyonSayisi; jenerasyon++)
            {
                // İlerleme bildir
                ilerlemeBildirimi?.Invoke(jenerasyon);

                // Yeni popülasyon oluştur
                Populasyon yeniPopulasyon = new Populasyon(0, _problem.Boyut,
                                                           _problem.AltSinirlar, _problem.UstSinirlar, _random);

                // Seçkinlik - en iyi kromozomları doğrudan yeni popülasyona aktar. Bu, algoritmanın en iyi çözümleri kaybetmemesini sağlar.
                // Doğada da benzer şekilde, en güçlü bireyler hayatta kalma ve üreme şansı en yüksek olanlardır.
                // Seçkinlik sayesinde, algoritma her zaman en azından bir önceki nesil kadar iyi çözümlere sahip olur.
                if (SeckinlikSayisi > 0)
                {
                    List<Kromozom> elitler = _populasyon.Kromozomlar
                        .OrderBy(k => k.UygunlukDegeri)
                        .Take(SeckinlikSayisi)
                        .Select(k => new Kromozom(k))
                        .ToList();

                    foreach (var elit in elitler)
                    {
                        yeniPopulasyon.Kromozomlar.Add(elit);
                    }
                }

                // Popülasyonun geri kalanını oluştur - doğadaki üreme sürecini modelleyen bu aşamada, ebeveynler seçilir, çaprazlanır ve mutasyona uğrar
                while (yeniPopulasyon.Kromozomlar.Count < PopulasyonBoyutu)
                {
                    // Turnuva seçimi ile ebeveynleri seç - doğal seçilimi modelleyen bu yaklaşım, daha uygun bireylerin seçilme şansını artırır
                    Kromozom ebeveyn1 = TurnuvaSecimi();
                    Kromozom ebeveyn2 = TurnuvaSecimi();

                    // Ebeveynlerin kopyalarını oluştur
                    Kromozom yavru1 = new Kromozom(ebeveyn1);
                    Kromozom yavru2 = new Kromozom(ebeveyn2);

                    // Çaprazlama - belirli bir olasılıkla (CaprazlamaOrani) ebeveynlerin genlerini karıştırarak yeni yavrular oluşturur
                    if (_random.NextDouble() < CaprazlamaOrani)
                    {
                        AritmetikCaprazla(yavru1, yavru2);
                    }

                    // Mutasyon - doğadaki rastgele genetik değişimleri modelleyen bu süreç, algoritmanın yerel optimumlardan kaçmasını sağlar
                    Mutasyon(yavru1);
                    Mutasyon(yavru2);

                    // Yeni popülasyona ekle
                    yeniPopulasyon.Kromozomlar.Add(yavru1);
                    if (yeniPopulasyon.Kromozomlar.Count < PopulasyonBoyutu)
                    {
                        yeniPopulasyon.Kromozomlar.Add(yavru2);
                    }
                }

                // Eski popülasyonu yenisiyle değiştir - nesiller arası geçişi temsil eder
                _populasyon = yeniPopulasyon;

                // Yeni popülasyonu değerlendir
                _populasyon.UygunlukDegerleriniHesapla(_problem);

                // En iyi uygunluk değerini kaydet - algoritmanın yakınsama sürecini takip etmek için önemlidir
                EnIyiUygunlukGecmisi.Add(_populasyon.EnIyiKromozom.UygunlukDegeri);
            }

            // Son ilerleme bilgisini gönder
            ilerlemeBildirimi?.Invoke(maksJenerasyonSayisi);

            // En iyi çözümü döndür - algoritmanın nihai çıktısı
            return _populasyon.EnIyiKromozom;
        }

        // Turnuva seçimi, doğadaki rekabeti modelleyen bir seçim mekanizmasıdır. Rastgele seçilen bireyler arasında en uygun olanın seçilmesini sağlar. Bu yaklaşım, iyi çözümlere daha fazla üreme şansı verirken, popülasyondaki çeşitliliği de korur. Basit ama etkili bu seçim yöntemi, genetik algoritmanın yönlendirilmiş rastgelelik prensibini yansıtır - tamamen rastgele değil, ama kesin deterministik de değil.
        private Kromozom TurnuvaSecimi(int turnuvaBoyutu = 3)
        {
            // Turnuva için rastgele kromozomlar seç
            List<Kromozom> turnuva = new List<Kromozom>();
            for (int i = 0; i < turnuvaBoyutu; i++)
            {
                int indeks = _random.Next(_populasyon.Kromozomlar.Count);
                turnuva.Add(_populasyon.Kromozomlar[indeks]);
            }

            // En iyi uygunluk değerine sahip olanı döndür (minimizasyon)
            return turnuva.OrderBy(k => k.UygunlukDegeri).First();
        }

        // Aritmetik çaprazlama, sürekli değişkenlerle çalışan genetik algoritmalarda yaygın olarak kullanılan bir operatördür. Bu yöntem, ebeveynlerin gen değerlerinin ağırlıklı ortalamasını alarak yeni yavrular oluşturur. Rastgele bir alfa değeri ile her gen için farklı bir ağırlık kullanılır, böylece çeşitli yeni çözümler üretilir. Bu, biyolojideki gen rekombinasyonuna benzer ve çözüm uzayının etkin bir şekilde araştırılmasını sağlar.
        private void AritmetikCaprazla(Kromozom yavru1, Kromozom yavru2)
        {
            for (int i = 0; i < yavru1.Genler.Length; i++)
            {
                double alfa = _random.NextDouble(); // Ağırlık faktörü

                double gen1 = yavru1.Genler[i];
                double gen2 = yavru2.Genler[i];

                // Ağırlıklı ortalama ile yeni değerleri hesapla
                yavru1.Genler[i] = alfa * gen1 + (1 - alfa) * gen2;
                yavru2.Genler[i] = alfa * gen2 + (1 - alfa) * gen1;
            }
        }

        // Mutasyon, genetik algoritmaların kritik bir bileşenidir ve doğadaki genetik mutasyonları modellemektedir. Her bir gen, belirli bir olasılıkla (MutasyonOrani) rastgele değiştirilir. Bu, algoritmanın çözüm uzayını daha geniş bir şekilde araştırmasını sağlar ve yerel minimumlara takılma riskini azaltır. Mutasyon olmadan, algoritma çeşitliliğini kaybedebilir ve erken yakınsama sorunu yaşayabilir.
        private void Mutasyon(Kromozom kromozom)
        {
            for (int i = 0; i < kromozom.Genler.Length; i++)
            {
                // Her gen için mutasyon olasılığını kontrol et
                if (_random.NextDouble() < MutasyonOrani)
                {
                    // Geni alt ve üst sınırlar arasında rastgele bir değerle değiştir
                    kromozom.Genler[i] = _problem.AltSinirlar[i] +
                                         (_problem.UstSinirlar[i] - _problem.AltSinirlar[i]) *
                                         _random.NextDouble();
                }
            }
        }
    }

    /// Test Problemi 7 - Custom function
    /// Minimum: yaklaşık (-15, 0.0225) = 0.25

    //"Test problemi, algoritmanın çözmesi gereken matematiksel fonksiyonu tanımlar"
    //"Problem 7, özel bir optimizasyon problemidir ve global minimum değeri (-15, 0.0225) noktasında yaklaşık 0.25'tir"
    //"Alt ve üst sınırlar, çözüm uzayının sınırlarını belirler"
    //"Degerlendir metodu, algoritmanın minimumunu bulmaya çalıştığı matematiksel fonksiyondur"
    public class TestProblemi7 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -15, -3 };
        public double[] UstSinirlar => new double[] { -5, 3 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return 100 * Math.Sqrt(Math.Abs(y - 0.01 * x * x)) + 0.01 * Math.Abs(x + 10);
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = 100√|y - 0.01x²| + 0.01|x + 10|";
        }
    }

    
    public partial class Form1 : Form
    {
        private GenetikAlgoritma ga;
        private ITestProblemi seciliProblem;
        private Kromozom enIyiCozum;
        private bool calisiyor = false;
        private int maksJenerasyon;
        private int problemNo = 7; // Her zaman Problem 7 olacak

        // Form elemanları sınıf seviyesinde tanımlanıyor
        private Label lblProblemFormul;
        private ProgressBar progressBar;
        private Chart chart;
        private TextBox txtSonuc;
        private ComboBox cmbProblem;
        private NumericUpDown numPopBoyut;
        private NumericUpDown numCaprazlamaOrani;
        private NumericUpDown numMutasyonOrani;
        private NumericUpDown numSeckinlikSayisi;
        private NumericUpDown numJenerasyonSayisi;

        public Form1()
        {
            // 1) Designer'ın oluşturduğu InitializeComponent() çağrısı
            InitializeComponent();

            // 2) Kendi özel kontrollerimizi ekleyen metot
            CustomizeComponents();

            // 3) Varsayılan problem seçimi vb. ayarlar
            InitializeTestProblem();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Form yüklenirken parametreleri ödev için istenen değerlere ayarla
            cmbProblem.SelectedIndex = 0; // ComboBox'da bir tane öğe olduğu için 0
            numPopBoyut.Value = 48;       // Popülasyon boyutu
            numCaprazlamaOrani.Value = 0.7m; // Çaprazlama oranı
            numMutasyonOrani.Value = 0.05m;  // Mutasyon oranı
            numJenerasyonSayisi.Value = 100; // Jenerasyon sayısı
        }

      
        /// El ile oluşturulan kontrolleri eklediğim özel metot.
        /// Eski "private new void InitializeComponent()" gövdesini buraya taşıdık.
     
        private void CustomizeComponents()
        {
            // Form genel özellikler
            this.Text = "Genetik Algoritma Uygulaması";
            this.Size = new System.Drawing.Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 1) ComboBox - Problem seçimi
            Label lblProblem = new Label()
            {
                Text = "Test Problemi:",
                Location = new Point(20, 20),
                Size = new Size(110, 20),
                Visible = false
            };
            this.cmbProblem = new ComboBox()
            {
                Location = new Point(130, 20),
                Size = new Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false, // Problem 7 sabit olduğu için devre dışı bırakıldı
                Visible = false
            };

            // Sadece Problem 7 gösterilecek
            cmbProblem.Items.Add("Problem 7");

            cmbProblem.SelectedIndexChanged += (sender, e) =>
            {
                // Sadece Problem 7 olduğu için bu olay tetiklenmeyecek
                problemNo = 7;
                InitializeTestProblem();
                lblProblemFormul.Text = seciliProblem.GetProblemAdi();
            };

            // 2) Label - Problem formülü
            this.lblProblemFormul = new Label()
            {
                Location = new Point(20, 50),
                Size = new Size(400, 40),
                Font = new Font("Consolas", 9)
            };

            // 3) Parametre giriş alanları
            Label lblPopBoyut = new Label()
            {
                Text = "Popülasyon Boyutu:",
                Location = new Point(20, 100),
                Size = new Size(150, 20)
            };
            this.numPopBoyut = new NumericUpDown()
            {
                Location = new Point(180, 100),
                Size = new Size(80, 20),
                Minimum = 10,
                Maximum = 1000,
                Value = 48, // Ödev için istenen değer
                Increment = 10
            };

            Label lblCaprazlamaOrani = new Label()
            {
                Text = "Çaprazlama Oranı:",
                Location = new Point(20, 130),
                Size = new Size(150, 20)
            };
            this.numCaprazlamaOrani = new NumericUpDown()
            {
                Location = new Point(180, 130),
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 1,
                Value = 0.7m, // Ödev için istenen değer
                Increment = 0.05m,
                DecimalPlaces = 2
            };

            Label lblMutasyonOrani = new Label()
            {
                Text = "Mutasyon Oranı:",
                Location = new Point(20, 160),
                Size = new Size(150, 20)
            };
            this.numMutasyonOrani = new NumericUpDown()
            {
                Location = new Point(180, 160),
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 1,
                Value = 0.05m, // Ödev için istenen değer
                Increment = 0.01m,
                DecimalPlaces = 2
            };

            Label lblSeckinlikSayisi = new Label()
            {
                Text = "Seçkinlik Sayısı:",
                Location = new Point(20, 190),
                Size = new Size(150, 20)
            };
            this.numSeckinlikSayisi = new NumericUpDown()
            {
                Location = new Point(180, 190),
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 10,
                Value = 2
            };

            Label lblJenerasyonSayisi = new Label()
            {
                Text = "Jenerasyon Sayısı:",
                Location = new Point(20, 220),
                Size = new Size(150, 20)
            };
            this.numJenerasyonSayisi = new NumericUpDown()
            {
                Location = new Point(180, 220),
                Size = new Size(80, 20),
                Minimum = 10,
                Maximum = 1000,
                Value = 100, // Ödev için istenen değer
                Increment = 10
            };

            // 4) Çalıştır butonu
            Button btnCalistir = new Button()
            {
                Text = "Çalıştır",
                Location = new Point(180, 260),
                Size = new Size(80, 30)
            };
            btnCalistir.Click += (sender, e) =>
            {
                if (!calisiyor)
                {
                    btnCalistir.Text = "Durdur";
                    calisiyor = true;

                    // GA parametrelerini al
                    int popBoyut = (int)numPopBoyut.Value;
                    double caprazlama = (double)numCaprazlamaOrani.Value;
                    double mutasyon = (double)numMutasyonOrani.Value;
                    int seckinlik = (int)numSeckinlikSayisi.Value;
                    maksJenerasyon = (int)numJenerasyonSayisi.Value;

                    // İlerleme çubuğunu sıfırla
                    progressBar.Value = 0;
                    progressBar.Maximum = maksJenerasyon;

                    // Grafiği temizle
                    chart.Series.Clear();
                    Series series = new Series("Yakınsama")
                    {
                        ChartType = SeriesChartType.Line
                    };
                    chart.Series.Add(series);

                    // GA'yı başlatır
                    ga = new GenetikAlgoritma(seciliProblem, popBoyut, caprazlama, mutasyon, seckinlik);

                    // Arka plan işlemi olarak çalıştır
                    BackgroundWorker worker = new BackgroundWorker
                    {
                        WorkerReportsProgress = true
                    };
                    worker.DoWork += (s, args) =>
                    {
                        enIyiCozum = ga.Calistir(maksJenerasyon, jenerasyon =>
                        {
                            // jenerasyon -> 0..maksJenerasyon
                            worker.ReportProgress((int)((float)jenerasyon / maksJenerasyon * 100));
                        });
                    };
                    worker.ProgressChanged += (s, args) =>
                    {
                        // Kaçıncı jendrasyondayız, progress bar'a yansıtalım
                        progressBar.Value = Math.Min(
                            args.ProgressPercentage * maksJenerasyon / 100,
                            maksJenerasyon
                        );

                        // Grafiği güncelle
                        chart.Series[0].Points.Clear();
                        for (int i = 0; i < ga.EnIyiUygunlukGecmisi.Count; i++)
                        {
                            chart.Series[0].Points.AddXY(i, ga.EnIyiUygunlukGecmisi[i]);
                        }
                    };
                    worker.RunWorkerCompleted += (s, args) =>
                    {
                        // Sonuçları göster
                        txtSonuc.Text =
                            $"En iyi çözüm değeri: {enIyiCozum.UygunlukDegeri:F6}\r\n\r\n";
                        txtSonuc.Text += "Parametre değerleri:\r\n";
                        for (int i = 0; i < enIyiCozum.Genler.Length; i++)
                        {
                            txtSonuc.Text +=
                                $"x{i + 1} = {enIyiCozum.Genler[i]:F6}\r\n";
                        }

                        btnCalistir.Text = "Çalıştır";
                        calisiyor = false;
                    };
                    worker.RunWorkerAsync();
                }
                else
                {
                    // Durdurma işlemi (gerçek uygulamada iptal mantığı vs. eklenebilir)
                    btnCalistir.Text = "Çalıştır";
                    calisiyor = false;
                }
            };

            // 5) İlerleme çubuğu
            this.progressBar = new ProgressBar()
            {
                Location = new Point(20, 300),
                Size = new Size(240, 20)
            };

            // 6) Sonuç kutusu
            Label lblSonuc = new Label()
            {
                Text = "Sonuç:",
                Location = new Point(20, 330),
                Size = new Size(100, 20)
            };
            this.txtSonuc = new TextBox()
            {
                Location = new Point(20, 350),
                Size = new Size(240, 200),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            // 7) Grafik
            this.chart = new Chart()
            {
                Location = new Point(280, 100),
                Size = new Size(580, 450),
                BorderlineWidth = 1,
                BorderlineColor = Color.LightGray,
                BorderlineDashStyle = ChartDashStyle.Solid
            };
            ChartArea chartArea = new ChartArea
            {
                AxisX = { Title = "Jenerasyon", MajorGrid = { LineColor = Color.LightGray } },
                AxisY = { Title = "Uygunluk Değeri", MajorGrid = { LineColor = Color.LightGray } }
            };
            chart.ChartAreas.Add(chartArea);

            // Form elemanlarını ekle
            this.Controls.Add(lblProblem);
            this.Controls.Add(cmbProblem);
            this.Controls.Add(lblProblemFormul);
            this.Controls.Add(lblPopBoyut);
            this.Controls.Add(numPopBoyut);
            this.Controls.Add(lblCaprazlamaOrani);
            this.Controls.Add(numCaprazlamaOrani);
            this.Controls.Add(lblMutasyonOrani);
            this.Controls.Add(numMutasyonOrani);
            this.Controls.Add(lblSeckinlikSayisi);
            this.Controls.Add(numSeckinlikSayisi);
            this.Controls.Add(lblJenerasyonSayisi);
            this.Controls.Add(numJenerasyonSayisi);
            this.Controls.Add(btnCalistir);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblSonuc);
            this.Controls.Add(txtSonuc);
            this.Controls.Add(chart);

            // Varsayılan olarak Problem 7 seçilsin:
            cmbProblem.SelectedIndex = 0;
        }

        /// <summary>
        /// Problem seçimini (problemNo) güncellediğimiz metot
        /// </summary>
        private void InitializeTestProblem()
        {
            //sadece Problem 7 kullanılıyor
            seciliProblem = new TestProblemi7();
        }
    }
}

// kromozom bir çözüm adayı
// gen bu çözümü yani kromozomu oluşturan her bir parametre. değişken. kodda bu genler dizisi olarak geçiyor 
//public class Kromozom
// { public double[] Genler { get; set; }  // Parametrelerin reel değerlerini tutan dizi
//Örnek olarak, Problem 7 iki değişkenli bir optimizasyon problemidir:
//Genler[0] → x değişkenini temsil eder
//Genler[1] → y değişkenini temsil eder
//Popülasyon: Kromozomlardan oluşan bir topluluk (kodda Populasyon sınıfı)
//Uygunluk Değeri: Bir çözümün ne kadar iyi olduğunu gösteren değer (kodda UygunlukDegeri özelliği)
//Çaprazlama: İki ebeveyn kromozomdan yeni kromozomlar oluşturma (kodda AritmetikCaprazla metodu)
//Seçkinlik: En iyi bireylerin doğrudan bir sonraki nesle aktarılması (kodda SeckinlikSayisi parametresi)
//Turnuva Seçimi: Ebeveyn seçiminde kullanılan bir yöntem (kodda TurnuvaSecimi metodu)
//Popülasyon boyutu: 48 (daha büyük popülasyon, daha geniş arama yapar)
//Çaprazlama oranı: 0.7(yeni çözümlerin oluşturulma olasılığı)
//Mutasyon oranı: 0.05(çeşitliliği sağlar ve yerel minimumlardan kaçar)
//Jenerasyon sayısı: 100(algoritmanın evrim adımı sayısı)
//X ekseni: Jenerasyon sayısı
//Y ekseni: En iyi uygunluk değeri
//Grafiğin aşağı doğru eğimi: Algoritmanın iyileşme gösterdiğini belirtir

//Popülasyon Boyutu(PopulasyonBoyutu) Genetik algoritmada her nesilde (jenerasyonda) kaç tane birey (kromozom) bulunacağını belirler.
//Örneğin 48 girerseniz, her jenerasyonda 48 farklı çözüm adayı vardır. kodda: int popBoyut = (int)numPopBoyut.Value; 
// ga = new GenetikAlgoritma(seciliProblem, popBoyut, caprazlama, mutasyon, seckinlik);
//Burada popBoyut değeri, GenetikAlgoritma sınıfının yapıcısına (constructor) gönderilir ve orada yeni bir Populasyon oluşturulurken kullanılır (Populasyon(popBoyut, ...)).

//Çaprazlama Oranı (CaprazlamaOrani) İki ebeveynden (kromozomdan) yeni yavrular oluşturulurken, çaprazlamanın (crossover) gerçekleşme olasılığını belirler.
//0.7 girerseniz, her ebeveyn seçimi sonrasında %70 ihtimalle çaprazlama yapılır. double caprazlama = (double)numCaprazlamaOrani.Value;
// ga = new GenetikAlgoritma(seciliProblem, popBoyut, caprazlama, mutasyon, seckinlik);
//Daha sonra GenetikAlgoritma sınıfında Calistir metodu içindeki şu satırda kontrol edilir: if (_random.NextDouble() < CaprazlamaOrani)
//{AritmetikCaprazla(yavru1, yavru2);}
//Yani rastgele bir sayı üretilip çaprazlama oranı ile karşılaştırılır; küçükse çaprazlama yapılır.


//mutasyon oranı: Bireylerin genlerinde(x, y gibi değişkenlerinde) rastgele küçük değişiklikler (mutasyon) yapma olasılığını belirler. Örneğin 0.05 girerseniz, her gende %5 ihtimalle mutasyon gerçekleşir. Bu, popülasyonun çeşitliliğini korumaya ve yerel minimumlara sıkışmayı önlemeye yardımcı olur.
// double mutasyon = (double)numMutasyonOrani.Value;
// ga = new GenetikAlgoritma(seciliProblem, popBoyut, caprazlama, mutasyon, seckinlik);
//Sonrasında GenetikAlgoritma içinde Mutasyon metodu şu şekilde çalışır: if (_random.NextDouble() < MutasyonOrani)
//{
//    kromozom.Genler[i] = ... // yeni rastgele değer
//}
//Her gende mutasyon olasılığına göre rastgele bir değere atama yapılır.Sonrasında GenetikAlgoritma içinde Mutasyon metodu şu şekilde çalışır:

//if (_random.NextDouble() < MutasyonOrani)
//{
//    kromozom.Genler[i] = ... // yeni rastgele değer
//}
//Her gende mutasyon olasılığına göre rastgele bir değere atama yapılır.

//Seçkinlik Sayısı(SeckinlikSayisi)

//Ne İşe Yarar?
//Elitizm (seçkinlik) mekanizmasında, en iyi bireylerden (en düşük uygunluk değerine sahip) kaç tanesinin doğrudan bir sonraki jenerasyona aktarılacağını belirler. Örneğin 2 girerseniz, her jenerasyondaki en iyi 2 çözüm, hiç değiştirilmeden yeni popülasyona kopyalanır. Bu sayede iyi çözümleri kaybetmemiş olursunuz.

//Kodda Nasıl Kullanılıyor?

//int seckinlik = (int)numSeckinlikSayisi.Value;
//// ...
//ga = new GenetikAlgoritma(seciliProblem, popBoyut, caprazlama, mutasyon, seckinlik);
//Daha sonra Calistir metodunda:

//// Seçkinlik
//if (SeckinlikSayisi > 0)
//{
//    List<Kromozom> elitler = _populasyon.Kromozomlar
//        .OrderBy(k => k.UygunlukDegeri)
//        .Take(SeckinlikSayisi)
//        .Select(k => new Kromozom(k))
//        .ToList();
//...
//}
//Bu kod, en iyi SeckinlikSayisi kadar bireyi kopyalayıp yeni popülasyona ekler.

//Jenerasyon Sayısı (Calistir metodu içinde maksJenerasyon)

//Ne İşe Yarar?
//Genetik algoritmanın kaç nesil (iteration) çalışacağını belirler. Örneğin 100 girerseniz, 100 kez “seçim, çaprazlama, mutasyon, elitizm” döngüsü tekrarlar.

//Kodda Nasıl Kullanılıyor?

//maksJenerasyon = (int)numJenerasyonSayisi.Value;
//// ...
//enIyiCozum = ga.Calistir(maksJenerasyon, jenerasyon => { ... });
//Calistir metodu içerisinde şu döngüde kullanılır:

//for (int j = 0; j < maksJenerasyonSayisi; j++)
//{
//    // GA’nın ana evrimsel işlemleri burada döner
//}
//Bu döngü bittiğinde algoritma durur ve elde edilen en iyi çözüm döndürülür.

//Özetle, bu parametreler (Popülasyon Boyutu, Çaprazlama Oranı, Mutasyon Oranı, Seçkinlik Sayısı, Jenerasyon Sayısı) genetik algoritmanın nasıl çalışacağını kontrol eden temel ayarlardır. Form üzerindeki NumericUpDown’lardan değerler alınıp, GenetikAlgoritma sınıfına aktarılır. Ardından:

//Popülasyon Boyutu → Kaç çözüm adayı (kromozom) olacağını belirler.

//Çaprazlama Oranı → İki ebeveynden yeni yavrular oluşturma (crossover) ihtimalini kontrol eder.

//Mutasyon Oranı → Genlerin rastgele değişme (mutasyon) ihtimalini kontrol eder.

//Seçkinlik Sayısı → En iyi kaç bireyin olduğu gibi korunacağını belirler.

//Jenerasyon Sayısı → Ana evrim döngüsünün kaç kez tekrarlanacağını belirler.