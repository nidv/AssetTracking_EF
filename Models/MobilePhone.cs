using System;
using System.Collections.Generic;
using System.Text;

namespace AssetTracking_EF.Models
{
    // -------- Level 1 requirement - MobilePhone Asset --------
    public class MobilePhone : Asset
    {
        public MobilePhone()
        {
            AssetType = AssetType.Phone;
        }
    }
}

