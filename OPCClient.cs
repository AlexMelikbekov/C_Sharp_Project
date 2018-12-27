using System;
using System.Collections.Generic;
using Opc.Da;
using Alpha.OpcEx.Net.Da;
using System.Threading;
using System.Windows.Forms;

namespace SettingCSPA
{
    public class ServerLink
    {
        //   DataProject inData;
        //создали сервер
        Alpha.OpcEx.Net.Da.Server server;
        string OPCServerName { get; set; }
        private static System.Timers.Timer aTimer;
        private static System.Timers.Timer aTimerRecconectAlpha;

        delegate void SetTextCallback(string text);
        String MessageToErrorPanel;
        Subscription groupSubscribe;
        public int counterReading;
        List<string> bufferMessages;
        public int TimeRecconectAlpha = 5;//время реконекта к альфе

        public Item[] items;
        bool FlagWriteinController = false;
        public delegate void MethodContainer(string x, string y);
        /// <summary>
        /// событие изменения состояния любого МНА
        /// </summary>
        public event MethodContainer MNAChange;
        public event MethodContainer HubStateChange;
        public event MethodContainer ZaprettagChange;
        public event MethodContainer ChangeErrorCode;
        public event MethodContainer ControllerStateChange;

        MainForm.ErrorMessagePanelWriteString MessagePanelWriteStr;//делегат для записи сообщений в основную форму

        public int indexTagToWrite;
        bool AlphaConnectFlag;
        ListView error;
        public string[] OPCValue { get; set; }
        public int CounSettingsTag;
        string[] tempOPCValue;
        long OPCValueIndex;
        static EventWaitHandle handle = new AutoResetEvent(false);
        static EventWaitHandle WaitComand = new AutoResetEvent(false);
        /// <summary>
        ///  класс для общения с OPC сервером
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="Service">сервис нейм OPC сервера</param>
        /// <param name="errorIN">делегат для передачи сообщений в основной поток</param>
        /// <param name="CountStartTag">количество обьектов подключения</param>
        /// <param name="MessagePanelWriteStr"></param>
        public ServerLink(string IP, string Service, ListView errorIN, int CountStartTag, MainForm.ErrorMessagePanelWriteString MessagePanelWriteStr)
        {
            this.MessagePanelWriteStr = MessagePanelWriteStr;
            CounSettingsTag = CountStartTag;
            OPCServerName = Service;
            Alpha.OpcEx.Net.Com.ExFactory factory = new Alpha.OpcEx.Net.Com.ExFactory();
            Opc.URL url = new Opc.URL("opcda://" + IP + "/" + Service);
            url.Scheme = Opc.UrlScheme.DA;
            url.Port = 135;
            server = new Alpha.OpcEx.Net.Da.Server(factory, url);
            error = errorIN;
            counterReading = 40;
            bufferMessages = new List<string>();
        }
        public void StartConnectAlpha()
        {
            if (AlphaConnectFlag)
                CheckConnectStatus();
            else
                st();

        }
        public void CheckConnectStatus()
        {
            
            DateTime dataTimeNow = DateTime.Now;
               
            try
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    server.GetStatus();
                }
            }
            catch (Opc.ResultIDException ex)
            {
                MessagePanelWriteStr(dataTimeNow.ToString() + ex + " Сервер отключился");
                st();
            }
            catch (Opc.NotConnectedException ex)
            {
                MessagePanelWriteStr(dataTimeNow.ToString() + ex + " Сервер отключился");
                st();
            }
        }
        public void st()
        {
            aTimerRecconectAlpha = new System.Timers.Timer(TimeRecconectAlpha * 1000);//создаем новый таймер для реконекта
            MessagePanelWriteStr("Попытка подклчючения к Альфа серверу");
            aTimerRecconectAlpha.Start();
            aTimerRecconectAlpha.Enabled = true;
            aTimerRecconectAlpha.Elapsed += Alpha_Reconnect;
        }
        void Alpha_Reconnect(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Invoke((MethodInvoker)delegate () { error.Items.Add("Повторное подключение через " + TimeRecconectSQL + " секунд"); });
            ConnectServer();
        }
        public bool ConnectServer()
        {
            DateTime dataTimeNow = DateTime.Now;
            try
            {
                if (server.IsConnected)
                {
                    MessagePanelWriteStr(dataTimeNow.ToString() + " Альфа сервер подключен" );
                    aTimerRecconectAlpha.Enabled = false;
                }
                else
                {
                    server.Connect();
                    server.SetClientName("Configurator_Sintek");
                    MessagePanelWriteStr(dataTimeNow.ToString() + " Альфа сервер подключен");
                    AlphaConnectFlag = true;
                    if  (aTimerRecconectAlpha != null)
                    aTimerRecconectAlpha.Enabled = false;
                   // CheckConnectStatus();
                }
               
            }
            catch (Exception e)
            {
                MessagePanelWriteStr(dataTimeNow + " Не удалось подключиться к Альфа серверу ");
                AlphaConnectFlag = false;
            }
            return (server.IsConnected);
        }

        /// <summary>
        /// запись в альфу по индексу подписки
        /// </summary>
        /// <param name="value">значение</param>
        /// <param name="index">индекс в группе</param>
        public void commandInController(bool value, int index)
        {
            ItemValue[] writeValues = new ItemValue[1];
            writeValues[0] = new Opc.Da.ItemValue(items[index]);
            writeValues[0].Value = value;
            Opc.IdentifiedResult[] idint = groupSubscribe.Write(writeValues);
        }

        /// <summary>
        ///   тестовый метод для записи сообщений в AE сервер
        /// </summary>
        public void commandAEToAlpha()
        {
            DateTime dataTimeNow = new DateTime(2000, 11, 01, 01, 00, 0);
            string textmes = @"<Subcondition Message=""";
            string textmes2 = @".Тестовое сообщение""/>";
            ItemValue[] writeValues = new ItemValue[1000];

            for (int i = 0; i < 1000; i++)
            {
                writeValues[i] = new Opc.Da.ItemValue(items[3]);
                writeValues[i].Value = textmes + @"/ " + i + @"/ " + dataTimeNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + textmes2;
                writeValues[i].Timestamp = dataTimeNow;

            }
            DateTime dataTimeNow1 = DateTime.Now;
            error.Items.Add(dataTimeNow1.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            Opc.IdentifiedResult[] idint = groupSubscribe.Write(writeValues);

        }
        // <summary>
        // команда записи в контроллер
        // </summary>
        // <param name = "timeout" > таймаут ожидания ответа от контроллера</param>
        // <param name = "index" > индекс тега для записи в альфу</param>
            // <returns></returns>
        public bool CommandOnWriteInController(int timeout, int index)
        {
       
            DateTime dataTimeNow = DateTime.Now;
            //  writeValues[0].ServerHandle = groupSubscribe.Items[groupSubscribe.Items.Length - 1].ServerHandle;
            try
            {
                commandInController(true, index);
                indexTagToWrite = index;
                // Opc.IdentifiedResult[] idint = groupSubscribe.Write(writeValues);
                error.Items.Add(dataTimeNow.ToString() + " " + "Команда записи");
                aTimer = new System.Timers.Timer(timeout);
                aTimer.Enabled = true;
                aTimer.Start();
                aTimer.Elapsed += aTimer_Elapsed;
                WaitComand.Reset();
                WaitComand.WaitOne();//ждем пока не истечет 5 секунд на подтверждение команды либо не произойдет событие 
                return FlagWriteinController;//флаг который возвращаем true если запись успешная и пришел ответ от контроллера
            }
            catch (Exception e)
            {
                error.Items.Add(e.Message);
                return false;
            }
        }

        /// <summary>
        /// функция на случай истечения времени на ожидание подтверждения прохождения команды от контроллера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void aTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            aTimer.Stop();
            DateTime dataTimeNow = DateTime.Now;
            MessageToErrorPanel = "Время вышло. Запись не удалась";
            commandInController(false, indexTagToWrite);
            WaitComand.Set();
            FlagWriteinController = false;
        }
        void DisconnectToOPC()
        {
            DateTime dataTimeNow = DateTime.Now;
            server.Disconnect();
            server.Dispose();
            error.Items.Add(dataTimeNow.ToString() + " Сервер отключился");
        }

        /// <summary>
        /// подписка на теги альфы
        /// </summary>
        /// <param name="Masstag">список тегов для подписки к альфе</param>
        public void CreateMassSubscribeGroup(string[] tagOPC)
        {
            //создание группы
            DateTime dataTimeNow = DateTime.Now;
            SubscriptionState groupStateSubscribe = new SubscriptionState();
            Opc.IRequest req;
            groupStateSubscribe.Name = "GroupRead";
            groupStateSubscribe.Active = true;
            groupStateSubscribe.UpdateRate = 500;
            tempOPCValue = new string[tagOPC.Length];
            OPCValue = new string[tagOPC.Length];
            groupSubscribe = (Subscription)server.CreateSubscription(groupStateSubscribe);
            // добавление сигнала в группу
            items = new Item[tagOPC.Length];
            bool FlagEror = false;
            for (int index = 0; index < items.Length; index++)
            {
                items[index] = new Item();
                items[index].ItemName = tagOPC[index];
            }
            items = groupSubscribe.AddItems(items);
            groupSubscribe.Read(groupSubscribe.Items, 123, new ReadCompleteEventHandler(ReadCompleteCallback), out req);

            handle.WaitOne();//ждем пока клиент вернет все теги
            if (groupSubscribe.Items.Length < items.Length)
            {
                OPCValueIndex = 0;
                for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                {
                    try
                    {
                        if (tagOPC[itemIndex].Equals(groupSubscribe.Items[OPCValueIndex].ItemName))
                            OPCValue[itemIndex] = tempOPCValue[OPCValueIndex++];
                        else
                            throw new Exception();
                    }
                    catch
                    {
                        error.Items.Add(dataTimeNow.ToString() + " Не удалось подписаться на тег " + tagOPC[itemIndex]);
                        FlagEror = true;
                        OPCValue[itemIndex] = "Error!";
                    }
                }
                if (FlagEror)
                    DisconnectToOPC();//если не нашли хотя бы 1 тег, отключаемся от сервера    
            }
            else
            {
                for (int itemIndex = 0; itemIndex < items.Length; itemIndex++)
                {
                    OPCValue[itemIndex] = tempOPCValue[itemIndex];
                }
            }
            try
            {
                groupSubscribe.DataChanged += new DataChangedEventHandler(OnTransactionMassCompleted); //подписываемся на входящие сигналы
            }
            catch (Exception ex)
            {
                error.Items.Add(dataTimeNow.ToString() + " " + ex.Message);
            }
        }
        private void ReadCompleteCallback(object clientHandle, Opc.Da.ItemValueResult[] results)
        {
            for (int i = 0; i < results.Length; i++)
            {
               tempOPCValue[i] = (results[i].Value == null) ? "null" : results[i].Value.ToString();
            }
            handle.Set();
        }
        /// <summary>
        /// функция проверяющая что в тегах на который подписались, есть изменения
        /// </summary>
        /// <param name="group"></param>
        /// <param name="hReq"></param>
        /// <param name="items"></param>
        private void OnTransactionMassCompleted(object group, object hReq, Opc.Da.ItemValueResult[] items)
        {
            if (counterReading > 0)
            {
                DateTime dataTimeNow = DateTime.Now;
                try
                {
                    for (int i = 0; i < items.GetLength(0); i++)
                    {
                        if (items[i].Quality.QualityBits >= qualityBits.good)//не вызываем событие если качество тега меньше 192
                        {
                            //если от 12(количество сигланов в альфе для конфигуратора) до количества мна то вызываем событие изменения состояния агрегата и отрисовываем заново
                            if (Convert.ToInt32(items[i].ServerHandle) < OPCValue.Length && CounSettingsTag < Convert.ToInt32(items[i].ServerHandle))
                                MNAChange?.Invoke(items[i].ServerHandle.ToString(), items[i].Value.ToString());//делегат вызывает событие изменения значения на экране

                            //событие изменения значений хаба
                            if (Convert.ToInt32(items[i].ServerHandle) == 1 || Convert.ToInt32(items[i].ServerHandle) == 2)
                                HubStateChange?.Invoke(items[i].ServerHandle.ToString(), items[i].Value.ToString());

                            if (Convert.ToInt32(items[i].ServerHandle) == 3 || Convert.ToInt32(items[i].ServerHandle) == 4)
                            {
                                bufferMessages.Add("Изменилось состояние ТУ");
                                ZaprettagChange?.Invoke(items[i].ServerHandle.ToString(), items[i].Value.ToString());
                            }
                            //событие изменения тегов состояния контроллера
                            if (Convert.ToInt32(items[i].ServerHandle) == 5 || Convert.ToInt32(items[i].ServerHandle) == 6)
                            {
                               
                                int value;
                                int.TryParse(items[i].Value.ToString(), out value);
                                bufferMessages.Add("Изменилось состояние контроллера");
                                ControllerStateChange?.Invoke(items[i].ServerHandle.ToString(), value.ToString());
                            }
                            if (Convert.ToInt32(items[i].ServerHandle) < 25 && 15 < Convert.ToInt32(items[i].ServerHandle))//сигналы флагов от контроллера
                            {
                                if (aTimer != null)//проверка при первой подписке на сигналы. таймер еще ни разу не запускался при старте
                                {
                                    if (aTimer.Enabled && indexTagToWrite + 1 == Convert.ToInt32(items[i].ServerHandle))//ожидаем возврата индекса +1 т.к. хендлы нумеруются с 1, а массив индексов с 0
                                    {
                                        if ((bool)items[i].Value == true)//проверка что пришел адекватный ответ от контроллера
                                        {
                                            WaitComand.Set();
                                            FlagWriteinController = true;
                                        }

                                    }
                                }
                            }
                            if (Convert.ToInt32(items[i].ServerHandle) < 34 && 24 < Convert.ToInt32(items[i].ServerHandle))//сигналы флагов от контроллера
                                ChangeErrorCode?.Invoke(items[i].ServerHandle.ToString(), items[i].Value.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageToErrorPanel = e.Message;
                    //  SendTableToForm();
                }
            }
            counterReading++;
        }
    }
}
