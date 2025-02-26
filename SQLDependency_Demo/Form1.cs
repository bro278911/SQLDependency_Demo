using System.Data;
using System.Data.SqlClient;
using SQLDependency_Demo.Models;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace SQLDependency_Demo
{
    public partial class Form1 : Form
    {
        private SqlTableDependency<ChargingData> tableDependency;
        private string connectionString = "Server=localhost;Initial Catalog=SignalR;User Id=Userid;Password=password;MultipleActiveResultSets=true;TrustServerCertificate=True;Max Pool Size=200;Pooling=true;Connection Timeout=30"; // �Эקאּ�z���s���r��

        public Form1()
        {
            InitializeComponent();
        }

        private void StartMonitoring()
        {
            try
            {
                tableDependency = new SqlTableDependency<ChargingData>(
                    connectionString,
                    "ChargingStorage"
                );

                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.OnError += TableDependency_OnError;
                tableDependency.Start();

                // ��l�d�߷�e�ƾ�
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(
                        @"SELECT ChargingID, SiteID, ReceivedTime, Datatime, Sequence, 
                                 PowerStatus, PcsStatus, AcbStatus, Soc, KWh, PcsPower, SitePower 
                          FROM ChargingStorage",
                        connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DisplayChargingData(new ChargingData
                                {
                                    ChargingID = reader["ChargingID"].ToString(),
                                    SiteID = reader["SiteID"].ToString(),
                                    ReceivedTime = Convert.ToDateTime(reader["ReceivedTime"]),
                                    Datatime = Convert.ToDateTime(reader["Datatime"]),
                                    Sequence = Convert.ToInt32(reader["Sequence"]),
                                    PowerStatus = reader["PowerStatus"].ToString(),
                                    PcsStatus = reader["PcsStatus"].ToString(),
                                    AcbStatus = reader["AcbStatus"].ToString(),
                                    Soc = Convert.ToDecimal(reader["Soc"]),
                                    KWh = Convert.ToDecimal(reader["KWh"]),
                                    PcsPower = Convert.ToDecimal(reader["PcsPower"]),
                                    SitePower = Convert.ToDecimal(reader["SitePower"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateRichTextBox($"�Ұʺʱ��ɵo�Ϳ��~: {ex.Message}");
                throw;
            }
        }

        private void TableDependency_Changed(object sender, RecordChangedEventArgs<ChargingData> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    string changeType = string.Empty;
                    switch (e.ChangeType)
                    {
                        case TableDependency.SqlClient.Base.Enums.ChangeType.Insert:
                            changeType = "�s�W";
                            break;
                        case TableDependency.SqlClient.Base.Enums.ChangeType.Update:
                            changeType = "��s";
                            break;
                        case TableDependency.SqlClient.Base.Enums.ChangeType.Delete:
                            changeType = "�R��";
                            break;
                    }

                    UpdateRichTextBox($"�˴���R�q�ƾ�{changeType}");
                    if (e.Entity != null)
                    {
                        DisplayChargingData(e.Entity);
                    }
                });
            }
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                UpdateRichTextBox($"�ʱ��o�Ϳ��~: {e.Error.Message}");
            });
        }

        private void DisplayChargingData(ChargingData data)
        {
            UpdateRichTextBox(
                $"�R�q�ƾ�: \n" +
                $"�R�qID: {data.ChargingID}\n" +
                $"���IID: {data.SiteID}\n" +
                $"�����ɶ�: {data.ReceivedTime:yyyy-MM-dd HH:mm:ss}\n" +
                $"�ƾڮɶ�: {data.Datatime:yyyy-MM-dd HH:mm:ss}\n" +
                $"�ǦC��: {data.Sequence}\n" +
                $"�q�����A: {data.PowerStatus}\n" +
                $"PCS���A: {data.PcsStatus}\n" +
                $"ACB���A: {data.AcbStatus}\n" +
                $"SOC: {data.Soc}%\n" +
                $"KWh: {data.KWh}\n" +
                $"PCS�\�v: {data.PcsPower}\n" +
                $"���I�\�v: {data.SitePower}\n" +
                $"------------------------"
            );
        }

        private void StopMonitoring()
        {
            if (tableDependency != null)
            {
                tableDependency.OnChanged -= TableDependency_Changed;
                tableDependency.OnError -= TableDependency_OnError;
                tableDependency.Stop();
                tableDependency.Dispose();
                tableDependency = null;
            }
            UpdateRichTextBox("����ʱ��R�q�ƾ��ܧ�");
        }

        private void UpdateRichTextBox(string message)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<string>(UpdateRichTextBox), message);
                return;
            }

            richTextBox1.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{message}\n");
            richTextBox1.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                StartMonitoring();
                UpdateRichTextBox("�}�l�ʱ��R�q�ƾ��ܧ�");
                button1.Enabled = false;
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�Ұʺʱ��ɵo�Ϳ��~: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                StopMonitoring();
                button1.Enabled = true;
                button2.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"����ʱ��ɵo�Ϳ��~: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopMonitoring();
            base.OnFormClosing(e);
        }
    }
}
