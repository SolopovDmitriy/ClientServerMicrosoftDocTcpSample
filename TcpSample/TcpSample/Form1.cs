using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TcpSample
{
   
    public partial class Form1 : Form
    {      
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        Socket listener;
        

        public Form1()
        {
            InitializeComponent();
          
        }

        private async void button_StartServer_Click(object sender, EventArgs e)
        {
            Task t = Task.Run(() =>
            {
                try
                {
                    if (listener == null)// если сокет пустой
                    {
                        IPAddress ipAddress;
                        if (IPAddress.TryParse(maskedTextBox_IP.Text, out ipAddress))
                        {
                            //_listener = new TcpListener(ipAddress, Convert.ToInt32(textBox_Port.Text));
                            //_listener.Start();                            
                            int port = Convert.ToInt32(textBox_Port.Text);
                            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
                            button_StartServer.Enabled = false;// устанавливает - неактивнау кнопку
                            StartListening(localEndPoint);
                        }
                        else
                        {
                            MessageBox.Show("Указан некорректный IP адресс", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }                 
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            });
            await t;               
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            //maskedTextBox_IP.ValidatingType =  typeof(System.Net.IPAddress);
        }


        public void StartListening(IPEndPoint localEndPoint) //метод - начать слушать клиентов
        {             
            // Create a TCP/IP socket.  
            listener = new Socket(localEndPoint.AddressFamily, // создаем пустой сокет, без привязки к ip адресу и портуipAddress.AddressFamily тип семейство адресов 
                SocketType.Stream, ProtocolType.Tcp);//SocketType.Stream - передача информации с помощью массива байт - byte[] buffer

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try // если не удается подключиться приложение не падает, а попадает в catch
            {
                listener.Bind(localEndPoint); // привязывам к сокету  listener айпи адресс и порт сервера, т.е. указываем адресс слушателя;
                listener.Listen(100);//ограничение в 100 клиентов (клиенты встают очередь до 100 )

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset(); // закрываем ворота для клиента и слушаем нового клиента, ждем пока появится новый клиент

                    // Start an asynchronous socket to listen for connections.  
                    //Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(//listener - сокет для прослушивания клиентов
                                         //попытка связаться с клиентами, ждет связи с клиентами,
                                         //если клиент не обращается к серверу, то AcceptCallback - не запускается
                        new AsyncCallback(AcceptCallback),// AcceptCallback вызывается, когда клиент отсылает на сервер сообщение
                        listener);// передаем AcceptCallback сокет listener; listener - один сокет для прослушивания всех клиентов
                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne(); // сами ворота
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); // Console.WriteLine(ex.ToString());
            }
            MessageBox.Show("server is off");
        }


        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set(); // посылается сообщениет главному потоку, что бы он продолжил работу, открываем ворота

            // Get the socket that handles the client request.

            //Socket listener = ar.AsyncState as Socket;
            Socket listener = (Socket)ar.AsyncState;// ar.AsyncState переводим к типу сокет, ar параметр callback функции AcceptCallback,
                                                    // которая хранит сокет
            Socket handler = listener.EndAccept(ar); // слушатель (listener) создает новый сокет (handler - ) сокет для передачи данных

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler; //  public Socket workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), state);
            // начинает ассихронный прием данных от клиента, а  когда прием данных заканчивается, 
            // тогда вызывается функция ReadCallback
            // первый параметр state.buffer - буфер куда копируется, данные полученные от клиента
            // второй параметр offset =0. Смещение внутри массива buffer, если равно нулю, то нет смещения и пишет в начало массива
            //StateObject.BufferSize - третий параметр, размер сообщения от клиента число в байтах - 1024 байт = 1 килобайт
            //0 - четвертый параметр, 0 == SocketFlags.None - неиспользуются флаги (флаги используються для того если данные не помещаються в буфер)
            // new AsyncCallback(ReadCallback) -  callback функция , которая вызывается, когда прием данных заканчивается
            // state - данные, которые передаються callback функции ReadCallback, когда BeginReceive заканивает прием данных от клиента

        }


        public void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState; // конвертируем(cast) объект ar.AsyncState полученный из предыдущего метода в StateObject
            Socket handler = state.workSocket;  // сохраняем сокет в handler (в переменной   handler - содержиться -  сокет для передачи данных)      
            int bytesRead = handler.EndReceive(ar); //закрывает чтение данных
            if (bytesRead > 0)// если клиент ничего не отправил, bytesRead = 0;
            {
                String messageFromClient = Encoding.UTF8.GetString(state.buffer, 0, bytesRead); // конвертируем массив байтов в string
                listBox_Messages.Items.Add(messageFromClient);
                Console.WriteLine("Я сервер получил сообщение от клиента: " + messageFromClient);
                if (messageFromClient.Equals("Poltava"))
                {
                    Send(handler, "In Poltava rain"); //сервер отправляет клиенту ответ
                }
                if (messageFromClient.Equals("Kyiv"))
                {
                    Send(handler, "In Kyiv sunny");
                }
            }
        }

        private void Send(Socket handler, String messageServerToClient)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.UTF8.GetBytes(messageServerToClient);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(SendCallback), handler);// начинаем передачу данных клинту от сервера
        }



        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar); // закрываем передачу данных               
                // MessageBox.Show($"Sent {bytesSent} bytes to client.");
                handler.Shutdown(SocketShutdown.Both);//отключает передачу данных
                handler.Close(); // закрываем передачу данных и освобождает все связанные с сокетом ресурсы
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }







        //private async void ClientRequestAsync()
        //{
        //    await Task.Run(() => {
        //        while (true)
        //        {
        //            if (_listener != null)
        //            {
        //                try
        //                {
        //                    TcpClient client = _listener.AcceptTcpClient();
        //                    StreamReader streamReader = new StreamReader(client.GetStream(), Encoding.UTF8);
        //                    string clientMessage = streamReader.ReadLine();

        //                    if (clientMessage.Equals("SHUTDOWN"))
        //                    {
        //                        _listener.Stop();
        //                        _listener = null;
        //                    }

        //                    listBox_Messages.Items.Add(clientMessage);
        //                    client.Close();
        //                }
        //                catch (Exception ex)
        //                {
        //                    break;
        //                }
        //            }   
        //        }
        //        _listener = null;
        //    });
        //}





    }




    // State object for reading client data asynchronously  
    public class StateObject // для хранения передаваемых данных и сокета
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder(); // для эффективной работы со строками

        // Client socket.
        public Socket workSocket = null; // сокет для связи с клиентом, для передачи данных от клиента к серверу и наоборот
                                         //сколько клиентов, столько и сокетов workSocket
    }


}
