using kssm.be.shared.Constants.DongGoi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kssm.be.applications.DongGoi.Config.Dtos
{
    public class GetDropDownTypeTT05Dto
    {
        public List<GetDropDownTypeTT05ValueDto> ObjectType { get; set; } = new List<GetDropDownTypeTT05ValueDto>();
    }


    public class GetDropDownTypeTT05ValueDto
    {
        //public List<string> ObjectType { get; set; } = new List<string>();
        public string Value { get; set; } = String.Empty;
        public PackageType PackageType { get; set; }
        public List<GetDropDownObjectTypeDto> ObjectTypes { get; set; } = new List<GetDropDownObjectTypeDto>();
    }

    public class GetDropDownObjectTypeDto
    {
        public MetadataObjectType Value { get; set; }
        public string DisplayName { get; set; } = String.Empty;
    }
}

