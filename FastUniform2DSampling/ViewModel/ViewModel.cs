using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FastUniform2DSampling.Model;
using Lomont.WPF;
using Lomont.WPF.MVVM;

namespace FastUniform2DSampling.ViewModel
{
    public class ViewModel :ViewModelBase
    {
        public ViewModel()
        {
            ClearCommand = new RelayCommand(Clear);
            SaveCommand = new RelayCommand(Save);
            Update();

           // MakeGIF(300, 130, 150);
        }
        int pixelSize = 3;
        public int PixelSize
        {
            get => pixelSize;
            set => Set(ref pixelSize, value, null, (_, _) => Update());
        }

        int width = 200;
        public int Width
        {
            get => width;
            set => Set(ref width, value, null, (_,_)=>Update());
        }
        int height = 200;
        public int Height
        {
            get => height;
            set => Set(ref height, value, null, (_, _) => Update());
        }
        int delta = 153;
        public int Delta
        {
            get => delta;
            set => Set(ref delta, value, null, (_, _) => Update());
        }
        int samples = 500;
        public int Samples
        {
            get => samples;
            set => Set(ref samples, value, null, (_, _) => Update());
        }

        int testCountMax = 10;
        public int TestCountMax
        {
            get => testCountMax;
            set => Set(ref testCountMax, value, null, (_, _) => Update());
        }

        bool autoDelta = false;
        public bool AutoDelta
        {
            get => autoDelta;
            set => Set(ref autoDelta, value, null, (_, _) => Update());
        }
        bool autoSample = false;
        public bool AutoSample
        {
            get => autoSample;
            set => Set(ref autoSample, value, null, (_, _) => Update());
        }

        bool showBasis = false;
        public bool ShowBasis
        {
            get => showBasis;
            set => Set(ref showBasis, value, null, (_, _) => Update());
        }
        bool showCell = false;
        public bool ShowCell
        {
            get => showCell;
            set => Set(ref showCell, value, null, (_, _) => Update());
        }


        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }

        void Clear()
        {
            Messages.Clear();
        }

        string Filename(Desc d) => $"img_{d.width}_{d.height}_{d.samples}_{d.delta}_{d.showBasis}_{d.showCell}_{testCountMax}";
        void Save()
        {
            var desc2 = new Desc(width, height, samples, delta, pixelSize, showCell, showBasis, testCountMax, autoDelta, autoSample);
            var (img, desc) = GenImage(desc2,Log);
            var fn = Filename(desc) + ".png";

            // Creates a new empty image with the pre-defined palette
            BitmapSource image = BitmapSource.Create(
                width*pixelSize,
                height * pixelSize,
                96,
                96,
                PixelFormats.Rgb24,
                null,
                img.pixels, width * 3*pixelSize);

            using FileStream stream = new FileStream(fn, FileMode.Create);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            //TextBlock myTextBlock = new TextBlock();
            //myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();
            //encoder.Interlace = PngInterlaceOption.On;
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
            Log($"{fn} saved");
        }

        public ObservableCollection<string> Messages { get; } = new();

        public void Log(string msg)
        {
            Messages.Add(msg);
        }

        void MakeGIF(int samples, int minD, int maxD)
        {
            var desc = new Desc(width, height, samples, delta, pixelSize, showCell, showBasis, testCountMax, autoDelta, autoSample);

            List<Imgb> images = new();
            for (int d = minD; d <= maxD; d++)
            {
                Samples = samples;
                Delta = d;
                images.Add(GenImage(desc,Log).img);
            }
            MakeGIF($"img_{samples}_{minD}_{maxD}.gif",images);
        }

        void Math(List<vec2> pts)
        {
            for (int i = 0; i < pts.Count; ++i)
            {
                int min = Int32.MaxValue;
                for (int j = 0; j < pts.Count; ++j)
                {
                    if (i == j) continue;
                    var p = pts[i];
                    var q = pts[j];
                    var d = (p - q).LengthSquared;
                    min = System.Math.Min(min,d);
                }
                Debug.WriteLine($"{i}=> {min}");

            }
        }


        class Imgb
        {
            public byte[] pixels;
            public int w, h, s;
            public Imgb(int w, int h, int s)
            {
                this.w = w;
                this.h = h;
                this.s = s;
                pixels = new byte[w * h * s * s*3];
            }

            public (int r, int g, int b) Get(int i, int j)
            {
                if (i < 0 || j < 0 || w <= i || h <= j) return (0, 0, 0);
                i *= s;
                j *= s;
                var ind = (i+j*(w*s))*3;
                var r = pixels[ind];
                var g = pixels[ind+1];
                var b = pixels[ind+2];
                return (r, g, b);
            }

            public void Set(int i, int j, int r, int g, int b)
            {
                if (i < 0 || j < 0 || w <= i || h <= j) return;
                for (var i1 = 0; i1 < s; ++i1)
                for (var j1 = 0; j1 < s; ++j1)
                {
                    int x = i * s + i1, y = j * s + j1;
                    int index = (x + y * w * s) * 3;
                    pixels[index] = (byte)r;
                    pixels[index + 1] = (byte)g;
                    pixels[index + 2] = (byte)b;
                }
            }
        }

        static vec2 Center(int w, int h, int delta)
        {
            var center = new vec2(w / 2, h / 2);
            vec2 best = new(0,0);
            int x = 0, y = 0;
            while (y < h)
            {
                var test = new vec2(x,y);
                if ((best - center).LengthSquared > (test - center).LengthSquared)
                    best = test;
                x += delta;
                while (x >= w)
                {
                    x -= w;
                    y++;
                }
            }

            return best;
        }

       

        void MakeGIF(string filename, List<Imgb> images)
        {

//            int width = 128;
//            int height = width;
//            int stride = width;
//            byte[] pixels = new byte[width * height];

            // Define the image palette
            BitmapPalette myPalette = BitmapPalettes.WebPalette;


            GifBitmapEncoder gifEncoder = new GifBitmapEncoder();
            //TextBlock myTextBlock = new TextBlock();
            //myTextBlock.Text = "Codec Author is: " + gifEncoder.CodecInfo.Author.ToString();

            static byte[] Make256(Imgb image)
            {
                var pixels = new byte[image.w*image.h];
                for (int j = 0; j < image.h;++j)
                for (int i = 0; i < image.w; ++i)
                {
                    var (r, g, b) = image.Get(i,j);
                    var ind = i + j * image.w;
                    if (r == 0 && g == 0 && b == 0)
                    {
                        pixels[ind] =  0;
                    }
                    else if (r == 255 && g == 255 && b == 255)
                    {
                        pixels[ind] =  215;
                    }
                    else if (r == 255 && g == 0 && b == 0)
                    {
                        pixels[ind] =  123;
                    }
                    else if (r == 0 && g == 255 && b == 0)
                    {
                        pixels[ind] =  17;
                    }
                    else
                        throw new Exception("not implemented");
                }

                return pixels;
            }

            // add some frames:
            foreach (var img in images)
            {
                var pixels = Make256(img);

                BitmapSource image = BitmapSource.Create(
                    img.w,
                    img.h,
                    96,
                    96,
                    PixelFormats.Indexed8,
                    myPalette,
                    pixels,
                    img.w /* stride */
                    );


                gifEncoder.Frames.Add(BitmapFrame.Create(image));
            }

            // looping GIF issue https://stackoverflow.com/questions/18719302/net-creating-a-looping-gif-using-gifbitmapencoder
            using (var ms = new MemoryStream())
            {
                gifEncoder.Save(ms);
                var fileBytes = ms.ToArray();
                // This is the NETSCAPE2.0 Application Extension.
                var applicationExtension = new byte[] { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };
                var newBytes = new List<byte>();
                newBytes.AddRange(fileBytes.Take(13));
                newBytes.AddRange(applicationExtension);
                newBytes.AddRange(fileBytes.Skip(13));
                File.WriteAllBytes(filename, newBytes.ToArray());
            }


        }

        record Desc(int width, int height, int samples, int delta, int pixelSize, 
            bool showCell, bool showBasis, int testCountMax,
            bool autoDelta,
            bool autoSample
            );
        
        static (Imgb img, Desc desc) GenImage(
            Desc d,
            Action<string> Log
            )
        {

            int kk = d.pixelSize; // size of a pixel

            int delta2 = d.delta;
            int samples2=d.samples;

            if (d.autoDelta)
            {
                delta2 = Fast2DSampler.MakeDelta(d.width, d.height, d.samples, d.testCountMax);
                Log($"Best delta {delta2}");
            }
            else if (d.autoSample)
            {
                int area = d.width * d.height;
                samples2 = area / d.delta;
                Log($"Samples {samples2}");
            }

            var im = new Imgb(d.width, d.height, kk);

            if (d.showCell || d.showBasis)
            {
                var (b1, b2) = Fast2DSampler.MakeBasis(delta2, d.width);
                var (m1, m2) = Fast2DSampler.LatticeReduction(b1, b2);

                var c = new vec2(0, 0);
                if (d.showBasis)
                {
                    Line(im, c, b1 + c, 255, 0, 0);
                    Line(im, c, b2 + c, 255, 0, 0);
                }

                // make point to right
                if (m1.x < 0) m1 = -m1;
                if (m2.x < 0) m2 = -m2;
                if (d.showCell)
                {
                    c = Center(d.width, d.height, delta2);
                    Line(im, c, m1 + c, 0, 255, 0);
                    Line(im, c, m2 + c, 0, 255, 0);
                }

                var r = m1.Length / m2.Length;
                Log($"Ratio {r:F3} {d.testCountMax}");
            }

            // byte[] pixels = new byte[width*height*3*kk*kk];
            int a = d.width * d.height;
            for (int k = 0; k < samples2; k++)
            {
                int t = (delta2 * k) % a;
                int i = t % d.width;
                int j = t / d.width;
                // pts.Add(new(i,j));
                im.Set(i, j, 255, 255, 255);
            }

            var d2 = d with {samples = samples2, delta = delta2};

            return (im, d2);
        }

        void Update()
        {

            List<vec2> pts = new();
            checked
            {

                var d = new Desc(width,height,samples,delta,pixelSize,showCell,showBasis,testCountMax,autoDelta,autoSample);
                var (im,desc) = GenImage(d,Log);

                int kk = desc.pixelSize;

                var img = new WriteableBitmap(width * kk, height * kk, 96.0, 96.0, PixelFormats.Rgb24, null);
                img.WritePixels(new Int32Rect(0, 0, width * kk, height * kk), im.pixels, width * 3 * kk, 0);
                Image = img;
                //Math(pts);

            }
        }

        static void Line(Imgb img, vec2 p1, vec2 p2, int r, int g, int b)
        {
            var pts = Lomont.Algorithms.DDA.Dim2(p1.x, p1.y, p2.x, p2.y);
            foreach (var (i, j) in pts)
            {
                img.Set(i, j, r, g, b);
            }
        }
        


        WriteableBitmap wbmp;

        public WriteableBitmap Image
        {
            get=> wbmp;
            set => Set(ref wbmp, value);

        }

    }
}
