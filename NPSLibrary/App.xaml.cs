using System.IO;
using System.Runtime.Loader;
using System.Windows;

namespace NPSLibrary
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), ".."));

            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "NPS_Browser_0.94.exe");
            AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

            base.OnStartup(e);
        }
    }
}
