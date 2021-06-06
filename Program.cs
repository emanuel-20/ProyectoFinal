using BotExamenFinal.Clases.Bots;
using System;
using System.Threading.Tasks;

namespace BotExamenFinal
{
    class Program
    {
        public static async Task Main()
        {
            await new ClsBotTelegram().iniciarBot();
        }
    }
}
