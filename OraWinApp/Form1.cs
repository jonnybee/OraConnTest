using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;

namespace OraWinApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }



        private void button1_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }
            else
            {
                backgroundWorker1.WorkerSupportsCancellation = true;
                backgroundWorker1.RunWorkerAsync(textBox1.Text);
                button1.Text = "Stop SqlRunner";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateOraclePerformanceCountersIfNotExists();
        }

        private void CreateOraclePerformanceCountersIfNotExists()
        {
            if (PerformanceCounterCategory.Exists("ODP.NET, Managed Driver")) return;

            var assembly = Assembly.GetAssembly(typeof (OracleConnection));
            var type = assembly.GetTypes().FirstOrDefault(p => p.Name == "OraclePerfCounterConfiguration");
            var method = type.GetMethod("CreateCounters", BindingFlags.Static | BindingFlags.NonPublic);
            method.Invoke(null, new object[] {new string[] {}});
            listBox1.Items.Add(string.Format("{0}: Oracle Performance Counters Created", DateTime.Now));
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!backgroundWorker1.CancellationPending)
            {
                try
                {
                    using (var conn = new OracleConnection(textBox1.Text))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT 'OK' FROM DUAL";
                            var result = (string) cmd.ExecuteScalar();
                            AppendText(string.Format("{0}: Sql executed OK", DateTime.Now));
                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString());
                    AppendText(string.Format("{0}: Error:{1}", DateTime.Now, ex.Message));
                }

                for (var i = 0; i < 100; i++)
                {
                    Thread.Sleep(30);
                    if (backgroundWorker1.CancellationPending) return; 
                }
            }
        }

        delegate void AppendTextCallback(string message);

        private void AppendText(string message)
        {
            if (textBox1.InvokeRequired)
            {
                AppendTextCallback d = AppendText;
                this.Invoke(d, message);
            }
            else
            {
                listBox1.Items.Insert(0, message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backgroundWorker1.IsBusy)
                backgroundWorker1.CancelAsync();

            while (backgroundWorker1.IsBusy)
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Text = "Start SqlRunner (runs SQL each 3 secs) ";
        }
    }
}
