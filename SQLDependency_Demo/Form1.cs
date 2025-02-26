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
        private string connectionString = "Server=localhost;Initial Catalog=SignalR;User Id=Userid;Password=password;MultipleActiveResultSets=true;TrustServerCertificate=True;Max Pool Size=200;Pooling=true;Connection Timeout=30"; // 請修改為您的連接字串

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

                // 初始查詢當前數據
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
                UpdateRichTextBox($"啟動監控時發生錯誤: {ex.Message}");
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
                            changeType = "新增";
                            break;
                        case TableDependency.SqlClient.Base.Enums.ChangeType.Update:
                            changeType = "更新";
                            break;
                        case TableDependency.SqlClient.Base.Enums.ChangeType.Delete:
                            changeType = "刪除";
                            break;
                    }

                    UpdateRichTextBox($"檢測到充電數據{changeType}");
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
                UpdateRichTextBox($"監控發生錯誤: {e.Error.Message}");
            });
        }

        private void DisplayChargingData(ChargingData data)
        {
            UpdateRichTextBox(
                $"充電數據: \n" +
                $"充電ID: {data.ChargingID}\n" +
                $"站點ID: {data.SiteID}\n" +
                $"接收時間: {data.ReceivedTime:yyyy-MM-dd HH:mm:ss}\n" +
                $"數據時間: {data.Datatime:yyyy-MM-dd HH:mm:ss}\n" +
                $"序列號: {data.Sequence}\n" +
                $"電源狀態: {data.PowerStatus}\n" +
                $"PCS狀態: {data.PcsStatus}\n" +
                $"ACB狀態: {data.AcbStatus}\n" +
                $"SOC: {data.Soc}%\n" +
                $"KWh: {data.KWh}\n" +
                $"PCS功率: {data.PcsPower}\n" +
                $"站點功率: {data.SitePower}\n" +
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
            UpdateRichTextBox("停止監控充電數據變更");
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
                UpdateRichTextBox("開始監控充電數據變更");
                button1.Enabled = false;
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"啟動監控時發生錯誤: {ex.Message}");
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
                MessageBox.Show($"停止監控時發生錯誤: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopMonitoring();
            base.OnFormClosing(e);
        }
    }
}
