using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClientServerMicrosoftDoc
{

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



    public class AsynchronousSocketListener //один сервер, обслуживает клиентов
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false); //

        public AsynchronousSocketListener()
        {
        }

        public static void StartListening() //метод - начать слушать клиентов
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];

            IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // ip adress сервера 127.0.0.1 - adress localhost
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000); // соединяет ip adress и порт в один (обЪект - localEndPoint) 

          
            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily, // создаем пустой сокет, без привязки к ip адресу и портуipAddress.AddressFamily тип семейство адресов 
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
                    Console.WriteLine("Waiting for a connection...");
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

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)  
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


        public static void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState; // конвертируем(cast) объект ar.AsyncState полученный из предыдущего метода в StateObject
            Socket handler = state.workSocket;  // сохраняем сокет в handler (в переменной   handler - содержиться -  сокет для передачи данных)      
            int bytesRead = handler.EndReceive(ar); //закрывает чтение данных
            if (bytesRead > 0)// если клиент ничего не отправил, bytesRead = 0;
            {
                String messageFromClient = Encoding.UTF8.GetString(state.buffer, 0, bytesRead); // конвертируем массив байтов в string
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



        //public static void ReadCallback2(IAsyncResult ar)
        //{
        //    String content = String.Empty; // 

        //    // cast
        //    //int y = 13;
        //    //double x = (double)y / 3;
        //    //object x1 = x;
        //    //double x2 = (double)x1;

        //    // Retrieve the state object and the handler socket  
        //    // from the asynchronous state object.  

        //    StateObject state = (StateObject)ar.AsyncState; // конвертируем(cast) объект ar.AsyncState полученный из предыдущего метода в StateObject
        //    Socket handler = state.workSocket;

        //    // Read data from the client socket.
        //    int bytesRead = handler.EndReceive(ar);

        //    if (bytesRead > 0)
        //    {
        //        ////////// There  might be more data, so store the data received so far.  
        //        ////////state.sb.Append(Encoding.ASCII.GetString(
        //        ////////    state.buffer, 0, bytesRead));

        //        ////////// Check for end-of-file tag. If it is not there, read
        //        ////////// more data.  
        //        ////////content = state.sb.ToString();
        //        content = Encoding.UTF8.GetString(state.buffer, 0, bytesRead);
        //        if (content.IndexOf("<EOF>") > -1)
        //        {
        //            // All the data has been read from the
        //            // client. Display it on the console.  
        //            Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
        //                content.Length, content);
        //            // Echo the data back to the client.
        //            // Send(handler, "Answer from server" + content);
        //            Send(handler, "Answer from server: Go  away" );
        //        }
        //        else
        //        {
        //            // Not all data received. Get more.  
        //            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //            new AsyncCallback(ReadCallback), state);
        //        }
        //    }
        //}





        private static void Send(Socket handler, String messageServerToClient)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.UTF8.GetBytes(messageServerToClient);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, SocketFlags.None,
                new AsyncCallback(SendCallback), handler);// начинаем передачу данных клинту от сервера
        }



        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar); // закрываем передачу данных
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);//отключает передачу данных
                handler.Close(); // закрываем передачу данных и освобождает все связанные с сокетом ресурсы

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }




        public static void Main(String[] args) // главный поток 
        {
            StartListening();// вызывается метод - начать слушать клиентов
            Console.ReadKey();
            // return 0;
        }
    }



}

