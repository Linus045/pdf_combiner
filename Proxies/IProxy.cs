using System.Windows;

namespace PDF_Combiner.Proxies
{
    public abstract class BindingProxy<T> : Freezable
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(T), typeof(BindingProxy<T>), new PropertyMetadata(default(T)));

        public T Data
        {
            get => (T)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
