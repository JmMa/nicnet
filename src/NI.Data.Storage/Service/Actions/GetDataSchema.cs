﻿#region License
/*
 * Open NIC.NET library (http://nicnet.googlecode.com/)
 * Copyright 2013 Vitalii Fedorchenko
 * Copyright 2014 NewtonIdeas
 * Distributed under the LGPL licence
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

using NI.Data.Storage.Model;
using NI.Data.Storage.Service.Schema;

namespace NI.Data.Storage.Service.Actions {
	
	public class GetDataSchema {
		
		DataSchema Schema;

		public GetDataSchema(DataSchema schema) {
			Schema = schema;
		}

		public GetDataSchemaResult Execute() {
			var r = new GetDataSchemaResult();

			foreach (var c in Schema.Classes) {
				var cInfo = new DataSchemaClassInfo() {
					ID = c.ID,
					Name = c.Name,
					IsPredicate = c.IsPredicate
				};
				foreach (var cProp in c.Properties) {
					cInfo.Properties.Add( new DataSchemaClassPropertyInfo() { ID = cProp.ID } );
				}
				r.Classes.Add(cInfo);
			}

			return r;
		}

	}

}
