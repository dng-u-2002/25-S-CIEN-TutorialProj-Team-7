namespace ConsoleApp1
{
    public class SceneManager
    {
        public static void LoadScene(int idx)
        {

        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            SceneManager.LoadScene(0);


            const int sceneIndex_MainPlayScene = 0;
            SceneManager.LoadScene(sceneIndex_MainPlayScene);
        }
    }
}
