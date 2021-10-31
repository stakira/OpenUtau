using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Unzipper {
    public partial class UnzipperForm : Form {
        public UnzipperForm() {
            InitializeComponent();
            Task.Run(() => Unzip());
        }

        private void Unzip() {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++) {
                Console.WriteLine($"{i} {args[i]}");
            }
            if (args.Length < 3) {
                numberLabel.Text = string.Empty;
                fileLabel.Text = "No Arguments";
                return;
            }
            string source = args[1];
            string dest = args[2];
            int retry = 3;
            bool success = false;
            while (!success && retry > 0) {
                retry--;
                try {
                    using (var unzip = new Unzip(source)) {
                        unzip.ExtractProgress += (s, e) => {
                            progressBar.Value = e.ProgressPercentage;
                            numberLabel.Text = $"{e.CurrentFile} / {e.TotalFiles}";
                            fileLabel.Text = e.FileName;
                        };
                        unzip.ExtractToDirectory(dest);
                    }
                    success = true;
                } catch (Exception e) {
                    numberLabel.Text = $"Failed to extract update! Retries left: {retry}";
                    fileLabel.Text = e.ToString();
                    Thread.Sleep(3000);
                }
            }
            if (success) {
                Close();
            }
        }
    }
}
