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

    /// <summary>
    /// Kromozom sınıfı - genetik algoritmada bir aday çözümü temsil eder
    /// </summary>
    public class Kromozom
    {
        public double[] Genler { get; set; }  // Parametrelerin reel değerlerini tutan dizi
        public double UygunlukDegeri { get; set; }  // Uygunluk değeri (düşük değer daha iyi - minimizasyon için)

        /// <summary>
        /// Rastgele değerlerle başlatılan kromozom oluşturur
        /// </summary>
        public Kromozom(int boyut, double[] altSinirlar, double[] ustSinirlar, Random random)
        {
            Genler = new double[boyut];

            // Alt ve üst sınırlar arasında rastgele değerler üret
            for (int i = 0; i < boyut; i++)
            {
                Genler[i] = altSinirlar[i] + (ustSinirlar[i] - altSinirlar[i]) * random.NextDouble();
            }

            UygunlukDegeri = double.MaxValue; // Minimizasyon problemi için varsayılan değer
        }

        /// <summary>
        /// Varolan bir kromozomun kopyasını oluşturur
        /// </summary>
        public Kromozom(Kromozom diger)
        {
            Genler = new double[diger.Genler.Length];
            Array.Copy(diger.Genler, Genler, diger.Genler.Length);
            UygunlukDegeri = diger.UygunlukDegeri;
        }
    }

    /// <summary>
    /// Test problemleri için arayüz
    /// </summary>
    public interface ITestProblemi
    {
        int Boyut { get; }  // Problem boyutu (parametre/değişken sayısı)
        double[] AltSinirlar { get; }  // Her parametrenin alt sınırı
        double[] UstSinirlar { get; }  // Her parametrenin üst sınırı
        double Degerlendir(double[] cozum);  // Çözümü değerlendirip uygunluk değerini döndürür
        string GetProblemAdi();  // Problemin adını veya formülünü döndürür
    }

    /// <summary>
    /// Populasyon sınıfı - kromozomların bir koleksiyonunu yönetir
    /// </summary>
    public class Populasyon
    {
        public List<Kromozom> Kromozomlar { get; private set; }
        public Kromozom EnIyiKromozom { get; private set; }

        /// <summary>
        /// Belirtilen boyutta rastgele bir popülasyon oluşturur
        /// </summary>
        public Populasyon(int boyut, int kromozomBoyutu, double[] altSinirlar, double[] ustSinirlar, Random random)
        {
            Kromozomlar = new List<Kromozom>();

            // Belirtilen sayıda rastgele kromozom oluştur
            for (int i = 0; i < boyut; i++)
            {
                Kromozomlar.Add(new Kromozom(kromozomBoyutu, altSinirlar, ustSinirlar, random));
            }
        }

        /// <summary>
        /// Popülasyondaki tüm kromozomların uygunluk değerlerini hesaplar
        /// </summary>
        public void UygunlukDegerleriniHesapla(ITestProblemi problem)
        {
            foreach (var kromozom in Kromozomlar)
            {
                kromozom.UygunlukDegeri = problem.Degerlendir(kromozom.Genler);
            }

            // En iyi kromozomu bul (minimizasyon için)
            var enIyi = Kromozomlar.OrderBy(k => k.UygunlukDegeri).First();

            // Global en iyi değeri güncelle
            if (EnIyiKromozom == null || enIyi.UygunlukDegeri < EnIyiKromozom.UygunlukDegeri)
            {
                EnIyiKromozom = new Kromozom(enIyi);
            }
        }
    }

    /// <summary>
    /// Genetik Algoritma ana sınıfı
    /// </summary>
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

        /// <summary>
        /// GenetikAlgoritma sınıfını başlatır
        /// </summary>
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

        /// <summary>
        /// Genetik algoritmayı çalıştırır ve en iyi çözümü döndürür
        /// </summary>
        public Kromozom Calistir(int maksJenerasyonSayisi, Action<int> ilerlemeBildirimi = null)
        {
            // İlk popülasyonu değerlendir
            _populasyon.UygunlukDegerleriniHesapla(_problem);

            // En iyi uygunluk değerini kaydet
            EnIyiUygunlukGecmisi.Add(_populasyon.EnIyiKromozom.UygunlukDegeri);

            // Ana GA döngüsü
            for (int jenerasyon = 0; jenerasyon < maksJenerasyonSayisi; jenerasyon++)
            {
                // İlerleme bildir
                ilerlemeBildirimi?.Invoke(jenerasyon);

                // Yeni popülasyon oluştur
                Populasyon yeniPopulasyon = new Populasyon(0, _problem.Boyut,
                                                           _problem.AltSinirlar, _problem.UstSinirlar, _random);

                // Seçkinlik - en iyi kromozomları doğrudan yeni popülasyona aktar
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

                // Popülasyonun geri kalanını oluştur
                while (yeniPopulasyon.Kromozomlar.Count < PopulasyonBoyutu)
                {
                    // Turnuva seçimi ile ebeveynleri seç
                    Kromozom ebeveyn1 = TurnuvaSecimi();
                    Kromozom ebeveyn2 = TurnuvaSecimi();

                    // Ebeveynlerin kopyalarını oluştur
                    Kromozom yavru1 = new Kromozom(ebeveyn1);
                    Kromozom yavru2 = new Kromozom(ebeveyn2);

                    // Çaprazlama
                    if (_random.NextDouble() < CaprazlamaOrani)
                    {
                        AritmetikCaprazla(yavru1, yavru2);
                    }

                    // Mutasyon
                    Mutasyon(yavru1);
                    Mutasyon(yavru2);

                    // Yeni popülasyona ekle
                    yeniPopulasyon.Kromozomlar.Add(yavru1);
                    if (yeniPopulasyon.Kromozomlar.Count < PopulasyonBoyutu)
                    {
                        yeniPopulasyon.Kromozomlar.Add(yavru2);
                    }
                }

                // Eski popülasyonu yenisiyle değiştir
                _populasyon = yeniPopulasyon;

                // Yeni popülasyonu değerlendir
                _populasyon.UygunlukDegerleriniHesapla(_problem);

                // En iyi uygunluk değerini kaydet
                EnIyiUygunlukGecmisi.Add(_populasyon.EnIyiKromozom.UygunlukDegeri);
            }

            // Son ilerleme bilgisini gönder
            ilerlemeBildirimi?.Invoke(maksJenerasyonSayisi);

            // En iyi çözümü döndür
            return _populasyon.EnIyiKromozom;
        }

        /// <summary>
        /// Turnuva seçimi - rastgele seçilen bireyler arasından en iyisini döndürür
        /// </summary>
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

        /// <summary>
        /// Aritmetik Çaprazlama - her gen için ebeveyn değerlerinin ağırlıklı ortalamasını alır
        /// </summary>
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

        /// <summary>
        /// Mutasyon - belirli bir olasılıkla bazı genleri değiştirir
        /// </summary>
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

    // TEST PROBLEMLERİ 

    /// <summary>
    /// Test Problemi 0 - Three-hump camel function
    /// Minimum: f(0,0) = 0
    /// </summary>
    public class TestProblemi0 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -5, -5 };
        public double[] UstSinirlar => new double[] { 5, 5 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return 2 * x * x - 1.05 * Math.Pow(x, 4) + Math.Pow(x, 6) / 6 + x * y + y * y;
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = 2x² - 1.05x⁴ + x⁶/6 + xy + y²";
        }
    }

    /// <summary>
    /// Test Problemi 1 - Ackley function
    /// Minimum: f(0,0) = 0
    /// </summary>
    public class TestProblemi1 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -5, -5 };
        public double[] UstSinirlar => new double[] { 5, 5 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return -20 * Math.Exp(-0.2 * Math.Sqrt(0.5 * (x * x + y * y))) -
                   Math.Exp(0.5 * (Math.Cos(2 * Math.PI * x) + Math.Cos(2 * Math.PI * y))) +
                   Math.E + 20;
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = -20exp(-0.2√(0.5(x² + y²))) - exp(0.5(cos(2πx) + cos(2πy))) + e + 20";
        }
    }

    /// <summary>
    /// Test Problemi 2 - Rosenbrock function
    /// Minimum: f(1,1,...,1) = 0
    /// </summary>
    public class TestProblemi2 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -10, -10 };
        public double[] UstSinirlar => new double[] { 10, 10 };

        public double Degerlendir(double[] cozum)
        {
            double sum = 0;
            for (int i = 0; i < cozum.Length - 1; i++)
            {
                sum += 100 * Math.Pow(cozum[i + 1] - cozum[i] * cozum[i], 2) + Math.Pow(cozum[i] - 1, 2);
            }
            return sum;
        }

        public string GetProblemAdi()
        {
            return "f(x) = Σ[100(x_{i+1} - x_i²)² + (x_i - 1)²]";
        }
    }

    /// <summary>
    /// Test Problemi 3 - Beale function
    /// Minimum: f(3,0.5) = 0
    /// </summary>
    public class TestProblemi3 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -4.5, -4.5 };
        public double[] UstSinirlar => new double[] { 4.5, 4.5 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return Math.Pow((1.5 - x + x * y), 2) +
                   Math.Pow((2.25 - x + x * y * y), 2) +
                   Math.Pow((2.625 - x + x * y * y * y), 2);
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = (1.5 - x + xy)² + (2.25 - x + xy²)² + (2.625 - x + xy³)²";
        }
    }

    /// <summary>
    /// Test Problemi 4 - Goldstein-Price function
    /// Minimum: f(0,-1) = 3
    /// </summary>
    public class TestProblemi4 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -2, -2 };
        public double[] UstSinirlar => new double[] { 2, 2 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return ((1 + (x + y) * (x + y)) *
                     (19 - 14 * x + 3 * x * x - 14 * y + 6 * x * y + 3 * y * y))
                 *
                   ((30 + (2 * x - 3 * y) * (2 * x - 3 * y)) *
                     (18 - 32 * x + 12 * x * x + 48 * y - 36 * x * y + 27 * y * y));
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = (1+(x+y)²)(19-14x+3x²-14y+6xy+3y²) * (30+(2x-3y)²)(18-32x+12x²+48y-36xy+27y²)";
        }
    }

    /// <summary>
    /// Test Problemi 5 - Matyas function
    /// Minimum: f(0,0) = 0
    /// </summary>
    public class TestProblemi5 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -10, -10 };
        public double[] UstSinirlar => new double[] { 10, 10 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return 0.26 * (x * x + y * y) - 0.48 * x * y;
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = 0.26(x² + y²) - 0.48xy";
        }
    }

    /// <summary>
    /// Test Problemi 6 - Himmelblau function
    /// Minimum: f(3,2) = 0, f(-2.805118, 3.131312) = 0, f(-3.779310, -3.283186) = 0, f(3.584428, -1.848126) = 0
    /// </summary>
    public class TestProblemi6 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -5, -5 };
        public double[] UstSinirlar => new double[] { 5, 5 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return Math.Pow(x * x + y - 11, 2) + Math.Pow(x + y * y - 7, 2);
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = (x² + y - 11)² + (x + y² - 7)²";
        }
    }

    /// <summary>
    /// Test Problemi 7 - Easom function
    /// Minimum: f(π,π) = -1
    /// </summary>
    public class TestProblemi7 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -100, -100 };
        public double[] UstSinirlar => new double[] { 100, 100 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return -Math.Cos(x) * Math.Cos(y)
                   * Math.Exp(-Math.Pow(x - Math.PI, 2) - Math.Pow(y - Math.PI, 2));
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = -cos(x)cos(y)exp(-(x-π)²-(y-π)²)";
        }
    }

    /// <summary>
    /// Test Problemi 8 - Cross-in-tray function
    /// Minimum: f(±1.3491, ±1.3491) = -2.06261
    /// </summary>
    public class TestProblemi8 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -10, -10 };
        public double[] UstSinirlar => new double[] { 10, 10 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return -0.0001 * Math.Pow(
                Math.Abs(
                    Math.Sin(x) * Math.Sin(y)
                    * Math.Exp(Math.Abs(100 - Math.Sqrt(x * x + y * y) / Math.PI))
                ) + 1, 0.1);
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = -0.0001(|sin(x)sin(y)exp(|100-√(x²+y²)/π|)|+1)^0.1";
        }
    }

    /// <summary>
    /// Test Problemi 9 - Eggholder function
    /// Minimum: f(512, 404.2319) = -959.6407
    /// </summary>
    public class TestProblemi9 : ITestProblemi
    {
        public int Boyut => 2;
        public double[] AltSinirlar => new double[] { -512, -512 };
        public double[] UstSinirlar => new double[] { 512, 512 };

        public double Degerlendir(double[] cozum)
        {
            double x = cozum[0];
            double y = cozum[1];

            return -(y + 47) * Math.Sin(Math.Sqrt(Math.Abs(y + x / 2 + 47)))
                   - x * Math.Sin(Math.Sqrt(Math.Abs(x - (y + 47))));
        }

        public string GetProblemAdi()
        {
            return "f(x,y) = -(y+47)sin(√|y+x/2+47|) - xsin(√|x-(y+47)|)";
        }
    }

    // -------------------- FORM KODU (partial class) --------------------
    public partial class Form1 : Form
    {
        private GenetikAlgoritma ga;
        private ITestProblemi seciliProblem;
        private Kromozom enIyiCozum;
        private bool calisiyor = false;
        private int maksJenerasyon;
        private int problemNo;

        // Form elemanları sınıf seviyesinde tanımlanıyor
        private Label lblProblemFormul;
        private ProgressBar progressBar;
        private Chart chart;
        private TextBox txtSonuc;
        private ComboBox cmbProblem;

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
            // Form yüklenirken ihtiyaç duyulan başlangıç işlemleri
        }

        /// <summary>
        /// El ile oluşturduğunuz kontrolleri eklediğiniz özel metot.
        /// Eski "private new void InitializeComponent()" gövdesini buraya taşıdık.
        /// </summary>
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
                Size = new Size(110, 20)
            };
            this.cmbProblem = new ComboBox()
            {
                Location = new Point(130, 20),
                Size = new Size(200, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 0; i < 10; i++)
            {
                cmbProblem.Items.Add("Problem " + i);
            }
            cmbProblem.SelectedIndexChanged += (sender, e) =>
            {
                problemNo = cmbProblem.SelectedIndex;
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
            NumericUpDown numPopBoyut = new NumericUpDown()
            {
                Location = new Point(180, 100),
                Size = new Size(80, 20),
                Minimum = 10,
                Maximum = 1000,
                Value = 50,
                Increment = 10
            };

            Label lblCaprazlamaOrani = new Label()
            {
                Text = "Çaprazlama Oranı:",
                Location = new Point(20, 130),
                Size = new Size(150, 20)
            };
            NumericUpDown numCaprazlamaOrani = new NumericUpDown()
            {
                Location = new Point(180, 130),
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 1,
                Value = 0.8m,
                Increment = 0.05m,
                DecimalPlaces = 2
            };

            Label lblMutasyonOrani = new Label()
            {
                Text = "Mutasyon Oranı:",
                Location = new Point(20, 160),
                Size = new Size(150, 20)
            };
            NumericUpDown numMutasyonOrani = new NumericUpDown()
            {
                Location = new Point(180, 160),
                Size = new Size(80, 20),
                Minimum = 0,
                Maximum = 1,
                Value = 0.1m,
                Increment = 0.01m,
                DecimalPlaces = 2
            };

            Label lblSeckinlikSayisi = new Label()
            {
                Text = "Seçkinlik Sayısı:",
                Location = new Point(20, 190),
                Size = new Size(150, 20)
            };
            NumericUpDown numSeckinlikSayisi = new NumericUpDown()
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
            NumericUpDown numJenerasyonSayisi = new NumericUpDown()
            {
                Location = new Point(180, 220),
                Size = new Size(80, 20),
                Minimum = 10,
                Maximum = 1000,
                Value = 100,
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

                    // GA'yı başlat
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

            // Varsayılan olarak Problem 0 seçilsin:
            cmbProblem.SelectedIndex = 0;
        }

        /// <summary>
        /// Problem seçimini (problemNo) güncellediğimiz metot
        /// </summary>
        private void InitializeTestProblem()
        {
            // Öğrenci numarasının son hanesine göre problem seçimi gibi durumlar olabilir.
            // Burada switch-case ile problemNo'ya bağlı olarak set ediliyor:
            switch (problemNo)
            {
                case 0:
                    seciliProblem = new TestProblemi0();
                    break;
                case 1:
                    seciliProblem = new TestProblemi1();
                    break;
                case 2:
                    seciliProblem = new TestProblemi2();
                    break;
                case 3:
                    seciliProblem = new TestProblemi3();
                    break;
                case 4:
                    seciliProblem = new TestProblemi4();
                    break;
                case 5:
                    seciliProblem = new TestProblemi5();
                    break;
                case 6:
                    seciliProblem = new TestProblemi6();
                    break;
                case 7:
                    seciliProblem = new TestProblemi7();
                    break;
                case 8:
                    seciliProblem = new TestProblemi8();
                    break;
                case 9:
                    seciliProblem = new TestProblemi9();
                    break;
                default:
                    seciliProblem = new TestProblemi0();
                    break;
            }
        }
    }
}
