﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Denudey.Api.Services.Cloudinary
{
    public class CloudinarySettings
    {
        
        public string CloudName { get; set; }

        
        public string ApiKey { get; set; }

        
        public string ApiSecret { get; set; }
    }

}
