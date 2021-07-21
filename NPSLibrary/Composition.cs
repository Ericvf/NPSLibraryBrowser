using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace NPSLibrary
{
    public static class Composition
    {
        public static void Initialize(object instance)
        {
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(MainWindow).Assembly));

            var container = new CompositionContainer(catalog);
            container.ComposeParts(instance);
        }
    }
}
