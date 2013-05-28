using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
{
    public class PersonItemViewModel : UnifiedItemViewModel<Person>
    {
        public PersonItemViewModel()
        {

        }

        public override string DisplayText
        {
            get
            {
                if (this.Source == null)
                    return null;

                string result = "";
                if (!string.IsNullOrWhiteSpace(this.Source.FirstName))
                {
                    result = this.Source.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(this.Source.LastName))
                {
                    if (result.Length > 0)
                    {
                        result += " ";
                    }
                    result += this.Source.LastName;
                }

                return result;
            }
        }

    }
}
