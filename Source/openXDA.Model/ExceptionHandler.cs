﻿//******************************************************************************************************
//  ExceptionHandler.cs - Gbtc
//
//  Copyright © 2017, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/25/2017 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace openXDA.Model
{
    public static class ExceptionHandler
    {
        public static bool IsUniqueViolation(Exception ex)
        {
            if ((object)ex == null)
                return false;

            List<Exception> exceptions = new List<Exception>() { ex };

            while ((object)exceptions.Last().InnerException != null)
                exceptions.Add(ex.InnerException);
            
            return exceptions
                .OfType<SqlException>()
                .Any(sqlException => (sqlException.Number == 2601) || (sqlException.Number == 2627));
        }
    }
}
