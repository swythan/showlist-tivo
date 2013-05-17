using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone.ViewModels
{
    public class UnifiedItemViewModel : PropertyChangedBase
    {
        private IUnifiedItem item;

        public UnifiedItemViewModel(IUnifiedItem item)
        {
            this.item = item;
        }

        public string DisplayText
        {
            get
            {
                var collection = this.item as Collection;
                if (collection != null)
                {
                    return "[C] " + collection.Title;
                }

                var person = this.item as Person;
                if (person != null)
                {
                    string result = "";
                    
                    if (!string.IsNullOrWhiteSpace(person.FirstName))
                    {
                        result = person.FirstName;
                    }

                    if (!string.IsNullOrWhiteSpace(person.LastName))
                    {
                        if (result.Length > 0)
                        {
                            result += " ";
                        }

                        result += person.LastName;
                    }

                    return "[P] " + result;
                }

                return null;
            }
        }
    }
}
