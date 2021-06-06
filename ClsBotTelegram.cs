using BotExamenFinal.Clases.Conexion;
using System;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotExamenFinal.Clases.Bots
{
    class ClsBotTelegram
    {
        private readonly String Token;
        private static TelegramBotClient Bot;
        private static String correo;
        private static String resultado;
        private static String[] pedido;
        private static String[] datosNuevos;
        private static int contador = 0;
        private static int contador2 = 0;
        private static bool botIniciado;

        public ClsBotTelegram()
        {
            Token = "1829265716:AAFPR1qHAwHEIXUWkpkIGdF_Lx18ohhCLtc";
        }

        public async Task iniciarBot()
        {
            Bot = new TelegramBotClient(Token);

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            Bot.OnMessage += BotCuandoRecibeMensajes;
            Bot.OnMessageEdited += BotCuandoRecibeMensajes;
            Bot.OnCallbackQuery += BotOnCallbackQueryRecibido;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Empezando a escuchar a @{me.Username}");

            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotCuandoRecibeMensajes(object sender, MessageEventArgs messageEventArgumentos)
        {
            var ObjetoMensajeTelegram = messageEventArgumentos;
            var mensaje = ObjetoMensajeTelegram.Message;

            string mensajeEntrante = mensaje.Text;

            if (mensaje == null || mensaje.Type != MessageType.Text) return;

            if (mensajeEntrante == "/Opciones")
            {
                botIniciado = true;

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(Emoji.Clapper+"Elija su película", "Elija"),
                        InlineKeyboardButton.WithCallbackData(Emoji.Currency_Exchange+"Verificar Pedido", "verificar"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(Emoji.Page_Facing_Up+"Editar Pedido", "Editar"),
                        InlineKeyboardButton.WithCallbackData(Emoji.X+"Eliminar Pedido", "borrar"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: mensaje.Chat.Id,
                    text: "Hola, elija una opción.",
                    replyMarkup: inlineKeyboard
                );
            }

            if (botIniciado == false)
            {
                await Bot.SendTextMessageAsync(
                    chatId: messageEventArgumentos.Message.Chat.Id,
                    text: "Click en /Opciones");
            }
        }

        private static async void BotOnCallbackQueryRecibido(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            switch (callbackQuery.Data.ToString())
            {
                case "Elija":
                    await ElijasuPelicula(callbackQuery);
                    break;

                case "verificar":
                    await VerificarPedido(callbackQuery);
                    break;

                case "Editar":
                    await EditarPedido(callbackQuery);
                    break;

                case "borrar":
                    await EliminarPedido(callbackQuery);
                    break;

                case "correo":
                    await enviarCorreo(callbackQuery);
                    break;
            }
        }

        static async Task ElijasuPelicula(CallbackQuery callbackQuery)
        {
            contador = 0;
            pedido = new string[4];
            pedido[3] = "";

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Catálago de peliculas y series disponibles: \n\n" +
                        "1. Bad Boys for Life - Pelicula\n" +
                        "2. Triple Frontera\n" +
                        "3. Misión Rescate\n" +
                        "4. No Respires\n" +
                        "5. Oscuro Deseo (Serie)\n" +
                        "6. Falcon y el Soldado del Invierno\n" +
                        "7. La Casa de Papel\n" +
                        "8. Stranger Things\n" +

                        "Ingrese el número de la película o serie que desea adquirir:"
            );

            Bot.OnMessage += BotCuandoRecibePedido;
            Bot.OnMessageEdited += BotCuandoRecibePedido;
        }

        static async Task VerificarPedido(CallbackQuery callbackQuery)
        {
            resultado = "";
            ClsConexionSqlServer cn = new ClsConexionSqlServer();
            string sql = $"select * from tb_pedidos where IDCliente = {callbackQuery.Message.Chat.Id}";
            DataTable dt;
            dt = cn.consultarDB(sql);

            foreach (DataRow dr in dt.Rows)
            {
                string aux = "Su pedido es ID: " + dr["IDCliente"].ToString() + "; Nombre: " + dr["Nombre"].ToString() + "; Película o Serie: " + dr["Película o Serie"].ToString() + "; Dirección: " + dr["Dirección"].ToString() + "; Correo Elextrónico: " + dr["Correo Electrónico"].ToString();
                resultado += aux + "\n";
            }

            if (resultado != "")
            {
                correo = Convert.ToString(dt.Rows[0].Field<String>("Correo Electrónico"));

                await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Su pedido es el siguiente:\n" +
                    resultado);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                         InlineKeyboardButton.WithCallbackData("Enviar correo.", "correo"),
                        //InlineKeyboardButton.WithCallbackData("Aceptar", "Correo Electrónico"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "Dele click en 'Enviar correo' para recibir el link",
                    replyMarkup: inlineKeyboard
                );
            }
            else
            {
                await Bot.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: "No ha realizado pedido.");
            }
        }

        static async Task EditarPedido(CallbackQuery callbackQuery)
        {
            contador2 = 0;

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Ingresa tu nueva dirección y correo electrónico separados por un espacio en blanco.");

            Bot.OnMessage += BotCuandoActualizaDatos;
            Bot.OnMessageEdited += BotCuandoActualizaDatos;
        }

        static async Task EliminarPedido(CallbackQuery callbackQuery)
        {
            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: "¿Seguro que desea eliminar sus pedidos?\n" +
                "/Si\n" +
                "/No");

            Bot.OnMessage += BotCuandoEliminarPedido;
            Bot.OnMessageEdited += BotCuandoEliminarPedido;
        }

        static async Task enviarCorreo(CallbackQuery callbackQuery)
        {
            try
            {
                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Credentials = new NetworkCredential("aavendanor2@miumg.edu.gt", "Cruciatus");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;

                //Búsqueda de Correo
                ClsConexionSqlServer cn = new ClsConexionSqlServer();
                string sql = $"select * from tb_pedidos where IDCliente = {callbackQuery.Message.Chat.Id}";
                DataTable dt;
                dt = cn.consultarDB(sql);

                string correo = string.Empty;

                foreach (DataRow dr in dt.Rows)
                {
                    correo = dr["Correo Electrónico"].ToString();

                }

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("aavendanor2@miumg.edu.gt", "Pedidos Blockbuster");
                mail.To.Add(new MailAddress(correo));//a quien envia
                mail.Subject = "SU PEDIDO: ";//asunto
                mail.IsBodyHtml = true;
                mail.Body = resultado;//Cuerpo de Mensaje
                smtp.EnableSsl = true;

                smtp.Send(mail);

                await Bot.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: "El link de su película o serie se envío a su correo."
                    );
            }
            catch (Exception ex) 
            {
                await Bot.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: ex.Message
                    );
            }

        }

        private static async void BotCuandoRecibePedido(object sender, MessageEventArgs messageEventArgumentos)
        {
            if (contador < 4 && messageEventArgumentos.Message.Text != "/Menu")
            {
                string[] columna = { "Nombre", "Dirección", "Correo Electrónico" };
                var ObjetoMensajeTelegram = messageEventArgumentos;
                var mensaje = ObjetoMensajeTelegram.Message;

                string mensajeEntrante = mensaje.Text;

                if (mensaje == null || mensaje.Type != MessageType.Text) return;

                if (contador < 3)
                {
                    await Bot.SendTextMessageAsync(
                        chatId: mensaje.Chat.Id,
                        text: $"Por favor, ingrese su {columna[contador]}.");
                }

                pedido[contador] = mensajeEntrante;

                contador++;

                if (pedido[3] != "")
                {
                    ClsConexionSqlServer cn = new ClsConexionSqlServer();
                    string sql = $"insert into tb_pedidos values({messageEventArgumentos.Message.Chat.Id}, '{pedido[1]}', '{pedido[0]}', '{pedido[2]}', '{pedido[3]}')";

                    cn.ejecutarSql(sql);

                    await Bot.SendTextMessageAsync(
                            chatId: messageEventArgumentos.Message.Chat.Id,
                            text: "Tu nuevo pedido se ha realizado con éxito.");
                    Bot.StopReceiving();
                    ClsBotTelegram obj = new ClsBotTelegram();
                    await obj.iniciarBot();
                }
            }
        }

        private static async void BotCuandoActualizaDatos(object sender, MessageEventArgs messageEventArgumentos)
        {
            if (contador2 < 1)
            {
                var ObjetoMensajeTelegram = messageEventArgumentos;
                var mensaje = ObjetoMensajeTelegram.Message;
                ClsConexionSqlServer cn = new ClsConexionSqlServer();
                datosNuevos = new string[2];

                string mensajeEntrante = mensaje.Text;

                if (mensaje == null || mensaje.Type != MessageType.Text) return;

                datosNuevos = mensajeEntrante.Split(' ');

                string sql = $"update tb_pedidos set Dirección = '{datosNuevos[0]}', [Correo Electrónico] = '{datosNuevos[1]}' where IDCliente = {messageEventArgumentos.Message.Chat.Id}";
                cn.ejecutarSql(sql);

                await Bot.SendTextMessageAsync(
                     chatId: messageEventArgumentos.Message.Chat.Id,
                     text: "Tus datos se han actualizado con éxito."
                    );

                contador2++;

                Bot.StopReceiving();
                ClsBotTelegram obj = new ClsBotTelegram();
                await obj.iniciarBot();
            }
        }

        private static async void BotCuandoEliminarPedido(object sender, MessageEventArgs messageEventArgumentos)
        {
            if (messageEventArgumentos.Message.Text == "/Si")
            {
                ClsConexionSqlServer cn = new ClsConexionSqlServer();
                string sql = $"delete from tb_pedidos where IDCliente = {messageEventArgumentos.Message.Chat.Id}";
                cn.ejecutarSql(sql);

                await Bot.SendTextMessageAsync(
                chatId: messageEventArgumentos.Message.Chat.Id,
                text: "Tus pedidos se han eliminado exitosamente.");
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("UPS!!! Recibo un error!!!: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}
