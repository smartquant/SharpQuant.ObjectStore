using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SharpQuant.ObjectStore
{
    [DataContract,Serializable]
    public class Catalogue : ICatalogue
    {
        [DataMember(Order = 1, Name = "CODE")]
        public string CODE { get; set; }
        [DataMember(Order = 2, Name = "Name")]
        public string Name { get; set; }
        [DataMember(Order = 3, Name = "Description")]
        public string Description { get; set; }
        [DataMember(Order = 4, Name = "Tags")]
        public IDictionary<string,string> Tags {get;set;}

        public Catalogue()
        {
            Tags = new Dictionary<string, string>();
        }

        public static Catalogue Create(ICatalogue catalogue)
        {
            return new Catalogue()
            {
                CODE = catalogue.CODE,
                Name = catalogue.Name,
                Description = catalogue.Description,
                Tags = new Dictionary<string,string>(catalogue.Tags),
            };
        }
    }

    public interface ICatalogue
    {
        string CODE { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        IDictionary<string, string> Tags { get; set; }
    }

}
