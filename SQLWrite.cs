using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;


namespace SettingCSPA
{
    public class SQLWrite
    {
        ListView error;
        SqlConnection connection;
        SqlConnection connectionRes;
        //статус первого сервера SQL
        bool status = false;
        //статус подключения второго сервера SQL
        bool statusres =  false;
        MainForm.ErrorMessagePanelWrite MessagePanelWrite;
        MainForm.ErrorMessagePanelWriteString MessagePanelWriteStr;
        int Category = 6;
        int Severity = 104;
        const int Qality = 216;
        const int Changemask = 1;
        const int SubCondition = 26;
        const int AtribID = 1;
        /// <summary>
        /// время реконнекта к серверу SQL при неудачной попытке
        /// </summary>
        int TimeRecconectSQL { get; set; }
        private  System.Timers.Timer aTimer ;
        int ID;

        /// <summary>
        ///  MessagePanelWrite(
        /// </summary>
        /// <param name="errorIN"></param>
        /// <param name="sqlConnectionString"></param>
        /// <param name="TimeRecconect"></param>
        /// <param name="MessagePanelWriteIn">делегат для записи сообщений в основной поток</param>
        public SQLWrite(ListView errorIN, string[] sqlConnectionString, int TimeRecconect, MainForm.ErrorMessagePanelWrite MessagePanelWriteIn, MainForm.ErrorMessagePanelWriteString MessagePanelWriteStr)
        {
            MessagePanelWrite = MessagePanelWriteIn;

            if (TimeRecconect == 0)
            {
                TimeRecconect = 1;
                MessagePanelWriteStr("Не допустимая выдержка времени переподключения к SQL, установленно минимальное значение 1 секунда");
            }
            TimeRecconectSQL = TimeRecconect;

            error = errorIN;
             connection = new SqlConnection(sqlConnectionString[0]);
             connectionRes = new SqlConnection(sqlConnectionString[1]);
        }
        public void ConnectServer()
        {      
                aTimer = new System.Timers.Timer(TimeRecconectSQL * 1000);//создаем новый таймер для реконекта
                aTimer.Start();
                aTimer.Enabled = false;
                aTimer.Elapsed += SQL_Reconnect;
                SQL_connect();
                connection.StateChange += HandleSqlConnectionDrop;
                connectionRes.StateChange += HandleSqlConnectionDropRes;
        }
        /// <summary>
        /// подключение к серверам
        /// </summary>
        /// <returns>3 - оба подключены, 2 - подключен второй, 1 - подключен первый, 0 - оба не подключены</returns>
        private void SQL_connect()
        {
            if (!status)
                try
            {
              //  connection.Open();
                status = true;
                MessagePanelWrite("Успешное подключение к серверу SQL №1",status,statusres);
				aTimer.Enabled = false;
                }
            catch {
                    status = false;
                   // MessagePanelWrite("Не успешное подключение к серверу №1", status, statusres);
                   // Invoke((System.Windows.Forms.MethodInvoker)delegate () { error.Items.Add("Не успешное подключение к серверу №1"); });
                    aTimer.Enabled = true;
                }
            if (!statusres)
            try
            {
               // connectionRes.Open();
                statusres = true;
                    MessagePanelWrite("Успешное подключение к серверу SQL №2", status, statusres);
                    aTimer.Enabled = false;
                }
            catch
            {
                statusres = false;
                aTimer.Enabled = true;
                }
       
        }
        /// <summary>
        /// событие по истечению времени подписки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SQL_Reconnect(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Invoke((MethodInvoker)delegate () { error.Items.Add("Повторное подключение через " + TimeRecconectSQL + " секунд"); });
            SQL_connect();
        }
       /// <summary>
       /// событие изменения состояния подключения к основному серверу
       /// </summary>
       /// <param name="connection"></param>
       /// <param name="args"></param>
        private  void HandleSqlConnectionDrop(object connection, System.Data.StateChangeEventArgs args)
        {
            status = false;
            MessagePanelWrite("Потеряно соединение с основным сервером", status, statusres);
           // MessagePanelWrite("Повторное подключение через " + TimeRecconectSQL + " секунд");
            SQL_connect();
        }

        /// <summary>
        /// событие изменения состояния подключения к резервному серверу
        /// </summary>
        /// <param name="connectionRes"></param>
        /// <param name="args"></param>
        private void HandleSqlConnectionDropRes(object connectionRes, System.Data.StateChangeEventArgs args)
        {
            statusres = false;
            MessagePanelWrite("Потеряно соединение с резервным сервером", status, statusres);
           // MessagePanelWrite("Повторное подключение через " + TimeRecconectSQL + " секунд");
            SQL_connect();
        }

        public int SQL_Request(string TUName,List<string> Message)
        {
            System.DateTime timeStartGMT = new System.DateTime();
            timeStartGMT = DateTime.UtcNow;

            SqlCommand cmd = new SqlCommand();
            ID  = CheckIDInSQL();
            ID = ID + 1;
            cmd.CommandType = System.Data.CommandType.Text;
            if(Message.Count == 0)
                return 2;
            
            cmd.CommandText = "SET IDENTITY_INSERT dbo.EventHistory ON INSERT dbo.EventHistory (EventTime, id,ActiveTime,Category,Severity,Condition,ChangeMask,NewState,Quality,Cookie,Message,ActorId,Source,Attrib00_Id,Attrib00_Value,Attrib01_Id,Attrib02_Id,Attrib02_Value,Attrib03_Value) VALUES";
            for (int i = 0;i < Message.Count; i++)
            {
                cmd.CommandText += "(" + "'" + timeStartGMT.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" + "," + (ID + i)
                + "," + "'" + timeStartGMT.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'" + ","
                + Category + "," + Severity + "," + Category  + ","
                +  Changemask + ",3," + Qality + ",168819,";

                if(!(i == Message.Count -  1))
                cmd.CommandText += "'"  + TUName + ". ЦСПА." + Message[i] + "'" +  ",1,1," + AtribID + ",1,1,1,1,1) ,";
                else
                    cmd.CommandText += "'" + TUName + ". ЦСПА." + Message[i] + "'" + ",1,1," + AtribID + ",1,1,1,1,1)";
               }

            cmd.CommandText += " SET IDENTITY_INSERT dbo.EventHistory OFF";
            cmd.Connection = connection;
            cmd.CommandTimeout = 10;
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// запрос айдишника последней записи из SQL 
        /// </summary>
        /// <returns>ID последней записи</returns>
        private int CheckIDInSQL()
        {
            SqlCommand commandread = new SqlCommand();
            commandread.CommandType = System.Data.CommandType.Text;
            commandread.CommandText = "SELECT TOP 1 EventHistory.id FROM EventHistory ORDER BY ID DESC";
            commandread.Connection = connection;

            SqlDataReader reader = commandread.ExecuteReader();
            reader.Read();
            ID = (int)reader[0];
            reader.Close();
            return ID;
        }

    }
}
