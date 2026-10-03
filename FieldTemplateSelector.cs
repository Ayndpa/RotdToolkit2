using System.Windows;
using System.Windows.Controls;

namespace RotdToolkit2
{
    public sealed class FieldTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate OtherTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FieldVm field)
            {
                switch (field.Kind)
                {
                    case FieldKind.Bool:
                        return BoolTemplate;
                    case FieldKind.Number:
                        return NumberTemplate;
                }
            }

            return OtherTemplate;
        }
    }
}

