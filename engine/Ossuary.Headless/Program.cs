namespace Ossuary.Validation
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            int result = Ossuary.Tools.HeadlessMain.Main(args);
            if (result == 0 && (args.Length == 0 || args[0] == "test")) Ossuary.Desktop.DesktopTests.Run();
            return result;
        }
    }
}
