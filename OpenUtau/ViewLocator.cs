using Avalonia.Controls;
using Avalonia.Controls.Templates;
using OpenUtau.App.ViewModels;
using System;

<<<<<<< HEAD
namespace OpenUtau.App {
    public class ViewLocator : IDataTemplate {
        public Control? Build(object? data) {
            if (data is null) {
                return null;
            }
=======
namespace OpenUtau.App
{
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public IControl Build(object data)
        {
>>>>>>> parent of d60f4037 (upgrade to avalonia 11 and fix compilation)
            var name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);
            if (type != null) {
                return (Control)Activator.CreateInstance(type)!;
            }
            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data) {
            return data is ViewModelBase;
        }
    }
}
