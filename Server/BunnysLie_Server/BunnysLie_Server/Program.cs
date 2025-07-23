namespace BunnysLie_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {

            InGameServer gameServer = new InGameServer();
            gameServer.Start(9000);

            while(true)
            {
                gameServer.Run_SingleTick();
            }

            Console.WriteLine("Press Any Key to Exit...");
            Console.ReadLine();
        }
    }
}
