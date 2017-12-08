using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using KR_MobilData.Model;
using KR_MobilData.Socket;
using KR_MobilData.View;
using SqlSugar;
using SuperSocket.ClientEngine;
using Telerik.Windows.Controls;

namespace KR_MobilData.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        //异常or提示信息集合
        private ObservableCollection<ExceptionModel> _exceptionModels = new ObservableCollection<ExceptionModel>();

        //监控点集合 
        private ObservableCollection<YXJK_JKD> _yxjkJkds = new ObservableCollection<YXJK_JKD>();

        //选择的监控点对象
        private YXJK_JKD _selectJkd = new YXJK_JKD();

        //RadMenuItem Click Command
        public DelegateCommand RmcCommand { get; set; }

        //Windows Close Event
        public DelegateCommand ClosedCommand { get; set; }

        //GridView 右键菜单Item Click
        public DelegateCommand GridMenuCommand { get; set; }

        private static bool CanExecute(object o)
        {
            return true;
        }

        private void RadMenuItemClick(object obj)
        {
            try
            {
                if (obj == null)
                {
                    return;
                }
                var compara = obj.ToString();
                if (compara.Equals("启动"))
                {
                    if (_taskFlag)
                    {
                        return;
                    }
                    _taskFlag = true;
                    TaskStart();
                    Task.Factory.StartNew(delegate
                    {
                        while (_taskFlag)
                        {
                            Thread.Sleep(Properties.Settings.Default.DelayTime_Update);
                            UpdateDbData();
                        }
                    });
                    WriteLog("启动", ExEnum.Infor);
                }
                else if (compara.Equals("停止"))
                {
                    _taskFlag = false;
                    WriteLog("停止", ExEnum.Infor);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, ExEnum.Error);
            }
        }

        //busy binding
        private bool _isBusy;

        //线程开始结束标识
        private bool _taskFlag;

        //SqlSugarClient
        public SqlSugarClient DbClient;

        public MainViewModel()
        {
            Task.Factory.StartNew(delegate
            {
                try
                {
                    IsBusy = true;
                    InitiCommand();
                    InitiJkd();
                }
                catch (Exception e)
                {
                    WriteLog(e.Message, ExEnum.Error);
                }
                finally
                {
                    IsBusy = false;
                }
            });
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        private void TaskStart()
        {
            Parallel.ForEach(YxjkJkds, jkd =>
            {
                Task.Factory.StartNew(async delegate
                {
                    var client = new EasyClient();

                    /***
                     * 初始化socket连接, 接受返回数据处理
                     * HxReceiveFilter为自定义的协议
                     * ***/
                    client.Initialize(new HxReceiveFilter(), (request) =>
                    {
                        try
                        {
                            string reqStr = request.Key + request.Body;
                            jkd.JKD_VALUE = reqStr;
                            jkd.CURR_TIME = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                        }
                        catch (Exception ex)
                        {
                            WriteLog(ex.Message, ExEnum.Error);
                        }
                    });
                    // Connect to the server
                    await client.ConnectAsync(new IPEndPoint(
                        IPAddress.Parse(Properties.Settings.Default.Service_Ip),
                        Properties.Settings.Default.Service_Port));
                    while (_taskFlag)
                    {
                        try
                        {
                            resend:
                            if (client.IsConnected)
                            {
                                //获取发送字符串
                                var enStr = GetSendStr(jkd.JKD_ID);
                                // Send data to the server
                                client.Send(Encoding.UTF8.GetBytes(enStr));
                            }
                            if (client.IsConnected == false && _taskFlag)
                            {
                                WriteLog($"{jkd.JKD_NAME}Socket连接失败,尝试重新连接...", ExEnum.Error);
                                await client.ConnectAsync(new IPEndPoint(
                                    IPAddress.Parse(Properties.Settings.Default.Service_Ip),
                                    Properties.Settings.Default.Service_Port));
                                goto resend;
                            }
                        }
                        catch (Exception e)
                        {
                            WriteLog(e.Message, ExEnum.Error);
                        }
                        finally
                        {
                            Thread.Sleep(Properties.Settings.Default.DelayTime);
                        }
                    }
                    await client.Close();
                    WriteLog($"{jkd.JKD_NAME} socket close", ExEnum.Infor);
                }, TaskCreationOptions.LongRunning);
            });
        }

        private void UpdateDbData()
        {
            foreach (YXJK_JKD jkd in YxjkJkds)
            {
                try
                {
                    string[] strings = jkd.JKD_VALUE.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (strings.Length != 3)
                    {
                        return;
                    }
                    string key = strings[0];
                    string body = strings[1];
                    string[] parameters = strings[2].Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string cs in parameters)
                    {
                        try
                        {
                            var css = cs.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
                            var csid = css[0];
                            var csvalue = css[1];
                            var uSql = $"UPDATE {jkd.JKD_LB} set {csid} = '{csvalue}' where jkd_id = '{jkd.JKD_ID}'";
                            var rowNum = DbClient.Ado.ExecuteCommand(uSql);
                            WriteLog(rowNum > 0 ? $"更新成功-{uSql}" : $"更新失败-{uSql}", ExEnum.Infor);
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"{jkd.JKD_NAME}-{ex.Message}", ExEnum.Error);
                        }
                    }
                }
                catch (Exception e)
                {
                    WriteLog(e.Message, ExEnum.Error);
                }
            }
        }

        /// <summary>
        /// 返回发送字符串
        /// </summary>
        /// <param name="jkdid">监控点ID</param>
        /// <returns></returns>
        private string GetSendStr(string jkdid)
        {
            var senStr = $"{Properties.Settings.Default.SendStr},{jkdid}";
            if (Properties.Settings.Default.Crypt)
            {
                senStr = DataPacketCodec.Encode(senStr, Properties.Settings.Default.CryptKey) + "#";
                return senStr;
            }
            return senStr + "#";
        }

        /// <summary>
        /// 初始化Command
        /// </summary>
        private void InitiCommand()
        {
            RmcCommand = new DelegateCommand(RadMenuItemClick, CanExecute);
            ClosedCommand = new DelegateCommand(WindowClosed, CanExecute);
            GridMenuCommand = new DelegateCommand(GridMenuItemClick, CanExecute);
        }

        private void GridMenuItemClick(object obj)
        {
            try
            {
                if (obj == null)
                {
                    return;
                }
                var compara = obj.ToString();
                if (compara.Equals("清空"))
                {
                    ExceptionModels?.Clear();
                }
                else if (compara.Equals("解密"))
                {
                    if (string.IsNullOrEmpty(SelectJkd.JKD_VALUE))
                    {
                        return;
                    }
                    if (SelectJkd.JKD_VALUE.TrimEnd('#').Length == 0)
                    {
                        return;
                    }
                    DeCodeWindow dcw = new DeCodeWindow();
                    var deStr = DataPacketCodec.Decode(SelectJkd.JKD_VALUE.TrimEnd('#'),
                        Properties.Settings.Default.CryptKey);
                    dcw.Tb1.Text = deStr;
                    dcw.ShowDialog();
                }
                else if (compara.Equals("打开"))
                {
                    if (string.IsNullOrEmpty(SelectJkd.JKD_VALUE))
                    {
                        return;
                    }
                    DeCodeWindow dcw = new DeCodeWindow();
                    dcw.Tb1.Text = SelectJkd.JKD_VALUE;
                    dcw.ShowDialog();
                }
            }
            catch (Exception e)
            {
                WriteLog(e.Message, ExEnum.Error);
            }
        }

        private void WindowClosed(object obj)
        {
            _taskFlag = false;
        }

        /// <summary>
        /// 获取监控点信息
        /// </summary>
        private void InitiJkd()
        {
            try
            {
                DbClient = new SqlSugarClient(new ConnectionConfig()
                {
                    ConnectionString = Properties.Settings.Default.OracleConnStr,
                    DbType = SqlSugar.DbType.Oracle
                });
                var jkds = DbClient.SqlQueryable<YXJK_JKD>(Properties.Settings.Default.JkdSql);
                if (!jkds.Any())
                {
                    MessageBox.Show("无换热站");
                    return;
                }
                YxjkJkds = new ObservableCollection<YXJK_JKD>(jkds.ToList());

                #region 测试用

                //                SoureJkds.Clear();
                //                for (int i = 0; i < 100; i++)
                //                {
                //                    SoureJkds.Add(new YXJK_JKD() {JKD_ID = "tyzx"+i, JKD_NAME = "体验中心"+i});
                //                }

                #endregion
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, ExEnum.Error);
            }
        }

        /// <summary>
        /// 添加log到集合中显示
        /// </summary>
        /// <param name="paramStr">log信息</param>
        /// <param name="paramLevel">log级别</param>
        public void WriteLog(string paramStr, ExEnum paramLevel)
        {
            string level = string.Empty;
            if (paramLevel == ExEnum.Infor)
            {
                level = "提示";
            }
            if (paramLevel == ExEnum.Error)
            {
                level = "异常";
            }
            if (!string.IsNullOrEmpty(level))
            {
                ExceptionModels.Add(new ExceptionModel()
                {
                    ExTime = DateTime.Now,
                    ExLevel = level,
                    ExMessage = paramStr
                });
            }
        }

        //异常or提示信息集合
        public ObservableCollection<ExceptionModel> ExceptionModels
        {
            get { return _exceptionModels; }

            set
            {
                _exceptionModels = value;
                OnPropertyChanged("ExceptionModels");
            }
        }

        //监控点集合 
        public ObservableCollection<YXJK_JKD> YxjkJkds
        {
            get { return _yxjkJkds; }

            set
            {
                _yxjkJkds = value;
                OnPropertyChanged("YxjkJkds");
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }

            set
            {
                _isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        public YXJK_JKD SelectJkd
        {
            get { return _selectJkd; }

            set
            {
                _selectJkd = value;
                OnPropertyChanged("SelectJkd");
            }
        }
    }
}